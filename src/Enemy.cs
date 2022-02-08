using Godot;
using System;

public class Enemy : KinematicBody2D
{
    private bool isServer;

    private PlayerNetworking currentTarget;

    private PlayerNetworking currentAtk;

    private Navigation2D nav;

    private Vector2[] currentPath;

    private Area2D visionArea;

    private Area2D atkArea;

    private Area2D leaveArea;

    public Vector2 InitialPosition;

    private int health = 100;

    [Signal]
    delegate void Killed();

    [Export]
    public LootTable DropTable;

    [Export]
    public bool IsMelee;

    [Export]
    public int BaseDamage;

    [Export]
    public float AtkSpeed;

    private float lastAtk;

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
        atkArea.Connect("body_exited", this, nameof(OnPlayerExitedAttack));

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

        PlayerNetworking enteredPlayer = (PlayerNetworking)body;
        if (enteredPlayer.Name != currentTarget.Name)
        {
            return;
        }

        currentTarget = null;
        currentPath = null;
        currentAtk = enteredPlayer;
    }

    public void OnPlayerExitedAttack(Node2D body)
    {
        if (currentAtk == null || !(body is PlayerNetworking))
        {
            return;
        }

        PlayerNetworking enteredPlayer = (PlayerNetworking)body;
        if (enteredPlayer.Name == currentAtk.Name)
        {
            currentAtk = null;
            currentTarget = enteredPlayer;
            OnTargetMove(currentTarget);
        }
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
        if (currentTarget != null)
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

        if (currentAtk != null)
        {
            if (!IsInstanceValid(currentAtk))
            {
                currentAtk = null;
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

        if (currentAtk != null)
        {
            lastAtk += delta;

            if (lastAtk >= AtkSpeed)
            {
                lastAtk = 0;
                currentAtk.SubHealth(BaseDamage);
            }

            if (currentAtk.GetHealth() <= 0)
            {
                currentAtk = null;
                currentTarget = null;
            }
        }

        // update path to player
        if(currentTarget != null)
        {
            if (currentTarget.GetHealth() <= 0)
            {
                currentAtk = null;
                currentTarget = null;
            }

            if(!leaveArea.OverlapsBody(currentTarget))
            {
                OnTargetMove(currentTarget);
            }
        }

        // basic movement
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
        
        if (currentAtk == null && currentTarget == null && currentPath == null)
        {
            if (InitialPosition.DistanceTo(GlobalPosition) > 5)
            {
                GD.Print($"reset to {InitialPosition} distance {InitialPosition.DistanceTo(GlobalPosition)}");
                Vector2[] fullPath = nav.GetSimplePath(GlobalPosition, InitialPosition);
                currentPath = shiftArray(fullPath);
            }
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

    public void ResetHealth()
    {
        health = 100;
        Rpc("SetHealth", health);
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

        ShaderMaterial shaderMat = (ShaderMaterial)GetNode<ColorRect>("OuterHealthbar/InnerHealthbar").Material;
        shaderMat.SetShaderParam("HealthFactor", (float)health / 100f);
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
