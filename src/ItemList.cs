using Godot;
using System;

public class ItemList : Resource
{
    [Export]
    public Godot.Collections.Dictionary<int, Item> Items;
}
