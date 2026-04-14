namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for OwnerTintResolver.GetTint (issue #90).
/// Covers player 0 neutral white, player 1 barbarian tint,
/// negative ids (grey fallback), arbitrary ids, stability, and channel bounds.
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

    // --- Negative id: returns neutral grey (0.5, 0.5, 0.5), no throw ---

    [Fact]
    public void Should_ReturnNeutralGrey_When_OwnerIdIsNegativeOne()
    {
        var (r, g, b) = OwnerTintResolver.GetTint(-1);

        Assert.Equal(0.5f, r, precision: 4);
        Assert.Equal(0.5f, g, precision: 4);
        Assert.Equal(0.5f, b, precision: 4);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Should_ReturnNeutralGrey_When_OwnerIdIsAnyNegative(int ownerId)
    {
        var (r, g, b) = OwnerTintResolver.GetTint(ownerId);

        Assert.Equal(0.5f, r, precision: 4);
        Assert.Equal(0.5f, g, precision: 4);
        Assert.Equal(0.5f, b, precision: 4);
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

    // --- Arbitrary ids differ from player-0 white ---

    [Fact]
    public void Should_ProduceDistinctTints_When_DifferentNonSpecialIdsGiven()
    {
        var tint2 = OwnerTintResolver.GetTint(2);
        var tint3 = OwnerTintResolver.GetTint(3);

        var white = (R: 1f, G: 1f, B: 1f);
        Assert.NotEqual(white, tint2);
        Assert.NotEqual(white, tint3);
    }
}
