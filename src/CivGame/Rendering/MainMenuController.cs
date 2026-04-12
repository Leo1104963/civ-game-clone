using Godot;

namespace CivGame.Rendering;

/// <summary>
/// Main menu controller. Wires the Play button to transition to the game scene.
/// </summary>
public partial class MainMenuController : Control
{
    public override void _Ready()
    {
        var playButton = GetNode<Button>("CenterContainer/VBoxContainer/PlayButton");
        playButton.Pressed += OnPlayPressed;
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/GameScene.tscn");
    }
}
