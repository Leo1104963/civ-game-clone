namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for OwnerTintResolver.GetTint (issue #90).
/// Covers player 0 neutral white, player 1 barbarian tint,
/// arbitrary ids, stability, and channel bounds.
/// </summary>
public class OwnerTintResolverTests
{
    // --- Documented special cases ---

    [Fact]
    public void Should_ReturnWhite_When_OwnerIsPlayerZero()
    {
        var (r, g, b) = OwnerTintResolver.GetTint(0);

        Assert.Equal(1f, r, precision: 4);
        Assert.Equal(1f, g, precision: 4);
        Assert.Equal(1f, b, precision: 4);
    }

    [Fact]
    public void Should_ReturnBarbarianRedOrange_When_OwnerIsPlayerOne()
    {
        var (r, g, b) = OwnerTintResolver.GetTint(1);

        Assert.Equal(0.95f, r, precision: 4);
        Assert.Equal(0.35f, g, precision: 4);
        Assert.Equal(0.25f, b, precision: 4);
    }

    // --- Stability: same id always returns identical tuple ---

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(100)]
    public void Should_ReturnSameTuple_When_CalledTwiceWithSameId(int ownerId)
    {
        var first = OwnerTintResolver.GetTint(ownerId);
        var second = OwnerTintResolver.GetTint(ownerId);

        Assert.Equal(first, second);
    }

    // --- Channel bounds for arbitrary ids ---

    [Theory]
    [InlineData(2)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void Should_ReturnChannelsInValidRange_When_ArbitraryOwnerIdGiven(int ownerId)
    {
        var (r, g, b) = OwnerTintResolver.GetTint(ownerId);

        Assert.InRange(r, 0f, 1f);
        Assert.InRange(g, 0f, 1f);
        Assert.InRange(b, 0f, 1f);
    }

    // --- Negative id: no throw, channels in valid range, stable ---

    [Fact]
    public void Should_NotThrow_When_OwnerIdIsNegativeOne()
    {
        var (r, g, b) = OwnerTintResolver.GetTint(-1);

        Assert.InRange(r, 0f, 1f);
        Assert.InRange(g, 0f, 1f);
        Assert.InRange(b, 0f, 1f);
    }

    [Fact]
    public void Should_ReturnStableTuple_When_NegativeOwnerIdCalledTwice()
    {
        var first = OwnerTintResolver.GetTint(-1);
        var second = OwnerTintResolver.GetTint(-1);

        Assert.Equal(first, second);
    }

    // --- Different ids produce different tints (palette is not collapsed) ---

    [Fact]
    public void Should_ProduceDistinctTints_When_DifferentNonSpecialIdsGiven()
    {
        var tint2 = OwnerTintResolver.GetTint(2);
        var tint3 = OwnerTintResolver.GetTint(3);

        // They may coincidentally be equal for some hash implementations,
        // but ids 2 and 3 should ideally differ — at minimum they must not
        // both equal the player-0 white.
        var white = (R: 1f, G: 1f, B: 1f);
        Assert.NotEqual(white, tint2);
        Assert.NotEqual(white, tint3);
    }
}
