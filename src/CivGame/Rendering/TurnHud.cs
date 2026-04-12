using Godot;
using CivGame.Core;

namespace CivGame.Rendering;

/// <summary>
/// HUD showing the turn counter and an End Turn button.
/// Anchored to top-right of the screen via the scene layout.
/// </summary>
public partial class TurnHud : Control
{
    private TurnManager? _turnManager;
    private Label? _turnLabel;
    private Button? _endTurnButton;

    public void Initialize(TurnManager turnManager)
    {
        _turnManager = turnManager;

        _turnLabel = GetNode<Label>("TurnLabel");
        _endTurnButton = GetNode<Button>("EndTurnButton");

        _turnLabel.Text = $"Turn: {_turnManager.CurrentTurn}";
        _endTurnButton.Pressed += OnEndTurnPressed;
    }

    public void UpdateTurnDisplay(int turn)
    {
        if (_turnLabel is not null)
        {
            _turnLabel.Text = $"Turn: {turn}";
        }
    }

    private void OnEndTurnPressed()
    {
        _turnManager?.EndTurn();
    }
}
