using CivGame.Tech;

namespace CivGame.Tech.Tests;

/// <summary>
/// Tests for TechCatalog: AllTechs count, GetById lookup, Validate(),
/// and prerequisite chain correctness for the hardcoded v4 catalog.
/// Covers issue #97 acceptance criteria: TechCatalog.
/// </summary>
public class TechCatalogTests
{
    // ------------------------------------------------------------------ //
    // AllTechs count                                                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ContainSevenTechs_When_CatalogLoaded()
    {
        Assert.Equal(7, TechCatalog.AllTechs.Count);
    }

    // ------------------------------------------------------------------ //
    // GetById — happy path                                                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnPottery_When_GetByIdCalledWithPottery()
    {
        var tech = TechCatalog.GetById("pottery");

        Assert.NotNull(tech);
        Assert.Equal("pottery", tech!.Id);
    }

    [Theory]
    [InlineData("pottery")]
    [InlineData("bronze-working")]
    [InlineData("writing")]
    [InlineData("masonry")]
    [InlineData("currency")]
    [InlineData("archery")]
    [InlineData("mathematics")]
    public void Should_ReturnNonNullTech_When_GetByIdCalledWithKnownId(string id)
    {
        var tech = TechCatalog.GetById(id);

        Assert.NotNull(tech);
    }

    // ------------------------------------------------------------------ //
    // GetById — case-insensitive                                           //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData("POTTERY")]
    [InlineData("Pottery")]
    [InlineData("pOtTeRy")]
    public void Should_ReturnPottery_When_GetByIdCalledCaseInsensitive(string id)
    {
        var tech = TechCatalog.GetById(id);

        Assert.NotNull(tech);
        Assert.Equal("pottery", tech!.Id);
    }

    // ------------------------------------------------------------------ //
    // GetById — unknown id returns null                                    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnNull_When_GetByIdCalledWithUnknownId()
    {
        var tech = TechCatalog.GetById("unknown-tech-xyz");

        Assert.Null(tech);
    }

    [Fact]
    public void Should_ReturnNull_When_GetByIdCalledWithEmptyString()
    {
        var tech = TechCatalog.GetById("");

        Assert.Null(tech);
    }

    // ------------------------------------------------------------------ //
    // Validate() — hardcoded catalog is consistent                        //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnEmptyErrors_When_HardcodedCatalogValidated()
    {
        var errors = TechCatalog.Validate();

        Assert.Empty(errors);
    }

    // ------------------------------------------------------------------ //
    // mathematics has both currency AND masonry as prerequisites           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveCurrencyPrerequisite_When_MathematicsInspected()
    {
        var math = TechCatalog.GetById("mathematics");

        Assert.NotNull(math);
        Assert.Contains("currency", math!.Prerequisites);
    }

    [Fact]
    public void Should_HaveMasonryPrerequisite_When_MathematicsInspected()
    {
        var math = TechCatalog.GetById("mathematics");

        Assert.NotNull(math);
        Assert.Contains("masonry", math!.Prerequisites);
    }

    [Fact]
    public void Should_HaveExactlyTwoPrerequisites_When_MathematicsInspected()
    {
        var math = TechCatalog.GetById("mathematics");

        Assert.NotNull(math);
        Assert.Equal(2, math!.Prerequisites.Count);
    }

    // ------------------------------------------------------------------ //
    // Techs with no prerequisites exist in catalog                        //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData("pottery")]
    [InlineData("bronze-working")]
    [InlineData("archery")]
    public void Should_HaveNoPrerequisites_When_EarlyTechInspected(string id)
    {
        var tech = TechCatalog.GetById(id);

        Assert.NotNull(tech);
        Assert.Empty(tech!.Prerequisites);
    }

    // ------------------------------------------------------------------ //
    // Validate() catches bad prerequisite references                      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnErrors_When_ValidateCalledOnCatalogWithDanglingPrereq()
    {
        // Build a mini-catalog with a bad prerequisite reference (not part of AllTechs).
        // We verify Validate() can detect the error — this tests the validation logic
        // is not just a stub that always returns empty.
        var badTech = new Technology("badtech", "Bad Tech", 10, new[] { "nonexistent-prereq" });
        var errors = TechCatalog.Validate(new[] { badTech });

        Assert.NotEmpty(errors);
    }
}
