using CivGame.Units;
using CivGame.World;

namespace CivGame.Units.Tests;

public class UnitManagerTests
{
    private static (HexGrid Grid, UnitManager Manager) CreateDefaultSetup()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();
        return (grid, manager);
    }

    // --- CreateUnit ---

    [Fact]
    public void Should_CreateUnit_When_PositionIsValid()
    {
        var (grid, manager) = CreateDefaultSetup();
        var pos = new HexCoord(2, 2);

        var unit = manager.CreateUnit("Warrior", pos, grid);

        Assert.NotNull(unit);
        Assert.Equal("Warrior", unit.UnitType);
        Assert.Equal(pos, unit.Position);
    }

    [Fact]
    public void Should_AddUnitToAllUnits_When_Created()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        Assert.Contains(unit, manager.AllUnits);
        Assert.Single(manager.AllUnits);
    }

    [Fact]
    public void Should_ThrowArgumentException_When_PositionOutOfBounds()
    {
        var (grid, manager) = CreateDefaultSetup();

        Assert.Throws<ArgumentException>(() =>
            manager.CreateUnit("Warrior", new HexCoord(-1, -1), grid));
    }

    [Fact]
    public void Should_ThrowArgumentException_When_PositionImpassable()
    {
        var grid = new HexGrid(5, 5);
        grid.GetCell(new HexCoord(2, 2))!.IsPassable = false;
        var manager = new UnitManager();

        Assert.Throws<ArgumentException>(() =>
            manager.CreateUnit("Warrior", new HexCoord(2, 2), grid));
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_PositionOccupied()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        Assert.Throws<InvalidOperationException>(() =>
            manager.CreateUnit("Warrior", new HexCoord(2, 2), grid));
    }

    [Fact]
    public void Should_ThrowArgumentException_When_UnknownUnitType()
    {
        var (grid, manager) = CreateDefaultSetup();

        Assert.Throws<ArgumentException>(() =>
            manager.CreateUnit("Dragon", new HexCoord(0, 0), grid));
    }

    // --- GetUnitAt ---

    [Fact]
    public void Should_ReturnUnit_When_UnitExistsAtCoord()
    {
        var (grid, manager) = CreateDefaultSetup();
        var pos = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", pos, grid);

        var found = manager.GetUnitAt(pos);

        Assert.NotNull(found);
        Assert.Equal(unit.Id, found!.Id);
    }

    [Fact]
    public void Should_ReturnNull_When_NoUnitAtCoord()
    {
        var (grid, manager) = CreateDefaultSetup();

        var found = manager.GetUnitAt(new HexCoord(2, 2));

        Assert.Null(found);
    }

    // --- IsOccupied ---

    [Fact]
    public void Should_ReturnTrue_When_CellIsOccupied()
    {
        var (grid, manager) = CreateDefaultSetup();
        var pos = new HexCoord(2, 2);
        manager.CreateUnit("Warrior", pos, grid);

        Assert.True(manager.IsOccupied(pos));
    }

    [Fact]
    public void Should_ReturnFalse_When_CellIsEmpty()
    {
        var (grid, manager) = CreateDefaultSetup();

        Assert.False(manager.IsOccupied(new HexCoord(2, 2)));
    }

    // --- GetReachableCells ---

    [Fact]
    public void Should_IncludeCurrentPosition_When_GettingReachableCells()
    {
        var (grid, manager) = CreateDefaultSetup();
        var pos = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", pos, grid);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.Contains(pos, reachable);
    }

    [Fact]
    public void Should_IncludeAdjacentCells_When_MovementRemaining()
    {
        var (grid, manager) = CreateDefaultSetup();
        var pos = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", pos, grid);

        var reachable = manager.GetReachableCells(unit, grid);

        // All 6 neighbors of (2,2) that are in-bounds should be reachable
        foreach (var neighbor in pos.Neighbors())
        {
            if (grid.InBounds(neighbor))
            {
                Assert.Contains(neighbor, reachable);
            }
        }
    }

    [Fact]
    public void Should_ExcludeImpassableCells_When_GettingReachableCells()
    {
        var grid = new HexGrid(5, 5);
        var blocker = new HexCoord(3, 2);
        grid.GetCell(blocker)!.IsPassable = false;
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.DoesNotContain(blocker, reachable);
    }

    [Fact]
    public void Should_ExcludeOccupiedCells_When_GettingReachableCells()
    {
        var (grid, manager) = CreateDefaultSetup();
        var blockerPos = new HexCoord(3, 2);
        manager.CreateUnit("Warrior", blockerPos, grid);
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.DoesNotContain(blockerPos, reachable);
    }

    [Fact]
    public void Should_RespectMovementRemaining_When_GettingReachableCells()
    {
        var grid = new HexGrid(10, 10);
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(5, 5), grid);

        var reachable = manager.GetReachableCells(unit, grid);

        // Cells 3 hexes away should not be reachable (Warrior has range 2)
        var farCell = new HexCoord(8, 5);
        Assert.DoesNotContain(farCell, reachable);
    }

    [Fact]
    public void Should_ReturnOnlyCurrentPosition_When_NoMovementRemaining()
    {
        var grid = new HexGrid(10, 10);
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(5, 5), grid);

        // Exhaust movement
        unit.TryMoveTo(new HexCoord(7, 5), grid, manager);
        Assert.Equal(0, unit.MovementRemaining);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.Single(reachable);
        Assert.Contains(unit.Position, reachable);
    }

    [Fact]
    public void Should_UseBfsNotStraightLine_When_CalculatingReachable()
    {
        // If an impassable cell blocks the shortest path, BFS should go around
        // and cells that are only reachable via a longer detour beyond movement
        // range should be excluded.
        var grid = new HexGrid(10, 10);
        var manager = new UnitManager();

        // Block the direct path
        grid.GetCell(new HexCoord(6, 5))!.IsPassable = false;

        var unit = manager.CreateUnit("Warrior", new HexCoord(5, 5), grid);
        var reachable = manager.GetReachableCells(unit, grid);

        // (7,5) is straight-line 2 away, but path through (6,5) is blocked.
        // Detour path is > 2, so it should not be reachable.
        Assert.DoesNotContain(new HexCoord(7, 5), reachable);
    }

    // --- ResetAllMovement ---

    [Fact]
    public void Should_ResetAllUnitsMovement_When_ResetAllMovement()
    {
        var grid = new HexGrid(10, 10);
        var manager = new UnitManager();
        var unit1 = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);
        var unit2 = manager.CreateUnit("Warrior", new HexCoord(5, 5), grid);

        // Use some movement on both
        unit1.TryMoveTo(new HexCoord(1, 0), grid, manager);
        unit2.TryMoveTo(new HexCoord(6, 5), grid, manager);

        manager.ResetAllMovement();

        Assert.Equal(unit1.MovementRange, unit1.MovementRemaining);
        Assert.Equal(unit2.MovementRange, unit2.MovementRemaining);
    }
}
