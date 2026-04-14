using CivGame.Buildings;

namespace CivGame.Buildings.Tests;

/// <summary>
/// Tests for the ScienceYield property on BuildingDefinition.
/// Covers issue #97 acceptance criteria: BuildingDefinition.ScienceYield.
/// </summary>
public class BuildingDefinitionScienceTests
{
    // ------------------------------------------------------------------ //
    // ScienceYield property exists and is readable                        //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_StoreAndReturnScienceYield_When_LibraryConstructed()
    {
        var library = new BuildingDefinition("Library", 8, scienceYield: 2);

        Assert.Equal(2, library.ScienceYield);
    }

    [Fact]
    public void Should_DefaultScienceYieldToZero_When_ConstructedWithoutScienceYield()
    {
        // Two-arg constructor must keep ScienceYield == 0 for backwards compat.
        var granary = new BuildingDefinition("Granary", 5);

        Assert.Equal(0, granary.ScienceYield);
    }

    // ------------------------------------------------------------------ //
    // ScienceYield zero is allowed                                         //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_AllowZeroScienceYield_When_ConstructedWithZero()
    {
        var building = new BuildingDefinition("Barracks", 6, scienceYield: 0);

        Assert.Equal(0, building.ScienceYield);
    }

    // ------------------------------------------------------------------ //
    // Negative ScienceYield throws ArgumentOutOfRangeException            //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    public void Should_ThrowArgumentOutOfRangeException_When_ScienceYieldIsNegative(int scienceYield)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BuildingDefinition("Library", 8, scienceYield: scienceYield));
    }

    // ------------------------------------------------------------------ //
    // Existing name/buildCost validation still works with 3-arg form      //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_ThrowArgumentException_When_NameIsNullOrWhitespace_ThreeArgForm(string? name)
    {
        Assert.Throws<ArgumentException>(() => new BuildingDefinition(name!, 5, scienceYield: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_ThrowArgumentOutOfRangeException_When_BuildCostIsNotPositive_ThreeArgForm(int cost)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BuildingDefinition("Library", cost, scienceYield: 1));
    }
}
