using Godot;
using System;

public class LootTable : Resource
{
    [Export]
    public Godot.Collections.Dictionary<int, float> Drops;

    public Godot.Collections.Array<int> RollDrops()
    {
        Godot.Collections.Array<int> list = new Godot.Collections.Array<int>();
        RandomNumberGenerator gen = new RandomNumberGenerator();
        gen.Seed = (ulong)(DateTime.Now.Ticks);

        foreach (int itemID in Drops.Keys)
        {
            float threshold = 100f - Drops[itemID];
            float rand = gen.RandfRange(0, 100);
            GD.Print($"itemID={itemID} threshold={threshold} and rand={rand}");

            if (rand >= threshold)
            {
                list.Add(itemID);
            }
        }

        return list;
    }
}
