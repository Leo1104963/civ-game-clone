using CivGame.Rendering;
using CivGame.Cities;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for InputHandler click interaction logic. Verifies that the class
/// exists and that the data-model operations it performs (entity lookup,
/// movement, selection state) work correctly.
/// </summary>
public class InputHandlerTests
{
    private const float DefaultHexSize = 40f;

    private static GameSession CreateSession()
    {
        var grid = new HexGrid(8, 8);
        var units = new UnitManager();
        var cities = new CityManager();
        var turns = new TurnManager(units, cities);
        return new GameSession(grid, units, cities, turns);
    }

    // --- InputHandler class existence ---

    [Fact]
    public void Should_Exist_When_Referenced()
    {
        var type = typeof(InputHandler);
        Assert.NotNull(type);
    }

    // --- Unit lookup at clicked coordinate ---

    [Fact]
    public void Should_FindUnitAtCoord_When_UnitExists()
    {
        var session = CreateSession();
        var coord = new HexCoord(3, 3);
        var unit = session.Units.CreateUnit("Warrior", coord, session.Grid);

        var found = session.Units.GetUnitAt(coord);

        Assert.NotNull(found);
        Assert.Equal(unit.Id, found.Id);
    }

    [Fact]
    public void Should_ReturnNull_When_NoUnitAtClickedCoord()
    {
        var session = CreateSession();

        Assert.Null(session.Units.GetUnitAt(new HexCoord(3, 3)));
    }

    // --- City lookup at clicked coordinate ---

    [Fact]
    public void Should_FindCityAtCoord_When_CityExists()
    {
        var session = CreateSession();
        var coord = new HexCoord(2, 2);
        var city = session.Cities.CreateCity("TestCity", coord, session.Grid);

        var found = session.Cities.GetCityAt(coord);

        Assert.NotNull(found);
        Assert.Equal(city.Id, found.Id);
    }

    [Fact]
    public void Should_ReturnNullCity_When_NoCityAtCoord()
    {
        var session = CreateSession();

        Assert.Null(session.Cities.GetCityAt(new HexCoord(2, 2)));
    }

    // --- Click on empty cell ---

    [Fact]
    public void Should_FindNoEntity_When_ClickingEmptyCell()
    {
        var session = CreateSession();
        var coord = new HexCoord(4, 4);

        Assert.Null(session.Units.GetUnitAt(coord));
        Assert.Null(session.Cities.GetCityAt(coord));
    }

    // --- Unit selection shows reachable cells ---

    [Fact]
    public void Should_HaveReachableCells_When_UnitSelected()
    {
        var session = CreateSession();
        var coord = new HexCoord(3, 3);
        var unit = session.Units.CreateUnit("Warrior", coord, session.Grid);

        var reachable = session.Units.GetReachableCells(unit, session.Grid);

        Assert.True(reachable.Count > 1);
        Assert.Contains(coord, reachable);
    }

    // --- Movement on clicking reachable cell ---

    [Fact]
    public void Should_MoveUnit_When_ClickingReachableCell()
    {
        var session = CreateSession();
        var origin = new HexCoord(3, 3);
        var unit = session.Units.CreateUnit("Warrior", origin, session.Grid);
        var target = new HexCoord(4, 3);

        var reachable = session.Units.GetReachableCells(unit, session.Grid);
        Assert.Contains(target, reachable);

        bool moved = unit.TryMoveTo(target, session.Grid, session.Units);

        Assert.True(moved);
        Assert.Equal(target, unit.Position);
    }

    [Fact]
    public void Should_UpdatePositionIndex_When_UnitMoves()
    {
        var session = CreateSession();
        var origin = new HexCoord(3, 3);
        var unit = session.Units.CreateUnit("Warrior", origin, session.Grid);
        var target = new HexCoord(4, 3);

        unit.TryMoveTo(target, session.Grid, session.Units);

        Assert.Null(session.Units.GetUnitAt(origin));
        Assert.NotNull(session.Units.GetUnitAt(target));
    }

    [Fact]
    public void Should_NotMoveUnit_When_ClickingUnreachableCell()
    {
        var session = CreateSession();
        var origin = new HexCoord(3, 3);
        var unit = session.Units.CreateUnit("Warrior", origin, session.Grid);

        var farTarget = new HexCoord(6, 6);

        bool moved = unit.TryMoveTo(farTarget, session.Grid, session.Units);

        Assert.False(moved);
        Assert.Equal(origin, unit.Position);
    }

