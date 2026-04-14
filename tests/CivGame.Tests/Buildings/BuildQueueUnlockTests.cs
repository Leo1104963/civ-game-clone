using CivGame.Buildings;
using CivGame.Cities;
using CivGame.Tech;
using CivGame.World;

namespace CivGame.Buildings.Tests;

/// <summary>
/// Failing tests for City.TryStartBuilding tech-unlock wiring per issue #99.
/// Tests compile but fail until TryStartBuilding is implemented.
/// </summary>
public class BuildQueueUnlockTests
{
    private const int PlayerId = 0;

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

    private static City CreateCity() =>
        new City("TestCity", new HexCoord(0, 0), PlayerId);

    // ------------------------------------------------------------------ //
    // TryStartBuilding — tech-locked returns structured failure            //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnLockedFailure_When_TryStartBuildingLibraryBeforeWritingResearched()
    {
        var (_, service) = CreateUnlocks();
        var city = CreateCity();

        var result = city.TryStartBuilding(BuildingCatalog.Library, service, PlayerId);

        Assert.False(result.Success);
        Assert.Equal("Requires Writing", result.LockedReason);
    }

    [Fact]
    public void Should_ReturnSuccess_When_TryStartBuildingLibraryAfterWritingCompleted()
    {
        var (research, service) = CreateUnlocks();
        CompleteResearch(research, PlayerId, "writing");
        var city = CreateCity();

        var result = city.TryStartBuilding(BuildingCatalog.Library, service, PlayerId);

        Assert.True(result.Success);
        Assert.Null(result.LockedReason);
    }

    // ------------------------------------------------------------------ //
    // TryStartBuilding — null unlocks bypasses tech check                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnSuccess_When_TryStartBuildingWithNullUnlocksRegardlessOfTechState()
    {
        var city = CreateCity();

        var result = city.TryStartBuilding(BuildingCatalog.Granary, null, PlayerId);

        Assert.True(result.Success);
        Assert.Null(result.LockedReason);
    }

    // ------------------------------------------------------------------ //
    // TryStartBuilding — existing-state checks take priority              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnAlreadyProducing_When_TryStartBuildingLibraryAlreadyInCurrentProduction()
    {
        // Even though Writing is not researched, "Already producing" wins over "Requires Writing"
        var (_, service) = CreateUnlocks();
        var city = CreateCity();

        // Queue the library via null-bypass so it's in CurrentProduction without needing the tech
        city.TryStartBuilding(BuildingCatalog.Library, null, PlayerId);

        // Now try again with the unlock service armed — should get "Already producing"
        var result = city.TryStartBuilding(BuildingCatalog.Library, service, PlayerId);

        Assert.False(result.Success);
        Assert.Equal("Already producing", result.LockedReason);
    }

    [Fact]
    public void Should_ReturnAlreadyBuilt_When_TryStartBuildingGranaryAlreadyInCompletedBuildings()
    {
        var (research, service) = CreateUnlocks();
        CompleteResearch(research, PlayerId, "pottery");
        var city = CreateCity();
        city.AddCompletedBuilding(BuildingCatalog.Granary);

        var result = city.TryStartBuilding(BuildingCatalog.Granary, service, PlayerId);

        Assert.False(result.Success);
        Assert.Equal("Already built", result.LockedReason);
    }

    // ------------------------------------------------------------------ //
    // StartBuilding wrapper — back-compat preserved                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnTrue_When_ExistingStartBuildingCalledOnFreshCity()
    {
        var city = CreateCity();

        bool result = city.StartBuilding(BuildingCatalog.Granary);

        Assert.True(result);
    }
}
