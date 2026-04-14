namespace CivGame.Tech;

/// <summary>
/// Answers unlock-state queries by consulting completed techs in ResearchManager.
/// Tags use the scheme "unit:X" or "building:X". Tags not gated by any catalog tech
/// are considered unlocked by default.
/// Plain C# — no Godot types.
/// </summary>
public sealed class TechUnlockService
{
    private readonly ResearchManager _research;

    // Lazy: built on first call. Maps tag (lower-case) -> first catalog-order tech that unlocks it.
    private Dictionary<string, Technology>? _gatingByTag;

    public TechUnlockService(ResearchManager research)
    {
        _research = research ?? throw new ArgumentNullException(nameof(research));
    }

    // ------------------------------------------------------------------ //
    // Public API                                                          //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// True iff <paramref name="tag"/> is unlocked for <paramref name="playerId"/>.
    /// Tags that are not gated by any catalog tech are considered unlocked by default.
    /// Empty/null tag returns false.
    /// Tag comparison is case-insensitive.
    /// </summary>
    public bool IsUnlocked(int playerId, string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        var gating = GetGatingMap();
        if (!gating.TryGetValue(tag.ToLowerInvariant(), out var gatingTech))
        {
            // Not gated by any tech — unlocked by default.
            return true;
        }

        return _research.IsCompleted(playerId, gatingTech.Id);
    }

    /// <summary>
    /// Returns the human-readable name of the tech that gates <paramref name="tag"/>,
    /// or null if the tag is not gated by any catalog tech.
    /// Empty/null tag returns null.
    /// If multiple techs unlock the same tag, returns the first in catalog order.
    /// </summary>
    public string? GatingTechName(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;

        var gating = GetGatingMap();
        return gating.TryGetValue(tag.ToLowerInvariant(), out var tech) ? tech.Name : null;
    }

    /// <summary>
    /// Returns all unlock tags available to <paramref name="playerId"/> from completed techs.
    /// Only tags from techs that are fully completed are included.
    /// </summary>
    public IReadOnlyCollection<string> GetUnlockedTags(int playerId)
    {
        var completedIds = _research.GetCompletedTechIds(playerId);
        var tags = new List<string>();

        foreach (var tech in TechCatalog.AllTechs)
        {
            if (completedIds.Contains(tech.Id))
            {
                tags.AddRange(tech.Unlocks);
            }
        }

        return tags.AsReadOnly();
    }

    // ------------------------------------------------------------------ //
    // Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private Dictionary<string, Technology> GetGatingMap()
    {
        if (_gatingByTag is not null) return _gatingByTag;

        var map = new Dictionary<string, Technology>(StringComparer.OrdinalIgnoreCase);
        foreach (var tech in TechCatalog.AllTechs)
        {
            foreach (var tag in tech.Unlocks)
            {
                // First catalog-order entry wins for duplicate tags.
                map.TryAdd(tag.ToLowerInvariant(), tech);
            }
        }

        _gatingByTag = map;
        return _gatingByTag;
    }
}
