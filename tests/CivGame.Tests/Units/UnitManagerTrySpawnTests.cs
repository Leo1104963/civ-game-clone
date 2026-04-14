using CivGame.Tech;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Units.Tests;

/// <summary>
/// Failing tests for UnitManager.TrySpawnUnit tech-unlock wiring per issue #99.
/// Tests compile but fail until TrySpawnUnit is implemented and Horseman is added to CreateUnit.
/// </summary>
public class UnitManagerTrySpawnTests
{
    private const int PlayerId = 0;

    private static HexGrid CreateGrid()
    {
        var grid = new HexGrid(5, 5);
        return grid;
    }

    private static HexCoord Center() => new HexCoord(2, 2);

    private static (ResearchManager research, TechUnlockService service) CreateUnlocks()
    {
        var research = new ResearchManager();
        var service = new TechUnlockService(research);
        return (research, service);
    }

    private static void CompleteResearch(ResearchManager research, int playerId, string techId)
    {
        var tech = TechCatalog.GetById(techId)
            ?? throw new InvalidOperationException($"Unknown tech: {techId}");
        bool started = research.StartResearch(playerId, techId);
        if (!started)
            throw new InvalidOperationException(
                $"Could not start research for '{techId}' — check prerequisites or state.");
        research.TickFor(playerId, tech.ScienceCost);
    }

    // ------------------------------------------------------------------ //
    // Ungated units — always spawn regardless of tech state               //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnSuccessWithUnit_When_TrySpawnWarriorWithNoTechsResearched()
    {
        var (_, service) = CreateUnlocks();
        var manager = new UnitManager();
        var grid = CreateGrid();

        var result = manager.TrySpawnUnit("Warrior", Center(), grid, PlayerId, service);

        Assert.NotNull(result.Unit);
        Assert.Null(result.LockedReason);
    }

    [Fact]
    public void Should_ReturnSuccessWithUnit_When_TrySpawnSettlerWithNoTechsResearched()
    {
        var (_, service) = CreateUnlocks();
        var manager = new UnitManager();
        var grid = CreateGrid();

        var result = manager.TrySpawnUnit("Settler", Center(), grid, PlayerId, service);

        Assert.NotNull(result.Unit);
        Assert.Null(result.LockedReason);
    }

    // ------------------------------------------------------------------ //
    // Tech-gated unit — locked before research                            //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnLockedFailure_When_TrySpawnHorsemanBeforeHorsebackRidingResearched()
    {
        var (_, service) = CreateUnlocks();
        var manager = new UnitManager();
        var grid = CreateGrid();

        var result = manager.TrySpawnUnit("Horseman", Center(), grid, PlayerId, service);

        Assert.Null(result.Unit);
        Assert.Equal("Requires Horseback Riding", result.LockedReason);
    }

    // ------------------------------------------------------------------ //
    // Tech-gated unit — succeeds after research                           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnSuccessWithUnit_When_TrySpawnHorsemanAfterHorsebackRidingCompleted()
    {
        var (research, service) = CreateUnlocks();
        CompleteResearch(research, PlayerId, "horseback-riding");
        var manager = new UnitManager();
        var grid = CreateGrid();

        var result = manager.TrySpawnUnit("Horseman", Center(), grid, PlayerId, service);

        Assert.NotNull(result.Unit);
        Assert.Null(result.LockedReason);
    }

    // ------------------------------------------------------------------ //
    // Null unlocks — bypass mode, programmer errors still throw           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ThrowArgumentException_When_TrySpawnSpearmanWithNullUnlocks()
    {
        // "Spearman" is gated by bronze-working but unknown to CreateUnit's switch.
        // With null unlocks, gate check is skipped and CreateUnit throws ArgumentException.
        var manager = new UnitManager();
        var grid = CreateGrid();

        Assert.Throws<ArgumentException>(() =>
            manager.TrySpawnUnit("Spearman", Center(), grid, PlayerId, null));
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_TrySpawnWarriorOnOccupiedCell()
    {
        var manager = new UnitManager();
        var grid = CreateGrid();
        // Occupy the center cell first
        manager.CreateUnit("Warrior", Center(), grid, PlayerId);

        Assert.Throws<InvalidOperationException>(() =>
            manager.TrySpawnUnit("Warrior", Center(), grid, PlayerId, null));
    }

    [Fact]
    public void Should_ThrowArgumentException_When_TrySpawnWarriorOutOfBounds()
    {
        var manager = new UnitManager();
        var grid = CreateGrid();
        var outOfBounds = new HexCoord(99, 99);

        Assert.Throws<ArgumentException>(() =>
            manager.TrySpawnUnit("Warrior", outOfBounds, grid, PlayerId, null));
    }

    // ------------------------------------------------------------------ //
    // Gate check runs before CreateUnit delegation                        //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnLockedFailure_NotThrow_When_TrySpawnGatedUnitWithArmedUnlocks()
    {
        // "Spearman" tag "unit:Spearman" is gated by bronze-working.
        // With an armed (non-null) unlocks, the gate check fires first and returns
        // (null, "Requires Bronze Working") — CreateUnit is never called, so no throw.
        var (_, service) = CreateUnlocks();
        var manager = new UnitManager();
        var grid = CreateGrid();

        var result = manager.TrySpawnUnit("Spearman", Center(), grid, PlayerId, service);

        Assert.Null(result.Unit);
        Assert.Equal("Requires Bronze Working", result.LockedReason);
    }
}
