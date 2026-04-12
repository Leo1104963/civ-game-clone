using CivGame.Buildings;

namespace CivGame.Buildings.Tests;

public class BuildingCatalogTests
{
    [Fact]
    public void Should_ReturnGranaryDefinition_When_AccessingGranaryProperty()
    {
        var granary = BuildingCatalog.Granary;

        Assert.NotNull(granary);
        Assert.Equal("Granary", granary.Name);
        Assert.Equal(5, granary.BuildCost);
    }

    [Fact]
    public void Should_ReturnGranary_When_LookingUpByExactName()
    {
        var result = BuildingCatalog.GetByName("Granary");

        Assert.NotNull(result);
        Assert.Equal("Granary", result!.Name);
    }

    [Theory]
    [InlineData("granary")]
    [InlineData("GRANARY")]
    [InlineData("Granary")]
    public void Should_ReturnGranary_When_LookingUpCaseInsensitive(string name)
    {
        var result = BuildingCatalog.GetByName(name);

        Assert.NotNull(result);
        Assert.Equal("Granary", result!.Name);
    }

    [Fact]
    public void Should_ReturnNull_When_LookingUpNonexistentBuilding()
    {
        var result = BuildingCatalog.GetByName("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnSameInstance_When_AccessingGranaryMultipleTimes()
    {
        var first = BuildingCatalog.Granary;
        var second = BuildingCatalog.Granary;

        Assert.Same(first, second);
    }
}
