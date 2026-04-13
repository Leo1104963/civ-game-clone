using CivGame.Units;

namespace CivGame.Rendering.Tests;

// NOTE: this file intentionally does NOT import CivGame.World (no VisibilityMap).
// CombatTargetResolver is fog-agnostic — any fog filtering is the caller's responsibility.

/// <summary>
/// Tests for CombatTargetResolver.CanAttackFrom (issue #94).
/// Uses adjacency semantics (mirrors TryAttack), not reachable-set semantics.
/// No VisibilityMap is constructed anywhere in this file.
/// </summary>
public class CombatTargetResolverTests
{
    private static (CivGame.World.HexGrid Grid, UnitManager Units) Setup()
    {
        var grid = new CivGame.World.HexGrid(5, 5);
        var units = new UnitManager();
        return (grid, units);
    }

    // 1. Adjacent enemy, warrior with movement → true
    [Fact]
    public void Should_ReturnTrue_When_AdjacentEnemyAndWarriorHasMovement()
    {
        var (grid, units) = Setup();
        var attacker = units.CreateUnit("Warrior", new CivGame.World.HexCoord(2, 2), grid, ownerId: 0);
        var adjacent = attacker.Position.Neighbor(0);
        units.CreateUnit("Warrior", adjacent, grid, ownerId: 1);

        Assert.True(CombatTargetResolver.CanAttackFrom(attacker, adjacent, grid, units));
    }

    // 2. Adjacent friendly → false
    [Fact]
    public void Should_ReturnFalse_When_AdjacentTileHasFriendlyUnit()
    {
        var (grid, units) = Setup();
        var attacker = units.CreateUnit("Warrior", new CivGame.World.HexCoord(2, 2), grid, ownerId: 0);
        var adjacent = attacker.Position.Neighbor(0);
        units.CreateUnit("Warrior", adjacent, grid, ownerId: 0);

        Assert.False(CombatTargetResolver.CanAttackFrom(attacker, adjacent, grid, units));
    }

    // 3. Non-adjacent enemy → false (regression guard against using reachable set)
    [Fact]
    public void Should_ReturnFalse_When_EnemyIsNotAdjacent()
    {
        var (grid, units) = Setup();
        var attacker = units.CreateUnit("Warrior", new CivGame.World.HexCoord(0, 0), grid, ownerId: 0);
        var twoAway = new CivGame.World.HexCoord(2, 0);
        units.CreateUnit("Warrior", twoAway, grid, ownerId: 1);

        Assert.False(CombatTargetResolver.CanAttackFrom(attacker, twoAway, grid, units));
    }

    // 4. Empty adjacent tile → false
    [Fact]
    public void Should_ReturnFalse_When_AdjacentTileIsEmpty()
    {
        var (grid, units) = Setup();
        var attacker = units.CreateUnit("Warrior", new CivGame.World.HexCoord(2, 2), grid, ownerId: 0);
        var adjacent = attacker.Position.Neighbor(0);

        Assert.False(CombatTargetResolver.CanAttackFrom(attacker, adjacent, grid, units));
    }

    // 5. Zero CombatStrength (Settler) → false
    [Fact]
    public void Should_ReturnFalse_When_AttackerHasZeroCombatStrength()
    {
        var (grid, units) = Setup();
        var settler = units.CreateUnit("Settler", new CivGame.World.HexCoord(2, 2), grid, ownerId: 0);
        var adjacent = settler.Position.Neighbor(0);
        units.CreateUnit("Warrior", adjacent, grid, ownerId: 1);

        Assert.False(CombatTargetResolver.CanAttackFrom(settler, adjacent, grid, units));
    }

    // 6. Zero MovementRemaining → false
    [Fact]
    public void Should_ReturnFalse_When_AttackerHasZeroMovementRemaining()
    {
        var (grid, units) = Setup();
        var attacker = units.CreateUnit("Warrior", new CivGame.World.HexCoord(2, 2), grid, ownerId: 0);
        var adjacent = attacker.Position.Neighbor(0);
        units.CreateUnit("Warrior", adjacent, grid, ownerId: 1);

        attacker.MovementRemaining = 0;

        Assert.False(CombatTargetResolver.CanAttackFrom(attacker, adjacent, grid, units));
    }

    // 7. Out-of-bounds target → false
    [Fact]
    public void Should_ReturnFalse_When_TargetCoordIsOutOfBounds()
    {
        var (grid, units) = Setup();
        var attacker = units.CreateUnit("Warrior", new CivGame.World.HexCoord(0, 0), grid, ownerId: 0);
        var oob = new CivGame.World.HexCoord(-1, -1);

        Assert.False(CombatTargetResolver.CanAttackFrom(attacker, oob, grid, units));
    }

    // 8. Dead attacker → false
    [Fact]
    public void Should_ReturnFalse_When_AttackerIsDead()
    {
        var (grid, units) = Setup();
        var attacker = units.CreateUnit("Warrior", new CivGame.World.HexCoord(2, 2), grid, ownerId: 0);
        var adjacent = attacker.Position.Neighbor(0);
        units.CreateUnit("Warrior", adjacent, grid, ownerId: 1);

        attacker.Hp = 0;

        Assert.False(CombatTargetResolver.CanAttackFrom(attacker, adjacent, grid, units));
    }

    // 9. Fog-agnostic tripwire: enemy on unseen tile → true (resolver never checks fog)
    [Fact]
    public void CanAttackFrom_ReturnsTrue_ForEnemyOnUnseenTile_BecauseResolverIsFogAgnostic()
    {
        // No VisibilityMap constructed anywhere in this test.
        // The resolver must not internally consult fog state.
        var (grid, units) = Setup();
        var attacker = units.CreateUnit("Warrior", new CivGame.World.HexCoord(2, 2), grid, ownerId: 0);
        var adjacent = attacker.Position.Neighbor(0);
        units.CreateUnit("Warrior", adjacent, grid, ownerId: 1);

        Assert.True(CombatTargetResolver.CanAttackFrom(attacker, adjacent, grid, units));
    }
}
