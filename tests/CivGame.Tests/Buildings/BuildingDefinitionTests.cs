using CivGame.Buildings;

namespace CivGame.Buildings.Tests;

public class BuildingDefinitionTests
{
    [Fact]
    public void Should_StoreNameAndBuildCost_When_ConstructedWithValidArgs()
    {
        var def = new BuildingDefinition("Granary", 5);

        Assert.Equal("Granary", def.Name);
        Assert.Equal(5, def.BuildCost);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_ThrowArgumentException_When_NameIsNullOrWhitespace(string? name)
    {
        Assert.Throws<ArgumentException>(() => new BuildingDefinition(name!, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_ThrowArgumentOutOfRangeException_When_BuildCostIsNotPositive(int cost)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BuildingDefinition("Test", cost));
    }
}
