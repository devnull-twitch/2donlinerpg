using Godot;
using System;

public class DebugPoint : Node2D
{
    public override void _Draw()
    {
        DrawCircle(new Vector2(0, 0), 50, new Color(1, 0, 0));
    }
}