using CivGame.Units;
using CivGame.World;

namespace CivGame.Tests.Units;

/// <summary>
/// Failing tests for issue #48: weighted terrain movement in Unit and UnitManager.
/// Written by the test-author agent as executable spec before implementation.
/// Covers: Forest cost-2 movement, Water impassability, Settler unit type,
/// and GetReachableCells respecting terrain costs.
/// </summary>
public class TerrainMovementTests
{
    // ------------------------------------------------------------------ //
    // Helpers                                                               //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Build a 5x5 grid where one specific cell has a given terrain type.
    /// All other cells remain Grass.
    /// </summary>
    private static HexGrid GridWithTerrain(HexCoord coord, TerrainType terrain)
    {
        var grid = new HexGrid(5, 5);
        grid.GetCell(coord)!.Terrain = terrain;
        return grid;
    }

    // ------------------------------------------------------------------ //
    // Unit.TryMoveTo — Forest costs 2 movement                             //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_SucceedMovingIntoForest_When_UnitHasTwoMovementRemaining()
    {
        // Warrior starts at (2,2); Forest is at (3,2), cost=2.
        // Warrior has MovementRange=2, so exactly enough to enter Forest.
        var grid = GridWithTerrain(new HexCoord(3, 2), TerrainType.Forest);
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        bool result = unit.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.True(result);
        Assert.Equal(new HexCoord(3, 2), unit.Position);
        Assert.Equal(0, unit.MovementRemaining);
    }

    [Fact]
    public void Should_FailMovingIntoForest_When_UnitHasOnlyOneMovementRemaining()
    {
        // Warrior at (2,2), moves to adjacent Grass (3,2), spending 1 movement.
        // Then tries to enter Forest at (4,2) — costs 2 but only 1 remaining.
        var grid = new HexGrid(5, 5);
        grid.GetCell(new HexCoord(4, 2))!.Terrain = TerrainType.Forest;
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        // First move to (3,2) at cost 1
        bool first = unit.TryMoveTo(new HexCoord(3, 2), grid, manager);
        Assert.True(first);
        Assert.Equal(1, unit.MovementRemaining);

        // Second move into Forest costs 2 — should fail
        bool second = unit.TryMoveTo(new HexCoord(4, 2), grid, manager);
        Assert.False(second);
        Assert.Equal(new HexCoord(3, 2), unit.Position);
        Assert.Equal(1, unit.MovementRemaining);
    }

    [Fact]
    public void Should_DeductTwoMovementPoints_When_MovingDirectlyIntoForest()
    {
        var grid = GridWithTerrain(new HexCoord(3, 2), TerrainType.Forest);
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        unit.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.Equal(0, unit.MovementRemaining);
    }

    // ------------------------------------------------------------------ //
    // Unit.TryMoveTo — Water is impassable                                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_FailMovingIntoWater_When_TargetIsWaterTerrain()
    {
        var grid = GridWithTerrain(new HexCoord(3, 2), TerrainType.Water);
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        bool result = unit.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.False(result);
        Assert.Equal(new HexCoord(2, 2), unit.Position);
    }

    [Fact]
    public void Should_NotDeductMovement_When_TargetIsWater()
    {
        var grid = GridWithTerrain(new HexCoord(3, 2), TerrainType.Water);
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        int before = unit.MovementRemaining;

        unit.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.Equal(before, unit.MovementRemaining);
    }

    // ------------------------------------------------------------------ //
    // Unit.TryMoveTo — Grass/Plains costs 1 (unchanged from v0)           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_DeductOneMovementPoint_When_MovingIntoPlains()
    {
        var grid = GridWithTerrain(new HexCoord(3, 2), TerrainType.Plains);
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        unit.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.Equal(1, unit.MovementRemaining);
    }

    // ------------------------------------------------------------------ //
    // UnitManager.GetReachableCells — terrain costs respected              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ExcludeWaterTiles_When_GettingReachableCells()
    {
        var grid = new HexGrid(5, 5);
        var waterCoord = new HexCoord(3, 2);
        grid.GetCell(waterCoord)!.Terrain = TerrainType.Water;
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.DoesNotContain(waterCoord, reachable);
    }

    [Fact]
    public void Should_ExcludeCellsBeyondWeightedBudget_When_ForestPresent()
    {
        // Warrior at (2,2), movement=2.
        // (3,2) is Forest (cost 2). After entering Forest, no movement left.
        // (4,2) costs 1 more from Forest — total from start would be 3 > budget.
        // So (4,2) must not be reachable.
        var grid = new HexGrid(10, 10);
        grid.GetCell(new HexCoord(3, 2))!.Terrain = TerrainType.Forest;
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        var reachable = manager.GetReachableCells(unit, grid);

        // Forest itself is reachable (cost 2 == budget)
        Assert.Contains(new HexCoord(3, 2), reachable);
        // The cell beyond Forest is NOT reachable (would cost 3 total)
        Assert.DoesNotContain(new HexCoord(4, 2), reachable);
    }

    [Fact]
    public void Should_IncludeForestCell_When_MovementBudgetCoversForestCost()
    {
        // Warrior (movement=2) adjacent to Forest (cost=2): Forest is within budget.
        var grid = new HexGrid(5, 5);
        var forestCoord = new HexCoord(3, 2);
        grid.GetCell(forestCoord)!.Terrain = TerrainType.Forest;
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.Contains(forestCoord, reachable);
    }

    [Fact]
    public void Should_IncludeCurrentPosition_When_GetReachableCellsWithTerrainAware()
    {
        var grid = new HexGrid(5, 5);
        grid.GetCell(new HexCoord(3, 2))!.Terrain = TerrainType.Water;
        var manager = new UnitManager();
        var pos = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", pos, grid);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.Contains(pos, reachable);
    }

    // ------------------------------------------------------------------ //
    // UnitManager.CreateUnit — Settler unit type                           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_CreateSettler_When_UnitTypeIsSettler()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();

        var settler = manager.CreateUnit("Settler", new HexCoord(1, 1), grid);

        Assert.NotNull(settler);
        Assert.Equal("Settler", settler.UnitType);
    }

    [Fact]
    public void Should_HaveMovementRange2_When_SettlerCreated()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();

        var settler = manager.CreateUnit("Settler", new HexCoord(1, 1), grid);

        Assert.Equal(2, settler.MovementRange);
    }

    [Fact]
    public void Should_HaveFullMovementRemaining_When_SettlerCreated()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();

        var settler = manager.CreateUnit("Settler", new HexCoord(1, 1), grid);

        Assert.Equal(settler.MovementRange, settler.MovementRemaining);
    }

    [Fact]
    public void Should_AddSettlerToAllUnits_When_Created()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();

        var settler = manager.CreateUnit("Settler", new HexCoord(1, 1), grid);

        Assert.Contains(settler, manager.AllUnits);
    }

    [Fact]
    public void Should_AllowSettlerToMoveOnGrass_When_MovementBudgetSufficient()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();
        var settler = manager.CreateUnit("Settler", new HexCoord(2, 2), grid);

        bool result = settler.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.True(result);
        Assert.Equal(new HexCoord(3, 2), settler.Position);
    }
}
