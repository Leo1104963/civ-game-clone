using CivGame.Buildings;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Cities.Tests;

/// <summary>
/// Tests for the yield-driven City.TickProduction(int productionYield) overload
/// introduced by issue #71.
/// </summary>
public class CityYieldTests
{
    private static City CreateCity() => new City("TestCity", new HexCoord(0, 0));

    // ------------------------------------------------------------------ //
    // TickProduction(0) does not advance production                        //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_NotAdvanceProduction_When_TickProductionCalledWithZero()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);
        int initialRemaining = city.CurrentProduction!.TurnsRemaining;

        city.TickProduction(0);

        Assert.Equal(initialRemaining, city.CurrentProduction!.TurnsRemaining);
    }

    // ------------------------------------------------------------------ //
    // TickProduction(3) on fresh Granary (cost 5): remaining = 2           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReduceRemainingCostToTwo_When_TickProduction3OnFreshGranary()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        city.TickProduction(3);

        Assert.NotNull(city.CurrentProduction);
        Assert.Equal(2, city.CurrentProduction!.TurnsRemaining);
    }

    // ------------------------------------------------------------------ //
    // TickProduction(5) on fresh Granary: completes, CurrentProduction null //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_CompleteGranary_When_TickProduction5OnFreshGranary()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        city.TickProduction(5);

        Assert.Null(city.CurrentProduction);
        Assert.Single(city.CompletedBuildings);
        Assert.Equal("Granary", city.CompletedBuildings[0].Name);
    }

    // ------------------------------------------------------------------ //
    // TickProduction(7) on fresh Granary: completes, overflow discarded    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_CompleteGranaryAndDiscardOverflow_When_TickProduction7OnFreshGranary()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        city.TickProduction(7);

        Assert.Null(city.CurrentProduction);
        Assert.Single(city.CompletedBuildings);
        Assert.Equal("Granary", city.CompletedBuildings[0].Name);
    }
}
