using Godot;
using System;

public class ItemLabel : Panel
{
    public InventoryItem Item;

    public override object GetDragData(Vector2 position)
    {
        GD.Print("check drag?");
        Label preview = new Label();
        preview.Text = Item.Name;
        SetDragPreview(preview);
        return Item;
    }

    public void DebugMouseEntered()
    {
        GD.Print("mouse has entered");
    }
}
