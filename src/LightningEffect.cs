using Godot;
using System;

public class LightningEffect : Line2D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        float length = 0;
        for (int i = 0; i < Points.Length-1; i++) 
        {
            length += Points[i].DistanceTo(Points[i+1]);
        }

        float scaledTextureWidth = Texture.GetSize().x * (Width / Texture.GetSize().y);
        int numberOfTextureRepeats = (int)Math.Round(length / scaledTextureWidth);
        float newScaledTextureWidth = length / numberOfTextureRepeats;
        Width = Texture.GetSize().y * (newScaledTextureWidth / Texture.GetSize().x);
    }
}
