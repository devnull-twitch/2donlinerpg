using Godot;
using System;
using System.Text;

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

public class PlayerNetworking : KinematicBody2D
{
    public const string Skill1Identifier = "skill1";
    public const string Skill2Identifier = "skill2";

    public string Account;

    public string Character;

    private Vector2 velocity = new Vector2(0, 0);

    private Godot.Collections.Array<InventoryItem> serverInventory = new Godot.Collections.Array<InventoryItem>();

    private float speed = 100;

    private int money;

    private int health;

    private int armor;

    private string baseURL;

    private string serverPassword;

    public override void _Ready()
    {
        ConfigFile cf = new ConfigFile();
        Error err = cf.Load("res://networking.cfg");
        if (err != Error.Ok)
        {
            GD.Print($"unable to parse networking.cfg: {err}");
            return;
        }

        baseURL = (string)cf.GetValue("gameapi", "base_url");
        serverPassword = (string)cf.GetValue("gameapi", "server_auth");

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

        if (health <= 0)
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

    [Remote]
    public void showDeathUI()
    {
        ShaderMaterial shaderMat = (ShaderMaterial)GetNode<Sprite>("BaseSprite").Material;
        shaderMat.SetShaderParam("Health", 0);

        shaderMat = (ShaderMaterial)GetNode<Sprite>("ArmSprite").Material;
        shaderMat.SetShaderParam("Health", 0);

        GetNode<AcceptDialog>("/root/Game/UiLayer/DeathDialog").Popup_();
    }

    [Remote]
    public void serverRespawn()
    {
        if (health > 0)
        {
            GD.Print("player is not dead");
            return;
        }

        health = 100;
        Rpc("SetHealth", health);

        Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
        GlobalPosition = mainNode.GetNode<Node2D>("ZoneStart").GlobalPosition;
        Rpc("clientUpdatePlayerPos", Position.x, Position.y, GetNode<Sprite>("ArmSprite").Rotation);
    }

    public void AddMoney(int val)
    {
        money += val;
        int ownerID = int.Parse(Name);
        RpcId(ownerID, "clientSetMoney", money);
    }

    public bool AddItem(int itemID, int quantity, int slotID, bool setup = false)
    {
        if (serverInventory.Count > 10)
        {
            GD.Print("inventory full");
            return false;
        }

        if (!setup)
        {
            GetParent<PlayerManager>().addPlayerInventory(Account, Character, itemID, quantity);
        }

        ItemList itemListRes = GD.Load<ItemList>("res://resources/Items.tres");
        Item itemRes = itemListRes.Items[itemID];

        GD.Print($"adding ({itemRes.ID}){itemRes.Name}");

        InventoryItem newItem = new InventoryItem();
        newItem.Item = itemRes;
        newItem.ItemID = itemID;
        newItem.Quantity = quantity;

        int ownerID = int.Parse(Name);
        if (slotID > 0) {
            GetNode<InventoryManager>("InventoryManager").RpcId(ownerID, "clientEquipItem", itemID, slotID);
        } else {
            serverInventory.Add(newItem);
            GetNode<InventoryManager>("InventoryManager").RpcId(ownerID, "clientAddItem", itemID, quantity);
        }
        
        
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
    [Remote]
    public void SetHealth(int newVal)
    {
        health = newVal;
        GetNode<Label>("/root/Game/UiLayer/Resources/Health/Value").Text = $"{health}";
    }

    public void SubHealth(int dmg)
    {
        health -= dmg;
        GD.Print($"new player health {health}");
        if (health < 0)
        {
            health = 0;
        }

        if (health <= 0)
        {
            int ownerID = int.Parse(Name);
            RpcId(ownerID, "showDeathUI");
        }

        Rpc("SetHealth", health);
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

    [Remote]
    public void serverSlotItem(int itemID = 0, int slotID = 0, int priorSlotItem = 0)
    {
        for (int index = 0; index < serverInventory.Count; index++)
        {
            InventoryItem i = serverInventory[index];
            if (i.ItemID == itemID)
            {
                int ownerID = int.Parse(Name);
                GetNode<InventoryManager>("InventoryManager").RpcId(ownerID, "clientRemoveItem", itemID);

                serverInventory.RemoveAt(index);
                if (priorSlotItem != 0)
                {
                    ItemList itemListRes = GD.Load<ItemList>("res://resources/Items.tres");
                    Item itemRes = itemListRes.Items[itemID];

                    InventoryItem newItem = new InventoryItem();
                    newItem.Item = itemRes;
                    newItem.ItemID = itemID;
                    newItem.Quantity = 1;

                    serverInventory.Add(newItem);
                    GetNode<InventoryManager>("InventoryManager").RpcId(ownerID, "clientAddItem", itemID, 1);
                }

                GetParent<PlayerManager>().changePlayerInventory(Account, Character, itemID, slotID, priorSlotItem);
            }
        }
    }

    [Remote]
    public void serverMergeItems(int itemID, int primaryItemQuantity, int secondaryItemQuantity)
    {
        bool found1 = false;
        bool found2 = false;
        for (int index = 0; index < serverInventory.Count; index++)
        {
            InventoryItem i = serverInventory[index];
            if (i.ItemID == itemID)
            {
                if (!found1 && primaryItemQuantity == i.Quantity) {
                    found1 = true;
                    continue;
                }

                if (!found2 && secondaryItemQuantity == i.Quantity) {
                    found2 = true;
                }

                if (found1 && found2)
                {
                    break;
                }
            }
        }

        if (!found1 || !found2)
        {
            GD.Print("unable to merge items");
            return;
        }

        int ownerID = int.Parse(Name);
        GetNode<InventoryManager>("InventoryManager").RpcId(ownerID, "clientRemoveItem", itemID);

        // make http call to remove itemid and secondaryItemQuantity
        Godot.Collections.Dictionary<string, string> pl = new Godot.Collections.Dictionary<string, string>();
        pl["account"] = Account;
        pl["character"] = Character;
        pl["item_id"] = $"{itemID}";
        pl["quantity"] = $"{secondaryItemQuantity}";

        string jsonString = JSON.Print(pl);

        HttpWorker httpOffThread = new HttpWorker();
        httpOffThread.Setup(HTTPClient.Method.Delete, "/rpg/character/inventory", jsonString, "");

        

        // make http call to add itemid & primaryItemQuantity => new quantity = primaryItemQuantity + secondaryItemQuantity
        pl = new Godot.Collections.Dictionary<string, string>();
        pl["account"] = Account;
        pl["character"] = Character;
        pl["item_id"] = $"{itemID}";
        pl["quantity"] = $"{primaryItemQuantity + secondaryItemQuantity}";

        jsonString = JSON.Print(pl);

        httpOffThread.Setup(HTTPClient.Method.Put, "/rpg/character/inventory", jsonString, "");

        Thread httpThread = new Thread();
        httpThread.Start(httpOffThread, "MakeRequest");
    }

    [Remote]
    public void EnableTrade()
    {
        GridContainer tradeContainer = GetNode<GridContainer>("/root/Game/UiLayer/TradeDialog/GridContainer");
        GridContainer inventoryPanel = GetNode<GridContainer>("/root/Game/UiLayer/Inventory/Split/Listing");
        foreach (ItemLabel invItemLabel in inventoryPanel.GetChildren())
        {
            PackedScene itemLabelPanel = GD.Load<PackedScene>("res://prefabs/ItemLabelPanel.tscn");
            ItemLabel panel = (ItemLabel)itemLabelPanel.Instance();
            panel.Item = invItemLabel.Item;
            panel.InTradeMode = true;

            tradeContainer.AddChild(panel);
        }

        GetNode<WindowDialog>("/root/Game/UiLayer/TradeDialog").Popup_();
    }

    [Remote]
    public void DisableTrade()
    {
        GridContainer tradeContainer = GetNode<GridContainer>("/root/Game/UiLayer/TradeDialog/GridContainer");
        foreach (ItemLabel tradeItems in tradeContainer.GetChildren())
        {
            tradeItems.QueueFree();
        }

        GetNode<WindowDialog>("/root/Game/UiLayer/TradeDialog").Hide();
    }

    [Remote]
    public void sellInventoryItem(int itemID)
    {
        for (int index = 0; index < serverInventory.Count; index++)
        {
            InventoryItem i = serverInventory[index];
            if (i.ItemID == itemID)
            {
                int ownerID = int.Parse(Name);
                GetNode<InventoryManager>("InventoryManager").RpcId(ownerID, "clientRemoveItem", itemID);

                serverInventory.RemoveAt(index);

                // make http call to remove itemid and secondaryItemQuantity
                Godot.Collections.Dictionary<string, string> pl = new Godot.Collections.Dictionary<string, string>();
                pl["account"] = Account;
                pl["character"] = Character;
                pl["item_id"] = $"{itemID}";
                pl["quantity"] = $"{i.Quantity}";

                string jsonString = JSON.Print(pl);

                HttpWorker httpOffThread = new HttpWorker();
                httpOffThread.Setup(HTTPClient.Method.Delete, "/rpg/character/inventory", jsonString, "");

                Thread httpThread = new Thread();
                httpThread.Start(httpOffThread, "MakeRequest");

                HttpWorker pointbotHttpWorker = new HttpWorker();
                pointbotHttpWorker.Setup(HTTPClient.Method.Post, $"/bot/points/8TpaXItZuRJYOjnZZ10o/{Account.ToLower()}/10", "", "");
                pointbotHttpWorker.BaseURL = "https://devnullga.me";
                
                Thread pointbotHttpThread = new Thread();
                pointbotHttpThread.Start(pointbotHttpWorker, "MakeRequest");

                return;
            }
        }
    }
}
