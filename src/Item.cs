using Godot;
using System;
using System.Collections.Generic;

public class Item : Area2D
{
    public bool PickupOnEnter = true;

    public int Money
    {
        get
        {
            return moneyVal;
        }

        set
        {
            moneyVal = value;
            isMoney = true;
            PickupOnEnter = true;
        }
    }

    [Export]
    public string ItemName
    {
        get
        {
            return itemName;
        }
        set
        {
            itemName = value;
            hasItem = true;
            PickupOnEnter = true;
        }
    }

    private string itemName;

    private bool hasItem;

    private bool isServer;
    
    private List<string> playersInRange;

    private bool isInventoryItem;

    private bool isMoney;

    private int moneyVal;

    public void NetworkReady()
    {
        isServer = GetTree().IsNetworkServer();        

        if (isServer)
        {
            playersInRange = new List<string>(64);

            Connect("body_entered", this, nameof(OnPlayerEntered));
            Connect("body_exited", this, nameof(OnPlayerExited));
        }
    }

    public void OnPlayerEntered(Node2D body)
    {
        if(body is PlayerNetworking && PickupOnEnter)
        {
            PlayerNetworking player = (PlayerNetworking)body;
            
            if (isMoney)
            {
                player.AddMoney(moneyVal);
            }

            if (hasItem)
            {
                player.AddItem(itemName);
            }

            Rpc("allRemoveItem");
            return;
        }

        playersInRange.Add(body.Name);
    }

    public void OnPlayerExited(Node2D body)
    {
        playersInRange.Remove(body.Name);
    }

    [RemoteSync]
    public void allRemoveItem()
    {
        GD.Print("removed item");
        QueueFree();
    }
}
