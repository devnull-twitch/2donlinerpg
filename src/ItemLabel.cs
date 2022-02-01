using Godot;
using System;

public class ItemLabel : Panel
{
    public InventoryItem Item;

    public override object GetDragData(Vector2 position)
    {
        Label preview = new Label();
        preview.Text = Item.Item.Name;
        SetDragPreview(preview);
        return Item;
    }
}
