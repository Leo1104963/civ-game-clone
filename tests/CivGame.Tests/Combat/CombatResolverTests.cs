using CivGame.Combat;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Combat.Tests;

/// <summary>
/// Failing tests for issue #69: deterministic combat resolver with HP and terrain defense.
/// All tests target the public API surface defined in the issue spec.
/// Tests avoid direct assignment of internal-set properties (Hp, MovementRemaining)
/// and instead manipulate state through the public/internal API only.
/// </summary>
public class CombatResolverTests
{
    // Grid size large enough that we never hit bounds issues.
    private static HexGrid MakeGrid(int size = 10) => new HexGrid(size, size);

    // (2,2) and its East neighbor (3,2) are adjacent (direction 0 = +1,0 in axial coords).
    private static readonly HexCoord AttackerPos = new HexCoord(2, 2);
    private static readonly HexCoord DefenderPos = new HexCoord(3, 2);
    // (0,0) is not adjacent to (2,2) — used for non-adjacent target tests.
    private static readonly HexCoord NonAdjacentPos = new HexCoord(0, 0);

    // ── Unit property tests ──────────────────────────────────────────────────

    [Fact]
    public void Should_HaveCorrectCombatStats_When_WarriorCreated()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var warrior = manager.CreateUnit("Warrior", AttackerPos, grid);

