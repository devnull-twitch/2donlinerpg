using Godot;
using System;

public class NetworkManager : Node
{
    private PlayerManager pm;

    private EnemyManager em;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        foreach (string arg in OS.GetCmdlineArgs())
        {
            if (arg == "--server")
            {
                Start();
                return;
            }
        }
    }

    public void Start()
    {
        pm = GetNode<PlayerManager>("PlayerManager");
        em = GetNode<EnemyManager>("EnemyManager");

        startServer();

        GetTree().Connect("network_peer_connected", this, nameof(onNetworkPeerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(onNetworkPeerDisconnected));

        GetTree().CallGroup("network_awaiting", "NetworkReady");
    }

    protected void startServer()
    {
        GD.PrintS("Starting Server!\n");

        NetworkedMultiplayerENet peer = new NetworkedMultiplayerENet();
        peer.ServerRelay = false;
        var error = peer.CreateServer(50123);
        if (error != Error.Ok) 
        {
            GD.PrintErr(error);
            return;
        }

        GetTree().NetworkPeer = peer;
    }

    public void onNetworkPeerConnected(int id)
    {
        pm.onNetworkPeerConnected(id);
        em.onNetworkPeerConnected(id);
    }

    public void onNetworkPeerDisconnected(int id)
    {
        pm.onNetworkPeerDisconnected(id);
    }
}
