using Godot;
using System;

public class Item : Resource
{
    [Export]
    public Texture FloorTexture;

    [Export]
    public Texture InventoryTexture;

    [Export]
    public int ArmotValue;

    [Export]
    public string Name;
}
