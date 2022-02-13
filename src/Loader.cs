using Godot;
using System;
using Godot.Collections;
using System.Text;

public class Loader : Node
{
    public string waitGameToken;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ConfigFile tokenCfg = new ConfigFile();
        Error err = tokenCfg.Load("user://jwt.cfg");
        if (err != Error.Ok)
        {
            EnableTwitchLoginBtn();
        }
        else 
        {
            string token = (string)tokenCfg.GetValue("user", "token");
            if (token.Length <= 0)
            {
                EnableTwitchLoginBtn();
                return;
            }
            
            LoadCharsAndStart(token);
        }
    }

    public void EnableTwitchLoginBtn()
    {
        Button LoginBtn = GetNode<Button>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/LoginBtn");
        LoginBtn.Connect("button_down", this, nameof(StartLogin));
        LoginBtn.Disabled = false;
    }

    public void StartLogin()
    {
        GetNode<Label>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/StatusLabel").Text = "Loading game token . . .";
        HttpWorker twStartLoader = new HttpWorker();
        twStartLoader.Setup(HTTPClient.Method.Post, "/rpg/twitch/start", "", "");
        twStartLoader.MakeRequest();

        if (twStartLoader.LastResponse.Length <= 0)
        {
            GetNode<Label>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/StatusLabel").Text = "Login error.\nTry again later.";
            return;
        }

        GD.Print(twStartLoader.LastResponse);
        JSONParseResult authJson = JSON.Parse(twStartLoader.LastResponse);
        Dictionary authData = (Dictionary)authJson.Result;
        LineEdit authUrlField = GetNode<LineEdit>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/AuthURL");
        authUrlField.Text = (string)authData["auth_url"];
        authUrlField.Visible = true;
        waitGameToken = (string)authData["wait_token"];
        
        Button RefreshBtn = GetNode<Button>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/RefreshBtn");
        RefreshBtn.Connect("button_down", this, nameof(DoRefresh));
        RefreshBtn.Visible = true;
    }

    public void DoRefresh()
    {
        GetNode<Button>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/RefreshBtn").Disabled = true;
        
        HttpWorker twCheckLoader = new HttpWorker();
        twCheckLoader.Setup(HTTPClient.Method.Get, $"/rpg/twitch/check?gametoken={waitGameToken}", "", "");
        twCheckLoader.MakeRequest();

        if (twCheckLoader.LastResponse.Length <= 0)
        {
            GetNode<Button>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/RefreshBtn").Disabled = false;
            return;
        }

        ConfigFile newUserCfg = new ConfigFile();
        newUserCfg.SetValue("user", "token", twCheckLoader.LastResponse);
        Error err = newUserCfg.Save("user://jwt.cfg");
        if (err != Error.Ok) {
            GD.Print($"unable to save jwt: {err}");
        }

        LoadCharsAndStart(twCheckLoader.LastResponse);
    }

    public void LoadCharsAndStart(string token)
    {
        HttpWorker charLoader = new HttpWorker();
        charLoader.Setup(HTTPClient.Method.Get, "/rpg/game/characters", "", token);
        charLoader.MakeRequest();
        
        if (charLoader.LastResponse.Length <= 0)
        {
            EnableTwitchLoginBtn();
            return;
        }

        JSONParseResult json = JSON.Parse(charLoader.LastResponse);
        Dictionary respData = (Dictionary)json.Result;
        Godot.Collections.Array listOfChars = (Godot.Collections.Array)respData["chars"];

        if (listOfChars.Count <= 0)
        {
            GetNode<Label>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/StatusLabel").Text = "Missing character.\nAsk for support.";
            return;
        }

        Dictionary charData = (Dictionary)listOfChars[0];
        string charName = (string)charData["name"];

        HttpWorker playLoader = new HttpWorker();
        playLoader.Setup(HTTPClient.Method.Post, $"/rpg/game/play?selected_char={charName}", "", token);
        playLoader.MakeRequest();

        if (playLoader.LastResponse.Length <= 0)
        {
            GetNode<Label>("UiLayer/CenterContainer/MarginContainer/CenterContainer/PanelContainer/LoginBox/StatusLabel").Text = "Server load error.\nTry again later.";
            return;
        }

        JSONParseResult playJson = JSON.Parse(playLoader.LastResponse);
        Dictionary playData = (Dictionary)playJson.Result;
        string playCharName = (string)playData["character_name"];
        string scene = (string)playData["scene"];
        string ip = (string)playData["ip"];
        int port = (int)((Single)playData["port"]);

        PackedScene packedGS = (PackedScene)ResourceLoader.Load($"res://scenes/Game.tscn");
        Node gameScene = packedGS.Instance();
        GetTree().Root.AddChild(gameScene);

        PackedScene ps = (PackedScene)ResourceLoader.Load($"res://worlds/{scene}.tscn");
        Node sceneNode = ps.Instance();
        GetNode<Node2D>("/root/Game/World").AddChild(sceneNode);

        gameScene.GetNode<PlayerClient>("PlayerClient").StartWithToken(token, playCharName, ip, port);
        
        GetTree().Root.GetNode<Node2D>("Menu").QueueFree();
    }
}