using Godot;
using Godot.Collections;
using System;
using System.Text;

public class Teleporter : Area2D
{
    private bool isServer;

    [Export]
    private string TargetScene;

    public void NetworkReady()
    {
        GD.Print("teleporter knows network is ready");

        Connect("body_entered", this, nameof(OnPlayerEntered));
    }

    public void OnPlayerEntered(Node2D body)
    {
        if (body is PlayerNetworking)
        {
            PlayerNetworking pn = (PlayerNetworking)body;
            int playerNetworkId = int.Parse(pn.Name);
            RpcId(playerNetworkId, "clientServerChange");
        }
    }

    [Remote]
    public void clientServerChange()
    {
        ConfigFile cf = new ConfigFile();
        Error err = cf.Load("res://networking.cfg");
        if (err != Error.Ok)
        {
            GD.Print($"unable to parse networking.cfg: {err}");
            return;
        }
        string baseURL = (string)cf.GetValue("gameapi", "base_url");

        // send message to server that we are about to leave
        GetTree().NetworkPeer = null;

        PlayerClient pc = GetNode<PlayerClient>("/root/Game/PlayerClient");

        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Bearer {pc.Token}";

        GD.Print("teleporter should make request");

        HTTPRequest req = GetNode<HTTPRequest>("HTTPRequest");
        req.Connect("request_completed", this, nameof(DoChangeScene));
        req.Request($"{baseURL}/game/play?target_scene={TargetScene}&selected_char={pc.CharName}", requestHeaders, false, HTTPClient.Method.Post, ""); 
    }

    public void DoChangeScene(int result, int response_code, string[] headers, byte[] body)
    {
            GD.Print("teleporter request complete");

            string respStr = Encoding.UTF8.GetString(body);
            GD.Print(respStr);
            JSONParseResult json = JSON.Parse(respStr);
            Dictionary respData = (Dictionary)json.Result;
            string scene = (string)respData["scene"];
            string ip = (string)respData["ip"];
            int port = (int)((Single)respData["port"]);

            // fetch stuff in current state
            Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
            PlayerClient pc = GetNode<PlayerClient>("/root/Game/PlayerClient");

            PackedScene ps = (PackedScene)ResourceLoader.Load($"res://worlds/{scene}.tscn");
            Node sceneNode = ps.Instance();
            GetNode<Node2D>("/root/Game/World").AddChild(sceneNode);

            mainNode.QueueFree();
            pc.StartWithToken(pc.Token, pc.CharName, ip, port);
    }
}
