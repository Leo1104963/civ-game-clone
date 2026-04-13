using CivGame.Buildings;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Cities.Tests;

/// <summary>
/// Tests for the yield-aware CityManager.TickAllProduction(HexGrid) and
/// CityManager.TickProductionFor(int, HexGrid) overloads introduced by issue #71.
/// </summary>
public class CityManagerYieldTests
{
    /// <summary>
    /// All-Grass grid: each city gets YieldCalculator.Calculate result.
    /// A city at center of a 10×10 Grass grid has 7 tiles × Prod:0 = 0,
    /// so we use Forest terrain to get non-zero production (7 × Prod:2 = 14).
    /// </summary>
    private static HexGrid CreateAllForestGrid(int width = 10, int height = 10)
    {
        var grid = new HexGrid(width, height);
        foreach (var cell in grid.AllCells())
        {
            cell.Terrain = TerrainType.Forest;
        }

        return grid;
    }

    // ------------------------------------------------------------------ //
    // TickAllProduction(grid) applies correct per-city production          //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_AdvanceProductionByYield_When_TickAllProductionCalledWithForestGrid()
    {
        var manager = new CityManager();
        var grid = CreateAllForestGrid();

        // City at (3,3) in a 10×10 all-Forest grid: 7 tiles × 2 = 14 production.
        var city = manager.CreateCity("Rome", new HexCoord(3, 3), grid);
        city.StartBuilding(BuildingCatalog.Granary); // cost 5

        manager.TickAllProduction(grid);

        // After 1 tick with 14 production, Granary (cost 5) is complete.
        Assert.Null(city.CurrentProduction);
        Assert.Single(city.CompletedBuildings);
    }

    [Fact]
    public void Should_AdvanceAllCities_When_TickAllProductionCalledWithMultipleCities()
    {
        var manager = new CityManager();
        var grid = CreateAllForestGrid();

        var rome = manager.CreateCity("Rome", new HexCoord(2, 2), grid);
        var athens = manager.CreateCity("Athens", new HexCoord(7, 7), grid);

        rome.StartBuilding(BuildingCatalog.Granary);
        athens.StartBuilding(BuildingCatalog.Granary);

        manager.TickAllProduction(grid);

        // Both complete in one tick (production >= 5).
        Assert.Null(rome.CurrentProduction);
        Assert.Null(athens.CurrentProduction);
        Assert.Single(rome.CompletedBuildings);
        Assert.Single(athens.CompletedBuildings);
    }

    // ------------------------------------------------------------------ //
    // TickProductionFor(int, grid) applies only to player-0 cities         //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ApplyProductionOnlyToPlayer0Cities_When_TickProductionForPlayer0()
    {
        var manager = new CityManager();
        var grid = CreateAllForestGrid();

        var player0City = manager.CreateCity("Rome", new HexCoord(2, 2), grid, ownerId: 0);
        var player1City = manager.CreateCity("Athens", new HexCoord(7, 7), grid, ownerId: 1);

        player0City.StartBuilding(BuildingCatalog.Granary);
        player1City.StartBuilding(BuildingCatalog.Granary);

        int player1InitialRemaining = player1City.CurrentProduction!.TurnsRemaining;

        manager.TickProductionFor(0, grid);

        // Player-0 city completes (production 14 >= cost 5).
        Assert.Null(player0City.CurrentProduction);

        // Player-1 city is unchanged.
        Assert.NotNull(player1City.CurrentProduction);
        Assert.Equal(player1InitialRemaining, player1City.CurrentProduction!.TurnsRemaining);
    }

    [Fact]
    public void Should_NotAdvancePlayer1Cities_When_TickProductionForPlayer0()
    {
        var manager = new CityManager();
        var grid = CreateAllForestGrid();

        var player1City = manager.CreateCity("Sparta", new HexCoord(5, 5), grid, ownerId: 1);
        player1City.StartBuilding(BuildingCatalog.Granary);

        int initialRemaining = player1City.CurrentProduction!.TurnsRemaining;

        manager.TickProductionFor(0, grid);

        Assert.Equal(initialRemaining, player1City.CurrentProduction!.TurnsRemaining);
    }
}
