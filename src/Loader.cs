using Godot;
using System;
using Godot.Collections;
using System.Text;

public class Loader : Node
{
    private string baseURL;
    private string token;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ConfigFile cf = new ConfigFile();
        Error err = cf.Load("res://networking.cfg");
        if (err != Error.Ok)
        {
            GD.Print($"unable to parse networking.cfg: {err}");
            return;
        }

        baseURL = (string)cf.GetValue("gameapi", "base_url");

        GetNode<Button>("/root/Menu/UiLayer/CenterContainer/LoginBox/LoginBtn").Connect("button_down", this, nameof(DoLogin));
        GetNode<Button>("/root/Menu/UiLayer/CenterContainer/LoginBox/SwitchRegBtn").Connect("button_down", this, nameof(SwitchToReg));
        GetNode<Button>("/root/Menu/UiLayer/CenterContainer/RegistrationBox/RegBtn").Connect("button_down", this, nameof(DoRegistration));
        GetNode<Button>("/root/Menu/UiLayer/CenterContainer/RegistrationBox/SwitchLoginBtn").Connect("button_down", this, nameof(SwitchToLogin));
        GetNode<Button>("/root/Menu/UiLayer/CenterContainer/CharacterListing/SwitchCharCreateBtn").Connect("button_down", this, nameof(SwitchToCreateCharacter));
        GetNode<Button>("/root/Menu/UiLayer/CenterContainer/CharacterCreateBox/CreateBtn").Connect("button_down", this, nameof(DoCreateCharacter));
        GetNode<Button>("/root/Menu/UiLayer/CenterContainer/CharacterCreateBox/CancelBtn").Connect("button_down", this, nameof(SwitchToCreateListing));
        
