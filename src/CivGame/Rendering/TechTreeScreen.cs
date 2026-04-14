using Godot;
using CivGame.Tech;

namespace CivGame.Rendering;

/// <summary>
/// Scrollable tech-tree list screen. Thin Godot wiring — all formatting logic
/// lives in TechRowFormatter. Shows each tech as a labeled row with a
/// "Research" button for researchable techs.
/// </summary>
public partial class TechTreeScreen : Control
{
    [Signal] public delegate void ResearchSelectedEventHandler(string techId);
    [Signal] public delegate void ClosedEventHandler();

    private ResearchManager? _research;
    private TechUnlockService? _unlocks;
    private int _playerId;
    private Func<int>? _sciencePerTurnSource;

    private Label? _currentResearchLabel;
    private Label? _scienceRateLabel;
    private VBoxContainer? _techList;

    /// <summary>
    /// Called once by GameController after scene load.
    /// </summary>
    public void Initialize(
        ResearchManager research,
        TechUnlockService unlocks,
        int playerId,
        Func<int> sciencePerTurnSource)
    {
        _research = research ?? throw new ArgumentNullException(nameof(research));
        _unlocks = unlocks ?? throw new ArgumentNullException(nameof(unlocks));
        _playerId = playerId;
        _sciencePerTurnSource = sciencePerTurnSource ?? throw new ArgumentNullException(nameof(sciencePerTurnSource));

        _currentResearchLabel = GetNode<Label>("CurrentResearchLabel");
        _scienceRateLabel = GetNode<Label>("ScienceRateLabel");
        _techList = GetNode<VBoxContainer>("ScrollContainer/TechList");

        var closeButton = GetNode<Button>("CloseButton");
        closeButton.Pressed += OnClosePressed;

        Visible = false;
    }

    /// <summary>
    /// Rebuilds the list rows. Safe to call repeatedly; idempotent when not visible.
    /// </summary>
    public void Refresh()
    {
        if (_research is null || _techList is null) return;

        int sciencePerTurn = _sciencePerTurnSource?.Invoke() ?? 0;

        // Update header labels
        var current = _research.GetCurrentResearch(_playerId);
        if (_currentResearchLabel is not null)
        {
            _currentResearchLabel.Text = current is not null
                ? $"Researching: {current.Name}"
                : "Researching: None";
        }

        if (_scienceRateLabel is not null)
        {
            _scienceRateLabel.Text = $"Science/turn: {sciencePerTurn}";
        }

        // Rebuild rows
        foreach (var child in _techList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var tech in TechCatalog.AllTechs)
        {
            var row = TechRowFormatter.Format(tech, _research, _playerId, sciencePerTurn);

            var hbox = new HBoxContainer();

            var nameLabel = new Label();
            nameLabel.Text = $"[{row.State}] {row.DisplayName}";
            nameLabel.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill;
            hbox.AddChild(nameLabel);

            var detailLabel = new Label();
            detailLabel.Text = row.DetailText;
            detailLabel.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill;
            hbox.AddChild(detailLabel);

            if (row.State == TechRowFormatter.TechRowState.Researchable)
            {
                var button = new Button();
                button.Text = "Research";
                var techId = tech.Id;
                button.Pressed += () => OnResearchPressed(techId);
                hbox.AddChild(button);
            }

            _techList.AddChild(hbox);
        }
    }

    /// <summary>Sets Visible=true and calls Refresh.</summary>
    public void ShowScreen()
    {
        Visible = true;
        Refresh();
    }

    /// <summary>Sets Visible=false and emits Closed signal.</summary>
    public void HidePanel()
    {
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    private void OnResearchPressed(string techId)
    {
        EmitSignal(SignalName.ResearchSelected, techId);
    }

    private void OnClosePressed()
    {
        HidePanel();
    }
}
