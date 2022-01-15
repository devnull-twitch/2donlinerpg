using Godot;
using System.Collections.Generic;
using System;

public class EnemyManager : Node
{
    private ICollection<EnemySpawner> spawners;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        spawners = new List<EnemySpawner>(GetChildCount());
        foreach (Node n in GetChildren())
        {
            if (n is EnemySpawner)
            {
                EnemySpawner spawner = (EnemySpawner)n;
                spawners.Add(spawner);
            }
        }
    }

    public void onNetworkPeerConnected(int id)
    {
        foreach(EnemySpawner spawner in spawners)
        {
            if (spawner.target != null)
            {
                spawner.RpcId(id, "Spawn");
                Vector2 enemyPosition = spawner.target.GlobalPosition;
                spawner.target.RpcId(id, "clientUpdateEnemyPos", enemyPosition.x, enemyPosition.y);
            }
        }
    }
}
