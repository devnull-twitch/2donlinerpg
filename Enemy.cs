using Godot;
using System;

public class Enemy : KinematicBody2D
{
    private bool isServer;

    private PlayerNetworking currentTarget;

    private Navigation2D nav;

    private Vector2[] currentPath;

    private Area2D visionArea;

    private Area2D atkArea;

    private Area2D leaveArea;

    private int health = 10;

    [Signal]
    delegate void Killed();

    public override void _Ready()
    {
        isServer = isServer = GetTree().IsNetworkServer();

        if(!isServer)
        {
            return;
        }

        Node sceneRootNode = GetTree().Root.GetChild(0);
        nav = sceneRootNode.GetNode<Navigation2D>("Nav");

        visionArea = GetNode<Area2D>("Vision");
        visionArea.Connect("body_entered", this, nameof(OnPlayerEnteredVision));

        atkArea = GetNode<Area2D>("Attack");
        atkArea.Connect("body_entered", this, nameof(OnPlayerEnteredAttack));

        leaveArea = GetNode<Area2D>("TargetMove");
    }

    public void OnPlayerEnteredVision(Node2D body)
    {
        if (!(body is PlayerNetworking))
        {
            return;
        }
        PlayerNetworking p = (PlayerNetworking)body;

        if (currentTarget != null)
        {
            return;
        }

        currentTarget = p;

        leaveArea.GlobalPosition = new Vector2(p.GlobalPosition.x, p.GlobalPosition.y);

        Vector2[] fullPath = nav.GetSimplePath(Position, p.Position);
        currentPath = shiftArray(fullPath);
    }

    public void OnPlayerEnteredAttack(Node2D body)
    {
        if (!(body is PlayerNetworking))
        {
            return;
        }

        currentTarget = null;
        currentPath = null;
    }

    public void OnTargetMove(Node2D body)
    {
        if(currentTarget != null && body.Name == currentTarget.Name)
        {
            leaveArea.GlobalPosition = new Vector2(currentTarget.GlobalPosition.x, currentTarget.GlobalPosition.y);

            Vector2[] fullPath = nav.GetSimplePath(GlobalPosition, currentTarget.GlobalPosition);
            currentPath = shiftArray(fullPath);
        }
    }

    public override void _Process(float delta)
    {
        if(currentTarget != null)
        {
            if(currentTarget.IsQueuedForDeletion())
            {
                currentTarget = null;
                currentPath = null;
            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!isServer)
        {
            return;
        }

        if (health <= 0)
        {
            Rpc("allRemoveEnemy");
            return;
        }

        if(currentTarget != null)
        {
            if(!leaveArea.OverlapsBody(currentTarget))
            {
                OnTargetMove(currentTarget);
            }
        }

        if(currentPath != null && currentPath.Length > 0)
        {
            float totalDistance = GlobalPosition.DistanceTo(currentPath[0]);
            float stepDistance = delta * 100;
            if(totalDistance < 20)
            {
                if(currentPath.Length > 1)
                {
                    currentPath = shiftArray(currentPath);
                    return;
                }
                else
                {
                    // end of path
                    currentPath = null;
                    return;
                }
            }

            Vector2 stepVector = GlobalPosition.LinearInterpolate(currentPath[0], stepDistance / totalDistance);
            MoveAndCollide(stepVector - GlobalPosition, false);
            Rpc("clientUpdateEnemyPos", GlobalPosition.x, GlobalPosition.y);
        }
    }

    private Vector2[] shiftArray(Vector2[] src)
    {
        if (src.Length < 1)
        {
            return src;
        }

        Vector2[] target = new Vector2[src.Length - 1];
        Array.Copy(src, 1, target, 0, src.Length - 1);

        return target;
    }

    public void SubHealth(int dmg)
    {
        health -= dmg;
        GD.Print($"new enemy health {health}");
        if (health < 0)
        {
            health = 0;
        }

        Rpc("SetHealth", health);
    }

    [Remote]
    public void SetHealth(int newHealth)
    {
        health = newHealth;
    }

    [Remote]
    public void clientUpdateEnemyPos(float x, float y)
    {
        this.GlobalPosition = new Vector2(x, y);
    }

    [RemoteSync]
    public void allRemoveEnemy()
    {
        EmitSignal("Killed");
        GD.Print("removed enemy");
        QueueFree();
    }
}
