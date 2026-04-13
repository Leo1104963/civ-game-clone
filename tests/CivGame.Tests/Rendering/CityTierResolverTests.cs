using CivGame.Buildings;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for CityTierResolver.ResolveTier (issue #91).
/// Covers all tier boundaries and null guard.
/// </summary>
public class CityTierResolverTests
{
    private static City CreateCity()
    {
        var grid = new HexGrid(10, 10);
        var manager = new CityManager();
        return manager.CreateCity("TestCity", new HexCoord(0, 0), grid);
    }

    /// <summary>
    /// Complete exactly <paramref name="count"/> buildings on the city.
    /// Uses Granary repeatedly by cycling unique positions via new cities
    /// — but since we only need CompletedBuildings.Count, we drive production
    /// directly on one city using a cost-1 building definition.
    /// </summary>
    private static City CreateCityWithBuildings(int count)
    {
        var city = CreateCity();

        for (int i = 0; i < count; i++)
        {
            // StartBuilding checks HasBuilding by name, so use a unique name per iteration
            var unique = new BuildingDefinition($"Building{i}", buildCost: 1);
            city.StartBuilding(unique);
            city.TickProduction(); // buildCost=1, completes immediately
        }

        return city;
    }

    // --- CityVisualTier enum ---

    [Fact]
    public void Should_ExposeOutpostTownCityValues_When_EnumDefined()
    {
        var values = Enum.GetValues<CityVisualTier>();
        Assert.Contains(CityVisualTier.Outpost, values);
        Assert.Contains(CityVisualTier.Town, values);
        Assert.Contains(CityVisualTier.City, values);
    }

    // --- Tier boundaries ---

    [Fact]
    public void Should_ReturnOutpost_When_ZeroBuildings()
    {
        var city = CreateCityWithBuildings(0);
        Assert.Equal(CityVisualTier.Outpost, CityTierResolver.ResolveTier(city));
    }

    [Fact]
    public void Should_ReturnTown_When_OneBuilding()
    {
        var city = CreateCityWithBuildings(1);
        Assert.Equal(CityVisualTier.Town, CityTierResolver.ResolveTier(city));
    }

    [Fact]
    public void Should_ReturnTown_When_TwoBuildings()
    {
        var city = CreateCityWithBuildings(2);
        Assert.Equal(CityVisualTier.Town, CityTierResolver.ResolveTier(city));
    }

    [Fact]
    public void Should_ReturnCity_When_ThreeBuildings()
    {
        var city = CreateCityWithBuildings(3);
        Assert.Equal(CityVisualTier.City, CityTierResolver.ResolveTier(city));
    }

    [Fact]
    public void Should_ReturnCity_When_FiveBuildings()
    {
        var city = CreateCityWithBuildings(5);
        Assert.Equal(CityVisualTier.City, CityTierResolver.ResolveTier(city));
    }

    // --- Null guard ---

    [Fact]
    public void Should_ThrowArgumentNullException_When_CityIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => CityTierResolver.ResolveTier(null!));
    }
}
