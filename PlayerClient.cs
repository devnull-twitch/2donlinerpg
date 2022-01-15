using Godot;
using System;

public class PlayerClient : Node
{
    public override void _Ready()
    {
        foreach (string arg in OS.GetCmdlineArgs())
        {
            if (arg == "--server")
            {
                return;
            }
        }

        GetTree().Connect("connected_to_server", this, nameof(onConnectedToServer));

        startClient();
    }

    protected void startClient()
    {
        GD.PrintS("Starting Client!\n");

        NetworkedMultiplayerENet peer = new NetworkedMultiplayerENet();
        var error = peer.CreateClient("127.0.0.1", 50123);
        if (error != Error.Ok) 
        {
            GD.PrintErr(error);
            return;
        }

        GetTree().NetworkPeer = peer;
    }

    public void onConnectedToServer()
    {
        GD.PrintS("connected\n");
    }
}
