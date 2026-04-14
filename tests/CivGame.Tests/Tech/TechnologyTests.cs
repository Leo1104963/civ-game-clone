using CivGame.Tech;

namespace CivGame.Tech.Tests;

/// <summary>
/// Tests for the Technology data model constructor validation and default values.
/// Covers issue #97 acceptance criteria: Technology constructor.
/// </summary>
public class TechnologyTests
{
    // ------------------------------------------------------------------ //
    // Happy path: valid construction                                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_StoreIdNameAndCost_When_ConstructedWithValidArgs()
    {
        var tech = new Technology("pottery", "Pottery", 20);

        Assert.Equal("pottery", tech.Id);
        Assert.Equal("Pottery", tech.Name);
        Assert.Equal(20, tech.ScienceCost);
    }

    [Fact]
    public void Should_HaveEmptyPrerequisites_When_ConstructedWithNoPrerequisites()
    {
        var tech = new Technology("pottery", "Pottery", 20);

        Assert.NotNull(tech.Prerequisites);
        Assert.Empty(tech.Prerequisites);
    }

    [Fact]
    public void Should_HaveEmptyPrerequisites_When_ConstructedWithNullPrerequisites()
    {
        var tech = new Technology("pottery", "Pottery", 20, null);

        Assert.NotNull(tech.Prerequisites);
        Assert.Empty(tech.Prerequisites);
    }

    [Fact]
    public void Should_StorePrerequisites_When_ConstructedWithPrerequisiteList()
    {
        var prereqs = new[] { "currency", "masonry" };
        var tech = new Technology("mathematics", "Mathematics", 60, prereqs);

        Assert.Contains("currency", tech.Prerequisites);
        Assert.Contains("masonry", tech.Prerequisites);
        Assert.Equal(2, tech.Prerequisites.Count);
    }

    // ------------------------------------------------------------------ //
    // Constructor validation: id                                           //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_ThrowArgumentException_When_IdIsNullOrWhitespace(string? id)
    {
        Assert.Throws<ArgumentException>(() => new Technology(id!, "Pottery", 20));
    }

    // ------------------------------------------------------------------ //
    // Constructor validation: name                                         //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_ThrowArgumentException_When_NameIsNullOrWhitespace(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Technology("pottery", name!, 20));
    }

    // ------------------------------------------------------------------ //
    // Constructor validation: scienceCost                                  //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_ThrowArgumentOutOfRangeException_When_ScienceCostIsNotPositive(int cost)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Technology("pottery", "Pottery", cost));
    }

    // ------------------------------------------------------------------ //
    // Prerequisites are read-only (not the original collection)           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ExposePrerequisitesAsReadOnly_When_Constructed()
    {
        var tech = new Technology("currency", "Currency", 30, new[] { "bronze-working" });

        // Prerequisites must implement IReadOnlyList<string> or IReadOnlyCollection<string>
        Assert.IsAssignableFrom<IReadOnlyList<string>>(tech.Prerequisites);
    }
}