        GetNode("LoginRequest").Connect("request_completed", this, nameof(OnLoginRequestCompleted));
        GetNode("CharacterRequest").Connect("request_completed", this, nameof(OnCharacterRequestCompleted));
        GetNode("PlayRequest").Connect("request_completed", this, nameof(OnPlayRequestCompleted));
        GetNode("RegistrationRequest").Connect("request_completed", this, nameof(OnRegistrationRequestCompleted));
        GetNode("ChracterCreateRequest").Connect("request_completed", this, nameof(OnChracterCreateRequestCompleted));
    }

    public void DoLogin()
    {
        Dictionary<string, string> pl = new Dictionary<string, string>();
        pl["Username"] = GetNode<LineEdit>("/root/Menu/UiLayer/CenterContainer/LoginBox/UsernameInput").Text;
        pl["Password"] = GetNode<LineEdit>("/root/Menu/UiLayer/CenterContainer/LoginBox/PasswordInput").Text;

        string jsonString = JSON.Print(pl);

        HTTPRequest httpRequest = GetNode<HTTPRequest>("LoginRequest");
        httpRequest.Request($"{baseURL}/account/login", null, false, HTTPClient.Method.Post, jsonString);
    }

    public void SwitchToReg()
    {
        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/LoginBox").Visible = false;
        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/RegistrationBox").Visible = true;
    }

    public void SwitchToLogin()
    {
        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/RegistrationBox").Visible = false;
        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/LoginBox").Visible = true;
    }

    public void SwitchToCreateCharacter()
    {
        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/CharacterListing").Visible = false;
        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/CharacterCreateBox").Visible = true;
    }

    public void SwitchToCreateListing()
    {
        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/CharacterListing").Visible = true;
        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/CharacterCreateBox").Visible = false;
    }

    public void DoRegistration()
    {
        Dictionary<string, string> pl = new Dictionary<string, string>();
        pl["username"] = GetNode<LineEdit>("/root/Menu/UiLayer/CenterContainer/RegistrationBox/UsernameInput").Text;

        string jsonString = JSON.Print(pl);

        HTTPRequest httpRequest = GetNode<HTTPRequest>("RegistrationRequest");
        httpRequest.Request($"{baseURL}/account", null, false, HTTPClient.Method.Post, jsonString);
    }

    public void DoCreateCharacter()
    {
        Dictionary<string, string> pl = new Dictionary<string, string>();
        pl["name"] = GetNode<LineEdit>("/root/Menu/UiLayer/CenterContainer/CharacterCreateBox/CharacterNameInput").Text;
        pl["base_color"] = GetNode<ColorPickerButton>("/root/Menu/UiLayer/CenterContainer/CharacterCreateBox/BaseColorInput").Color.ToHtml();

        string jsonString = JSON.Print(pl);

        HTTPRequest httpRequest = GetNode<HTTPRequest>("ChracterCreateRequest");
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Bearer {token}";
        httpRequest.Request($"{baseURL}/game/characters", requestHeaders, false, HTTPClient.Method.Post, jsonString);
    }

    public void OnLoginRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        if (response_code != 200)
        {
            GD.Print("login error");
            return;
        }

        GD.Print($"result={result} response_code={response_code}");
        token = Encoding.UTF8.GetString(body);

        HTTPRequest httpRequest = GetNode<HTTPRequest>("CharacterRequest");
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Bearer {token}";
        httpRequest.Request($"{baseURL}/game/characters", requestHeaders, false, HTTPClient.Method.Get, "");
    }

    public void OnRegistrationRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        if (response_code != 201)
        {
            GD.Print($"account registration error {response_code}");
            return;
        }

        SwitchToLogin();
    }

    public void OnCharacterRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        if (response_code != 200)
        {
            GD.Print($"character loading error {response_code}");
            return;
        }

        GD.Print(Encoding.UTF8.GetString(body));
        JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(body));
        Dictionary respData = (Dictionary)json.Result;
        Godot.Collections.Array listOfChars = (Godot.Collections.Array)respData["chars"];
        GetNode<VBoxContainer>("/root/Menu/UiLayer/CenterContainer/LoginBox").Visible = false;
        VBoxContainer listingNode = GetNodeOrNull<VBoxContainer>("/root/Menu/UiLayer/CenterContainer/CharacterListing");
        listingNode.Visible = true;

        if (listOfChars == null)
        {
            return;
        }
        
        foreach (Dictionary charInfo in listOfChars)
        {
            Button charButton = new Button();
            Godot.Collections.Array btnParams = new Godot.Collections.Array();
            btnParams.Add((string)charInfo["name"]);
            charButton.Connect("button_down", this, nameof(OnCharacterSelected), btnParams);
            charButton.Text = (string)charInfo["name"];

            listingNode.AddChild(charButton);
        }
    }

    public void OnChracterCreateRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        if (response_code != 201)
        {
            GD.Print($"account registration error {response_code}");
            return;
        }

        GetNode<BoxContainer>("/root/Menu/UiLayer/CenterContainer/CharacterCreateBox").Visible = false;

        HTTPRequest httpRequest = GetNode<HTTPRequest>("CharacterRequest");
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Bearer {token}";
        httpRequest.Request($"{baseURL}/game/characters", requestHeaders, false, HTTPClient.Method.Get, "");
    }

    public void OnCharacterSelected(string name)
    {
        HTTPRequest httpRequest = GetNode<HTTPRequest>("PlayRequest");
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Bearer {token}";

        Dictionary<string, string> pl = new Dictionary<string, string>();
        pl["Character"] = name;
        string jsonString = JSON.Print(pl);

        httpRequest.Request($"{baseURL}/game/play?selected_char={name}", requestHeaders, false, HTTPClient.Method.Post, jsonString);
    }

    public void OnPlayRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        if (response_code != 200)
        {
            GD.Print("game server fetch error");
            return;
        }

        GD.Print(Encoding.UTF8.GetString(body));

        JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(body));
        Dictionary respData = (Dictionary)json.Result;
        string charName = (string)respData["character_name"];
        string scene = (string)respData["scene"];
        string ip = (string)respData["ip"];
        int port = (int)((Single)respData["port"]);

        PackedScene packedGS = (PackedScene)ResourceLoader.Load($"res://scenes/Game.tscn");
        Node gameScene = packedGS.Instance();
        GetTree().Root.AddChild(gameScene);

        PackedScene ps = (PackedScene)ResourceLoader.Load($"res://worlds/{scene}.tscn");
        Node sceneNode = ps.Instance();
        GetNode<Node2D>("/root/Game/World").AddChild(sceneNode);
        gameScene.GetNode<PlayerClient>("PlayerClient").StartWithToken(token, charName, ip, port);
        
        GetTree().Root.GetNode<Node2D>("Menu").QueueFree();
    }
}