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

    [Export]
    public LootTable DropTable; 

    public override void _Ready()
    {
        isServer = isServer = GetTree().IsNetworkServer();

        if(!isServer)
        {
            return;
        }

        Node2D sceneRootNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
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
        if (!IsInstanceValid(currentTarget)) 
        {
            currentTarget = null;
            currentPath = null;
        } else {
            if(!currentTarget.IsInsideTree() || currentTarget.IsQueuedForDeletion())
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
            if (DropTable != null)
            {
                Godot.Collections.Array<int> dropItemIDs = DropTable.RollDrops();
                foreach(int itemID in dropItemIDs)
                {
                    string uniqueName = $"{GetRid().ToString()}_{itemID}";
                    Rpc("allSpawnItem", itemID, uniqueName);
                }
            }
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

            Vector2 pathVector = GlobalPosition.LinearInterpolate(currentPath[0], 100 / totalDistance);
            MoveAndSlide(pathVector - GlobalPosition, Vector2.Up, false, 4, 0);
            KinematicCollision2D c = GetLastSlideCollision();
            if (c != null && c.Travel.Length() < 0.08)
            {
                GD.Print("unstuck enemy");
                RandomNumberGenerator randGen = new RandomNumberGenerator();
                Vector2 radomPushAnchor = c.Position + (new Vector2(randGen.Randf(), randGen.Randf()) * 30);
                Vector2 toCol = GlobalPosition.LinearInterpolate(radomPushAnchor, 1);
                MoveAndCollide(toCol.Inverse() * 300);
            }
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

    [RemoteSync]
    public void allSpawnItem(int itemID, string uniqueName)
    {
        ItemList itemListRes = GD.Load<ItemList>("res://resources/Items.tres");
        Item itemRes = itemListRes.Items[itemID];

        PackedScene dropItemScene = GD.Load<PackedScene>("res://prefabs/DropItem.tscn");
        FloorDrop dropItem = (FloorDrop)dropItemScene.Instance();

        Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
        dropItem.GlobalPosition = GlobalPosition;
        dropItem.ItemID = itemID;
        dropItem.Name = uniqueName;
        dropItem.GetNode<Sprite>("Sprite").Texture = itemRes.FloorTexture;

        mainNode.AddChild(dropItem);
        dropItem.NetworkReady();
    }
}
