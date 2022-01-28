using Godot;
using System;

public class InventoryManager : Node
{
    public int maxSlots = 10;

    [RemoteSync]
    public void clientServerAddItem(string name)
    {
        PackedScene dropItemScene = GD.Load<PackedScene>(name);
        Item dropItem = (Item)dropItemScene.Instance();

        PackedScene itemScene = GD.Load<PackedScene>("res://prefabs/InventoryItem.tscn");
        Node itemInstance = itemScene.Instance();
        InventoryItem newItem = (InventoryItem)itemInstance;
        newItem.Name = name;
        newItem.Texture = dropItem.GetNode<Sprite>("Sprite").Texture;

        switch(name)
        {
            case "TestItem":
                newItem.Armor = 10;
                break;
        }

        AddChild(newItem);

        if (GetTree().IsNetworkServer())
        {
            GD.Print("handle item effects on server?");
            if(GetChildCount() > maxSlots)
            {
                GD.Print("no... too much stuff for inventory");
                return;
            }

            if (newItem.Armor > 0)
            {
                GetParent<PlayerNetworking>().AddArmor(newItem.Armor);
            }
        }
        else
        {
            VBoxContainer inventoryPanel = GetNode<VBoxContainer>("/root/Game/UiLayer/Inventory/Split/Listing");

            PackedScene itemLabelPanel = GD.Load<PackedScene>("res://prefabs/ItemLabelPanel.tscn");
            ItemLabel panel = (ItemLabel)itemLabelPanel.Instance();

            panel.Item = newItem;
            panel.GetNode<Label>("Label").Text = newItem.Name;
            inventoryPanel.AddChild(panel);
        }
    }
}