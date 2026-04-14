using CivGame.Tech;

namespace CivGame.Tech.Tests;

/// <summary>
/// Failing tests for TechUnlockService per issue #99.
/// Tests compile but fail until TechUnlockService is implemented and TechCatalog is re-tagged.
/// </summary>
public class TechUnlockServiceTests
{
    private const int PlayerId = 0;
    private const int OtherPlayerId = 1;

    private static (ResearchManager research, TechUnlockService service) Create()
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
    // IsUnlocked — tech-gated tags, before and after research              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFalse_When_IsUnlockedCalledForLibraryBeforeWritingResearched()
    {
        var (_, service) = Create();

        Assert.False(service.IsUnlocked(PlayerId, "building:Library"));
    }

    [Fact]
    public void Should_ReturnTrue_When_IsUnlockedCalledForLibraryAfterWritingCompleted()
    {
        var (research, service) = Create();
        CompleteResearch(research, PlayerId, "writing");

        Assert.True(service.IsUnlocked(PlayerId, "building:Library"));
    }

    [Fact]
    public void Should_ReturnFalse_When_IsUnlockedCalledForGranaryBeforePotteryResearched()
    {
        var (_, service) = Create();

        Assert.False(service.IsUnlocked(PlayerId, "building:Granary"));
    }

    [Fact]
    public void Should_ReturnTrue_When_IsUnlockedCalledForGranaryAfterPotteryCompleted()
    {
        var (research, service) = Create();
        CompleteResearch(research, PlayerId, "pottery");

        Assert.True(service.IsUnlocked(PlayerId, "building:Granary"));
    }

    // ------------------------------------------------------------------ //
    // IsUnlocked — ungated tags default to true                           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnTrue_When_IsUnlockedCalledForTagNotInAnyCatalogTech()
    {
        var (_, service) = Create();

        Assert.True(service.IsUnlocked(PlayerId, "building:Palace"));
    }

    // ------------------------------------------------------------------ //
    // IsUnlocked — empty/null returns false                               //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFalse_When_IsUnlockedCalledWithEmptyTag()
    {
        var (_, service) = Create();

        Assert.False(service.IsUnlocked(PlayerId, ""));
    }

    [Fact]
    public void Should_ReturnFalse_When_IsUnlockedCalledWithNullTag()
    {
        var (_, service) = Create();

        Assert.False(service.IsUnlocked(PlayerId, null!));
    }

    // ------------------------------------------------------------------ //
    // IsUnlocked — case-insensitivity anchored to research state          //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData("BUILDING:LIBRARY")]
    [InlineData("building:library")]
    [InlineData("Building:Library")]
    public void Should_ReturnFalse_When_IsUnlockedCalledWithVariousCasingsBeforeWritingCompleted(string tag)
    {
        var (_, service) = Create();

        Assert.False(service.IsUnlocked(PlayerId, tag));
    }

    [Theory]
    [InlineData("BUILDING:LIBRARY")]
    [InlineData("building:library")]
    [InlineData("Building:Library")]
    public void Should_ReturnTrue_When_IsUnlockedCalledWithVariousCasingsAfterWritingCompleted(string tag)
    {
        var (research, service) = Create();
        CompleteResearch(research, PlayerId, "writing");

        Assert.True(service.IsUnlocked(PlayerId, tag));
    }

    // ------------------------------------------------------------------ //
    // IsUnlocked — player isolation                                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFalse_ForOtherPlayer_When_OnlyOnePlayerCompletedWriting()
    {
        var (research, service) = Create();
        CompleteResearch(research, PlayerId, "writing");

        Assert.False(service.IsUnlocked(OtherPlayerId, "building:Library"));
    }

    // ------------------------------------------------------------------ //
    // GatingTechName — known gated tags                                   //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnWriting_When_GatingTechNameCalledForBuildingLibrary()
    {
        var (_, service) = Create();

        Assert.Equal("Writing", service.GatingTechName("building:Library"));
    }

    [Fact]
    public void Should_ReturnHorsebackRiding_When_GatingTechNameCalledForUnitHorseman()
    {
        var (_, service) = Create();

        Assert.Equal("Horseback Riding", service.GatingTechName("unit:Horseman"));
    }

    // ------------------------------------------------------------------ //
    // GatingTechName — ungated/empty/null returns null                   //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnNull_When_GatingTechNameCalledForUngatedTag()
    {
        var (_, service) = Create();

        Assert.Null(service.GatingTechName("building:Palace"));
    }

    [Fact]
    public void Should_ReturnNull_When_GatingTechNameCalledWithEmptyString()
    {
        var (_, service) = Create();

        Assert.Null(service.GatingTechName(""));
    }

    [Fact]
    public void Should_ReturnNull_When_GatingTechNameCalledWithNull()
    {
        var (_, service) = Create();

        Assert.Null(service.GatingTechName(null!));
    }

    // ------------------------------------------------------------------ //
    // GetUnlockedTags — empty for fresh player                            //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnEmpty_When_GetUnlockedTagsCalledForPlayerWithNoCompletedResearch()
    {
        var (_, service) = Create();

        Assert.Empty(service.GetUnlockedTags(PlayerId));
    }

    // ------------------------------------------------------------------ //
    // GetUnlockedTags — union of all completed techs                      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ContainGranary_When_GetUnlockedTagsCalledAfterPotteryCompleted()
    {
        var (research, service) = Create();
        CompleteResearch(research, PlayerId, "pottery");

        Assert.Contains("building:Granary", service.GetUnlockedTags(PlayerId));
    }

    [Fact]
    public void Should_NotContainLibrary_When_GetUnlockedTagsCalledAfterOnlyPotteryCompleted()
    {
        var (research, service) = Create();
        CompleteResearch(research, PlayerId, "pottery");

        Assert.DoesNotContain("building:Library", service.GetUnlockedTags(PlayerId));
    }

    [Fact]
    public void Should_ContainBothGranaryAndLibrary_When_GetUnlockedTagsCalledAfterPotteryAndWritingCompleted()
    {
        var (research, service) = Create();
        CompleteResearch(research, PlayerId, "pottery");
        CompleteResearch(research, PlayerId, "writing");

        var tags = service.GetUnlockedTags(PlayerId);
        Assert.Contains("building:Granary", tags);
        Assert.Contains("building:Library", tags);
    }
}
