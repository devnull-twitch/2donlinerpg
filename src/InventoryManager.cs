using Godot;
using System;

public class InventoryManager : Node
{
    public int maxSlots = 10;

    [Remote]
    public void clientAddItem(int itemID, int quantity)
    {
        ItemList itemListRes = GD.Load<ItemList>("res://resources/Items.tres");
        Item itemRes = itemListRes.Items[itemID];

        InventoryItem newItem = GD.Load<InventoryItem>("res://resources/InventoryItem.tres");
        newItem.Item = itemRes;
        newItem.ItemID = itemID;
        newItem.Quantity = quantity;

        GridContainer inventoryPanel = GetNode<GridContainer>("/root/Game/UiLayer/Inventory/Split/Listing");

        PackedScene itemLabelPanel = GD.Load<PackedScene>("res://prefabs/ItemLabelPanel.tscn");
        ItemLabel panel = (ItemLabel)itemLabelPanel.Instance();

        panel.Item = newItem;
        panel.GetNode<TextureRect>("Sprite").Texture = newItem.Item.InventoryTexture;
        inventoryPanel.AddChild(panel);
    }

    [Remote]
    public void clientRemoveItem(int itemID)
    {
        GridContainer inventoryPanel = GetNode<GridContainer>("/root/Game/UiLayer/Inventory/Split/Listing");
        foreach (ItemLabel il in inventoryPanel.GetChildren())
        {
            if (il.Item.ItemID == itemID)
            {
                il.Free();
                break;
            }
        }
    } 
}