        Assert.Equal(10, warrior.MaxHp);
        Assert.Equal(10, warrior.Hp);
        Assert.Equal(5, warrior.CombatStrength);
    }

    [Fact]
    public void Should_HaveCorrectCombatStats_When_SettlerCreated()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var settler = manager.CreateUnit("Settler", AttackerPos, grid);

        Assert.Equal(5, settler.MaxHp);
        Assert.Equal(5, settler.Hp);
        Assert.Equal(0, settler.CombatStrength);
    }

    [Fact]
    public void Should_ReturnFalseIsDead_When_UnitAtFullHp()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var warrior = manager.CreateUnit("Warrior", AttackerPos, grid);

        Assert.False(warrior.IsDead);
    }

    [Fact]
    public void Should_ReturnTrueIsDead_When_HpReducedToZeroByTryAttack()
    {
        // Settler (Hp=5, CombatStrength=0) is one-shot by Warrior via TryAttack,
        // which applies HP changes. After TryAttack, settler.Hp == 0 so IsDead == true.
        var grid = MakeGrid();
        var manager = new UnitManager();
        var warrior = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var settler = manager.CreateUnit("Settler", DefenderPos, grid, ownerId: 2);

        manager.TryAttack(warrior, DefenderPos, grid);

        // TryAttack applies Hp = result.DefenderHpAfter to the settler.
        Assert.True(settler.IsDead);
    }

    // ── CombatResolver.Resolve – Grass terrain ───────────────────────────────

    [Fact]
    public void Should_DealSymmetricDamage_When_TwoWarriorsOnGrass()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var defender = manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);

        var result = CombatResolver.Resolve(attacker, defender, TerrainType.Grass);

        Assert.Equal(5, result.AttackerHpAfter);
        Assert.Equal(5, result.DefenderHpAfter);
    }

    [Fact]
    public void Should_SetKilledFlagsFalse_When_BothWarriorsOnGrass()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var defender = manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);

        var result = CombatResolver.Resolve(attacker, defender, TerrainType.Grass);

        Assert.False(result.AttackerKilled);
        Assert.False(result.DefenderKilled);
    }

    // ── CombatResolver.Resolve – Forest terrain ──────────────────────────────

    [Fact]
    public void Should_ApplyForestBonus_When_DefenderInForest()
    {
        // Forest: effectiveDefenderStr = 5 * 1.25 = 6.25
        // attackerDamage = round(5 * 6.25 / 5) = round(6.25) = 6 → AttackerHpAfter = 10-6 = 4
        // defenderDamage = round(5 * 5 / 6.25) = round(4.0) = 4  → DefenderHpAfter = 10-4 = 6
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var defender = manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);

        var result = CombatResolver.Resolve(attacker, defender, TerrainType.Forest);

        Assert.Equal(4, result.AttackerHpAfter);
        Assert.Equal(6, result.DefenderHpAfter);
    }

    // ── CombatResolver.Resolve – Settler (CombatStrength == 0) ──────────────

    [Fact]
    public void Should_OneShotSettler_When_WarriorAttacksOnGrass()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var warrior = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var settler = manager.CreateUnit("Settler", DefenderPos, grid, ownerId: 2);

        var result = CombatResolver.Resolve(warrior, settler, TerrainType.Grass);

        Assert.Equal(0, result.DefenderHpAfter);
        Assert.True(result.DefenderKilled);
    }

    [Fact]
    public void Should_LeaveAttackerUntouched_When_DefenderHasCombatStrengthZero()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var warrior = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var settler = manager.CreateUnit("Settler", DefenderPos, grid, ownerId: 2);

        var result = CombatResolver.Resolve(warrior, settler, TerrainType.Grass);

        Assert.Equal(warrior.Hp, result.AttackerHpAfter);
        Assert.False(result.AttackerKilled);
    }

    // ── Damage floor: min 1 per attack ───────────────────────────────────────

    [Fact]
    public void Should_DealAtLeastOneDamage_When_AttackerVsDefenderOnGrass()
    {
        // Equal-strength warriors: each deals exactly 5, both well above 1 floor.
        // Confirms floor is respected (damage is never 0).
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var defender = manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);

        var result = CombatResolver.Resolve(attacker, defender, TerrainType.Grass);

        // damage to attacker = attacker.Hp - AttackerHpAfter >= 1
        Assert.True(attacker.Hp - result.AttackerHpAfter >= 1);
        // damage to defender = defender.Hp - DefenderHpAfter >= 1
        Assert.True(defender.Hp - result.DefenderHpAfter >= 1);
    }

    // ── HP never goes negative ────────────────────────────────────────────────

    [Fact]
    public void Should_NeverProduceNegativeDefenderHp_When_SettlerOneShot()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var warrior = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var settler = manager.CreateUnit("Settler", DefenderPos, grid, ownerId: 2);

        var result = CombatResolver.Resolve(warrior, settler, TerrainType.Grass);

        Assert.True(result.DefenderHpAfter >= 0);
    }

    // ── Determinism ──────────────────────────────────────────────────────────

    [Fact]
    public void Should_ProduceSameResult_When_CalledRepeatedly()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var defender = manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);

        var first = CombatResolver.Resolve(attacker, defender, TerrainType.Grass);
        for (int i = 0; i < 99; i++)
        {
            var subsequent = CombatResolver.Resolve(attacker, defender, TerrainType.Grass);
            Assert.Equal(first, subsequent);
        }
    }

    // ── TryAttack null-return precondition checks ────────────────────────────

    [Fact]
    public void Should_ReturnNull_When_AttackerAndDefenderSameOwner()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 1);

        var result = manager.TryAttack(attacker, DefenderPos, grid);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnNull_When_TargetIsNonAdjacent()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        manager.CreateUnit("Warrior", NonAdjacentPos, grid, ownerId: 2);

        var result = manager.TryAttack(attacker, NonAdjacentPos, grid);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnNull_When_TargetHexIsEmpty()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);

        var result = manager.TryAttack(attacker, DefenderPos, grid);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnNull_When_AttackerHasNoMovementRemaining()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        // Use a large grid so we can move without hitting bounds.
        var bigGrid = MakeGrid(20);
        var attacker = manager.CreateUnit("Warrior", AttackerPos, bigGrid, ownerId: 1);
        manager.CreateUnit("Warrior", DefenderPos, bigGrid, ownerId: 2);

        // Exhaust Warrior's 2 movement points via two moves away from defender.
        // Move West twice: (2,2) -> (1,2) -> (0,2)
        attacker.TryMoveTo(new HexCoord(1, 2), bigGrid, manager);
        attacker.TryMoveTo(new HexCoord(0, 2), bigGrid, manager);
        Assert.Equal(0, attacker.MovementRemaining);

        // Defender is now at (3,2), no longer adjacent to attacker at (0,2) — but
        // that tests non-adjacent too. Place a separate defender adjacent to final attacker pos.
        // Instead test with original positions: create a fresh manager for cleaner setup.
        var grid2 = MakeGrid(20);
        var manager2 = new UnitManager();
        var attacker2 = manager2.CreateUnit("Warrior", AttackerPos, grid2, ownerId: 1);
        manager2.CreateUnit("Warrior", DefenderPos, grid2, ownerId: 2);
        // Exhaust movement by moving away from defender side, then back... actually
        // just use TryMoveTo north twice: (2,2)->(2,1)->(2,0).
        attacker2.TryMoveTo(new HexCoord(2, 1), grid2, manager2);
        attacker2.TryMoveTo(new HexCoord(2, 0), grid2, manager2);
        Assert.Equal(0, attacker2.MovementRemaining);

        // Now move attacker back adjacent to defender is impossible (no movement).
        // Test TryAttack on a fresh unit that has been exhausted. We verify TryAttack
        // returns null for 0 movement using the movement-exhausted attacker2 on any target.
        // Defender is still at DefenderPos in manager2 — attacker is at (2,0) which is not
        // adjacent. So we need an adjacent defender. Create one at (2,0)'s east neighbor (3,0).
        manager2.CreateUnit("Warrior", new HexCoord(3, 0), grid2, ownerId: 2);

        var result = manager2.TryAttack(attacker2, new HexCoord(3, 0), grid2);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnNull_When_AttackerHasCombatStrengthZero()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        // Settler has CombatStrength == 0.
        var settler = manager.CreateUnit("Settler", AttackerPos, grid, ownerId: 1);
        manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);

        var result = manager.TryAttack(settler, DefenderPos, grid);

        Assert.Null(result);
    }

    // ── TryAttack movement cost ───────────────────────────────────────────────

    [Fact]
    public void Should_DecrementMovementByOne_When_TryAttackSucceeds()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);
        int movementBefore = attacker.MovementRemaining;

        manager.TryAttack(attacker, DefenderPos, grid);

        Assert.Equal(movementBefore - 1, attacker.MovementRemaining);
    }

    // ── Dead unit removal after TryAttack ────────────────────────────────────

    [Fact]
    public void Should_RemoveDeadDefender_When_TryAttackKillsDefender()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        // Settler has Hp=5, CombatStrength=0: warrior one-shots it.
        manager.CreateUnit("Settler", DefenderPos, grid, ownerId: 2);

        manager.TryAttack(attacker, DefenderPos, grid);

        Assert.Null(manager.GetUnitAt(DefenderPos));
    }

    [Fact]
    public void Should_NotBeInAllUnits_When_DefenderKilledByTryAttack()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var defender = manager.CreateUnit("Settler", DefenderPos, grid, ownerId: 2);

        manager.TryAttack(attacker, DefenderPos, grid);

        Assert.DoesNotContain(defender, manager.AllUnits);
    }

    [Fact]
    public void Should_RemoveDeadAttacker_When_TryAttackKillsAttacker()
    {
        // Two warriors on Grass each deal 5 damage. After round 1: both at 5 Hp.
        // Reset attacker movement, fight again → both at 0 Hp (both killed).
        // We verify attacker is removed from AllUnits.
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        var defender = manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);

        // Round 1: both survive at 5 Hp.
        var r1 = manager.TryAttack(attacker, DefenderPos, grid);
        Assert.NotNull(r1);
        Assert.Equal(5, r1!.Value.AttackerHpAfter);
        Assert.Equal(5, r1!.Value.DefenderHpAfter);

        // Restore attacker movement for round 2.
        attacker.ResetMovement();

        // Round 2: both at 5 Hp, 5 damage each → both reach 0.
        manager.TryAttack(attacker, DefenderPos, grid);

        Assert.DoesNotContain(attacker, manager.AllUnits);
    }

    [Fact]
    public void Should_ReturnNonNull_When_TryAttackSucceeds()
    {
        var grid = MakeGrid();
        var manager = new UnitManager();
        var attacker = manager.CreateUnit("Warrior", AttackerPos, grid, ownerId: 1);
        manager.CreateUnit("Warrior", DefenderPos, grid, ownerId: 2);

        var result = manager.TryAttack(attacker, DefenderPos, grid);

        Assert.NotNull(result);
    }
}
