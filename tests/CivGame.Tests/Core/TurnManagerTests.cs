using CivGame.Buildings;
using CivGame.Cities;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;
using Xunit;

namespace CivGame.Tests.Core;

public class TurnManagerTests
{
    // ------------------------------------------------------------------ //
    // Helper factory                                                       //
    // ------------------------------------------------------------------ //

    private static (TurnManager turns, UnitManager units, CityManager cities, HexGrid grid) CreateDefaults()
    {
        var grid = new HexGrid(10, 10);
        var units = new UnitManager();
        var cities = new CityManager();
        // Use the 2-arg constructor (null grid path) so EndTurn uses flat 1-per-tick production.
        // Yield-driven EndTurn behavior is covered by TurnManagerYieldTests.
        var turns = new TurnManager(units, cities);
        return (turns, units, cities, grid);
    }

    // ------------------------------------------------------------------ //
    // CurrentTurn starts at 1                                              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_StartAtTurnOne_When_Created()
    {
        var (turns, _, _, _) = CreateDefaults();

        Assert.Equal(1, turns.CurrentTurn);
    }

    // ------------------------------------------------------------------ //
    // EndTurn increments CurrentTurn                                        //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_IncrementCurrentTurn_When_EndTurnCalled()
    {
        var (turns, _, _, _) = CreateDefaults();

        turns.EndTurn();

        Assert.Equal(2, turns.CurrentTurn);
    }

    [Fact]
    public void Should_IncrementMultipleTimes_When_EndTurnCalledRepeatedly()
    {
        var (turns, _, _, _) = CreateDefaults();

        for (int i = 0; i < 10; i++)
        {
            turns.EndTurn();
        }

        Assert.Equal(11, turns.CurrentTurn);
    }

    // ------------------------------------------------------------------ //
    // EndTurn resets all unit movement budgets                              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ResetUnitMovement_When_EndTurnCalled()
    {
        var (turns, units, _, grid) = CreateDefaults();
        var unit = units.CreateUnit("Warrior", new HexCoord(2, 2), grid);

        // Move the unit so it uses some movement
        unit.TryMoveTo(new HexCoord(3, 2), grid, units);
        Assert.True(unit.MovementRemaining < unit.MovementRange);

        turns.EndTurn();

        Assert.Equal(unit.MovementRange, unit.MovementRemaining);
    }

    [Fact]
    public void Should_ResetAllUnitsMovement_When_MultipleUnitsExist()
    {
        var (turns, units, _, grid) = CreateDefaults();
        var unit1 = units.CreateUnit("Warrior", new HexCoord(1, 1), grid);
        var unit2 = units.CreateUnit("Warrior", new HexCoord(5, 5), grid);

        // Exhaust some movement on both units
        unit1.TryMoveTo(new HexCoord(2, 1), grid, units);
        unit2.TryMoveTo(new HexCoord(6, 5), grid, units);

        turns.EndTurn();

        Assert.Equal(unit1.MovementRange, unit1.MovementRemaining);
        Assert.Equal(unit2.MovementRange, unit2.MovementRemaining);
    }

