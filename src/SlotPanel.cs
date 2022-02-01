using Godot;
using System;

public class SlotPanel : Panel
{
    [Export]
    public int SlotID;

    private int CurrentItemID = 0;

    public override bool CanDropData(Vector2 position, object data)
    {
        return (data is InventoryItem && ((InventoryItem)data).Item.Wearable);
    }

    public override void DropData(Vector2 position, object data)
    {
        int priorSlottedItemID = CurrentItemID;
        InventoryItem item = (InventoryItem)data;
        CurrentItemID = item.ItemID;
        GetNode<TextureRect>("TextureRect").Texture = item.Item.InventoryTexture;

        Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
        int currentPeerId = GetTree().GetNetworkUniqueId();
        PlayerNetworking pn = mainNode.GetNode<PlayerNetworking>($"NetworkManager/PlayerManager/{currentPeerId}");
        GD.Print($"item.ItemID={item.ItemID} SlotID={SlotID} priorSlottedItemID={priorSlottedItemID}");
        pn.RpcId(1, "serverSlotItem", item.ItemID, SlotID, priorSlottedItemID);
    }
}
