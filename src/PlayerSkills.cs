using Godot;
using System;

public interface SkillProcess {
    Godot.Collections.Array Execute(PlayerSkills skill, PlayerNetworking player, Vector2 targetPos);
}

public class PlayerSkills : Node
{
    public int Cooldown;

    public int Damage;

    public int MaxRange;

    public SkillProcess Processor;

    private bool enabled = true;

    private ShaderMaterial shaderMat;

    private float remainingTimeToReady;

    public override void _Ready()
    {
        if (!GetTree().IsNetworkServer())
        {
            TextureRect skillButtonIcon = GetNodeOrNull<TextureRect>($"/root/Game/UiLayer/BottomRow/SkillList/{Name}_button/TextureRect");
            if (skillButtonIcon == null)
            {
                GD.Print($"skill {Name} has no icon?");
                return;
            }
            shaderMat = (ShaderMaterial)skillButtonIcon.Material;
        }
    }

    public override void _Process(float delta)
    {
        if (!GetTree().IsNetworkServer())
        {
            if (remainingTimeToReady > 0)
            {
                remainingTimeToReady -= delta;
                float percentage = (Cooldown - remainingTimeToReady) / Cooldown;
                percentage = Math.Max(0, Math.Min(1, percentage));
                shaderMat.SetShaderParam("ReadyPercentage", percentage);
            }
        }
    }

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

        Godot.Collections.Array enemies = Processor.Execute(this, player, targetPos);

        foreach (Node2D n in enemies)
        {
            if (n is Enemy)
            {
                Enemy targetEnemy = (Enemy)n;
                targetEnemy.SubHealth(Damage);
                Vector2 enemyGP = targetEnemy.GlobalPosition;
                
                EffectManager effectManager = GetNode<EffectManager>("/root/Game/EffectManager");
                effectManager.Rpc("AddEffect", Name, player.GlobalPosition.x, player.GlobalPosition.y, enemyGP.x, enemyGP.y);

                int peerID = int.Parse(GetParent<PlayerNetworking>().Name);
                RpcId(peerID, "clientCooldownStart");
                
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

        int peerID = int.Parse(GetParent<PlayerNetworking>().Name);
        RpcId(peerID, "clientCooldownDone");
    }

    [Remote]
    public void clientCooldownStart()
    {
        GD.Print("cooldown reset");
        shaderMat.SetShaderParam("ReadyPercentage", 0);
        remainingTimeToReady = Cooldown;
    }

    [Remote]
    public void clientCooldownDone()
    {
        GD.Print("cooldown reset");
        remainingTimeToReady = 0;
        shaderMat.SetShaderParam("ReadyPercentage", 1);
    }
}