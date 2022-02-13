using Godot;
using System;

public class SellDropPanel : Panel
{
    public override bool CanDropData(Vector2 position, object data)
    {
        return (data is InventoryItem);
    }

    public override void DropData(Vector2 position, object data)
    {
        InventoryItem dropItem = (InventoryItem)data;

        Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
        int currentPeerId = GetTree().GetNetworkUniqueId();
        PlayerNetworking pn = mainNode.GetNode<PlayerNetworking>($"NetworkManager/PlayerManager/{currentPeerId}");
        pn.RpcId(1, "sellInventoryItem", dropItem.ItemID);

        GridContainer tradeContainer = GetNode<GridContainer>("/root/Game/UiLayer/TradeDialog/GridContainer");
        foreach (ItemLabel tradeItems in tradeContainer.GetChildren())
        {
            if (tradeItems.Item.ItemID == dropItem.ItemID && tradeItems.Item.Quantity == dropItem.Quantity)
            {
                tradeItems.QueueFree();
                return;
            }
        }
    }
}
