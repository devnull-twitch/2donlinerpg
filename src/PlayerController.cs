using Godot;
using System;

public class PlayerController : Node
{
    public PlayerNetworking PlayerNode;
    private Vector2 dir = new Vector2();

    private string currentSkill = "";

    public override void _Process(float delta)
    {
        Vector2 newVec = new Vector2();

        if (Input.IsActionPressed("move_right"))
        {
            newVec.x = 1;
        }

        if (Input.IsActionPressed("move_left"))
        {
            newVec.x = -1;
        }

        if (Input.IsActionPressed("move_up"))
        {
            newVec.y = -1;
        }

        if (Input.IsActionPressed("move_down"))
        {
            newVec.y = 1;
        }

        if (Input.IsActionJustReleased("skill_1"))
        {
            if (currentSkill == PlayerNetworking.Skill1Identifier)
            {
                currentSkill = "";
            }
            else
            {
                currentSkill = PlayerNetworking.Skill1Identifier;
            }
        }

        if (Input.IsActionJustReleased("skill_2"))
        {
            if (currentSkill == PlayerNetworking.Skill2Identifier)
            {
                currentSkill = "";
            }
            else
            {
                currentSkill = PlayerNetworking.Skill2Identifier;
            }
        }

        if (Input.IsActionJustPressed("skill_use"))
        {
            if (currentSkill != "")
            {
                Camera2D cam = GetNode<Camera2D>("../Camera");
                Vector2 mp = cam.GetGlobalMousePosition();

                PlayerNode.RpcId(1, "serverUseSkill", currentSkill, mp.x, mp.y);
                currentSkill = "";
            }
        }

        if (Input.IsActionJustPressed("inventory_toggle"))
        {
            WindowDialog inventoryPanel = GetNode<WindowDialog>("/root/Game/UiLayer/Inventory");
            if (inventoryPanel.Visible)
            {
                inventoryPanel.Hide();
            }
            else 
            {
                inventoryPanel.Popup_();
            }
        }

        if (newVec.x != dir.x || newVec.y != dir.y) 
        {
            PlayerNode.RpcId(1, "serverUpdateVector", newVec.x, newVec.y);
            dir = newVec;
        }
    }
}
