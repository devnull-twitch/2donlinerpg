using Godot;
using System;

public class EffectManager : Node
{
    [Export]
    public Resource EffectSkill1;

    [Export]
    public Resource EffectSkill2;

    [Remote]
    public void AddEffect(string name, float startX, float startY, float endX, float endY)
    {
        Node2D effectNode = null;

        PackedScene ps;
        switch (name)
        {
            case PlayerNetworking.Skill1Identifier:
                ps = (PackedScene)EffectSkill1;
                effectNode = (Node2D)ps.Instance();
                effectNode.GlobalPosition = new Vector2(startX, startY);
                break;

            case PlayerNetworking.Skill2Identifier:
                ps = (PackedScene)EffectSkill2;
                effectNode = (Node2D)ps.Instance();
                effectNode.GlobalPosition = new Vector2(endX, endY);
                break;
                
        }

        if (effectNode != null)
        {
            if (effectNode is Line2D)
            {
                Line2D lineEffect = (Line2D)effectNode;
                lineEffect.ClearPoints();
                lineEffect.AddPoint(new Vector2(0, 0));
                lineEffect.AddPoint(new Vector2(endX - startX, endY - startY));
            }

            Godot.Collections.Array timerParams = new Godot.Collections.Array();
            timerParams.Add(effectNode.Name);

            SceneTreeTimer removeEffectTimer = GetTree().CreateTimer(1);
            removeEffectTimer.Connect("timeout", this, nameof(removeEffect), timerParams);

            AddChild(effectNode);
        }
    }

    public void removeEffect(string effectNodeName)
    {
        Node2D effectNode = GetNodeOrNull<Node2D>(effectNodeName);
        if (effectNode != null)
        {
            effectNode.Free();
        }
    }
}
