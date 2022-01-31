using Godot;
using System;

public class PlayerClient : Node
{
    public string Token;

    public string CharName;

    private string ip;

    private int port;

    public override void _Ready()
    {
        GetTree().Connect("connected_to_server", this, nameof(onConnectedToServer));
    }

    public void StartWithToken(string token, string charName, string ip, int port)
    {
        GD.Print("StartWithToken called");

        this.CharName = charName;
        this.Token = token;
        this.ip = ip;
        this.port = port;

        foreach (string arg in OS.GetCmdlineArgs())
        {
            if (arg == "--server")
            {
                return;
            }
        }

        startClient();
    }

    protected void startClient()
    {
        GD.PrintS("Starting Client!\n");

        NetworkedMultiplayerENet peer = new NetworkedMultiplayerENet();
        var error = peer.CreateClient(ip, port);
        if (error != Error.Ok) 
        {
            GD.PrintErr(error);
            return;
        }

        GetTree().NetworkPeer = peer;
    }

    public void onConnectedToServer()
    {
        Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
        mainNode.GetNode<NetworkManager>("NetworkManager").RpcId(1, "serverClientAuth", Token, CharName);
    }
}
