using Godot;
using System;

public class Game : Node
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        string[] args = OS.GetCmdlineArgs(); 
        for (int key = 0; key < args.Length; ++key)
        {
            string arg = args[key];
            if (arg == "--world")
            {
                PackedScene ps = (PackedScene)ResourceLoader.Load($"res://worlds/{args[key+1]}.tscn");
                Node sceneNode = ps.Instance();
                GetNode<Node2D>("/root/Game/World").AddChild(sceneNode);
                GD.Print($"loaded world {args[key+1]}");
            }
        }
    }
}
