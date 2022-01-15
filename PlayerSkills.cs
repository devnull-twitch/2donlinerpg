using Godot;
using System;

public class PlayerSkills : Node
{
    public int Cooldown;

    public int Damage;

    public int MaxRange;

    private bool enabled = true;

    public bool Trigger(PlayerNetworking player, Vector2 targetPos)
    {
        if (!enabled)
        {
            GD.Print($"ON COOLDOWN");
            return false;
        }

        float dis = player.GlobalPosition.DistanceTo(targetPos);
        if (dis > MaxRange)
        {
            GD.Print($"OOR MAX={MaxRange} DISTANCE={dis}");
            return false;
        }

        float interpolationWeight = MaxRange / dis;
        Vector2 pointAtMaxRange = player.GlobalPosition.LinearInterpolate(targetPos, interpolationWeight);

        Physics2DDirectSpaceState spaceState = player.GetWorld2d().DirectSpaceState;
        Godot.Collections.Dictionary result = spaceState.IntersectRay(
            player.GlobalPosition,
            pointAtMaxRange,
            new Godot.Collections.Array { player },
            GetParent<PlayerNetworking>().CollisionMask
        );

        if (result.Count > 0)
        {
            Godot.Object other = (Godot.Object)result["collider"];
            if (other is Enemy)
            {
                Enemy targetEnemy = (Enemy)other;
                targetEnemy.SubHealth(Damage);
                Vector2 enemyGP = targetEnemy.GlobalPosition;
                player.Rpc("clientPlayerEffect", Name, enemyGP.x, enemyGP.y);
                enabled = false;
                SceneTreeTimer timer = GetTree().CreateTimer(Cooldown);
                timer.Connect("timeout", this, nameof(cooldownDonw));
                return true;
            }
        }

        return false;
    }

    public void cooldownDonw()
    {
        GD.Print("cooldown reset");
        enabled = true;
    }

    [Remote]
    public void clientCooldownStart()
    {
        GD.Print("cooldown reset");
        enabled = false;
    }

    [Remote]
    public void clientCooldownDone()
    {
        GD.Print("cooldown reset");
        enabled = false;
    }
}