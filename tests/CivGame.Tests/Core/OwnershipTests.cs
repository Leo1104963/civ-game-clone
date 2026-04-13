using CivGame.Buildings;
using CivGame.Cities;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Tests.Core;

/// <summary>
/// Failing tests for issue #68: Unit ownership refactor (OwnerId + per-player TurnManager).
/// </summary>
public class OwnershipTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static HexGrid MakeGrid(int w = 10, int h = 10) => new HexGrid(w, h);

    private static (UnitManager Units, CityManager Cities, HexGrid Grid) MakeManagers()
    {
        var grid = MakeGrid();
        return (new UnitManager(), new CityManager(), grid);
    }

    // -----------------------------------------------------------------------
    // AC1 — Unit.OwnerId stores explicit value
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_StoreExplicitOwnerId_When_UnitConstructedWithOwnerId7()
    {
        var coord = new HexCoord(0, 0);

        var unit = new Unit("Warrior", coord, 2, ownerId: 7);

        Assert.Equal(7, unit.OwnerId);
    }

    // -----------------------------------------------------------------------
    // AC2 — Unit.OwnerId defaults to 0
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_DefaultOwnerIdToZero_When_UnitConstructedWithoutOwnerId()
    {
        var coord = new HexCoord(0, 0);

        var unit = new Unit("Warrior", coord, 2);

        Assert.Equal(0, unit.OwnerId);
    }

    // -----------------------------------------------------------------------
    // AC3 — City.OwnerId stores explicit value
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_StoreExplicitOwnerId_When_CityConstructedWithOwnerId3()
    {
        var coord = new HexCoord(0, 0);

        var city = new City("X", coord, ownerId: 3);

        Assert.Equal(3, city.OwnerId);
    }

    // -----------------------------------------------------------------------
    // AC4 — UnitManager.CreateUnit default and explicit-owner overloads
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_DefaultOwnerIdToZero_When_CreateUnitCalledWithoutOwnerId()
    {
        var (units, _, grid) = MakeManagers();

        var unit = units.CreateUnit("Warrior", new HexCoord(0, 0), grid);

        Assert.Equal(0, unit.OwnerId);
    }

    [Fact]
    public void Should_StoreExplicitOwnerId_When_CreateUnitCalledWithOwnerId5()
    {
        var (units, _, grid) = MakeManagers();

        var unit = units.CreateUnit("Warrior", new HexCoord(0, 0), grid, ownerId: 5);

        Assert.Equal(5, unit.OwnerId);
    }

    // -----------------------------------------------------------------------
    // AC5 — UnitManager.UnitsOwnedBy
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ReturnOnlyPlayer0Units_When_UnitsOwnedByCalledWithZero()
    {
        var (units, _, grid) = MakeManagers();
        var u0a = units.CreateUnit("Warrior", new HexCoord(0, 0), grid, ownerId: 0);
        var u0b = units.CreateUnit("Warrior", new HexCoord(1, 0), grid, ownerId: 0);
        units.CreateUnit("Warrior", new HexCoord(2, 0), grid, ownerId: 1);

        var owned = units.UnitsOwnedBy(0).ToList();

        Assert.Equal(2, owned.Count);
        Assert.Contains(u0a, owned);
        Assert.Contains(u0b, owned);
    }

    [Fact]
    public void Should_ReturnEmpty_When_UnitsOwnedByCalledWithUnknownOwnerId()
    {
        var (units, _, grid) = MakeManagers();
        units.CreateUnit("Warrior", new HexCoord(0, 0), grid, ownerId: 0);

        var owned = units.UnitsOwnedBy(99).ToList();

        Assert.Empty(owned);
    }

    // -----------------------------------------------------------------------
    // AC6 — UnitManager.ResetMovementFor resets only the specified owner
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ResetOnlyPlayer0Units_When_ResetMovementForCalledWithZero()
    {
        var (units, _, grid) = MakeManagers();
        var player0Unit = units.CreateUnit("Warrior", new HexCoord(0, 0), grid, ownerId: 0);
        var player1Unit = units.CreateUnit("Warrior", new HexCoord(5, 5), grid, ownerId: 1);

        // Spend movement on both units
        player0Unit.TryMoveTo(new HexCoord(1, 0), grid, units);
        player1Unit.TryMoveTo(new HexCoord(6, 5), grid, units);
        int player1MovementAfterMove = player1Unit.MovementRemaining;

        units.ResetMovementFor(0);

        Assert.Equal(player0Unit.MovementRange, player0Unit.MovementRemaining);
        Assert.Equal(player1MovementAfterMove, player1Unit.MovementRemaining);
    }

    // -----------------------------------------------------------------------
    // AC7 — CityManager.TickProductionFor ticks only the specified owner
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_TickOnlyPlayer0Cities_When_TickProductionForCalledWithZero()
    {
        var (_, cities, grid) = MakeManagers();

        // Use Forest terrain so Production yield is non-zero (Forest → Prod:2 per tile).
        foreach (var cell in grid.AllCells())
        {
            cell.Terrain = TerrainType.Forest;
        }

        var city0 = cities.CreateCity("Rome", new HexCoord(3, 3), grid, ownerId: 0);
        var city1 = cities.CreateCity("Athens", new HexCoord(7, 7), grid, ownerId: 1);
        city0.StartBuilding(BuildingCatalog.Granary);
        city1.StartBuilding(BuildingCatalog.Granary);

        int city0TurnsBefore = city0.CurrentProduction!.TurnsRemaining;
        int city1TurnsBefore = city1.CurrentProduction!.TurnsRemaining;

        cities.TickProductionFor(0, grid);

        // Player-0 city advanced (either completed or cost decreased).
        bool city0Advanced = city0.CurrentProduction == null ||
                             city0.CurrentProduction.TurnsRemaining < city0TurnsBefore;
        Assert.True(city0Advanced);

        // Player-1 city is unchanged.
        Assert.Equal(city1TurnsBefore, city1.CurrentProduction!.TurnsRemaining);
    }

    // -----------------------------------------------------------------------
    // AC8 — TurnManager default constructor: PlayerOrder==[0], CurrentPlayerId==0
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_HavePlayerOrder0AndCurrentPlayerId0_When_DefaultTurnManagerCreated()
    {
        var (units, cities, _) = MakeManagers();

        var turns = new TurnManager(units, cities);

        Assert.Equal(new[] { 0 }, turns.PlayerOrder.ToArray());
        Assert.Equal(0, turns.CurrentPlayerId);
    }

    // -----------------------------------------------------------------------
    // AC9 — TurnManager with [0,1]: cycling and CurrentTurn increment
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_AdvanceCurrentPlayerAndWrapCurrentTurn_When_TwoPlayerEndTurnCycled()
    {
        var (units, cities, grid) = MakeManagers();
        var turns = new TurnManager(units, cities, grid, new[] { 0, 1 });

        Assert.Equal(0, turns.CurrentPlayerId);
        Assert.Equal(1, turns.CurrentTurn);

        turns.EndTurn();

        Assert.Equal(1, turns.CurrentPlayerId);
        Assert.Equal(1, turns.CurrentTurn); // not yet wrapped

        turns.EndTurn();

        Assert.Equal(0, turns.CurrentPlayerId);
        Assert.Equal(2, turns.CurrentTurn); // wrapped → increments
    }

    // -----------------------------------------------------------------------
    // AC10 — EndTurn resets movement only for current player
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ResetOnlyCurrentPlayerMovement_When_EndTurnCalled()
    {
        var (units, cities, grid) = MakeManagers();
        var player0Unit = units.CreateUnit("Warrior", new HexCoord(0, 0), grid, ownerId: 0);
        var player1Unit = units.CreateUnit("Warrior", new HexCoord(5, 5), grid, ownerId: 1);

        // Spend movement on both
        player0Unit.TryMoveTo(new HexCoord(1, 0), grid, units);
        player1Unit.TryMoveTo(new HexCoord(6, 5), grid, units);
        int player1MovementAfterMove = player1Unit.MovementRemaining;

        var turns = new TurnManager(units, cities, grid, new[] { 0, 1 });

        turns.EndTurn(); // ends player 0's turn

        Assert.Equal(player0Unit.MovementRange, player0Unit.MovementRemaining);
        Assert.Equal(player1MovementAfterMove, player1Unit.MovementRemaining);
    }

    // -----------------------------------------------------------------------
    // AC11 — TurnManager throws ArgumentException for empty playerOrder
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ThrowArgumentException_When_EmptyPlayerOrderProvided()
    {
        var (units, cities, grid) = MakeManagers();

        Assert.Throws<ArgumentException>(() =>
            new TurnManager(units, cities, grid, new int[0]));
    }

    // -----------------------------------------------------------------------
    // AC12 — GameSession(16,16): capital/warrior/settler all have OwnerId==0
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_HaveOwnerId0ForAllStartingEntities_When_GameSessionConstructed()
    {
        var session = new GameSession(16, 16);

        var capital = session.Cities.AllCities[0];
        Assert.Equal(0, capital.OwnerId);

        foreach (var unit in session.Units.AllUnits)
        {
            Assert.Equal(0, unit.OwnerId);
        }
    }

    // -----------------------------------------------------------------------
    // AC13 — City default OwnerId is 0 (backwards compat for existing tests)
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_DefaultOwnerIdToZero_When_CityConstructedWithoutOwnerId()
    {
        var city = new City("TestCity", new HexCoord(0, 0));

        Assert.Equal(0, city.OwnerId);
    }

    // -----------------------------------------------------------------------
    // AC13 — CityManager.CreateCity default OwnerId is 0 (backwards compat)
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_DefaultOwnerIdToZero_When_CreateCityCalledWithoutOwnerId()
    {
        var (_, cities, grid) = MakeManagers();

        var city = cities.CreateCity("Rome", new HexCoord(0, 0), grid);

        Assert.Equal(0, city.OwnerId);
    }
}
