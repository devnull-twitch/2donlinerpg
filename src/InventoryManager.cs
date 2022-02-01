using Godot;
using System;

public class InventoryManager : Node
{
    public int maxSlots = 10;

    [Remote]
    public void clientAddItem(int itemID)
    {
        ItemList itemListRes = GD.Load<ItemList>("res://resources/Items.tres");
        Item itemRes = itemListRes.Items[itemID];

        PackedScene itemScene = GD.Load<PackedScene>("res://prefabs/InventoryItem.tscn");
        Node itemInstance = itemScene.Instance();
        InventoryItem newItem = (InventoryItem)itemInstance;
        newItem.Name = itemRes.Name;
        newItem.Item = itemRes;
        newItem.ItemID = itemID;

        AddChild(newItem);

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
        foreach(Node n in GetChildren())
        {
            if (n is InventoryItem && ((InventoryItem)n).ItemID == itemID)
            {
                n.QueueFree();
            }
        }
    } 
}