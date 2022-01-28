using Godot;
using System;

public class LootTable : Resource
{
    [Export]
    public Godot.Collections.Dictionary<PackedScene, int> Drops;

    public Godot.Collections.Array<string> RollDrops()
    {
        Godot.Collections.Array<string> list = new Godot.Collections.Array<string>();
        RandomNumberGenerator gen = new RandomNumberGenerator();

        foreach (PackedScene key in Drops.Keys)
        {
            int chance = Drops[key];
            int rand = (int)Math.Round(gen.RandfRange(0, 100));
            GD.Print($"chance={chance} and rand={rand}");

            if (rand > chance)
            {
                Item dropItem = (Item)key.Instance();
                list.Add(key.ResourcePath);
            }
        }

        return list;
    }
}
