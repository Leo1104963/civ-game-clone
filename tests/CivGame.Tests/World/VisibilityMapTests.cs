using System;
using System.Collections.Generic;
using System.Linq;
using CivGame.Cities;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;
using Xunit;

namespace CivGame.Tests.World;

public class VisibilityMapTests
{
    // ------------------------------------------------------------------ //
    // VisibilityState enum sanity                                          //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveCorrectOrdinalValues_When_EnumDefined()
    {
        Assert.Equal(0, (int)VisibilityState.Unseen);
        Assert.Equal(1, (int)VisibilityState.Explored);
        Assert.Equal(2, (int)VisibilityState.Visible);
    }

    // ------------------------------------------------------------------ //
    // New VisibilityMap — all cells Unseen                                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnUnseen_When_FreshMapQueriedForPlayer0()
    {
        var grid = new HexGrid(8, 8);
        var vm = new VisibilityMap(grid);

        Assert.Equal(VisibilityState.Unseen, vm.IsAt(0, new HexCoord(0, 0)));
        Assert.Equal(VisibilityState.Unseen, vm.IsAt(0, new HexCoord(4, 4)));
        Assert.Equal(VisibilityState.Unseen, vm.IsAt(0, new HexCoord(7, 7)));
    }

    [Fact]
    public void Should_ReturnFalseForIsVisibleTo_When_FreshMapQueried()
    {
        var grid = new HexGrid(6, 6);
        var vm = new VisibilityMap(grid);

        Assert.False(vm.IsVisibleTo(0, new HexCoord(3, 3)));
    }

    [Fact]
    public void Should_ReturnFalseForIsExplored_When_FreshMapQueried()
    {
        var grid = new HexGrid(6, 6);
        var vm = new VisibilityMap(grid);

        Assert.False(vm.IsExplored(0, new HexCoord(3, 3)));
    }

    // ------------------------------------------------------------------ //
    // Recompute radius 2 — Visible and Unseen zones                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_MarkCellVisible_When_WithinSightRadius()
    {
        var grid = new HexGrid(10, 10);
        var vm = new VisibilityMap(grid);
        var origin = new HexCoord(5, 5);

        vm.Recompute(0, new[] { origin }, sightRadius: 2);

        // Every in-bounds cell within distance 2 must be Visible.
        foreach (var cell in grid.AllCells())
        {
            int dist = origin.DistanceTo(cell.Coord);
            if (dist <= 2)
                Assert.Equal(VisibilityState.Visible, vm.IsAt(0, cell.Coord));
        }
    }

    [Fact]
    public void Should_LeaveUnseenCellsUnseen_When_OutsideSightRadius()
    {
        var grid = new HexGrid(10, 10);
        var vm = new VisibilityMap(grid);
        var origin = new HexCoord(5, 5);

        vm.Recompute(0, new[] { origin }, sightRadius: 2);

        // Cells at distance >= 3 that have never been visible must stay Unseen.
        foreach (var cell in grid.AllCells())
        {
            int dist = origin.DistanceTo(cell.Coord);
            if (dist >= 3)
                Assert.Equal(VisibilityState.Unseen, vm.IsAt(0, cell.Coord));
        }
    }

    // ------------------------------------------------------------------ //
    // IsVisibleTo / IsExplored convenience methods                         //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnTrueForIsVisibleTo_When_CellIsVisible()
    {
        var grid = new HexGrid(8, 8);
        var vm = new VisibilityMap(grid);
        var origin = new HexCoord(4, 4);

        vm.Recompute(0, new[] { origin }, sightRadius: 2);

        Assert.True(vm.IsVisibleTo(0, origin));
        Assert.True(vm.IsVisibleTo(0, new HexCoord(4, 5))); // dist 1
    }

    [Fact]
    public void Should_ReturnTrueForIsExplored_When_CellIsVisible()
    {
        var grid = new HexGrid(8, 8);
        var vm = new VisibilityMap(grid);
        var origin = new HexCoord(4, 4);

        vm.Recompute(0, new[] { origin }, sightRadius: 2);

        // Visible implies Explored.
        Assert.True(vm.IsExplored(0, origin));
    }

    [Fact]
    public void Should_ReturnTrueForIsExplored_When_CellWasPreviouslyVisible()
    {
        var grid = new HexGrid(10, 10);
        var vm = new VisibilityMap(grid);

        // First recompute: origin at (5,5), cell (5,6) is at dist 1 — Visible.
        vm.Recompute(0, new[] { new HexCoord(5, 5) }, sightRadius: 2);
        // Move observer far away so (5,6) is no longer in sight.
        vm.Recompute(0, new[] { new HexCoord(0, 0) }, sightRadius: 2);

        // (5,6) should now be Explored (not Visible, not Unseen).
        Assert.Equal(VisibilityState.Explored, vm.IsAt(0, new HexCoord(5, 6)));
        Assert.True(vm.IsExplored(0, new HexCoord(5, 6)));
        Assert.False(vm.IsVisibleTo(0, new HexCoord(5, 6)));
    }

