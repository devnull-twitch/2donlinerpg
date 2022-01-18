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
        isServer = GetTree().IsNetworkServer();        

        if (!isServer)
        {
            Connect("body_entered", this, nameof(OnPlayerEntered));
        }
    }

    public void OnPlayerEntered(Node2D body)
    {
        if (body is PlayerNetworking)
        {
            PlayerNetworking p = (PlayerNetworking)body;

            GetTree().NetworkPeer = null;

            PlayerClient pc = GetNode<PlayerClient>("/root/World/NetworkManager/PlayerClient");

            HTTPRequest httpRequest = GetNode<HTTPRequest>("PlayRequest");
            string[] requestHeaders = new string[1];
            requestHeaders[0] = $"Authorization: Bearer {pc.Token}";

            HTTPRequest req = GetNode<HTTPRequest>("HTTPRequest");
            req.Connect("request_completed", this, nameof(DoChangeScene));
            req.Request($"http://127.0.0.1:8082/game/change_scene?targte_scene={TargetScene}", requestHeaders, false, HTTPClient.Method.Post, "");   
        }
    }

    public void DoChangeScene(int result, int response_code, string[] headers, byte[] body)
    {
            JSONParseResult json = JSON.Parse(Encoding.UTF8.GetString(body));
            Dictionary respData = (Dictionary)json.Result;
            string scene = (string)respData["scene"];
            string ip = (string)respData["ip"];
            int port = (int)((Single)respData["port"]);

            PlayerClient pc = GetNode<PlayerClient>("/root/World/NetworkManager/PlayerClient");

            PackedScene ps = (PackedScene)ResourceLoader.Load($"res://{scene}.tscn");
            Node sceneNode = ps.Instance();
            GetTree().Root.AddChild(sceneNode);
            sceneNode.GetNode<PlayerClient>("/root/World/NetworkManager/PlayerClient").StartWithToken(pc.Token, ip, port);

            GetTree().Root.GetNode<Node2D>("Menu").Free();
    }
}
