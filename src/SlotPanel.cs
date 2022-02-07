using Godot;
using System;

public class SlotPanel : Panel
{
    [Export]
    public int SlotID;

    private InventoryItem item = null;

    public InventoryItem CurrentItem {
        get {
            return item;
        }
        set {
            GetNode<TextureRect>("TextureRect").Texture = value.Item.InventoryTexture;
        }
    }

    public override bool CanDropData(Vector2 position, object data)
    {
        return (data is InventoryItem && ((InventoryItem)data).Item.Wearable);
    }

    public override void DropData(Vector2 position, object data)
    {
        int priorSlottedItemID = 0;
        if (item != null)
        {
            priorSlottedItemID = item.ItemID;
        }
        InventoryItem dropItem = (InventoryItem)data;
        CurrentItem = dropItem;

        Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
        int currentPeerId = GetTree().GetNetworkUniqueId();
        PlayerNetworking pn = mainNode.GetNode<PlayerNetworking>($"NetworkManager/PlayerManager/{currentPeerId}");
        GD.Print($"item.ItemID={item.ItemID} SlotID={SlotID} priorSlottedItemID={priorSlottedItemID}");
        pn.RpcId(1, "serverSlotItem", item.ItemID, SlotID, priorSlottedItemID);
    }
}
