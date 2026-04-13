using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for TerrainTextureSet.PickVariantIndex (issue #89).
/// All tests are headless — no Godot texture loading.
/// </summary>
public class TerrainTextureSetTests
{
    // --- Determinism ---

    [Theory]
    [InlineData(TerrainType.Grass)]
    [InlineData(TerrainType.Plains)]
    [InlineData(TerrainType.Forest)]
    [InlineData(TerrainType.Water)]
    public void Should_ReturnSameIndex_When_CalledTwiceWithSameArguments(TerrainType terrain)
    {
        var coord = new HexCoord(2, 3);
        int first = TerrainTextureSet.PickVariantIndex(terrain, coord, 3);
        int second = TerrainTextureSet.PickVariantIndex(terrain, coord, 3);

        Assert.Equal(first, second);
    }

    // --- Index in range ---

    [Theory]
    [InlineData(TerrainType.Grass,  1)]
    [InlineData(TerrainType.Grass,  2)]
    [InlineData(TerrainType.Grass,  3)]
    [InlineData(TerrainType.Plains, 3)]
    [InlineData(TerrainType.Forest, 3)]
    [InlineData(TerrainType.Water,  3)]
    public void Should_ReturnIndexInRange_When_VariantCountGiven(TerrainType terrain, int variantCount)
    {
        var coord = new HexCoord(1, 1);
        int index = TerrainTextureSet.PickVariantIndex(terrain, coord, variantCount);

        Assert.InRange(index, 0, variantCount - 1);
    }

    [Fact]
    public void Should_ReturnZero_When_VariantCountIsOne()
    {
        var coord = new HexCoord(0, 0);
        int index = TerrainTextureSet.PickVariantIndex(TerrainType.Grass, coord, 1);

        Assert.Equal(0, index);
    }

    // --- Guard on variantCount <= 0 ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_ThrowArgumentOutOfRangeException_When_VariantCountIsNotPositive(int variantCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TerrainTextureSet.PickVariantIndex(TerrainType.Grass, new HexCoord(0, 0), variantCount));
    }

    // --- Different coords produce variation (not all identical) ---

    [Fact]
    public void Should_ProduceAtLeastTwoDistinctIndices_When_ManyCoordsWithThreeVariants()
    {
        var terrain = TerrainType.Grass;
        var indices = new HashSet<int>();

        for (int q = 0; q < 5; q++)
            for (int r = 0; r < 5; r++)
                indices.Add(TerrainTextureSet.PickVariantIndex(terrain, new HexCoord(q, r), 3));

        // With a 5x5 grid and 3 variants the hash should produce at least 2 distinct values
        Assert.True(indices.Count >= 2,
            "PickVariantIndex should not map all coords to the same variant");
    }

    // --- Different terrain types can produce different indices for the same coord ---

    [Fact]
    public void Should_NotRequireAllTerrainsToMatchForSameCoord()
    {
        var coord = new HexCoord(3, 3);
        // Just verify all terrain types return valid in-range values — no throw
        foreach (var terrain in Enum.GetValues<TerrainType>())
        {
            int index = TerrainTextureSet.PickVariantIndex(terrain, coord, 3);
            Assert.InRange(index, 0, 2);
        }
    }
}
