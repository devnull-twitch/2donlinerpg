using Godot;
using System;

public class InventoryManager : Node
{
    public int maxSlots = 10;

    [RemoteSync]
    public void clientServerAddItem(string name)
    {
        PackedScene itemScene = GD.Load<PackedScene>("res://InventoryItem.tscn");
        Node itemInstance = itemScene.Instance();
        InventoryItem newItem = (InventoryItem)itemInstance;
        newItem.Name = name;

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
            VBoxContainer inventoryPanel = GetNode<VBoxContainer>("/root/World/UiLayer/Inventory/InventoryListing");
            Label newItemName = new Label();
            newItemName.Text = name;
            inventoryPanel.AddChild(newItemName);
        }
    }
}