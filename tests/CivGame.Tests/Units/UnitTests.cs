using CivGame.Units;
using CivGame.World;

namespace CivGame.Units.Tests;

public class UnitTests
{
    /// <summary>
    /// Helper: create a 5x5 all-passable hex grid and a fresh UnitManager.
    /// </summary>
    private static (HexGrid Grid, UnitManager Manager) CreateDefaultSetup()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();
        return (grid, manager);
    }

    // --- Property tests ---

    [Fact]
    public void Should_HaveUniqueId_When_Created()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit1 = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);
        var unit2 = manager.CreateUnit("Warrior", new HexCoord(1, 0), grid);

        Assert.NotEqual(unit1.Id, unit2.Id);
    }

    [Fact]
    public void Should_HaveCorrectUnitType_When_Created()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        Assert.Equal("Warrior", unit.UnitType);
    }

    [Fact]
    public void Should_HaveCorrectPosition_When_Created()
    {
        var (grid, manager) = CreateDefaultSetup();
        var pos = new HexCoord(2, 3);
        var unit = manager.CreateUnit("Warrior", pos, grid);

        Assert.Equal(pos, unit.Position);
    }

    [Fact]
    public void Should_HaveMovementRange2_When_Warrior()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        Assert.Equal(2, unit.MovementRange);
    }

    [Fact]
    public void Should_HaveFullMovementRemaining_When_Created()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        Assert.Equal(unit.MovementRange, unit.MovementRemaining);
    }

    [Fact]
    public void Should_ReturnCanMoveTrue_When_MovementRemaining()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        Assert.True(unit.CanMove);
    }

    // --- TryMoveTo success ---

    [Fact]
    public void Should_MoveToAdjacentCell_When_CellIsPassableAndEmpty()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        var target = new HexCoord(3, 2);

        bool result = unit.TryMoveTo(target, grid, manager);

        Assert.True(result);
        Assert.Equal(target, unit.Position);
    }

    [Fact]
    public void Should_DeductOneMovementPoint_When_MovingOneHex()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        int before = unit.MovementRemaining;

        unit.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.Equal(before - 1, unit.MovementRemaining);
    }

    [Fact]
    public void Should_DeductTwoMovementPoints_When_MovingTwoHexes()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        // Move to a cell 2 hexes away (using axial neighbor-of-neighbor)
        // In offset coords for a 5x5 grid, (2,2) neighbors include (3,2),
        // and (3,2) neighbors include (4,2). So (4,2) is distance 2.
        var target = new HexCoord(4, 2);
        bool result = unit.TryMoveTo(target, grid, manager);

        Assert.True(result);
        Assert.Equal(0, unit.MovementRemaining);
    }

    [Fact]
    public void Should_AllowMultipleMoves_When_MovementBudgetRemains()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        // First move: 1 hex
        bool first = unit.TryMoveTo(new HexCoord(3, 2), grid, manager);
        Assert.True(first);
        Assert.Equal(1, unit.MovementRemaining);

        // Second move: 1 hex
        bool second = unit.TryMoveTo(new HexCoord(4, 2), grid, manager);
        Assert.True(second);
        Assert.Equal(0, unit.MovementRemaining);
    }

    // --- TryMoveTo failure cases ---

    [Fact]
    public void Should_ReturnFalse_When_TargetOutOfBounds()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        bool result = unit.TryMoveTo(new HexCoord(-1, -1), grid, manager);

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_TargetIsImpassable()
    {
        var grid = new HexGrid(5, 5);
        grid.GetCell(new HexCoord(3, 2))!.IsPassable = false;
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        bool result = unit.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_TargetIsOccupied()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateUnit("Warrior", new HexCoord(3, 2), grid);
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        bool result = unit.TryMoveTo(new HexCoord(3, 2), grid, manager);

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_NoMovementRemaining()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        // Exhaust movement (2 hexes for Warrior)
        unit.TryMoveTo(new HexCoord(4, 2), grid, manager);
        Assert.Equal(0, unit.MovementRemaining);
        Assert.False(unit.CanMove);

        // Try to move again
        bool result = unit.TryMoveTo(new HexCoord(4, 3), grid, manager);

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_TargetBeyondMovementRange()
    {
        var grid = new HexGrid(10, 10);
        var manager = new UnitManager();
        var unit = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        // 3 hexes away -- beyond Warrior's range of 2
        var target = new HexCoord(3, 0);
        bool result = unit.TryMoveTo(target, grid, manager);

        Assert.False(result);
    }

    [Fact]
    public void Should_NotChangePosition_When_MoveFails()
    {
        var (grid, manager) = CreateDefaultSetup();
        var origin = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", origin, grid);

        unit.TryMoveTo(new HexCoord(-1, -1), grid, manager);

        Assert.Equal(origin, unit.Position);
    }

    [Fact]
    public void Should_NotDeductMovement_When_MoveFails()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        int before = unit.MovementRemaining;

        unit.TryMoveTo(new HexCoord(-1, -1), grid, manager);

        Assert.Equal(before, unit.MovementRemaining);
    }

    [Fact]
    public void Should_UsePathDistance_When_DirectPathBlocked()
    {
        // Set up a scenario where straight-line distance is 1 but BFS path is longer
        // due to an impassable cell blocking the direct route.
        var grid = new HexGrid(10, 10);
        var manager = new UnitManager();

        // Block (3,2) so unit at (2,2) must go around to reach (4,2)
        grid.GetCell(new HexCoord(3, 2))!.IsPassable = false;

        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        // (4,2) is 2 hexes in a straight line, but direct path through (3,2) is blocked.
        // The detour path is longer than 2, so it should fail with movement range 2.
        bool result = unit.TryMoveTo(new HexCoord(4, 2), grid, manager);

        Assert.False(result);
    }

    [Fact]
    public void Should_NotPathThroughOccupiedCells_When_Moving()
    {
        var grid = new HexGrid(10, 10);
        var manager = new UnitManager();

        // Place a blocker between start and target
        manager.CreateUnit("Warrior", new HexCoord(3, 2), grid);

        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        // (4,2) requires pathing through (3,2) which is occupied
        // The detour may exceed movement range of 2
        bool result = unit.TryMoveTo(new HexCoord(4, 2), grid, manager);

        Assert.False(result);
    }

    // --- ResetMovement ---

    [Fact]
    public void Should_RestoreFullMovement_When_ResetMovement()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        // Use some movement
        unit.TryMoveTo(new HexCoord(3, 2), grid, manager);
        Assert.Equal(1, unit.MovementRemaining);

        unit.ResetMovement();

        Assert.Equal(unit.MovementRange, unit.MovementRemaining);
        Assert.True(unit.CanMove);
    }
}
