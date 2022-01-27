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
        ConfigFile cf = new ConfigFile();
        Error err = cf.Load("res://networking.cfg");
        if (err != Error.Ok)
        {
            GD.Print($"unable to parse networking.cfg: {err}");
            return;
        }

        GetTree().Connect("connected_to_server", this, nameof(onConnectedToServer));

        ip = (string)cf.GetValue("gameserver", "ip");
        port = (int)cf.GetValue("gameserver", "port");

        if (ip != "" && port > 0)
        {
            StartWithToken(Token, "Tester", ip, port);
        }
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
        GD.PrintS("connected\n");
    }
}
