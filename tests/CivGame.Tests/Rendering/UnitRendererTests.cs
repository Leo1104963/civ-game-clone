using CivGame.Rendering;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for UnitRenderer. Verifies that the class exists with the expected
/// public API and that the data model supports rendering all units with
/// correct pixel positions and selection state.
/// </summary>
public class UnitRendererTests
{
    private const float DefaultHexSize = 40f;

    private static (HexGrid Grid, UnitManager Manager) CreateDefaultSetup()
    {
        var grid = new HexGrid(5, 5);
        var manager = new UnitManager();
        return (grid, manager);
    }

    // --- UnitRenderer class existence and API ---

    [Fact]
    public void Should_Exist_When_Referenced()
    {
        // UnitRenderer must exist in the CivGame.Rendering namespace
        var type = typeof(UnitRenderer);
        Assert.NotNull(type);
    }

    [Fact]
    public void Should_HaveSelectedUnitIdProperty_When_Constructed()
    {
        var renderer = new UnitRenderer();
        Assert.Equal(-1, renderer.SelectedUnitId);
    }

    [Fact]
    public void Should_TrackSelectedUnitId_When_Set()
    {
        var renderer = new UnitRenderer();
        renderer.SelectedUnitId = 42;
        Assert.Equal(42, renderer.SelectedUnitId);
    }

    [Fact]
    public void Should_ClearSelection_When_SetToNegativeOne()
    {
        var renderer = new UnitRenderer();
        renderer.SelectedUnitId = 5;
        renderer.SelectedUnitId = -1;
        Assert.Equal(-1, renderer.SelectedUnitId);
    }

    [Fact]
    public void Should_HaveInitializeMethod_When_Called()
    {
        var renderer = new UnitRenderer();
        var (grid, manager) = CreateDefaultSetup();

        // Initialize should accept UnitManager, HexGrid, and hexSize
        renderer.Initialize(manager, grid, DefaultHexSize);
    }

    [Fact]
    public void Should_HaveRefreshMethod_When_Called()
    {
        var renderer = new UnitRenderer();

        // Refresh should exist (triggers QueueRedraw in Godot context)
        renderer.Refresh();
    }

    // --- Data model correctness for rendering ---

    [Fact]
    public void Should_ComputeCorrectCircleCenter_When_UnitAtOrigin()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        var (px, py) = HexGrid.HexToPixel(new HexCoord(0, 0), DefaultHexSize);
        Assert.Equal(0f, px, precision: 2);
        Assert.Equal(0f, py, precision: 2);
    }

    [Fact]
    public void Should_ComputeCorrectCircleCenter_When_UnitAtArbitraryPosition()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 3), grid);

        var (px, py) = HexGrid.HexToPixel(unit.Position, DefaultHexSize);

        float expectedX = DefaultHexSize * (3f / 2f * 2);
        float expectedY = DefaultHexSize * (MathF.Sqrt(3f) / 2f * 2 + MathF.Sqrt(3f) * 3);
        Assert.Equal(expectedX, px, precision: 2);
        Assert.Equal(expectedY, py, precision: 2);
    }

    [Fact]
    public void Should_DrawAllUnits_When_MultipleUnitsExist()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);
        manager.CreateUnit("Warrior", new HexCoord(1, 0), grid);
        manager.CreateUnit("Warrior", new HexCoord(2, 0), grid);

        Assert.Equal(3, manager.AllUnits.Count);
    }

    [Fact]
    public void Should_DistinguishSelectedUnit_When_ComparingIds()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit1 = manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);
        var unit2 = manager.CreateUnit("Warrior", new HexCoord(1, 0), grid);

        var renderer = new UnitRenderer();
        renderer.SelectedUnitId = unit2.Id;

        Assert.NotEqual(unit1.Id, renderer.SelectedUnitId);
        Assert.Equal(unit2.Id, renderer.SelectedUnitId);
    }

    [Fact]
    public void Should_ReflectUpdatedPosition_When_UnitMoves()
    {
        var (grid, manager) = CreateDefaultSetup();
        var unit = manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        var target = new HexCoord(3, 2);

        unit.TryMoveTo(target, grid, manager);

        Assert.Equal(target, manager.AllUnits[0].Position);
    }

    [Fact]
    public void Should_ProduceNonOverlappingCircleCenters_When_MultipleUnitsRendered()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateUnit("Warrior", new HexCoord(0, 0), grid);
        manager.CreateUnit("Warrior", new HexCoord(1, 1), grid);
        manager.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        var centers = new HashSet<(float, float)>();
        foreach (var unit in manager.AllUnits)
        {
            var (px, py) = HexGrid.HexToPixel(unit.Position, DefaultHexSize);
            var rounded = (MathF.Round(px, 2), MathF.Round(py, 2));
            Assert.True(centers.Add(rounded));
        }

        Assert.Equal(3, centers.Count);
    }
}
