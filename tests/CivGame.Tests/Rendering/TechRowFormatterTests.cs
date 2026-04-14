using CivGame.Rendering;
using CivGame.Tech;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Failing tests for TechRowFormatter per issue #101.
/// Pure C# — no Godot dependencies. Tests compile but fail until TechRowFormatter is implemented.
/// </summary>
public class TechRowFormatterTests
{
    private const int PlayerId = 0;

    private static ResearchManager CreateFreshResearch() => new ResearchManager();

    private static void CompleteResearch(ResearchManager research, int playerId, string techId)
    {
        var tech = TechCatalog.GetById(techId)
            ?? throw new InvalidOperationException($"Unknown tech: {techId}");
        bool started = research.StartResearch(playerId, techId);
        if (!started)
            throw new InvalidOperationException(
                $"Could not start research for '{techId}' — check prerequisites or state.");
        research.TickFor(playerId, tech.ScienceCost);
    }

    // ------------------------------------------------------------------ //
    // Completed state                                                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnCompletedState_When_PotteryIsInCompletedIds()
    {
        var research = CreateFreshResearch();
        CompleteResearch(research, PlayerId, "pottery");
        var pottery = TechCatalog.GetById("pottery")!;

        var row = TechRowFormatter.Format(pottery, research, PlayerId, 5);

        Assert.Equal(TechRowFormatter.TechRowState.Completed, row.State);
    }

    [Fact]
    public void Should_ReturnCompletedDetailText_When_PotteryIsInCompletedIds()
    {
        var research = CreateFreshResearch();
        CompleteResearch(research, PlayerId, "pottery");
        var pottery = TechCatalog.GetById("pottery")!;

        var row = TechRowFormatter.Format(pottery, research, PlayerId, 5);

        Assert.Equal("Completed", row.DetailText);
    }

    [Fact]
    public void Should_ReturnCorrectTechIdAndDisplayName_When_PotteryCompleted()
    {
        var research = CreateFreshResearch();
        CompleteResearch(research, PlayerId, "pottery");
        var pottery = TechCatalog.GetById("pottery")!;

        var row = TechRowFormatter.Format(pottery, research, PlayerId, 5);

        Assert.Equal("pottery", row.TechId);
        Assert.Equal("Pottery", row.DisplayName);
    }

    // ------------------------------------------------------------------ //
    // InProgress state — positive sciencePerTurn                          //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnInProgressState_When_WritingIsCurrentResearch()
    {
        var research = CreateFreshResearch();
        research.StartResearch(PlayerId, "writing");
        research.TickFor(PlayerId, 12);
        var writing = TechCatalog.GetById("writing")!;

        var row = TechRowFormatter.Format(writing, research, PlayerId, 5);

        Assert.Equal(TechRowFormatter.TechRowState.InProgress, row.State);
    }

    [Fact]
    public void Should_ReturnCorrectDetailText_When_WritingInProgressWith12AccumulatedAnd5PerTurn()
    {
        // Writing costs 30. Accumulated = 12. sciencePerTurn = 5.
        // turns = ceil((30-12)/5) = ceil(18/5) = ceil(3.6) = 4
        var research = CreateFreshResearch();
        research.StartResearch(PlayerId, "writing");
        research.TickFor(PlayerId, 12);
        var writing = TechCatalog.GetById("writing")!;

        var row = TechRowFormatter.Format(writing, research, PlayerId, 5);

        Assert.Equal("12/30 beakers — 4 turns", row.DetailText);
    }

    // ------------------------------------------------------------------ //
    // InProgress state — zero sciencePerTurn (paused)                    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnPausedDetailText_When_WritingInProgressAndSciencePerTurnIsZero()
    {
        var research = CreateFreshResearch();
        research.StartResearch(PlayerId, "writing");
        research.TickFor(PlayerId, 12);
        var writing = TechCatalog.GetById("writing")!;

        var row = TechRowFormatter.Format(writing, research, PlayerId, 0);

        Assert.Equal("12/30 beakers — paused", row.DetailText);
    }

    [Fact]
    public void Should_ReturnPausedDetailText_When_WritingInProgressAndSciencePerTurnIsNegative()
    {
        var research = CreateFreshResearch();
        research.StartResearch(PlayerId, "writing");
        research.TickFor(PlayerId, 12);
        var writing = TechCatalog.GetById("writing")!;

        var row = TechRowFormatter.Format(writing, research, PlayerId, -1);

        Assert.Equal("12/30 beakers — paused", row.DetailText);
    }

    // ------------------------------------------------------------------ //
    // InProgress state — ceil math edge cases                             //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnThreeTurns_When_PotteryInProgressWith0AccumulatedAnd7PerTurn()
    {
        // Pottery costs 20. Accumulated = 0. sciencePerTurn = 7.
        // turns = ceil(20/7) = 3
        var research = CreateFreshResearch();
        research.StartResearch(PlayerId, "pottery");
        var pottery = TechCatalog.GetById("pottery")!;

        var row = TechRowFormatter.Format(pottery, research, PlayerId, 7);

        Assert.Contains("3 turns", row.DetailText);
    }

