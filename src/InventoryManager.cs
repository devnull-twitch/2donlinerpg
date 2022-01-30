using Godot;
using System;

public class InventoryManager : Node
{
    public int maxSlots = 10;

    [RemoteSync]
    public void clientAddItem(int itemID)
    {
        ItemList itemListRes = GD.Load<ItemList>("res://resources/Items.tres");
        Item itemRes = itemListRes.Items[itemID];

        PackedScene itemScene = GD.Load<PackedScene>("res://prefabs/InventoryItem.tscn");
        Node itemInstance = itemScene.Instance();
        InventoryItem newItem = (InventoryItem)itemInstance;
        newItem.Name = itemRes.Name;
        newItem.Texture = itemRes.InventoryTexture;
        newItem.Armor = itemRes.ArmotValue;

        AddChild(newItem);

        VBoxContainer inventoryPanel = GetNode<VBoxContainer>("/root/Game/UiLayer/Inventory/Split/Listing");

        PackedScene itemLabelPanel = GD.Load<PackedScene>("res://prefabs/ItemLabelPanel.tscn");
        ItemLabel panel = (ItemLabel)itemLabelPanel.Instance();

        panel.Item = newItem;
        panel.GetNode<Label>("Label").Text = newItem.Name;
        inventoryPanel.AddChild(panel);
    }
}