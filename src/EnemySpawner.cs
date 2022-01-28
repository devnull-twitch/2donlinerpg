using Godot;
using System;

public class EnemySpawner : Node2D
{
    public Timer timer;

    public Enemy target;

    [Export]
    public Resource EnemyTemplate;

    [Export]
    public LootTable DropTable; 

    public string Somethging;

    public void NetworkReady()
    {
        if (GetTree().IsNetworkServer())
        {
            timer = GetNode<Timer>("Timer");
            timer.Connect("timeout", this, nameof(DoSpawn));
        }
    }

    public void WaitNextSpawn()
    {
        GD.Print("enemy spwan timer started");
        target = null;
        timer.Start();
    }

    public void DoSpawn()
    {
        timer.Stop();

        GD.Print("spawn enemy");
        Rpc("Spawn");

        if (target != null)
        {
            target.Connect("Killed", this, nameof(WaitNextSpawn));
        }
    }

    [RemoteSync]
    public void Spawn()
    {
        if (EnemyTemplate is PackedScene)
        {
            PackedScene ps = (PackedScene)EnemyTemplate;
            target = (Enemy)ps.Instance();
            AddChild(target);
            target.GlobalPosition = new Vector2(GlobalPosition.x, GlobalPosition.y);
            target.DropTable = DropTable;
        }
        else
        {
            GD.Print("no :(");
        }
    }
}
