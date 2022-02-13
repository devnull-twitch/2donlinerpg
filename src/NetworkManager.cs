using Godot;
using System;

public class NetworkManager : Node
{
    private PlayerManager pm;

    private EnemyManager em;

    private Godot.Collections.Array<int> unauthedPeers = new Godot.Collections.Array<int>();

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
        unauthedPeers.Add(id);
    }

    [Remote]
    public void serverClientAuth(string token, string charName)
    {
        GD.Print("client send auth");
        int id = GetTree().GetRpcSenderId();

        string payloadEncoded = token.Split(".")[1];
        payloadEncoded = payloadEncoded.PadRight(payloadEncoded.Length + (payloadEncoded.Length % 4), '=');
        byte[] decodedPayloadBytes = System.Convert.FromBase64String(payloadEncoded);
        string decodedPayloadStr = System.Text.Encoding.UTF8.GetString(decodedPayloadBytes);
        JSONParseResult json = JSON.Parse(decodedPayloadStr);
        Godot.Collections.Dictionary respData = (Godot.Collections.Dictionary)json.Result;

        onPlayerAuthed(id, (string)respData["sub"], charName);
    }

    public void onPlayerAuthed(int id, string accountName, string chara)
    {
        pm.onNetworkPeerConnected(id, accountName, chara);
        em.onNetworkPeerConnected(id);
    }

    public void onNetworkPeerDisconnected(int id)
    {
        pm.onNetworkPeerDisconnected(id);
    }
}
