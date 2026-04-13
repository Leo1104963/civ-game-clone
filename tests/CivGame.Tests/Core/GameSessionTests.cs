using CivGame.Cities;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;
using Xunit;

namespace CivGame.Tests.Core;

public class GameSessionTests
{
    // ------------------------------------------------------------------ //
    // GameSession(gridWidth, gridHeight) — default constructor             //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_CreateGrid_When_DefaultConstructorUsed()
    {
        var session = new GameSession(10, 8);

        Assert.NotNull(session.Grid);
        Assert.Equal(10, session.Grid.Width);
        Assert.Equal(8, session.Grid.Height);
    }

    [Fact]
    public void Should_CreateUnitManager_When_DefaultConstructorUsed()
    {
        var session = new GameSession(10, 10);

        Assert.NotNull(session.Units);
    }

    [Fact]
    public void Should_CreateCityManager_When_DefaultConstructorUsed()
    {
        var session = new GameSession(10, 10);

        Assert.NotNull(session.Cities);
    }

    [Fact]
    public void Should_CreateTurnManager_When_DefaultConstructorUsed()
    {
        var session = new GameSession(10, 10);

        Assert.NotNull(session.Turns);
        Assert.Equal(1, session.Turns.CurrentTurn);
    }

    [Fact]
    public void Should_PlaceCityAtCenter_When_DefaultConstructorUsed()
    {
        var session = new GameSession(10, 10);

        Assert.Single(session.Cities.AllCities);
        var city = session.Cities.AllCities[0];
        Assert.Equal("Capital", city.Name);
        Assert.Equal(new HexCoord(5, 5), city.Position);
    }

    [Fact]
    public void Should_PlaceWarriorAdjacentToCity_When_DefaultConstructorUsed()
    {
        var session = new GameSession(10, 10);

        // #53: bootstrap now places Warrior + Settler, so AllUnits.Count is 2.
        var unit = session.Units.AllUnits.FirstOrDefault(u => u.UnitType == "Warrior");
        Assert.NotNull(unit);

        var city = session.Cities.AllCities[0];
        Assert.Equal(1, city.Position.DistanceTo(unit!.Position));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_WidthIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(0, 10));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_HeightIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(10, 0));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_WidthIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(-1, 10));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_HeightIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(10, -1));
    }

    // ------------------------------------------------------------------ //
    // GameSession(grid, units, cities, turns) — full control constructor    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_AcceptPreBuiltComponents_When_FullConstructorUsed()
    {
        var grid = new HexGrid(5, 5);
        var units = new UnitManager();
        var cities = new CityManager();
        var turns = new TurnManager(units, cities);

        var session = new GameSession(grid, units, cities, turns);

        Assert.Same(grid, session.Grid);
        Assert.Same(units, session.Units);
        Assert.Same(cities, session.Cities);
        Assert.Same(turns, session.Turns);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_GridIsNull()
    {
        var units = new UnitManager();
        var cities = new CityManager();
        var turns = new TurnManager(units, cities);

        Assert.Throws<ArgumentNullException>(() => new GameSession(null!, units, cities, turns));
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_UnitsIsNull()
    {
        var grid = new HexGrid(5, 5);
        var cities = new CityManager();
        var units = new UnitManager();
        var turns = new TurnManager(units, cities);

        Assert.Throws<ArgumentNullException>(() => new GameSession(grid, null!, cities, turns));
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_CitiesIsNull()
    {
        var grid = new HexGrid(5, 5);
        var units = new UnitManager();
        var turns = new TurnManager(units, new CityManager());

        Assert.Throws<ArgumentNullException>(() => new GameSession(grid, units, null!, turns));
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_TurnsIsNull()
    {
        var grid = new HexGrid(5, 5);
        var units = new UnitManager();
        var cities = new CityManager();

        Assert.Throws<ArgumentNullException>(() => new GameSession(grid, units, cities, null!));
    }

    // ------------------------------------------------------------------ //
    // Integration: EndTurn works through GameSession                        //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_AdvanceTurn_When_EndTurnCalledViaSession()
    {
        var session = new GameSession(10, 10);

        session.Turns.EndTurn();

        Assert.Equal(2, session.Turns.CurrentTurn);
    }

    [Fact]
    public void Should_ResetWarriorMovement_When_EndTurnCalledViaSession()
    {
        var session = new GameSession(10, 10);
        var warrior = session.Units.AllUnits[0];

        // Move the warrior to use some movement
        var neighbors = session.Grid.GetNeighbors(warrior.Position);
        var target = neighbors[0].Coord;
        if (session.Units.IsOccupied(target) || session.Cities.GetCityAt(target) != null)
        {
            target = neighbors[1].Coord;
        }
        warrior.TryMoveTo(target, session.Grid, session.Units);

        session.Turns.EndTurn();

        Assert.Equal(warrior.MovementRange, warrior.MovementRemaining);
    }
}
