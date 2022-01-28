using Godot;
using System;

public class SlotPanel : Panel
{
    public override bool CanDropData(Vector2 position, object data)
    {
        return (data is InventoryItem);
    }

    public override void DropData(Vector2 position, object data)
    {
        InventoryItem item = (InventoryItem)data;
        GetNode<TextureRect>("TextureRect").Texture = item.Texture;
    }
}
