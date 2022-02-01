using Godot;
using System;
using System.Text;

public class PlayerManager : Node
{
    private string baseURL;

    private string serverPassword;

    private Godot.Collections.Dictionary<string, int> idMap = new Godot.Collections.Dictionary<string, int>();

    public override void _Ready()
    {
        ConfigFile cf = new ConfigFile();
        Error err = cf.Load("res://networking.cfg");
        if (err != Error.Ok)
        {
            GD.Print($"unable to parse networking.cfg: {err}");
            return;
        }

        baseURL = (string)cf.GetValue("gameapi", "base_url");
        serverPassword = (string)cf.GetValue("gameapi", "server_auth");

        GetNode<HTTPRequest>("InventoryLoader").Connect("request_completed", this, nameof(onInventoryFetched));
    }

    public void onNetworkPeerDisconnected(int id)
    {
        GD.PrintS("client disconnected!\n", id);
        Rpc("clientAndServerRemovePlayer", $"{id}");
    }


    [RemoteSync]
    public void clientAndServerRemovePlayer(string playerName)
    {
        Node n = GetNodeOrNull(playerName);
        if (n != null) 
        {
            n.Free();
        }
    }

    public void onNetworkPeerConnected(int id, string accountName, string characterName)
    {
        GD.PrintS("client connected!\n", id);

        // sync all known player infos to new client
        foreach(object o in GetChildren())
        {
            if (o is PlayerNetworking)
            {
                PlayerNetworking on = (PlayerNetworking)o;
                RpcId(id, "clientCreatePlayer", on.Name);
                on.RpcId(id, "clientUpdatePlayerPos", on.Position.x, on.Position.y, on.GetNode<Sprite>("ArmSprite").Rotation);
                on.RpcId(id, "clientSetStats", on.GetHealth(), on.GetArmor());
            }
        }

        // sync the new client to itself ( and everyone else )
        Rpc("clientCreatePlayer", $"{id}");

        idMap[$"{accountName}.{characterName}"] = id;

        // create new client
        PackedScene ps = GD.Load<PackedScene>("res://prefabs/Player.tscn");
        Node p = ps.Instance();
        PlayerNetworking pn = (PlayerNetworking)p;
        pn.Name = $"{id}";
        pn.Account = accountName;
        pn.Character = characterName;
        AddChild(p);

        pn.SetHealth(100);
        HTTPRequest invLoaderReq = GetNode<HTTPRequest>("InventoryLoader");
        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("gameserver:" + serverPassword));
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Basic {svcCredentials}";
        invLoaderReq.Request($"{baseURL}/character/inventory?account={accountName}&char={characterName}", requestHeaders, false, HTTPClient.Method.Get, "");

        pn.Rpc("clientSetStats", pn.GetHealth(), pn.GetArmor());
    }

    public void addPlayerInventory(string account, string character, int itemID)
    {
        Godot.Collections.Dictionary<string, string> pl = new Godot.Collections.Dictionary<string, string>();
        pl["account"] = account;
        pl["character"] = character;
        pl["item_id"] = $"{itemID}";

        string jsonString = JSON.Print(pl);

        HTTPRequest invLoaderReq = GetNode<HTTPRequest>("InventorySaver");
        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("gameserver:" + serverPassword));
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Basic {svcCredentials}";
        invLoaderReq.Request($"{baseURL}/character/inventory", requestHeaders, false, HTTPClient.Method.Post, jsonString);
    }

    public void changePlayerInventory(string account, string character, int removeID, int slotID, int addID)
    {
        Godot.Collections.Dictionary<string, string> pl = new Godot.Collections.Dictionary<string, string>();
        pl["account"] = account;
        pl["character"] = character;
        pl["slot_id"] = $"{slotID}";
        pl["remove_item_id"] = $"{removeID}";
        pl["add_item_id"] = $"{addID}";

        string jsonString = JSON.Print(pl);

        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("gameserver:" + serverPassword));
        string[] requestHeaders = new string[1];
        requestHeaders[0] = $"Authorization: Basic {svcCredentials}";
        GetNode<HTTPRequest>("InventorySaver").Request($"{baseURL}/character/inventory/change", requestHeaders, false, HTTPClient.Method.Post, jsonString);
    }

    public void onInventoryFetched(int result, int response_code, string[] headers, byte[] body)
    {
        if (response_code != 200)
        {
            GD.Print("game server inventory fetch error");
            return;
        }

        string bodyStr = Encoding.UTF8.GetString(body);
        GD.Print(bodyStr);
        JSONParseResult json = JSON.Parse(bodyStr);
        Godot.Collections.Dictionary respData = (Godot.Collections.Dictionary)json.Result;
        string account = (string)respData["account"];
        string chracter = (string)respData["character"];
        Godot.Collections.Array inventoryItems = (Godot.Collections.Array)respData["item_ids"];
        
        int peerID = idMap[$"{account}.{chracter}"];
        foreach (float itemID in inventoryItems)
        {
            GetNode<PlayerNetworking>($"{peerID}").AddItem((int)itemID, true);
        }

        GD.Print(inventoryItems);
    } 

    [Remote]
    public void clientCreatePlayer(string name)
    {
        PackedScene ps = GD.Load<PackedScene>("res://prefabs/Player.tscn");
        Node pn = ps.Instance();
        if (pn is PlayerNetworking)
        {
            PlayerNetworking p = (PlayerNetworking)pn;
            p.Name = name;
            AddChild(p);
        }
    }
}