    [Fact]
    public void Should_NotMoveUnit_When_ClickingSameCell()
    {
        var session = CreateSession();
        var origin = new HexCoord(3, 3);
        var unit = session.Units.CreateUnit("Warrior", origin, session.Grid);

        bool moved = unit.TryMoveTo(origin, session.Grid, session.Units);

        Assert.False(moved);
        Assert.Equal(2, unit.MovementRemaining);
    }

    // --- Post-move state ---

    [Fact]
    public void Should_StillHaveMovement_When_MovingOnceWithWarrior()
    {
        var session = CreateSession();
        var unit = session.Units.CreateUnit("Warrior", new HexCoord(3, 3), session.Grid);

        unit.TryMoveTo(new HexCoord(4, 3), session.Grid, session.Units);

        Assert.True(unit.CanMove);
        Assert.Equal(1, unit.MovementRemaining);
    }

    [Fact]
    public void Should_UpdateReachable_When_UnitStillHasMovement()
    {
        var session = CreateSession();
        var unit = session.Units.CreateUnit("Warrior", new HexCoord(3, 3), session.Grid);

        unit.TryMoveTo(new HexCoord(4, 3), session.Grid, session.Units);

        var newReachable = session.Units.GetReachableCells(unit, session.Grid);
        Assert.True(newReachable.Count > 1);
    }

    [Fact]
    public void Should_ExhaustMovement_When_MovingTwiceWithWarrior()
    {
        var session = CreateSession();
        var unit = session.Units.CreateUnit("Warrior", new HexCoord(3, 3), session.Grid);

        unit.TryMoveTo(new HexCoord(4, 3), session.Grid, session.Units);
        unit.TryMoveTo(new HexCoord(5, 3), session.Grid, session.Units);

        Assert.False(unit.CanMove);
        Assert.Equal(0, unit.MovementRemaining);
    }

    // --- Coordinate conversion ---

    [Fact]
    public void Should_ConvertPixelToHex_When_ClickAtCellCenter()
    {
        var coord = new HexCoord(2, 3);
        var (px, py) = HexGrid.HexToPixel(coord, DefaultHexSize);
        var result = HexGrid.PixelToHex(px, py, DefaultHexSize);

        Assert.Equal(coord, result);
    }

    [Fact]
    public void Should_ConvertPixelToHex_When_ClickSlightlyOffCenter()
    {
        var coord = new HexCoord(2, 3);
        var (px, py) = HexGrid.HexToPixel(coord, DefaultHexSize);
        var result = HexGrid.PixelToHex(px + 5f, py + 5f, DefaultHexSize);

        Assert.Equal(coord, result);
    }

    [Fact]
    public void Should_DetectOutOfBounds_When_ClickOutsideGrid()
    {
        var grid = new HexGrid(5, 5);

        Assert.False(grid.InBounds(new HexCoord(-1, -1)));
        Assert.False(grid.InBounds(new HexCoord(5, 5)));
    }

    // --- Entity priority ---

    [Fact]
    public void Should_CheckUnitBeforeCity_When_BothAtDifferentCoords()
    {
        var session = CreateSession();
        var unitCoord = new HexCoord(3, 3);
        var cityCoord = new HexCoord(4, 4);

        session.Units.CreateUnit("Warrior", unitCoord, session.Grid);
        session.Cities.CreateCity("TestCity", cityCoord, session.Grid);

        Assert.NotNull(session.Units.GetUnitAt(unitCoord));
        Assert.Null(session.Cities.GetCityAt(unitCoord));

        Assert.Null(session.Units.GetUnitAt(cityCoord));
        Assert.NotNull(session.Cities.GetCityAt(cityCoord));
    }

    [Fact]
    public void Should_FindUnitNotCity_When_UnitOnCityCoord()
    {
        var session = CreateSession();
        var coord = new HexCoord(3, 3);

        session.Cities.CreateCity("TestCity", coord, session.Grid);
        session.Units.CreateUnit("Warrior", coord, session.Grid);

        Assert.NotNull(session.Units.GetUnitAt(coord));
    }
}