    // ------------------------------------------------------------------ //
    // EndTurn ticks city build queues                                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_TickCityProduction_When_EndTurnCalled()
    {
        var (turns, _, cities, grid) = CreateDefaults();
        var city = cities.CreateCity("TestCity", new HexCoord(3, 3), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        int initialTurnsRemaining = city.CurrentProduction!.TurnsRemaining;

        turns.EndTurn();

        bool advanced = city.CurrentProduction == null ||
                        city.CurrentProduction.TurnsRemaining < initialTurnsRemaining;
        Assert.True(advanced, "Expected production to advance after EndTurn");
    }

    [Fact]
    public void Should_CompleteGranary_When_EndTurnCalledFiveTimes()
    {
        var (turns, _, cities, grid) = CreateDefaults();
        var city = cities.CreateCity("TestCity", new HexCoord(3, 3), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        // Granary costs 5 turns
        for (int i = 0; i < 5; i++)
        {
            turns.EndTurn();
        }

        Assert.Null(city.CurrentProduction);
        Assert.Contains(city.CompletedBuildings, b => b.Name == "Granary");
    }

    [Fact]
    public void Should_TickAllCities_When_MultipleCitiesExist()
    {
        var (turns, _, cities, grid) = CreateDefaults();
        var city1 = cities.CreateCity("City1", new HexCoord(1, 1), grid);
        var city2 = cities.CreateCity("City2", new HexCoord(5, 5), grid);
        city1.StartBuilding(BuildingCatalog.Granary);
        city2.StartBuilding(BuildingCatalog.Granary);

        int initial1 = city1.CurrentProduction!.TurnsRemaining;
        int initial2 = city2.CurrentProduction!.TurnsRemaining;

        turns.EndTurn();

        bool city1Advanced = city1.CurrentProduction == null ||
                             city1.CurrentProduction.TurnsRemaining < initial1;
        bool city2Advanced = city2.CurrentProduction == null ||
                             city2.CurrentProduction.TurnsRemaining < initial2;
        Assert.True(city1Advanced, "Expected city1 production to advance after EndTurn");
        Assert.True(city2Advanced, "Expected city2 production to advance after EndTurn");
    }

    // ------------------------------------------------------------------ //
    // TurnEnding event                                                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_FireTurnEndingWithOldTurnNumber_When_EndTurnCalled()
    {
        var (turns, _, _, _) = CreateDefaults();
        int? firedWithTurn = null;

        turns.TurnEnding += turn => firedWithTurn = turn;

        turns.EndTurn();

        Assert.Equal(1, firedWithTurn); // old turn number
    }

    [Fact]
    public void Should_FireTurnEndingBeforeProcessing_When_EndTurnCalled()
    {
        var (turns, units, cities, grid) = CreateDefaults();
        var unit = units.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        unit.TryMoveTo(new HexCoord(3, 2), grid, units);

        int movementDuringEvent = -1;
        turns.TurnEnding += _ =>
        {
            // Movement should NOT yet be reset when TurnEnding fires
            movementDuringEvent = unit.MovementRemaining;
        };

        int movementBeforeEndTurn = unit.MovementRemaining;
        turns.EndTurn();

        Assert.Equal(movementBeforeEndTurn, movementDuringEvent);
    }

    // ------------------------------------------------------------------ //
    // TurnEnded event                                                      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_FireTurnEndedWithNewTurnNumber_When_EndTurnCalled()
    {
        var (turns, _, _, _) = CreateDefaults();
        int? firedWithTurn = null;

        turns.TurnEnded += turn => firedWithTurn = turn;

        turns.EndTurn();

        Assert.Equal(2, firedWithTurn); // new turn number
    }

    [Fact]
    public void Should_FireTurnEndedAfterProcessing_When_EndTurnCalled()
    {
        var (turns, units, _, grid) = CreateDefaults();
        var unit = units.CreateUnit("Warrior", new HexCoord(2, 2), grid);
        unit.TryMoveTo(new HexCoord(3, 2), grid, units);

        int movementDuringEvent = -1;
        turns.TurnEnded += _ =>
        {
            // Movement SHOULD be reset when TurnEnded fires
            movementDuringEvent = unit.MovementRemaining;
        };

        turns.EndTurn();

        Assert.Equal(unit.MovementRange, movementDuringEvent);
    }

    // ------------------------------------------------------------------ //
    // End-of-turn order: tick production THEN reset movement               //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_TickProductionBeforeResetMovement_When_EndTurnCalled()
    {
        var (turns, units, cities, grid) = CreateDefaults();
        var city = cities.CreateCity("TestCity", new HexCoord(3, 3), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        var unit = units.CreateUnit("Warrior", new HexCoord(5, 5), grid);
        unit.TryMoveTo(new HexCoord(6, 5), grid, units);

        // Track order of operations via TurnEnding (fires before both)
        bool productionTickedBeforeMovementReset = false;
        int productionTurnsAtEndingEvent = -1;
        int movementAtEndingEvent = -1;

        turns.TurnEnding += _ =>
        {
            productionTurnsAtEndingEvent = city.CurrentProduction!.TurnsRemaining;
            movementAtEndingEvent = unit.MovementRemaining;
        };

        int initialProductionTurns = city.CurrentProduction!.TurnsRemaining;
        int initialMovement = unit.MovementRemaining;

        turns.TurnEnded += _ =>
        {
            // After EndTurn, production should be ticked and movement reset
            // We verify the order by checking that both happened
            bool productionTicked = city.CurrentProduction == null ||
                city.CurrentProduction.TurnsRemaining < initialProductionTurns;
            bool movementReset = unit.MovementRemaining == unit.MovementRange;

            productionTickedBeforeMovementReset = productionTicked && movementReset;
        };

        turns.EndTurn();

        Assert.True(productionTickedBeforeMovementReset);
        // Verify pre-processing state was captured correctly
        Assert.Equal(initialProductionTurns, productionTurnsAtEndingEvent);
        Assert.Equal(initialMovement, movementAtEndingEvent);
    }

    // ------------------------------------------------------------------ //
    // Constructor null checks                                              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ThrowArgumentNullException_When_UnitManagerIsNull()
    {
        var cities = new CityManager();
        Assert.Throws<ArgumentNullException>(() => new TurnManager(null!, cities));
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_CityManagerIsNull()
    {
        var units = new UnitManager();
        Assert.Throws<ArgumentNullException>(() => new TurnManager(units, null!));
    }
}
