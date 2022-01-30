using Godot;
using System;
using System.Collections.Generic;

public class FloorDrop : Area2D
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
    public int ItemID
    {
        get
        {
            return itemID;
        }
        set
        {
            itemID = value;
            hasItem = true;
            PickupOnEnter = true;
        }
    }

    private int itemID;

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
        GD.Print("Does this even work?");
        if(body is PlayerNetworking && PickupOnEnter)
        {
            PlayerNetworking player = (PlayerNetworking)body;
            
            if (isMoney)
            {
                player.AddMoney(moneyVal);
            }

            if (hasItem)
            {
                if (!player.AddItem(ItemID)) 
                {
                    return;
                }
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
