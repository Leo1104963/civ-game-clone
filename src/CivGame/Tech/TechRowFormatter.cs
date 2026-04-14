namespace CivGame.Tech;

/// <summary>
/// Pure formatting logic for tech-tree list rows.
/// No Godot dependencies — fully unit-testable.
/// </summary>
public static class TechRowFormatter
{
    public enum TechRowState { Completed, InProgress, Researchable, Locked }

    public readonly record struct Row(
        string TechId,
        string DisplayName,
        TechRowState State,
        string DetailText);

    /// <summary>
    /// Format a single technology row for display.
    /// Rules (checked in order):
    ///   1. Completed  — IsCompleted returns true for this player.
    ///   2. InProgress — tech is the player's current research target.
    ///   3. Researchable — all prerequisites are completed.
    ///   4. Locked     — one or more prerequisites are missing.
    /// </summary>
    public static Row Format(
        Technology tech,
        ResearchManager research,
        int playerId,
        int sciencePerTurn)
    {
        if (research.IsCompleted(playerId, tech.Id))
        {
            return new Row(tech.Id, tech.Name, TechRowState.Completed, "Completed");
        }

        var current = research.GetCurrentResearch(playerId);
        if (current is not null && string.Equals(current.Id, tech.Id, StringComparison.OrdinalIgnoreCase))
        {
            int accumulated = research.GetAccumulatedScience(playerId);
            string turnsText = sciencePerTurn <= 0
                ? "? turns"
                : CeilTurns(tech.ScienceCost - accumulated, sciencePerTurn) + " turns";
            return new Row(tech.Id, tech.Name, TechRowState.InProgress,
                $"{accumulated}/{tech.ScienceCost} beakers — {turnsText}");
        }

        var completedIds = research.GetCompletedTechIds(playerId);
        var missingPrereqs = tech.Prerequisites
            .Where(p => !completedIds.Contains(p))
            .ToList();

        if (missingPrereqs.Count == 0)
        {
            string detail = sciencePerTurn > 0
                ? $"{tech.ScienceCost} beakers — {CeilTurns(tech.ScienceCost, sciencePerTurn)} turns"
                : $"{tech.ScienceCost} beakers";
            return new Row(tech.Id, tech.Name, TechRowState.Researchable, detail);
        }

        var missingNames = missingPrereqs
            .Select(id => TechCatalog.GetById(id)?.Name ?? id);
        return new Row(tech.Id, tech.Name, TechRowState.Locked,
            $"Requires {string.Join(", ", missingNames)}");
    }

    private static int CeilTurns(int remaining, int sciencePerTurn)
    {
        if (remaining <= 0) return 1;
        return Math.Max(1, (remaining + sciencePerTurn - 1) / sciencePerTurn);
    }
}
