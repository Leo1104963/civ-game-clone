using CivGame.Tech;

namespace CivGame.Tech.Tests;

/// <summary>
/// Tests for ResearchManager: all happy-path behaviors plus every edge-case
/// acceptance criterion from issue #97.
/// </summary>
public class ResearchManagerTests
{
    private const int PlayerId = 0;
    private const int OtherPlayerId = 1;

    private static ResearchManager CreateManager() => new ResearchManager();

    // ------------------------------------------------------------------ //
    // StartResearch — happy path                                           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnTrue_When_StartResearchCalledForFreshPlayerWithNoPrerequirements()
    {
        var manager = CreateManager();

        bool result = manager.StartResearch(PlayerId, "pottery");

        Assert.True(result);
    }

    [Fact]
    public void Should_SetCurrentResearch_When_StartResearchSucceeds()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");

        var current = manager.GetCurrentResearch(PlayerId);

        Assert.NotNull(current);
        Assert.Equal("pottery", current!.Id);
    }

    // ------------------------------------------------------------------ //
    // TickFor — science accumulation                                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_AccumulateScience_When_TickForCalledMultipleTimes()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");

        manager.TickFor(PlayerId, 10);
        manager.TickFor(PlayerId, 10);
        manager.TickFor(PlayerId, 10);

        Assert.Equal(30, manager.GetAccumulatedScience(PlayerId));
    }

    // ------------------------------------------------------------------ //
    // TechUnlocked fires synchronously when threshold reached              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_FireTechUnlocked_When_AccumulatedScienceReachesCost()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");

        Technology? unlockedTech = null;
        int? unlockedPlayer = null;
        manager.TechUnlocked += (playerId, tech) =>
        {
            unlockedPlayer = playerId;
            unlockedTech = tech;
        };

        // Pottery costs 20; tick exactly enough to complete.
        var pottery = TechCatalog.GetById("pottery")!;
        manager.TickFor(PlayerId, pottery.ScienceCost);

        Assert.NotNull(unlockedTech);
        Assert.Equal(PlayerId, unlockedPlayer);
        Assert.Equal("pottery", unlockedTech!.Id);
    }

    [Fact]
    public void Should_MarkTechCompleted_When_TechUnlockedFires()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");

        var pottery = TechCatalog.GetById("pottery")!;

        bool completedDuringHandler = false;
        manager.TechUnlocked += (playerId, tech) =>
        {
            // Inside the handler, IsCompleted must already be true.
            completedDuringHandler = manager.IsCompleted(playerId, tech.Id);
        };

        manager.TickFor(PlayerId, pottery.ScienceCost);

        Assert.True(completedDuringHandler);
    }

    [Fact]
    public void Should_HaveNullCurrentResearch_When_TechUnlockedFires()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");

        var pottery = TechCatalog.GetById("pottery")!;

        Technology? currentDuringHandler = new Technology("sentinel", "Sentinel", 1);
        manager.TechUnlocked += (playerId, tech) =>
        {
            // Inside the handler, GetCurrentResearch must already be null.
            currentDuringHandler = manager.GetCurrentResearch(playerId);
        };

        manager.TickFor(PlayerId, pottery.ScienceCost);

        Assert.Null(currentDuringHandler);
    }

    // ------------------------------------------------------------------ //
    // After completion: IsCompleted and GetCurrentResearch                //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnTrueForIsCompleted_When_TechFullyResearched()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");
        var pottery = TechCatalog.GetById("pottery")!;

        manager.TickFor(PlayerId, pottery.ScienceCost);

        Assert.True(manager.IsCompleted(PlayerId, "pottery"));
    }

    [Fact]
    public void Should_ReturnNullCurrentResearch_When_TechCompleted()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");
        var pottery = TechCatalog.GetById("pottery")!;

        manager.TickFor(PlayerId, pottery.ScienceCost);

        Assert.Null(manager.GetCurrentResearch(PlayerId));
    }

    // ------------------------------------------------------------------ //
    // Edge case: TickFor with zero science                                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_NotChangeAccumulatedScience_When_TickForCalledWithZero()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");
        manager.TickFor(PlayerId, 5); // accumulate something first

        manager.TickFor(PlayerId, 0);

        Assert.Equal(5, manager.GetAccumulatedScience(PlayerId));
    }

    [Fact]
    public void Should_NotFireTechUnlocked_When_TickForCalledWithZero()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");

        bool fired = false;
        manager.TechUnlocked += (_, _) => fired = true;

        manager.TickFor(PlayerId, 0);

        Assert.False(fired);
    }

    // ------------------------------------------------------------------ //
    // Edge case: TickFor with negative science                             //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_NotChangeAccumulatedScience_When_TickForCalledWithNegative()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");
        manager.TickFor(PlayerId, 5);

        manager.TickFor(PlayerId, -5);

        Assert.Equal(5, manager.GetAccumulatedScience(PlayerId));
    }

    [Fact]
    public void Should_NotFireTechUnlocked_When_TickForCalledWithNegative()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");

        bool fired = false;
        manager.TechUnlocked += (_, _) => fired = true;

        manager.TickFor(PlayerId, -1);

        Assert.False(fired);
    }

    // ------------------------------------------------------------------ //
    // Edge case: no current research — TickFor still pools science         //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_AddToPool_When_TickForCalledWithNoCurrentResearch()
    {
        var manager = CreateManager();
        // No StartResearch call.

        manager.TickFor(PlayerId, 10);

        Assert.Equal(10, manager.GetAccumulatedScience(PlayerId));
    }

    [Fact]
    public void Should_NotFireTechUnlocked_When_TickForCalledWithNoCurrentResearch()
    {
        var manager = CreateManager();

        bool fired = false;
        manager.TechUnlocked += (_, _) => fired = true;

        manager.TickFor(PlayerId, 100);

        Assert.False(fired);
    }

    // ------------------------------------------------------------------ //
    // Edge case: prerequisite blocking                                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFalse_When_StartResearchCalledForTechWithUnmetPrerequisite()
    {
        var manager = CreateManager();
        // currency requires bronze-working; fresh player hasn't researched it.

        bool result = manager.StartResearch(PlayerId, "currency");

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_CanResearchCalledForTechWithUnmetPrerequisite()
    {
        var manager = CreateManager();

        bool result = manager.CanResearch(PlayerId, "currency");

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnTrue_When_CanResearchCalledAfterCompletingPrerequisite()
    {
        var manager = CreateManager();
        // Research bronze-working first.
        manager.StartResearch(PlayerId, "bronze-working");
        var bw = TechCatalog.GetById("bronze-working")!;
        manager.TickFor(PlayerId, bw.ScienceCost);

        bool result = manager.CanResearch(PlayerId, "currency");

        Assert.True(result);
    }

    // ------------------------------------------------------------------ //
    // Edge case: mathematics requires BOTH currency AND masonry            //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFalse_When_MathematicsResearchedWithOnlyCurrencyCompleted()
    {
        var manager = CreateManager();
        // Complete the prerequisites of currency first (bronze-working).
        CompleteResearch(manager, PlayerId, "bronze-working");
        CompleteResearch(manager, PlayerId, "currency");

        bool result = manager.CanResearch(PlayerId, "mathematics");

        Assert.False(result); // masonry not yet completed
    }

    [Fact]
    public void Should_ReturnFalse_When_MathematicsResearchedWithOnlyMasonryCompleted()
    {
        var manager = CreateManager();
        CompleteResearch(manager, PlayerId, "masonry");

        bool result = manager.CanResearch(PlayerId, "mathematics");

        Assert.False(result); // currency not yet completed
    }

    [Fact]
    public void Should_ReturnTrue_When_MathematicsResearchedWithBothPrerequisitesCompleted()
    {
        var manager = CreateManager();
        CompleteResearch(manager, PlayerId, "bronze-working");
        CompleteResearch(manager, PlayerId, "currency");
        CompleteResearch(manager, PlayerId, "masonry");

        bool result = manager.CanResearch(PlayerId, "mathematics");

        Assert.True(result);
    }

    // ------------------------------------------------------------------ //
    // Edge case: StartResearch for already-completed tech                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFalse_When_StartResearchCalledForAlreadyCompletedTech()
    {
        var manager = CreateManager();
        CompleteResearch(manager, PlayerId, "pottery");

        bool result = manager.StartResearch(PlayerId, "pottery");

        Assert.False(result);
    }

    [Fact]
    public void Should_NotChangeState_When_StartResearchCalledForAlreadyCompletedTech()
    {
        var manager = CreateManager();
        CompleteResearch(manager, PlayerId, "pottery");
        // Start a new valid research.
        manager.StartResearch(PlayerId, "bronze-working");

        // Try to restart pottery (already done).
        manager.StartResearch(PlayerId, "pottery");

        // Current research should remain bronze-working, not change.
        var current = manager.GetCurrentResearch(PlayerId);
        Assert.NotNull(current);
        Assert.Equal("bronze-working", current!.Id);
    }

    // ------------------------------------------------------------------ //
    // Edge case: StartResearch for already-current tech                    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFalse_When_StartResearchCalledForAlreadyCurrentTech()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");

        bool result = manager.StartResearch(PlayerId, "pottery");

        Assert.False(result);
    }

    // ------------------------------------------------------------------ //
    // Edge case: unknown tech id                                           //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFalse_When_CanResearchCalledWithUnknownId()
    {
        var manager = CreateManager();

        bool result = manager.CanResearch(PlayerId, "nonexistent-tech");

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_StartResearchCalledWithUnknownId()
    {
        var manager = CreateManager();

        bool result = manager.StartResearch(PlayerId, "nonexistent-tech");

        Assert.False(result);
    }

    [Fact]
    public void Should_NotThrow_When_CanResearchCalledWithUnknownId()
    {
        var manager = CreateManager();

        // Must not throw — just return false.
        var ex = Record.Exception(() => manager.CanResearch(PlayerId, "nonexistent-tech"));

        Assert.Null(ex);
    }

    [Fact]
    public void Should_NotThrow_When_StartResearchCalledWithUnknownId()
    {
        var manager = CreateManager();

        var ex = Record.Exception(() => manager.StartResearch(PlayerId, "nonexistent-tech"));

        Assert.Null(ex);
    }

    // ------------------------------------------------------------------ //
    // Player isolation                                                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_NotAffectOtherPlayerAccumulatedScience_When_TickingPlayer()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");
        manager.StartResearch(OtherPlayerId, "pottery");

        manager.TickFor(PlayerId, 10);

        Assert.Equal(10, manager.GetAccumulatedScience(PlayerId));
        Assert.Equal(0, manager.GetAccumulatedScience(OtherPlayerId));
    }

    [Fact]
    public void Should_ReturnSeparateCurrentResearch_When_TwoPlayersResearchDifferentTechs()
    {
        var manager = CreateManager();
        manager.StartResearch(PlayerId, "pottery");
        manager.StartResearch(OtherPlayerId, "animal-husbandry");

        Assert.Equal("pottery", manager.GetCurrentResearch(PlayerId)!.Id);
        Assert.Equal("animal-husbandry", manager.GetCurrentResearch(OtherPlayerId)!.Id);
    }

    // ------------------------------------------------------------------ //
    // GetAccumulatedScience — returns 0 for fresh player                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnZeroAccumulatedScience_When_PlayerHasNeverTicked()
    {
        var manager = CreateManager();

        Assert.Equal(0, manager.GetAccumulatedScience(PlayerId));
    }

    // ------------------------------------------------------------------ //
    // GetCurrentResearch — returns null for fresh player                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnNullCurrentResearch_When_PlayerHasNotStartedResearch()
    {
        var manager = CreateManager();

        Assert.Null(manager.GetCurrentResearch(PlayerId));
    }

    // ------------------------------------------------------------------ //
    // Helper                                                               //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Completes a tech by ticking exactly its science cost.
    /// Precondition: all prerequisites must already be completed.
    /// </summary>
    private static void CompleteResearch(ResearchManager manager, int playerId, string techId)
    {
        var tech = TechCatalog.GetById(techId)
            ?? throw new InvalidOperationException($"Unknown tech: {techId}");

        bool started = manager.StartResearch(playerId, techId);
        if (!started)
            throw new InvalidOperationException(
                $"Could not start research for '{techId}' — check prerequisites or state.");

        manager.TickFor(playerId, tech.ScienceCost);
    }
}
