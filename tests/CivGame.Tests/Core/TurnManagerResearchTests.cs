using CivGame.Buildings;
using CivGame.Cities;
using CivGame.Core;
using CivGame.Tech;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Tests.Core;

/// <summary>
/// Tests for TurnManager + ResearchManager integration introduced by issue #97.
/// Verifies the new TurnManager overload that accepts a ResearchManager and
/// calls TickFor with the city's science yield on each EndTurn.
/// </summary>
public class TurnManagerResearchTests
{
    private const int PlayerId = 0;

    // ------------------------------------------------------------------ //
    // Helper                                                               //
    // ------------------------------------------------------------------ //

    private static (TurnManager turns, CityManager cities, ResearchManager research, HexGrid grid)
        CreateSetup()
    {
        var grid = new HexGrid(10, 10);
        // Set all cells to Plains so cities produce Science: 1 per tile.
        foreach (var cell in grid.AllCells())
            cell.Terrain = TerrainType.Plains;

        var units = new UnitManager();
        var cities = new CityManager();
        var research = new ResearchManager();
        var turns = new TurnManager(units, cities, grid, research, new[] { PlayerId });
        return (turns, cities, research, grid);
    }

    // ------------------------------------------------------------------ //
    // Constructor: ResearchManager overload compiles and constructs        //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_CreateTurnManager_When_ResearchManagerProvided()
    {
        var (turns, _, _, _) = CreateSetup();

        Assert.NotNull(turns);
        Assert.Equal(1, turns.CurrentTurn);
    }

    // ------------------------------------------------------------------ //
    // EndTurn ticks research science from city yields                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_IncreaseAccumulatedScience_When_EndTurnCalledAndCitiesProduceScience()
    {
        var (turns, cities, research, grid) = CreateSetup();
        // Create a Plains city — YieldCalculator will return Science > 0.
        cities.CreateCity("Rome", new HexCoord(3, 3), grid, ownerId: PlayerId);

        research.StartResearch(PlayerId, "pottery");

        turns.EndTurn();

        Assert.True(research.GetAccumulatedScience(PlayerId) > 0,
            "AccumulatedScience should be > 0 after EndTurn when cities produce science.");
    }

    [Fact]
    public void Should_NotTickResearchForOtherPlayers_When_EndTurnCalledForOnePlayer()
    {
        var grid = new HexGrid(10, 10);
        foreach (var cell in grid.AllCells())
            cell.Terrain = TerrainType.Plains;

        var units = new UnitManager();
        var cities = new CityManager();
        var research = new ResearchManager();
        // Two-player order: 0 then 1.
        var turns = new TurnManager(units, cities, grid, research, new[] { 0, 1 });

        cities.CreateCity("Rome", new HexCoord(3, 3), grid, ownerId: 0);
        research.StartResearch(0, "pottery");
        research.StartResearch(1, "pottery");

        // EndTurn once — advances player 0's turn only.
        turns.EndTurn();

        int player0Science = research.GetAccumulatedScience(0);
        int player1Science = research.GetAccumulatedScience(1);

        Assert.True(player0Science > 0, "Player 0 should have gained science.");
        Assert.Equal(0, player1Science);
    }

    // ------------------------------------------------------------------ //
    // Constructor null check: ResearchManager is required                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ThrowArgumentNullException_When_ResearchManagerIsNull()
    {
        var grid = new HexGrid(10, 10);
        var units = new UnitManager();
        var cities = new CityManager();

        Assert.Throws<ArgumentNullException>(
            () => new TurnManager(units, cities, grid, (ResearchManager)null!, new[] { 0 }));
    }
}
