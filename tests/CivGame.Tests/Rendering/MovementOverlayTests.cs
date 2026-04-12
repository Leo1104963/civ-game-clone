using CivGame.Rendering;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for MovementOverlay. Verifies that the class exists with the expected
/// public API (ShowReachable, Clear) and that the reachable-cell data and
/// overlay geometry are correct.
/// </summary>
public class MovementOverlayTests
{
    private const float DefaultHexSize = 40f;

    private static (HexGrid Grid, UnitManager Manager) CreateDefaultSetup()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();
        return (grid, manager);
    }

    // --- MovementOverlay class existence and API ---

    [Fact]
    public void Should_Exist_When_Referenced()
    {
        var type = typeof(MovementOverlay);
        Assert.NotNull(type);
    }

    [Fact]
    public void Should_HaveShowReachableMethod_When_Called()
    {
        var overlay = new MovementOverlay();
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        var reachable = manager.GetReachableCells(unit, grid);

        overlay.ShowReachable(reachable, grid, DefaultHexSize);
    }

    [Fact]
    public void Should_HaveClearMethod_When_CalledWithoutPriorShow()
    {
        var overlay = new MovementOverlay();
        overlay.Clear();
    }

    [Fact]
    public void Should_HaveClearMethod_When_CalledAfterShow()
    {
        var overlay = new MovementOverlay();
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        var reachable = manager.GetReachableCells(unit, grid);

        overlay.ShowReachable(reachable, grid, DefaultHexSize);
        overlay.Clear();
    }

    // --- Reachable cells data ---

    [Fact]
    public void Should_ContainCurrentPosition_When_ShowingReachableCells()
    {
        var (grid, manager) = CreateDefaultSetup();
        var origin = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", origin, grid);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.Contains(origin, reachable);
    }

    [Fact]
    public void Should_ContainAllAdjacentCells_When_UnitHasFullMovement()
    {
        var (grid, manager) = CreateDefaultSetup();
        var origin = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", origin, grid);

        var reachable = manager.GetReachableCells(unit, grid);

        foreach (var neighbor in origin.Neighbors())
        {
            if (grid.InBounds(neighbor))
            {
                Assert.Contains(neighbor, reachable);
            }
        }
    }

    [Fact]
    public void Should_ContainTwoStepCells_When_WarriorHasFullMovement()
    {
        var (grid, manager) = CreateDefaultSetup();
        var origin = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", origin, grid);

        var reachable = manager.GetReachableCells(unit, grid);

        var twoEast = new HexCoord(4, 2);
        Assert.Contains(twoEast, reachable);
    }

    [Fact]
    public void Should_ExcludeOccupiedCells_When_ShowingReachableCells()
    {
        var (grid, manager) = CreateDefaultSetup();
        var origin = new HexCoord(2, 2);
        manager.CreateUnit("Warrior", origin, grid);

        var blockedCoord = new HexCoord(3, 2);
        manager.CreateUnit("Warrior", blockedCoord, grid);

        var reachable = manager.GetReachableCells(manager.GetUnitAt(origin)!, grid);

        Assert.DoesNotContain(blockedCoord, reachable);
    }

    [Fact]
    public void Should_ShrinkReachable_When_UnitPartiallyMoved()
    {
        var (grid, manager) = CreateDefaultSetup();
        var origin = new HexCoord(2, 2);
        var unit = manager.CreateUnit("Warrior", origin, grid);

        var fullReachable = manager.GetReachableCells(unit, grid);

        unit.TryMoveTo(new HexCoord(3, 2), grid, manager);
        Assert.True(unit.CanMove);

        var partialReachable = manager.GetReachableCells(unit, grid);

        Assert.True(partialReachable.Count < fullReachable.Count);
    }

    [Fact]
    public void Should_OnlyContainCurrentPosition_When_MovementExhausted()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        unit.TryMoveTo(new HexCoord(3, 2), grid, manager);
        unit.TryMoveTo(new HexCoord(4, 2), grid, manager);

        Assert.False(unit.CanMove);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.Single(reachable);
        Assert.Contains(new HexCoord(4, 2), reachable);
    }

    // --- Overlay hex geometry ---

    [Fact]
    public void Should_UseInsetHexSize_When_DrawingOverlay()
    {
        float overlaySize = DefaultHexSize * 0.9f;
        Assert.Equal(36f, overlaySize);
    }

    [Fact]
    public void Should_ProduceCorrectOverlayVertices_When_RenderingHighlight()
    {
        float overlaySize = DefaultHexSize * 0.9f;
        var coord = new HexCoord(1, 1);
        var (px, py) = HexGrid.HexToPixel(coord, DefaultHexSize);

        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i;
            float angleRad = angleDeg * MathF.PI / 180f;
            float vx = px + overlaySize * MathF.Cos(angleRad);
            float vy = py + overlaySize * MathF.Sin(angleRad);

            float dx = vx - px;
            float dy = vy - py;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            Assert.Equal(overlaySize, dist, precision: 2);
        }
    }

    [Fact]
    public void Should_DrawOneOverlayHexPerReachableCell_When_Showing()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        var reachable = manager.GetReachableCells(unit, grid);

        Assert.True(reachable.Count > 1);

        foreach (var coord in reachable)
        {
            var (px, py) = HexGrid.HexToPixel(coord, DefaultHexSize);
            Assert.True(float.IsFinite(px));
            Assert.True(float.IsFinite(py));
        }
    }
}
