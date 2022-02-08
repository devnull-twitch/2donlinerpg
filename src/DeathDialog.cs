using Godot;
using System;

public class DeathDialog : AcceptDialog
{
    public override void _Ready()
    {
        Connect("confirmed", this, nameof(RequestRespawn));    
    }

    public void RequestRespawn()
    {
        Node2D mainNode = (Node2D)GetNode<Node2D>("/root/Game/World").GetChild(0);
        PlayerNetworking player = mainNode.GetNode<PlayerNetworking>($"NetworkManager/PlayerManager/{GetTree().GetNetworkUniqueId()}");
        player.RpcId(1, "serverRespawn");

        ShaderMaterial shaderMat = (ShaderMaterial)player.GetNode<Sprite>("BaseSprite").Material;
        shaderMat.SetShaderParam("Health", 1);

        shaderMat = (ShaderMaterial)player.GetNode<Sprite>("ArmSprite").Material;
        shaderMat.SetShaderParam("Health", 1);

        Hide();
    }
}