    // ------------------------------------------------------------------ //
    // Two successive Recomputes — Visible becomes Explored                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_TransitionVisibleToExplored_When_ObserverMoveAway()
    {
        var grid = new HexGrid(12, 12);
        var vm = new VisibilityMap(grid);

        var firstPos = new HexCoord(6, 6);
        var secondPos = new HexCoord(0, 0);
        var watchedCell = new HexCoord(6, 7); // dist 1 from firstPos, dist >= 3 from secondPos

        vm.Recompute(0, new[] { firstPos }, sightRadius: 2);
        Assert.Equal(VisibilityState.Visible, vm.IsAt(0, watchedCell));

        vm.Recompute(0, new[] { secondPos }, sightRadius: 2);
        Assert.Equal(VisibilityState.Explored, vm.IsAt(0, watchedCell));
    }

    // ------------------------------------------------------------------ //
    // Monotonic: Explored never reverts to Unseen                          //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_NeverRevertToUnseen_When_CellWasOnceVisible()
    {
        var grid = new HexGrid(12, 12);
        var vm = new VisibilityMap(grid);

        var exploredCell = new HexCoord(6, 6);

        // Make it Visible.
        vm.Recompute(0, new[] { exploredCell }, sightRadius: 0);
        Assert.Equal(VisibilityState.Visible, vm.IsAt(0, exploredCell));

        // Move observer completely away, recompute many times.
        vm.Recompute(0, new[] { new HexCoord(0, 0) }, sightRadius: 0);
        vm.Recompute(0, new[] { new HexCoord(11, 11) }, sightRadius: 0);
        vm.Recompute(0, new[] { new HexCoord(0, 11) }, sightRadius: 0);

        // Must be Explored, never back to Unseen.
        var state = vm.IsAt(0, exploredCell);
        Assert.True(state == VisibilityState.Explored,
            $"Expected Explored but got {state}");
    }

    // ------------------------------------------------------------------ //
    // Player isolation                                                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_NotAffectPlayer1_When_RecomputeCalledForPlayer0()
    {
        var grid = new HexGrid(10, 10);
        var vm = new VisibilityMap(grid);
        var origin = new HexCoord(5, 5);

        vm.Recompute(0, new[] { origin }, sightRadius: 2);

        // Player 1 has never had Recompute called — all cells remain Unseen.
        foreach (var cell in grid.AllCells())
        {
            Assert.Equal(VisibilityState.Unseen, vm.IsAt(1, cell.Coord));
        }
    }

    [Fact]
    public void Should_MaintainIndependentState_When_BothPlayersRecomputed()
    {
        var grid = new HexGrid(10, 10);
        var vm = new VisibilityMap(grid);

        var pos0 = new HexCoord(2, 2);
        var pos1 = new HexCoord(8, 8);

        vm.Recompute(0, new[] { pos0 }, sightRadius: 2);
        vm.Recompute(1, new[] { pos1 }, sightRadius: 2);

        // Player 0 can see around pos0, not pos1.
        Assert.Equal(VisibilityState.Visible, vm.IsAt(0, pos0));
        Assert.Equal(VisibilityState.Unseen, vm.IsAt(0, pos1));

        // Player 1 can see around pos1, not pos0.
        Assert.Equal(VisibilityState.Visible, vm.IsAt(1, pos1));
        Assert.Equal(VisibilityState.Unseen, vm.IsAt(1, pos0));
    }

    // ------------------------------------------------------------------ //
    // OOB observer coord — must not throw                                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_NotThrow_When_ObserverCoordIsOutOfBounds()
    {
        var grid = new HexGrid(8, 8);
        var vm = new VisibilityMap(grid);
        var oobCoord = new HexCoord(-5, -5);

        // Must not throw.
        vm.Recompute(0, new[] { oobCoord }, sightRadius: 2);
    }

    // ------------------------------------------------------------------ //
    // sightRadius: 0 — only observer's own cell Visible                    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_MarkOnlyObserverCellVisible_When_SightRadiusIsZero()
    {
        var grid = new HexGrid(8, 8);
        var vm = new VisibilityMap(grid);
        var origin = new HexCoord(4, 4);

        vm.Recompute(0, new[] { origin }, sightRadius: 0);

        Assert.Equal(VisibilityState.Visible, vm.IsAt(0, origin));

        // All other cells must be Unseen (no history yet).
        foreach (var cell in grid.AllCells())
        {
            if (cell.Coord == origin) continue;
            Assert.Equal(VisibilityState.Unseen, vm.IsAt(0, cell.Coord));
        }
    }

