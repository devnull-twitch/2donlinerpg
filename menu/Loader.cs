using Godot;
using System;
using Godot.Collections;
using System.Text;

public class Loader : Node
{
    private string token;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetNode<Button>("/root/Menu/UiLayer/CenterContainer/LoginBox/LoginBtn").Connect("button_down", this, nameof(DoLogin));
        GetNode("LoginRequest").Connect("request_completed", this, "OnLoginRequestCompleted");
        GetNode("CharacterRequest").Connect("request_completed", this, "OnCharacterRequestCompleted");
        GetNode("PlayRequest").Connect("request_completed", this, "OnPlayRequestCompleted");
    }

    public void DoLogin()
    {
        Dictionary<string, string> pl = new Dictionary<string, string>();
        pl["Username"] = GetNode<LineEdit>("/root/Menu/UiLayer/CenterContainer/LoginBox/UsernameInput").Text;
        pl["Password"] = GetNode<LineEdit>("/root/Menu/UiLayer/CenterContainer/LoginBox/PasswordInput").Text;

        string jsonString = JSON.Print(pl);

        HTTPRequest httpRequest = GetNode<HTTPRequest>("LoginRequest");
        httpRequest.Request("http://127.0.0.1:8082/game/login", null, false, HTTPClient.Method.Post, jsonString);
    }

    public void OnLoginRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        if (response_code != 200)
        {
            GD.Print("login error");
            return;
        }

        GD.Print($"result={result} response_code={response_code}");

        JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(body));
        token = (string)json.Get("Token");

        HTTPRequest httpRequest = GetNode<HTTPRequest>("CharacterRequest");
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Bearer {token}";
        httpRequest.Request("http://127.0.0.1:8082/game/characters", requestHeaders, false, HTTPClient.Method.Get, "");
    }

    public void OnCharacterRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        if (response_code != 200)
        {
            GD.Print($"character loading error {response_code}");
            return;
        }

        JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(body));
        Dictionary respData = (Dictionary)json.Result;
        Godot.Collections.Array listOfChars = (Godot.Collections.Array)respData["chars"];
        GetNode<VBoxContainer>("/root/Menu/UiLayer/CenterContainer/LoginBox").Visible = false;
        VBoxContainer listingNode = GetNodeOrNull<VBoxContainer>("/root/Menu/UiLayer/CenterContainer/CharacterListing");
        if (listOfChars == null)
        {
            GD.Print("this is really null?");
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
        listingNode.Visible = true;
    }

    public void OnCharacterSelected(string name)
    {
        HTTPRequest httpRequest = GetNode<HTTPRequest>("PlayRequest");
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Bearer {token}";

        Dictionary<string, string> pl = new Dictionary<string, string>();
        pl["Character"] = name;
        string jsonString = JSON.Print(pl);

        httpRequest.Request($"http://127.0.0.1:8082/game/play?selected_char={name}", requestHeaders, false, HTTPClient.Method.Post, jsonString);
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
        string scene = (string)respData["scene"];
        string ip = (string)respData["ip"];
        int port = (int)((Single)respData["port"]);

        PackedScene ps = (PackedScene)ResourceLoader.Load($"res://{scene}.tscn");
        Node sceneNode = ps.Instance();
        GetTree().Root.AddChild(sceneNode);
        sceneNode.GetNode<PlayerClient>("NetworkManager/PlayerClient").StartWithToken(token, ip, port);
        
        GetTree().Root.GetNode<Node2D>("Menu").Free();
    }
}