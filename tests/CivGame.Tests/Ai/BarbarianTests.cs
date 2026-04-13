using CivGame.Ai;
using CivGame.Cities;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Ai.Tests;

/// <summary>
/// Failing tests for issue #70: Barbarian faction (pure spawner + simple AI).
/// All tests target the public API surface defined in the issue spec.
/// Pure C# — no Godot runtime required.
/// </summary>
public class BarbarianTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Build a flat all-Grass grid of the given dimensions.
    /// All cells are passable by default (HexGrid default terrain is Grass).
    /// </summary>
    private static HexGrid MakeGrid(int width = 20, int height = 20) => new HexGrid(width, height);

    /// <summary>
    /// Place a player-0 warrior at the center of a 20×20 grid and return
    /// the grid, unit manager, and city manager ready for spawner tests.
    /// </summary>
    private static (HexGrid grid, UnitManager units, CityManager cities) MakeSceneWithPlayerWarrior()
    {
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        // Player-0 warrior placed at (10, 10) — center of the 20×20 grid.
        units.CreateUnit("Warrior", new HexCoord(10, 10), grid, ownerId: 0);
        return (grid, units, cities);
    }

    // ── BarbarianSpawner.ChooseSpawn — turn-modulo guard ─────────────────────

    [Fact]
    public void Should_ReturnNull_When_TurnModuloSpawnEveryNTurnsIsNonZero()
    {
        var (grid, units, cities) = MakeSceneWithPlayerWarrior();

        // Turn 1 % 5 == 1, so no spawn expected.
        var coord = BarbarianSpawner.ChooseSpawn(1, grid, units, cities, new Random(0));

        Assert.Null(coord);
    }

    [Fact]
    public void Should_ReturnNull_When_TurnIsThreeAndSpawnEveryFiveTurns()
    {
        var (grid, units, cities) = MakeSceneWithPlayerWarrior();

        var coord = BarbarianSpawner.ChooseSpawn(3, grid, units, cities, new Random(0));

        Assert.Null(coord);
    }

    // ── BarbarianSpawner.ChooseSpawn — max-barbarian guard ───────────────────

    [Fact]
    public void Should_ReturnNull_When_BarbarianCountEqualsMaxBarbarians()
    {
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();

        // Place 6 barbarian warriors (maxBarbarians default = 6).
        // Put them far from any player units (no player units exist here).
        for (int i = 0; i < 6; i++)
        {
            units.CreateUnit("Warrior", new HexCoord(i, 0), grid, ownerId: BarbarianSpawner.BarbarianOwnerId);
        }

        // Turn 5 would normally spawn (5 % 5 == 0), but count >= max.
        var coord = BarbarianSpawner.ChooseSpawn(5, grid, units, cities, new Random(0), maxBarbarians: 6);

        Assert.Null(coord);
    }

    [Fact]
    public void Should_ReturnNull_When_BarbarianCountExceedsMaxBarbarians()
    {
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();

        for (int i = 0; i < 7; i++)
        {
            units.CreateUnit("Warrior", new HexCoord(i, 0), grid, ownerId: BarbarianSpawner.BarbarianOwnerId);
        }

        var coord = BarbarianSpawner.ChooseSpawn(5, grid, units, cities, new Random(0), maxBarbarians: 6);

        Assert.Null(coord);
    }

    // ── BarbarianSpawner.ChooseSpawn — distance constraint ───────────────────

    [Fact]
    public void Should_ReturnCoordWithSufficientDistanceFromPlayerUnit_When_SpawnOccurs()
    {
        var (grid, units, cities) = MakeSceneWithPlayerWarrior();
        var playerPos = units.AllUnits[0].Position; // (10, 10)

        // Turn 5: 5 % 5 == 0, no barbarians yet → should find a candidate.
        var coord = BarbarianSpawner.ChooseSpawn(5, grid, units, cities, new Random(42));

        Assert.NotNull(coord);
        Assert.True(coord!.Value.DistanceTo(playerPos) >= BarbarianSpawner.DefaultMinDistanceFromPlayer,
            $"Spawn coord {coord.Value} is only {coord.Value.DistanceTo(playerPos)} hexes from player at {playerPos}");
    }

    [Fact]
    public void Should_ReturnCoordWithSufficientDistanceFromPlayerCity_When_SpawnOccurs()
    {
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        // No player units, one player city at center.
        var cityPos = new HexCoord(10, 10);
        cities.CreateCity("Capital", cityPos, grid, ownerId: 0);

        var coord = BarbarianSpawner.ChooseSpawn(5, grid, units, cities, new Random(42));

        Assert.NotNull(coord);
        Assert.True(coord!.Value.DistanceTo(cityPos) >= BarbarianSpawner.DefaultMinDistanceFromPlayer,
            $"Spawn coord {coord.Value} is only {coord.Value.DistanceTo(cityPos)} hexes from city at {cityPos}");
    }

    [Fact]
    public void Should_ReturnNull_When_AllCellsAreWithinMinDistanceOfPlayerUnit()
    {
        // Use a tiny 3×3 grid; player unit at center (1,1).
        // All 9 cells are within distance 2 of (1,1) on a 3×3 grid,
        // so minDistanceFromPlayer=3 leaves no candidates.
        var grid = new HexGrid(3, 3);
        var units = new UnitManager();
        var cities = new CityManager();
        units.CreateUnit("Warrior", new HexCoord(1, 1), grid, ownerId: 0);

        var coord = BarbarianSpawner.ChooseSpawn(5, grid, units, cities, new Random(0), minDistanceFromPlayer: 3);

        Assert.Null(coord);
    }

    // ── BarbarianSpawner.ChooseSpawn — determinism ───────────────────────────

    [Fact]
    public void Should_ReturnSameCoord_When_SameSeedAndInputsUsedTwice()
    {
        var (grid, units, cities) = MakeSceneWithPlayerWarrior();

        var coord1 = BarbarianSpawner.ChooseSpawn(5, grid, units, cities, new Random(99));
        var coord2 = BarbarianSpawner.ChooseSpawn(5, grid, units, cities, new Random(99));

        Assert.Equal(coord1, coord2);
    }

    // ── BarbarianSpawner.TrySpawn ─────────────────────────────────────────────

    [Fact]
    public void Should_CreateWarriorWithOwnerId1_When_TrySpawnSucceeds()
    {
        var (grid, units, cities) = MakeSceneWithPlayerWarrior();

        var unit = BarbarianSpawner.TrySpawn(5, grid, units, cities, new Random(42));

        Assert.NotNull(unit);
        Assert.Equal("Warrior", unit!.UnitType);
        Assert.Equal(BarbarianSpawner.BarbarianOwnerId, unit.OwnerId);
        Assert.Equal(1, unit.OwnerId); // BarbarianOwnerId == 1
    }

    [Fact]
    public void Should_IncreaseBarbarianUnitCount_When_TrySpawnSucceeds()
    {
        var (grid, units, cities) = MakeSceneWithPlayerWarrior();
        int before = units.UnitsOwnedBy(BarbarianSpawner.BarbarianOwnerId).Count();

        BarbarianSpawner.TrySpawn(5, grid, units, cities, new Random(42));

        int after = units.UnitsOwnedBy(BarbarianSpawner.BarbarianOwnerId).Count();
        Assert.Equal(before + 1, after);
    }

    [Fact]
    public void Should_ReturnNull_When_TrySpawnConditionsNotMet()
    {
        var (grid, units, cities) = MakeSceneWithPlayerWarrior();

        // Turn 1 % 5 != 0 → no spawn.
        var unit = BarbarianSpawner.TrySpawn(1, grid, units, cities, new Random(42));

        Assert.Null(unit);
    }

    // ── BarbarianAi.TakeTurn — adjacent combat ───────────────────────────────

    [Fact]
    public void Should_InvokeCombat_When_BarbarianAdjacentToPlayerWarrior()
    {
        // Barbarian at (2,2), player warrior at (3,2) — distance 1, adjacent.
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        var barbarian = units.CreateUnit("Warrior", new HexCoord(2, 2), grid, ownerId: 1);
        var player = units.CreateUnit("Warrior", new HexCoord(3, 2), grid, ownerId: 0);

        int playerHpBefore = player.Hp;
        int barbarianMovBefore = barbarian.MovementRemaining;

        BarbarianAi.TakeTurn(grid, units, cities);

        // Combat must have occurred: either movement was spent (barbarian attacked)
        // or Hp changed on one of the units (or both).
        bool movementSpent = barbarian.MovementRemaining < barbarianMovBefore || barbarian.IsDead;
        bool hpChanged = player.Hp < playerHpBefore || (barbarian.IsDead && !units.AllUnits.Contains(barbarian));
        Assert.True(movementSpent || hpChanged,
            "Expected combat to occur (movement spent or HP change), but neither was observed.");
    }

    [Fact]
    public void Should_DecreaseCombatantHp_When_BarbarianAdjacentToPlayerWarrior()
    {
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        units.CreateUnit("Warrior", new HexCoord(2, 2), grid, ownerId: 1);
        var player = units.CreateUnit("Warrior", new HexCoord(3, 2), grid, ownerId: 0);

        int playerHpBefore = player.Hp;

        BarbarianAi.TakeTurn(grid, units, cities);

        // After combat: player Hp should decrease (warrior vs warrior, both deal 5 damage).
        Assert.True(player.Hp < playerHpBefore || !units.AllUnits.Contains(player),
            $"Expected player HP to decrease from {playerHpBefore}, but it is {player.Hp}");
    }

    // ── BarbarianAi.TakeTurn — movement toward player ────────────────────────

    [Fact]
    public void Should_MoveBarbarianCloser_When_BarbarianThreeHexesFromPlayerWarrior()
    {
        // Barbarian at (0,0), player warrior at (3,0): distance = 3 on this grid.
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        var barbarian = units.CreateUnit("Warrior", new HexCoord(0, 0), grid, ownerId: 1);
        var player = units.CreateUnit("Warrior", new HexCoord(3, 0), grid, ownerId: 0);

        int distanceBefore = barbarian.Position.DistanceTo(player.Position);

        BarbarianAi.TakeTurn(grid, units, cities);

        int distanceAfter = barbarian.Position.DistanceTo(player.Position);
        Assert.True(distanceAfter < distanceBefore,
            $"Expected barbarian to move closer (distance {distanceBefore} → <{distanceBefore}), but distance is {distanceAfter}");
    }

    // ── BarbarianAi.TakeTurn — no targets, no-op ─────────────────────────────

    [Fact]
    public void Should_NotThrow_When_NoPlayerUnitsOrCitiesExist()
    {
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        units.CreateUnit("Warrior", new HexCoord(5, 5), grid, ownerId: 1);

        // Must not throw.
        var exception = Record.Exception(() => BarbarianAi.TakeTurn(grid, units, cities));
        Assert.Null(exception);
    }

    [Fact]
    public void Should_NotMoveBarbarian_When_NoPlayerUnitsOrCitiesExist()
    {
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        var barbarian = units.CreateUnit("Warrior", new HexCoord(5, 5), grid, ownerId: 1);
        var positionBefore = barbarian.Position;

        BarbarianAi.TakeTurn(grid, units, cities);

        Assert.Equal(positionBefore, barbarian.Position);
    }

    // ── BarbarianAi.TakeTurn — targets city ──────────────────────────────────

    [Fact]
    public void Should_MoveBarbarianTowardCity_When_NoPlayerUnitsExist()
    {
        // Barbarian at (0,0), player city at (5,0): distance 5. No player units.
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        var barbarian = units.CreateUnit("Warrior", new HexCoord(0, 0), grid, ownerId: 1);
        cities.CreateCity("Capital", new HexCoord(5, 0), grid, ownerId: 0);

        int distanceBefore = barbarian.Position.DistanceTo(new HexCoord(5, 0));

        BarbarianAi.TakeTurn(grid, units, cities);

        int distanceAfter = barbarian.Position.DistanceTo(new HexCoord(5, 0));
        Assert.True(distanceAfter < distanceBefore,
            $"Expected barbarian to move toward city (distance {distanceBefore} → <{distanceBefore}), but distance is {distanceAfter}");
    }

    // ── BarbarianAi — no friendly fire ───────────────────────────────────────

    [Fact]
    public void Should_NotAttackOtherBarbarians_When_TwoBarbarianUnitsAreAdjacent()
    {
        // Two barbarians adjacent to each other, no player units.
        var grid = MakeGrid();
        var units = new UnitManager();
        var cities = new CityManager();
        var barb1 = units.CreateUnit("Warrior", new HexCoord(5, 5), grid, ownerId: 1);
        var barb2 = units.CreateUnit("Warrior", new HexCoord(6, 5), grid, ownerId: 1);

        int hp1Before = barb1.Hp;
        int hp2Before = barb2.Hp;

        BarbarianAi.TakeTurn(grid, units, cities);

        // Neither barbarian should have taken damage.
        Assert.Equal(hp1Before, barb1.Hp);
        Assert.Equal(hp2Before, barb2.Hp);
    }

    // ── GameSession integration — barbarian spawn after N turns ──────────────

    [Fact]
    public void Should_SpawnAtLeastOneBarbarian_When_EndTurnCalledFiveTimes()
    {
        // Default GameSession (20×20 gives plenty of space far from player units).
        // With spawnEveryNTurns=5, a barbarian should spawn when CurrentTurn reaches 5.
        // The default session uses a 2-player turn order [0, 1], so EndTurn must be called
        // twice per game turn: once for player 0, once for player 1.
        // 5 game turns = 10 EndTurn calls.
        var session = new GameSession(20, 20);

        for (int i = 0; i < 10; i++)
        {
            session.Turns.EndTurn();
        }

        int barbarianCount = session.Units.UnitsOwnedBy(1).Count();
        Assert.True(barbarianCount >= 1,
            $"Expected at least 1 barbarian after 10 EndTurn calls (5 game turns), but found {barbarianCount}");
    }

    [Fact]
    public void Should_NotThrow_When_EndTurnCalledTenTimes()
    {
        var session = new GameSession(20, 20);

        var exception = Record.Exception(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                session.Turns.EndTurn();
            }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Should_NotPlaceBarbariansOnPlayerUnitCells_When_EndTurnCalledTenTimes()
    {
        var session = new GameSession(20, 20);

        for (int i = 0; i < 10; i++)
        {
            session.Turns.EndTurn();
        }

        var playerPositions = session.Units.UnitsOwnedBy(0)
            .Select(u => u.Position)
            .ToHashSet();

        foreach (var barb in session.Units.UnitsOwnedBy(1))
        {
            Assert.False(playerPositions.Contains(barb.Position),
                $"Barbarian at {barb.Position} overlaps a player unit cell");
        }
    }

    [Fact]
    public void Should_PreservePlayerUnitExistenceOrConfirmCombatKill_When_EndTurnCalledTenTimes()
    {
        // Player units survive unless legitimately killed by barbarian combat.
        // We just verify no exception is thrown and the session state is consistent
        // (no unit occupies the same cell as another unit).
        var session = new GameSession(20, 20);

        for (int i = 0; i < 10; i++)
        {
            session.Turns.EndTurn();
        }

        // All unit positions must be distinct.
        var positions = session.Units.AllUnits.Select(u => u.Position).ToList();
        Assert.Equal(positions.Count, positions.Distinct().Count());
    }

    // ── BarbarianSpawner constant ─────────────────────────────────────────────

    [Fact]
    public void Should_HaveBarbarianOwnerIdEqualsOne()
    {
        Assert.Equal(1, BarbarianSpawner.BarbarianOwnerId);
    }
}
