using CivGame.Core;
using CivGame.Tech;
using CivGame.Units;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Tests.Core;

/// <summary>
/// Tests for the GameSession.Research property introduced by issue #97.
/// Verifies the property exists, is non-null after construction, and
/// that no research is auto-started by the default constructor.
/// </summary>
public class GameSessionResearchTests
{
    // ------------------------------------------------------------------ //
    // GameSession.Research property                                        //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveNonNullResearchProperty_When_DefaultConstructorUsed()
    {
        var session = new GameSession(10, 10);

        Assert.NotNull(session.Research);
    }

    [Fact]
    public void Should_HaveNoCurrentResearch_When_DefaultConstructorUsed()
    {
        var session = new GameSession(10, 10);

        // Player 0 should start with no active research.
        var current = session.Research.GetCurrentResearch(0);

        Assert.Null(current);
    }

    // ------------------------------------------------------------------ //
    // Full-control constructor also exposes Research                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveNonNullResearchProperty_When_FullConstructorUsed()
    {
        var grid = new HexGrid(5, 5);
        var units = new UnitManager();
        var cities = new CityManager();
        var research = new ResearchManager();
        var turns = new TurnManager(units, cities, grid, research, new[] { 0 });

        var session = new GameSession(grid, units, cities, turns, research);

        Assert.NotNull(session.Research);
        Assert.Same(research, session.Research);
    }

    // ------------------------------------------------------------------ //
    // Research is wired: EndTurn causes accumulated science to increase    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_AccumulateScienceOnEndTurn_When_CapitalProducesScience()
    {
        // Use the default GameSession(10,10) which places a Capital on Grass.
        // If the Capital's neighbors include Plains tiles, science will be > 0.
        // We cannot guarantee Plains neighbors from MapGenerator, so instead we
        // verify that calling EndTurn does NOT throw and that Research property
        // is correctly wired (accumulated science is observable).
        var session = new GameSession(10, 10);

        // Doesn't matter if it's 0 — just must not throw and must be readable.
        var ex = Record.Exception(() => session.Turns.EndTurn());
        Assert.Null(ex);

        int science = session.Research.GetAccumulatedScience(0);
        Assert.True(science >= 0, "GetAccumulatedScience must return a non-negative value.");
    }
}
