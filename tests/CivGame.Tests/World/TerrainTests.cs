using CivGame.World;

namespace CivGame.Tests.World;

/// <summary>
/// Failing tests for issue #48: Terrain types and weighted movement.
/// Written by the test-author agent as executable spec before implementation.
/// </summary>
public class TerrainTests
{
    // ------------------------------------------------------------------ //
    // TerrainType enum — four values                                       //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveFourValues_When_TerrainTypeEnumDefined()
    {
        var values = Enum.GetValues<TerrainType>();
        Assert.Equal(4, values.Length);
    }

    [Fact]
    public void Should_ContainGrass_When_TerrainTypeEnumDefined()
    {
        Assert.True(Enum.IsDefined(typeof(TerrainType), "Grass"));
    }

    [Fact]
    public void Should_ContainPlains_When_TerrainTypeEnumDefined()
    {
        Assert.True(Enum.IsDefined(typeof(TerrainType), "Plains"));
    }

    [Fact]
    public void Should_ContainForest_When_TerrainTypeEnumDefined()
    {
        Assert.True(Enum.IsDefined(typeof(TerrainType), "Forest"));
    }

    [Fact]
    public void Should_ContainWater_When_TerrainTypeEnumDefined()
    {
        Assert.True(Enum.IsDefined(typeof(TerrainType), "Water"));
    }

    // ------------------------------------------------------------------ //
    // TerrainRules.IsPassable                                              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnTrue_When_TerrainIsGrass()
    {
        Assert.True(TerrainRules.IsPassable(TerrainType.Grass));
    }

    [Fact]
    public void Should_ReturnTrue_When_TerrainIsPlains()
    {
        Assert.True(TerrainRules.IsPassable(TerrainType.Plains));
    }

    [Fact]
    public void Should_ReturnTrue_When_TerrainIsForest()
    {
        Assert.True(TerrainRules.IsPassable(TerrainType.Forest));
    }

    [Fact]
    public void Should_ReturnFalse_When_TerrainIsWater()
    {
        Assert.False(TerrainRules.IsPassable(TerrainType.Water));
    }

    // ------------------------------------------------------------------ //
    // TerrainRules.MovementCost                                            //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnOne_When_MovementCostForGrass()
    {
        Assert.Equal(1, TerrainRules.MovementCost(TerrainType.Grass));
    }

    [Fact]
    public void Should_ReturnOne_When_MovementCostForPlains()
    {
        Assert.Equal(1, TerrainRules.MovementCost(TerrainType.Plains));
    }

    [Fact]
    public void Should_ReturnTwo_When_MovementCostForForest()
    {
        Assert.Equal(2, TerrainRules.MovementCost(TerrainType.Forest));
    }

    [Fact]
    public void Should_ReturnIntMaxValue_When_MovementCostForWater()
    {
        Assert.Equal(int.MaxValue, TerrainRules.MovementCost(TerrainType.Water));
    }

    // ------------------------------------------------------------------ //
    // HexCell.IsPassable — derived from TerrainRules                      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_BePassable_When_HexCellHasGrassTerrain()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Grass);
        Assert.True(cell.IsPassable);
    }

    [Fact]
    public void Should_BePassable_When_HexCellHasPlainsTerrain()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Plains);
        Assert.True(cell.IsPassable);
    }

    [Fact]
    public void Should_BePassable_When_HexCellHasForestTerrain()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Forest);
        Assert.True(cell.IsPassable);
    }

    [Fact]
    public void Should_BeImpassable_When_HexCellHasWaterTerrain()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Water);
        Assert.False(cell.IsPassable);
    }

    [Fact]
    public void Should_UpdatePassability_When_TerrainChangedToWater()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Grass);
        Assert.True(cell.IsPassable);

        cell.Terrain = TerrainType.Water;
        Assert.False(cell.IsPassable);
    }

    [Fact]
    public void Should_BecomePassable_When_TerrainChangedFromWaterToGrass()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Water);
        Assert.False(cell.IsPassable);

        cell.Terrain = TerrainType.Grass;
        Assert.True(cell.IsPassable);
    }
}
