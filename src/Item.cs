using Godot;
using System;

public class Item : Resource
{
    [Export]
    public int ID;

    [Export]
    public Texture FloorTexture;

    [Export]
    public Texture InventoryTexture;

    [Export]
    public bool Wearable;

    [Export]
    public int ArmorValue;

    [Export]
    public string Name;

    [Export]
    public int MonetaryValue;
}
