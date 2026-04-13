using CivGame.Buildings;
using CivGame.Cities;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Tests.Core;

/// <summary>
/// Tests for TurnManager and GameSession changes introduced by issue #71:
/// - TurnManager gains a HexGrid parameter so yield-driven production applies.
/// - GameSession(16,16).Turns.EndTurn() does not throw.
/// </summary>
public class TurnManagerYieldTests
{
    // ------------------------------------------------------------------ //
    // Helpers                                                             //
    // ------------------------------------------------------------------ //

    private static HexGrid CreateAllForestGrid(int width = 10, int height = 10)
    {
        var grid = new HexGrid(width, height);
        foreach (var cell in grid.AllCells())
        {
            cell.Terrain = TerrainType.Forest;
        }

        return grid;
    }

    private static (TurnManager turns, CityManager cities, HexGrid grid) CreateYieldSetup()
    {
        var grid = CreateAllForestGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        var turns = new TurnManager(units, cities, grid);
        return (turns, cities, grid);
    }

    // ------------------------------------------------------------------ //
    // TurnManager(UnitManager, CityManager, HexGrid) constructor          //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_CreateTurnManager_When_HexGridParameterProvided()
    {
        var grid = new HexGrid(10, 10);
        var units = new UnitManager();
        var cities = new CityManager();

        var turns = new TurnManager(units, cities, grid);

        Assert.NotNull(turns);
        Assert.Equal(1, turns.CurrentTurn);
    }

    [Fact]
    public void Should_CreateTurnManagerWithPlayerOrder_When_HexGridAndPlayerOrderProvided()
    {
        var grid = new HexGrid(10, 10);
        var units = new UnitManager();
        var cities = new CityManager();

        var turns = new TurnManager(units, cities, grid, new[] { 0, 1 });

        Assert.NotNull(turns);
        Assert.Equal(0, turns.CurrentPlayerId);
    }

    // ------------------------------------------------------------------ //
    // EndTurn applies production yield from city terrain                   //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ApplyYieldDrivenProduction_When_EndTurnCalledOnDefaultGameSession()
    {
        // GameSession(16,16) bootstraps a Capital at Grass center.
        // Center cell of a generated map is Grass (guarantee from MapGenerator).
        // After EndTurn, the capital's build queue should advance by the computed yield.
        var session = new GameSession(16, 16);
        var capital = session.Cities.AllCities[0];
        capital.StartBuilding(BuildingCatalog.Granary);

        int initialRemaining = capital.CurrentProduction!.TurnsRemaining;

        session.Turns.EndTurn();

        // Production yield for a Grass-centered city is >= 1 (at least center tile).
        // So TurnsRemaining should have decreased, or building is already complete.
        bool advanced = capital.CurrentProduction == null ||
                        capital.CurrentProduction.TurnsRemaining < initialRemaining;

        Assert.True(advanced,
            $"Expected production to advance; TurnsRemaining stayed at {capital.CurrentProduction?.TurnsRemaining}");
    }

    [Fact]
    public void Should_NotThrow_When_EndTurnCalledOnDefaultGameSession16x16()
    {
        var session = new GameSession(16, 16);

        // Must not throw — this is the bootstrap smoke test.
        var ex = Record.Exception(() => session.Turns.EndTurn());

        Assert.Null(ex);
    }

    // ------------------------------------------------------------------ //
    // Explicit yield-driven scenario: all-Forest grid, city at center     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_CompleteGranaryInOneTurn_When_ForestGridYieldExceedsCost()
    {
        // All-Forest 10×10 grid, city at (3,3): yield = 14 production/turn.
        // Granary costs 5; should complete in a single EndTurn call.
        var (turns, cities, grid) = CreateYieldSetup();

        var city = cities.CreateCity("Carthage", new HexCoord(3, 3), grid, ownerId: 0);
        city.StartBuilding(BuildingCatalog.Granary);

        turns.EndTurn();

        Assert.Null(city.CurrentProduction);
        Assert.Single(city.CompletedBuildings);
        Assert.Equal("Granary", city.CompletedBuildings[0].Name);
    }

    [Fact]
    public void Should_NotAdvanceNonCurrentPlayerCities_When_EndTurnCalledForPlayer0()
    {
        var (turns, cities, grid) = CreateYieldSetup();

        var player0City = cities.CreateCity("Rome", new HexCoord(2, 2), grid, ownerId: 0);
        var player1City = cities.CreateCity("Athens", new HexCoord(7, 7), grid, ownerId: 1);

        player0City.StartBuilding(BuildingCatalog.Granary);
        player1City.StartBuilding(BuildingCatalog.Granary);

        int player1InitialRemaining = player1City.CurrentProduction!.TurnsRemaining;

        turns.EndTurn(); // player 0's turn ends

        // Player-0 city completed (yield 14 >= cost 5).
        Assert.Null(player0City.CurrentProduction);

        // Player-1 city untouched.
        Assert.Equal(player1InitialRemaining, player1City.CurrentProduction!.TurnsRemaining);
    }
}
