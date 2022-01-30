using Godot;
using System;

public class LineProcess : SkillProcess 
{
    public Godot.Collections.Array Execute(PlayerSkills skill, PlayerNetworking player, Vector2 targetPos)
    {
        float dis = player.GlobalPosition.DistanceTo(targetPos);
        float interpolationWeight = skill.MaxRange / dis;
        Vector2 pointAtMaxRange = player.GlobalPosition.LinearInterpolate(targetPos, interpolationWeight);

        Physics2DDirectSpaceState spaceState = player.GetWorld2d().DirectSpaceState;
        Godot.Collections.Dictionary result = spaceState.IntersectRay(
            player.GlobalPosition,
            pointAtMaxRange,
            new Godot.Collections.Array { player },
            player.CollisionMask
        );

        Godot.Collections.Array enemies = new Godot.Collections.Array();

        if (result.Count > 0)
        {
            Godot.Object other = (Godot.Object)result["collider"];
            if (other is Enemy)
            {
                enemies.Add(other);
            }
        }

        return enemies;
    }
}

public class AreaOfEffect : SkillProcess 
{
    public Godot.Collections.Array Execute(PlayerSkills skill, PlayerNetworking player, Vector2 targetPos)
    {
        Physics2DDirectSpaceState spaceState = player.GetWorld2d().DirectSpaceState;

        Physics2DShapeQueryParameters query = new Physics2DShapeQueryParameters();
        CircleShape2D queryShape = new CircleShape2D();
        queryShape.Radius = 100;
        query.SetShape(queryShape);
        query.Transform = new Transform2D(new Vector2(1, 0), new Vector2(0, 1), targetPos);
        Godot.Collections.Array allInArea = spaceState.IntersectShape(query);

        Godot.Collections.Array enemies = new Godot.Collections.Array();

        foreach (Godot.Collections.Dictionary result in allInArea)
        {
            Godot.Object other = (Godot.Object)result["collider"];
            if (other is Enemy)
            {
                enemies.Add(other);
            }
        }

        GD.Print($"Enemies in Area {enemies.Count}");
        return enemies;
    }
}

public class InventorySlot 
{
    public int ItemID;

    public int Quantity;

    public InventorySlot(int itemID, int quanitity)
    {
        ItemID = itemID;
        Quantity = quanitity;
    }
}

public class PlayerNetworking : KinematicBody2D
{
    public const string Skill1Identifier = "skill1";
    public const string Skill2Identifier = "skill2";

    private Vector2 velocity = new Vector2(0, 0);

    private Godot.Collections.Array<InventorySlot> serverInventory = new Godot.Collections.Array<InventorySlot>();

    private float speed = 100;

    private int money;

    private int health;

    private int armor;

    public override void _Ready()
    {
        GD.Print($"PN thoughts about beeing a server = {GetTree().IsNetworkServer()}");

        if (!GetTree().IsNetworkServer())
        {
            if(int.Parse(this.Name) == GetTree().GetNetworkUniqueId())
            {
                Camera2D cam = new Camera2D();
                cam.Name = "Camera";
                AddChild(cam);
                cam.MakeCurrent();

                PlayerController controller = new PlayerController();
                controller.PlayerNode = this;
                AddChild(controller);

                createSkills();
            }
        }

        if (GetTree().IsNetworkServer())
        {
            // make nodes for all player skills
            createSkills();
        }
    }

    public void createSkills()
    {
        PackedScene ps = GD.Load<PackedScene>("res://prefabs/Skill.tscn");
        Node p = ps.Instance();
        PlayerSkills skill1 = (PlayerSkills)p;
        skill1.Name = Skill1Identifier;
        skill1.Cooldown = 5;
        skill1.Damage = 10;
        skill1.MaxRange = 500;
        skill1.Processor = new LineProcess();

        Node p2 = ps.Instance();
        PlayerSkills skill2 = (PlayerSkills)p2;
        skill2.Name = Skill2Identifier;
        skill2.Cooldown = 10;
        skill2.Damage = 100;
        skill2.MaxRange = 500;
        skill2.Processor = new AreaOfEffect();

        AddChild(skill1);
        AddChild(skill2);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!GetTree().IsNetworkServer())
        {
            return;
        }

        if (velocity.x != 0 || velocity.y != 0)
        {
            Vector2 multiVeloccity = velocity * speed;
            MoveAndSlide(multiVeloccity);
            Rpc("clientUpdatePlayerPos", Position.x, Position.y, GetNode<Sprite>("ArmSprite").Rotation);
        }
    }

    public void AddMoney(int val)
    {
        money += val;
        int ownerID = int.Parse(Name);
        RpcId(ownerID, "clientSetMoney", money);
    }

    public bool AddItem(int itemID)
    {
        if (serverInventory.Count > 10)
        {
            GD.Print("inventory full");
            return false;
        }

        serverInventory.Add(new InventorySlot(itemID, 1));
        int ownerID = int.Parse(Name);
        GetNode<InventoryManager>("InventoryManager").RpcId(ownerID, "clientAddItem", itemID);
        
        return true;
    }

    public int GetMoney()
    {
        return money;
    }

    public int GetHealth()
    {
        return health;
    }

    // Should only be called on server to set health
    public void SetHealth(int newVal)
    {
        health = newVal;
        int ownerID = int.Parse(Name);
        RpcId(ownerID, "clientSetStats", health, armor);
    }

    public int GetArmor()
    {
        return armor;
    }

    // Should only be called on server to change armor val
    public void AddArmor(int armorVal)
    {
        GD.Print("server adds armor?");
        armor += armorVal;
        int ownerID = int.Parse(Name);
        RpcId(ownerID, "clientSetStats", health, armor);
    }

    [Remote]
    public void clientSetStats(int healthVal, int armorVal)
    {
        health = healthVal;
        GetNode<Label>("/root/Game/UiLayer/Resources/Health/Value").Text = $"{health}";

        armor = armorVal;
        GetNode<Label>("/root/Game/UiLayer/Resources/Armor/Value").Text = $"{armor}";
    }

    [Remote]
    public void clientUpdatePlayerPos(float x, float y, float rot)
    {
        Position = new Vector2(x, y);
        GetNode<Sprite>("ArmSprite").Rotation = rot;
    }

    [Remote]
    public void serverUpdateVector(float x, float y)
    {
        this.velocity = new Vector2(x, y);
    }

    [Remote]
    public void clientSetMoney(int money)
    {
        GetNode<Label>("/root/Game/UiLayer/Resources/Money/Value").Text = $"{money}";
    }

    [Remote]
    public void serverUseSkill(string skillName, float x, float y)
    {
        Vector2 target = new Vector2(x, y);
        Sprite arm = GetNode<Sprite>("ArmSprite");
        arm.LookAt(target);
        arm.Rotation += Godot.Mathf.Pi;
        Rpc("clientUpdatePlayerPos", Position.x, Position.y, arm.Rotation);

        int triggerPlayerID = GetTree().GetRpcSenderId();
        GD.Print($"User {Name} uses {skillName} rotation {GlobalRotation}");

        switch (skillName)
        {
            case PlayerNetworking.Skill1Identifier:
                GetNode<PlayerSkills>(Skill1Identifier).Trigger(this, target);
                break;

            case PlayerNetworking.Skill2Identifier:
                GetNode<PlayerSkills>(Skill2Identifier).Trigger(this, target);
                break;
        }
    }
}
