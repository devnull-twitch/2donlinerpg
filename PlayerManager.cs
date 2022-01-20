using Godot;
using System;

public class PlayerManager : Node
{
    public void onNetworkPeerDisconnected(int id)
    {
        GD.PrintS("client disconnected!\n", id);
        Rpc("clientAndServerRemovePlayer");
    }


    [RemoteSync]
    public void clientAndServerRemovePlayer(int playerNetId)
    {
        Node n = GetNodeOrNull($"{playerNetId}");
        if (n != null) 
        {
            n.QueueFree();
        }
    }

    public void onNetworkPeerConnected(int id)
    {
        GD.PrintS("client connected!\n", id);

        // sync all known player infos to new client
        foreach(object o in GetChildren())
        {
            if (o is PlayerNetworking)
            {
                PlayerNetworking on = (PlayerNetworking)o;
                RpcId(id, "clientCreatePlayer", on.Name);
                on.RpcId(id, "clientSetStats", on.GetHealth(), on.GetArmor());
            }
        }

        // sync the new client to itself ( and everyone else )
        Rpc("clientCreatePlayer", $"{id}");

        // create new client
        PackedScene ps = GD.Load<PackedScene>("res://Player.tscn");
        Node p = ps.Instance();
        PlayerNetworking pn = (PlayerNetworking)p;
        pn.Name = $"{id}";
        AddChild(p);

        pn.SetHealth(100);
        // here we would load persistent player data

        pn.Rpc("clientSetStats", pn.GetHealth(), pn.GetArmor());
    }

    [Remote]
    public void clientCreatePlayer(string name)
    {
        PackedScene ps = GD.Load<PackedScene>("res://Player.tscn");
        Node pn = ps.Instance();
        if (pn is PlayerNetworking)
        {
            PlayerNetworking p = (PlayerNetworking)pn;
            p.Name = name;
            AddChild(p);
        }
    }
}
