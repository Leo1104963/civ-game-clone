using CivGame.Cities;
using CivGame.World;

namespace CivGame.Cities.Tests;

public class TerrainYieldsTests
{
    [Fact]
    public void Should_ReturnFood2Production0_When_GrassTerrain()
    {
        var result = TerrainYields.Of(TerrainType.Grass);

        Assert.Equal(2, result.Food);
        Assert.Equal(0, result.Production);
    }

    [Fact]
    public void Should_ReturnFood1Production1_When_PlainsTerrain()
    {
        var result = TerrainYields.Of(TerrainType.Plains);

        Assert.Equal(1, result.Food);
        Assert.Equal(1, result.Production);
    }

    [Fact]
    public void Should_ReturnFood0Production2_When_ForestTerrain()
    {
        var result = TerrainYields.Of(TerrainType.Forest);

        Assert.Equal(0, result.Food);
        Assert.Equal(2, result.Production);
    }

    [Fact]
    public void Should_ReturnFood1Production0_When_WaterTerrain()
    {
        var result = TerrainYields.Of(TerrainType.Water);

        Assert.Equal(1, result.Food);
        Assert.Equal(0, result.Production);
    }
}
