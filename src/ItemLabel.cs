using Godot;
using System;

public class ItemLabel : Panel
{
    public InventoryItem Item;

    public override bool CanDropData(Vector2 position, object data)
    {
        if (data.Equals(this)) 
        {
            return false;
        }

        return (data is InventoryItem && ((InventoryItem)data).ItemID == Item.ItemID && ((InventoryItem)data).Item.Stackable);
    }

    public override object GetDragData(Vector2 position)
    {
        Label preview = new Label();
        preview.Text = Item.Item.Name;
        SetDragPreview(preview);
        return Item;
    }

    public override void DropData(Vector2 position, object data)
    {
        InventoryItem otherItem = (InventoryItem)data;

        Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
        int currentPeerId = GetTree().GetNetworkUniqueId();
        PlayerNetworking pn = mainNode.GetNode<PlayerNetworking>($"NetworkManager/PlayerManager/{currentPeerId}");
        GD.Print($"serverMergeItems => item.ItemID={otherItem.ItemID} Quantity1={Item.Quantity} Quantity2={otherItem.Quantity}");
        pn.RpcId(1, "serverMergeItems", Item.ItemID, Item.Quantity, otherItem.Quantity);
    }
}