    [Fact]
    public void Should_ReturnOneTurn_When_PotteryInProgressWith0AccumulatedAndExactDivisor()
    {
        // Pottery costs 20. Accumulated = 0. sciencePerTurn = 20.
        // turns = ceil(20/20) = 1
        var research = CreateFreshResearch();
        research.StartResearch(PlayerId, "pottery");
        var pottery = TechCatalog.GetById("pottery")!;

        var row = TechRowFormatter.Format(pottery, research, PlayerId, 20);

        Assert.Contains("1 turns", row.DetailText);
    }

    // ------------------------------------------------------------------ //
    // Researchable state                                                   //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnResearchableState_When_PotteryFreshResearchManager()
    {
        var research = CreateFreshResearch();
        var pottery = TechCatalog.GetById("pottery")!;

        var row = TechRowFormatter.Format(pottery, research, PlayerId, 5);

        Assert.Equal(TechRowFormatter.TechRowState.Researchable, row.State);
    }

    [Fact]
    public void Should_ReturnCostDetailText_When_PotteryResearchable()
    {
        var research = CreateFreshResearch();
        var pottery = TechCatalog.GetById("pottery")!;

        var row = TechRowFormatter.Format(pottery, research, PlayerId, 5);

        Assert.Equal("Cost: 20 beakers", row.DetailText);
    }

    [Fact]
    public void Should_ReturnResearchableState_When_CurrencyPrereqCompleted()
    {
        var research = CreateFreshResearch();
        CompleteResearch(research, PlayerId, "bronze-working");
        var currency = TechCatalog.GetById("currency")!;

        var row = TechRowFormatter.Format(currency, research, PlayerId, 5);

        Assert.Equal(TechRowFormatter.TechRowState.Researchable, row.State);
    }

    [Fact]
    public void Should_ReturnCostDetailText_When_CurrencyResearchable()
    {
        var research = CreateFreshResearch();
        CompleteResearch(research, PlayerId, "bronze-working");
        var currency = TechCatalog.GetById("currency")!;

        var row = TechRowFormatter.Format(currency, research, PlayerId, 5);

        Assert.Equal("Cost: 40 beakers", row.DetailText);
    }

    // ------------------------------------------------------------------ //
    // Locked state — single missing prerequisite                          //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnLockedState_When_CurrencyFreshResearchManager()
    {
        var research = CreateFreshResearch();
        var currency = TechCatalog.GetById("currency")!;

        var row = TechRowFormatter.Format(currency, research, PlayerId, 5);

        Assert.Equal(TechRowFormatter.TechRowState.Locked, row.State);
    }

    [Fact]
    public void Should_ReturnLockedRequiresBronzeWorking_When_CurrencyLocked()
    {
        var research = CreateFreshResearch();
        var currency = TechCatalog.GetById("currency")!;

        var row = TechRowFormatter.Format(currency, research, PlayerId, 5);

        Assert.Equal("Locked: Requires Bronze Working", row.DetailText);
    }

    // ------------------------------------------------------------------ //
    // Locked state — multiple missing prerequisites                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnLockedWithBothPrereqs_When_MathematicsFreshResearchManager()
    {
        // Mathematics prereqs in declaration order: ["currency", "masonry"]
        // Both missing → "Locked: Requires Currency, Masonry"
        var research = CreateFreshResearch();
        var mathematics = TechCatalog.GetById("mathematics")!;

        var row = TechRowFormatter.Format(mathematics, research, PlayerId, 5);

        Assert.Equal(TechRowFormatter.TechRowState.Locked, row.State);
        Assert.Equal("Locked: Requires Currency, Masonry", row.DetailText);
    }

    [Fact]
    public void Should_ReturnLockedWithOnlyMissingPrereq_When_MathematicsOnlyCurrencyCompleted()
    {
        // Currency is completed; masonry is still missing.
        // Missing prereqs = ["masonry"] → "Locked: Requires Masonry"
        var research = CreateFreshResearch();
        CompleteResearch(research, PlayerId, "bronze-working");
        CompleteResearch(research, PlayerId, "currency");
        var mathematics = TechCatalog.GetById("mathematics")!;

        var row = TechRowFormatter.Format(mathematics, research, PlayerId, 5);

        Assert.Equal(TechRowFormatter.TechRowState.Locked, row.State);
        Assert.Equal("Locked: Requires Masonry", row.DetailText);
    }

    // ------------------------------------------------------------------ //
    // State exhaustiveness — no exceptions for any valid ResearchManager  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_NotThrow_When_FormatCalledForAllTechsOnFreshResearchManager()
    {
        var research = CreateFreshResearch();

        foreach (var tech in TechCatalog.AllTechs)
        {
            var ex = Record.Exception(() => TechRowFormatter.Format(tech, research, PlayerId, 5));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void Should_ReturnNonNullRow_When_FormatCalledForAllTechsOnFreshResearchManager()
    {
        var research = CreateFreshResearch();

        foreach (var tech in TechCatalog.AllTechs)
        {
            var row = TechRowFormatter.Format(tech, research, PlayerId, 5);
            Assert.NotNull(row.TechId);
            Assert.NotNull(row.DisplayName);
            Assert.NotNull(row.DetailText);
        }
    }
}
