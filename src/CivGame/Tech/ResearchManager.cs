namespace CivGame.Tech;

/// <summary>
/// Per-player science accumulation and technology research tracker.
/// Push-driven: TurnManager calls TickFor each turn. Plain C# — no Godot types.
/// </summary>
public sealed class ResearchManager
{
    private sealed class PlayerState
    {
        public Technology? CurrentResearch;
        public int AccumulatedScience;
        public readonly HashSet<string> CompletedIds = new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly Dictionary<int, PlayerState> _players = new();

    /// <summary>
    /// Creates a ResearchManager that uses the default <see cref="TechCatalog"/>.
    /// </summary>
    public ResearchManager() { }

    /// <summary>
    /// Creates a ResearchManager pre-loaded with a specific catalog.
    /// The catalog parameter is accepted for future extensibility but the
    /// current implementation always resolves techs via <see cref="TechCatalog.GetById"/>.
    /// </summary>
    public ResearchManager(IReadOnlyList<Technology> catalog) { }

    // ------------------------------------------------------------------ //
    // Event                                                               //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Fires synchronously inside TickFor when a tech completes.
    /// Args: (playerId, completedTech). Fires exactly once per completion.
    /// When handlers run, IsCompleted and GetCurrentResearch already reflect
    /// the post-completion state.
    /// </summary>
    public event Action<int, Technology>? TechUnlocked;

    // ------------------------------------------------------------------ //
    // Helpers                                                             //
    // ------------------------------------------------------------------ //

    private PlayerState GetOrCreate(int playerId)
    {
        if (!_players.TryGetValue(playerId, out var state))
        {
            state = new PlayerState();
            _players[playerId] = state;
        }

        return state;
    }

    // ------------------------------------------------------------------ //
    // Read-only queries                                                   //
    // ------------------------------------------------------------------ //

    public Technology? GetCurrentResearch(int playerId) =>
        _players.TryGetValue(playerId, out var s) ? s.CurrentResearch : null;

    public int GetAccumulatedScience(int playerId) =>
        _players.TryGetValue(playerId, out var s) ? s.AccumulatedScience : 0;

    public IReadOnlyCollection<string> GetCompletedTechIds(int playerId) =>
        _players.TryGetValue(playerId, out var s) ? s.CompletedIds : Array.Empty<string>();

    public bool IsCompleted(int playerId, string techId)
    {
        if (string.IsNullOrEmpty(techId)) return false;
        return _players.TryGetValue(playerId, out var s) && s.CompletedIds.Contains(techId);
    }

    // ------------------------------------------------------------------ //
    // CanResearch                                                         //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns true iff the tech exists in TechCatalog, is not already completed,
    /// is not currently being researched, and all prerequisites are completed.
    /// </summary>
    public bool CanResearch(int playerId, string techId)
    {
        if (string.IsNullOrEmpty(techId)) return false;

        var tech = TechCatalog.GetById(techId);
        if (tech is null) return false;

        var state = _players.TryGetValue(playerId, out var s) ? s : null;

        // Already completed?
        if (state is not null && state.CompletedIds.Contains(tech.Id)) return false;

        // Already current research?
        if (state is not null && state.CurrentResearch is not null &&
            string.Equals(state.CurrentResearch.Id, tech.Id, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // All prerequisites completed?
        foreach (var prereqId in tech.Prerequisites)
        {
            if (state is null || !state.CompletedIds.Contains(prereqId)) return false;
        }

        return true;
    }

    // ------------------------------------------------------------------ //
    // StartResearch                                                       //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Sets the player's current research target.
    /// Returns false (no exception) when CanResearch is false.
    /// Accumulated science is NOT reset on start (carry-over retained).
    /// </summary>
    public bool StartResearch(int playerId, string techId)
    {
        if (!CanResearch(playerId, techId)) return false;

        var tech = TechCatalog.GetById(techId)!;
        var state = GetOrCreate(playerId);
        state.CurrentResearch = tech;
        return true;
    }

    // ------------------------------------------------------------------ //
    // TickFor                                                             //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Push-model: TurnManager calls this per player per turn.
    /// sciencePerTurn &lt;= 0 is a no-op.
    /// At most one tech completes per call; excess science is pooled.
    /// </summary>
    public void TickFor(int playerId, int sciencePerTurn)
    {
        if (sciencePerTurn <= 0) return;

        var state = GetOrCreate(playerId);
        state.AccumulatedScience += sciencePerTurn;

        if (state.CurrentResearch is null) return;

        if (state.AccumulatedScience >= state.CurrentResearch.ScienceCost)
        {
            var completed = state.CurrentResearch;
            state.AccumulatedScience -= completed.ScienceCost;

            // Update state BEFORE firing event (event observers must see final state).
            state.CompletedIds.Add(completed.Id);
            state.CurrentResearch = null;

            TechUnlocked?.Invoke(playerId, completed);
        }
    }
}