    // ------------------------------------------------------------------ //
    // sightRadius: -1 — throws ArgumentOutOfRangeException                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_SightRadiusIsNegative()
    {
        var grid = new HexGrid(8, 8);
        var vm = new VisibilityMap(grid);

        void Act() => vm.Recompute(0, new[] { new HexCoord(4, 4) }, sightRadius: -1);
        Assert.Throws<ArgumentOutOfRangeException>(Act);
    }

    // ------------------------------------------------------------------ //
    // RecomputeForPlayer — uses all player-0 units AND cities               //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_MarkCellsVisibleAroundPlayer0Units_When_RecomputeForPlayerCalled()
    {
        var grid = new HexGrid(12, 12);
        var units = new UnitManager();
        var cities = new CityManager();
        var vm = new VisibilityMap(grid);

        var unitPos = new HexCoord(3, 3);
        units.CreateUnit("Warrior", unitPos, grid, ownerId: 0);

        vm.RecomputeForPlayer(0, units, cities, sightRadius: 2);

        Assert.Equal(VisibilityState.Visible, vm.IsAt(0, unitPos));
        // A cell at dist 2 from unitPos should also be Visible.
        Assert.Equal(VisibilityState.Visible, vm.IsAt(0, new HexCoord(3, 5))); // dist 2
    }

    [Fact]
    public void Should_MarkCellsVisibleAroundPlayer0Cities_When_RecomputeForPlayerCalled()
    {
        var grid = new HexGrid(12, 12);
        var units = new UnitManager();
        var cities = new CityManager();
        var vm = new VisibilityMap(grid);

        var cityPos = new HexCoord(7, 7);
        cities.CreateCity("Capital", cityPos, grid, ownerId: 0);

        vm.RecomputeForPlayer(0, units, cities, sightRadius: 2);

        Assert.Equal(VisibilityState.Visible, vm.IsAt(0, cityPos));
    }

    [Fact]
    public void Should_NotMarkCellsForPlayer1Units_When_RecomputeForPlayer0Called()
    {
        var grid = new HexGrid(12, 12);
        var units = new UnitManager();
        var cities = new CityManager();
        var vm = new VisibilityMap(grid);

        // Player 1 unit at (9,9) — far from any player-0 entity.
        var p1UnitPos = new HexCoord(9, 9);
        units.CreateUnit("Warrior", p1UnitPos, grid, ownerId: 1);

        // Player 0 unit at (1,1) with radius 2 — cannot reach (9,9).
        units.CreateUnit("Warrior", new HexCoord(1, 1), grid, ownerId: 0);

        vm.RecomputeForPlayer(0, units, cities, sightRadius: 2);

        // The cell at p1UnitPos should NOT be visible (too far from player 0's unit).
        Assert.Equal(VisibilityState.Unseen, vm.IsAt(0, p1UnitPos));
    }

    // ------------------------------------------------------------------ //
    // GameSession integration — construction auto-recomputes player 0      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ExposeVisibilityProperty_When_GameSessionConstructed()
    {
        var session = new GameSession(16, 16);
        Assert.NotNull(session.Visibility);
    }

    [Fact]
    public void Should_MakeCapitalVisibleToPlayer0_When_GameSessionConstructed()
    {
        var session = new GameSession(16, 16);
        var capitalPosition = session.Cities.AllCities[0].Position;

        Assert.True(session.Visibility.IsVisibleTo(0, capitalPosition));
    }

    [Fact]
    public void Should_MakeStartingUnitsVisibleToPlayer0_When_GameSessionConstructed()
    {
        var session = new GameSession(16, 16);

        foreach (var unit in session.Units.AllUnits)
        {
            Assert.True(session.Visibility.IsVisibleTo(0, unit.Position),
                $"Unit {unit.UnitType} at {unit.Position} should be Visible to player 0 on session start");
        }
    }

    // ------------------------------------------------------------------ //
    // GameSession integration — EndTurn updates visibility                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_UpdateVisibility_When_EndTurnCalled()
    {
        // Use the 4-arg constructor so we can use a pre-built grid without
        // auto-placement, then manually place a unit and verify EndTurn
        // causes it to be visible.
        var grid = new HexGrid(10, 10);
        var units = new UnitManager();
        var cities = new CityManager();
        var turns = new TurnManager(units, cities, grid);
        var session = new GameSession(grid, units, cities, turns);

        // Place a player-0 warrior.
        var unitPos = new HexCoord(5, 5);
        units.CreateUnit("Warrior", unitPos, grid, ownerId: 0);

        // Before EndTurn, visibility has never been recomputed — cell is Unseen.
        Assert.Equal(VisibilityState.Unseen, session.Visibility.IsAt(0, unitPos));

        // After EndTurn, visibility should be recomputed and the unit's cell Visible.
        session.Turns.EndTurn();

        Assert.True(session.Visibility.IsVisibleTo(0, unitPos),
            "After EndTurn, the warrior's cell should be Visible to player 0");
    }
}
