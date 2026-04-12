using CivGame.Buildings;

namespace CivGame.Buildings.Tests;

public class BuildQueueItemTests
{
    [Fact]
    public void Should_HaveTurnsRemainingEqualToBuildCost_When_Created()
    {
        var def = new BuildingDefinition("Granary", 5);
        var item = new BuildQueueItem(def);

        Assert.Equal(5, item.TurnsRemaining);
        Assert.Same(def, item.Definition);
    }

    [Fact]
    public void Should_NotBeComplete_When_Created()
    {
        var item = new BuildQueueItem(new BuildingDefinition("Granary", 5));

        Assert.False(item.IsComplete);
    }

    [Fact]
    public void Should_DecrementTurnsRemaining_When_Ticked()
    {
        var item = new BuildQueueItem(new BuildingDefinition("Granary", 5));

        item.Tick();

        Assert.Equal(4, item.TurnsRemaining);
    }

    [Fact]
    public void Should_BeComplete_When_TurnsRemainingReachesZero()
    {
        var item = new BuildQueueItem(new BuildingDefinition("Test", 2));

        item.Tick();
        Assert.False(item.IsComplete);

        item.Tick();
        Assert.True(item.IsComplete);
        Assert.Equal(0, item.TurnsRemaining);
    }

    [Fact]
    public void Should_NotGoNegative_When_TickedAfterComplete()
    {
        var item = new BuildQueueItem(new BuildingDefinition("Test", 1));

        item.Tick(); // TurnsRemaining = 0, complete
        Assert.True(item.IsComplete);

        item.Tick(); // should be a no-op
        Assert.Equal(0, item.TurnsRemaining);
    }

    [Fact]
    public void Should_CompleteGranaryAfterExactlyFiveTicks()
    {
        var item = new BuildQueueItem(BuildingCatalog.Granary);

        for (int i = 0; i < 4; i++)
        {
            item.Tick();
            Assert.False(item.IsComplete);
        }

        item.Tick(); // 5th tick
        Assert.True(item.IsComplete);
        Assert.Equal(0, item.TurnsRemaining);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_DefinitionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BuildQueueItem(null!));
    }
}
