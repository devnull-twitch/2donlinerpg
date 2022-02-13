using Godot;
using System;

public class PointTrader : Sprite
{
    public override void _Ready()
    {
        Area2D tradeArea = GetNode<Area2D>("TradeArea");
        tradeArea.Connect("body_entered", this, nameof(OnPlayerEntered));
        tradeArea.Connect("body_exited", this, nameof(OnPlayerExited));
    }

    public void OnPlayerEntered(Node body)
    {
        if (body is PlayerNetworking)
        {
            PlayerNetworking pn = (PlayerNetworking)body;
            int netID = int.Parse(pn.Name);
            pn.RpcId(netID, "EnableTrade");
        }
    }

    public void OnPlayerExited(Node body)
    {
        if (body is PlayerNetworking)
        {
            PlayerNetworking pn = (PlayerNetworking)body;
            int netID = int.Parse(pn.Name);
            pn.RpcId(netID, "DisableTrade");
        }
    }
}
