using Godot;
using System;

public class NetworkManager : Node
{
    private PlayerManager pm;

    private EnemyManager em;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        string[] args = OS.GetCmdlineArgs(); 
        for (int key = 0; key < args.Length; ++key)
        {
            string arg = args[key];
            if (arg == "--server")
            {
                int port = int.Parse(args[key+1]);
                Start(port);
                return;
            }
        }
    }

    public void Start(int port)
    {
        pm = GetNode<PlayerManager>("PlayerManager");
        em = GetNode<EnemyManager>("EnemyManager");

        startServer(port);

        GetTree().Connect("network_peer_connected", this, nameof(onNetworkPeerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(onNetworkPeerDisconnected));

        GetTree().CallGroup("network_awaiting", "NetworkReady");
    }

    protected void startServer(int port)
    {
        GD.PrintS($"Starting Server on port {port}!\n");

        NetworkedMultiplayerENet peer = new NetworkedMultiplayerENet();
        peer.ServerRelay = false;
        var error = peer.CreateServer(port);
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
