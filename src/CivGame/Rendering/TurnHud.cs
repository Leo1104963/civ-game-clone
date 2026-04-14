using Godot;
using CivGame.Core;
using CivGame.Tech;

namespace CivGame.Rendering;

/// <summary>
/// HUD showing the turn counter, an End Turn button, science readout, and Research button.
/// Anchored to top-right of the screen via the scene layout.
/// </summary>
public partial class TurnHud : Control
{
    [Signal] public delegate void ResearchPressedEventHandler();

    private TurnManager? _turnManager;
    private Label? _turnLabel;
    private Button? _endTurnButton;

    // Research extension fields (populated by the research overload of Initialize)
    private ResearchManager? _research;
    private Func<int>? _sciencePerTurnSource;
    private int _playerId;
    private Label? _scienceLabel;
    private Button? _researchButton;

    /// <summary>
    /// Existing single-arg initialize — back-compat, no research wiring.
    /// </summary>
    public void Initialize(TurnManager turnManager)
    {
        _turnManager = turnManager;

        _turnLabel = GetNode<Label>("TurnLabel");
        _endTurnButton = GetNode<Button>("EndTurnButton");

        _turnLabel.Text = $"Turn: {_turnManager.CurrentTurn}";
        _endTurnButton.Pressed += OnEndTurnPressed;
    }

    /// <summary>
    /// Extended initialize that also wires science readout and Research button.
    /// </summary>
    public void Initialize(
        TurnManager turnManager,
        ResearchManager research,
        Func<int> sciencePerTurnSource,
        int playerId)
    {
        _research = research ?? throw new ArgumentNullException(nameof(research));
        _sciencePerTurnSource = sciencePerTurnSource ?? throw new ArgumentNullException(nameof(sciencePerTurnSource));
        _playerId = playerId;

        Initialize(turnManager);

        _scienceLabel = GetNode<Label>("ScienceLabel");
        _researchButton = GetNode<Button>("ResearchButton");
        _researchButton.Pressed += OnResearchPressed;

        RefreshScience();
    }

    public void UpdateTurnDisplay(int turn)
    {
        if (_turnLabel is not null)
        {
            _turnLabel.Text = $"Turn: {turn}";
        }
    }

    /// <summary>
    /// Updates the science label with current research state and science/turn.
    /// Safe to call when research wiring is not active (no-op).
    /// </summary>
    public void RefreshScience()
    {
        if (_scienceLabel is null || _research is null || _sciencePerTurnSource is null) return;

        int sciencePerTurn = _sciencePerTurnSource();
        var current = _research.GetCurrentResearch(_playerId);
        string researchText = current is not null ? current.Name : "None";
        _scienceLabel.Text = $"Science: {sciencePerTurn}/turn | Researching: {researchText}";
    }

    private void OnEndTurnPressed()
    {
        _turnManager?.EndTurn();
    }

    private void OnResearchPressed()
    {
        EmitSignal(SignalName.ResearchPressed);
    }
}
