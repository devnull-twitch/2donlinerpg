using Godot;
using System;

public class PlayerNetworking : KinematicBody2D
{
    public const string Skill1Identifier = "skill1";

    private bool isServer;

    private Vector2 velocity = new Vector2(0, 0);

    private float speed = 100;

    private int money;

    private int health;

    private int armor;

    public override void _Ready()
    {
        foreach (string arg in OS.GetCmdlineArgs())
        {
            if (arg == "--server")
            {
                isServer = true;
            }
        }

        if (!isServer)
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

        if (isServer)
        {
            // make nodes for all player skills
            createSkills();
        }
    }

    public void createSkills()
    {
        PackedScene ps = GD.Load<PackedScene>("res://Skill.tscn");
        Node p = ps.Instance();
        PlayerSkills skill1 = (PlayerSkills)p;
        skill1.Name = Skill1Identifier;
        skill1.Cooldown = 5;
        skill1.Damage = 10;
        skill1.MaxRange = 500;

        AddChild(skill1);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!isServer)
        {
            return;
        }

        if (velocity.x != 0 || velocity.y != 0)
        {
            Vector2 multiVeloccity = velocity * speed;
            MoveAndSlide(multiVeloccity);
            Rpc("clientUpdatePlayerPos", Position.x, Position.y, Rotation);
        }
    }

    public void AddMoney(int val)
    {
        money += val;
        int ownerID = int.Parse(Name);
        RpcId(ownerID, "clientSetMoney", money);
    }

    public void AddItem(string itemName)
    {
        int ownerID = int.Parse(Name);
        GetNode<InventoryManager>("InventoryManager").RpcId(ownerID, "clientServerAddItem", itemName);
        // clientServerAddItem has RemoteSync flag but if called with RpcId its not executed locally
        GetNode<InventoryManager>("InventoryManager").clientServerAddItem(itemName);
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
        GetNode<Label>("/root/World/UiLayer/Resources/Health/Value").Text = $"{health}";

        armor = armorVal;
        GetNode<Label>("/root/World/UiLayer/Resources/Armor/Value").Text = $"{armor}";
    }

    [Remote]
    public void clientUpdatePlayerPos(float x, float y, float rot)
    {
        Position = new Vector2(x, y);
        Rotation = rot;
    }

    [Remote]
    public void serverUpdateVector(float x, float y)
    {
        this.velocity = new Vector2(x, y);
    }

    [Remote]
    public void clientSetMoney(int money)
    {
        GetNode<Label>("/root/World/UiLayer/Resources/Money/Value").Text = $"{money}";
    }

    [Remote]
    public void serverUseSkill(string skillName, float x, float y)
    {
        Vector2 target = new Vector2(x, y);
        LookAt(target);
        Rpc("clientUpdatePlayerPos", Position.x, Position.y, Rotation);

        int triggerPlayerID = GetTree().GetRpcSenderId();

        switch (skillName)
        {
            case PlayerNetworking.Skill1Identifier:
                GD.Print($"User {Name} uses {skillName} rotation {GlobalRotation}");
                bool hasHit = GetNode<PlayerSkills>(Skill1Identifier).Trigger(this, target);
                break;
        }
    }

    [Remote]
    public void clientPlayerEffect(string skillName, float x, float y)
    {
        GD.Print($"Player {Name} casted {skillName} at X={x} Y={y}");
    }
}
