using CivGame.Buildings;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Cities.Tests;

/// <summary>
/// Tests for the Science component of YieldResult and YieldCalculator.
/// Covers issue #97 acceptance criteria: YieldResult.Science,
/// TerrainYields.Of().Science for Plains and other terrains, and
/// science contribution from Library building.
/// </summary>
public class YieldCalculatorScienceTests
{
    // ------------------------------------------------------------------ //
    // YieldResult.Science is readable                                      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveScienceProperty_When_YieldResultCreated()
    {
        var result = new YieldResult(Food: 2, Production: 0, Science: 1);

        Assert.Equal(1, result.Science);
    }

    [Fact]
    public void Should_ReturnZeroScience_When_YieldResultConstructedWithZeroScience()
    {
        // Science: 0 is valid — verify the property stores and returns it correctly.
        var result = new YieldResult(Food: 2, Production: 0, Science: 0);

        Assert.Equal(0, result.Science);
    }

    // ------------------------------------------------------------------ //
    // TerrainYields.Of — Science per terrain type                         //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnScienceOne_When_TerrainIsPlains()
    {
        var result = TerrainYields.Of(TerrainType.Plains);

        Assert.Equal(1, result.Science);
    }

    [Theory]
    [InlineData(TerrainType.Grass)]
    [InlineData(TerrainType.Forest)]
    [InlineData(TerrainType.Water)]
    public void Should_ReturnScienceZero_When_TerrainIsNotPlains(TerrainType terrain)
    {
        var result = TerrainYields.Of(terrain);

        Assert.Equal(0, result.Science);
    }

    // ------------------------------------------------------------------ //
    // YieldCalculator.Calculate — science from terrain                    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnScienceZero_When_AllGrassGrid()
    {
        var grid = new HexGrid(10, 10);
        foreach (var cell in grid.AllCells())
            cell.Terrain = TerrainType.Grass;

        var city = new City("Test", new HexCoord(3, 3));

        var result = YieldCalculator.Calculate(city, grid);

        Assert.Equal(0, result.Science);
    }

    [Fact]
    public void Should_ReturnScienceSeven_When_AllPlainsGridCityCentered()
    {
        // 1 center + 6 neighbors = 7 tiles, each Plains = Science 1 → total 7
        var grid = new HexGrid(10, 10);
        foreach (var cell in grid.AllCells())
            cell.Terrain = TerrainType.Plains;

        var city = new City("Test", new HexCoord(3, 3));

        var result = YieldCalculator.Calculate(city, grid);

        Assert.Equal(7, result.Science);
    }

    [Fact]
    public void Should_ReturnScienceFromPlainsOnly_When_MixedTerrain()
    {
        // City at (3,3) on Grass center; only the center cell is set to Plains.
        var grid = new HexGrid(10, 10);
        foreach (var cell in grid.AllCells())
            cell.Terrain = TerrainType.Grass;

        grid.GetCell(new HexCoord(3, 3))!.Terrain = TerrainType.Plains;

        var city = new City("Test", new HexCoord(3, 3));

        var result = YieldCalculator.Calculate(city, grid);

        Assert.Equal(1, result.Science);
    }

    // ------------------------------------------------------------------ //
    // YieldCalculator.Calculate — science from Library building            //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnSciencePlusTwo_When_CityHasLibraryBuilding()
    {
        var grid = new HexGrid(10, 10);
        foreach (var cell in grid.AllCells())
            cell.Terrain = TerrainType.Grass; // no terrain science

        var city = new City("Test", new HexCoord(3, 3));
        var library = new BuildingDefinition("Library", 8, scienceYield: 2);
        city.AddCompletedBuilding(library);

        var result = YieldCalculator.Calculate(city, grid);

        Assert.Equal(2, result.Science);
    }

    [Fact]
    public void Should_SumTerrainAndBuildingScience_When_PlainsAndLibrary()
    {
        // All Plains = 7 science from terrain; Library adds 2 → total 9.
        var grid = new HexGrid(10, 10);
        foreach (var cell in grid.AllCells())
            cell.Terrain = TerrainType.Plains;

        var city = new City("Test", new HexCoord(3, 3));
        var library = new BuildingDefinition("Library", 8, scienceYield: 2);
        city.AddCompletedBuilding(library);

        var result = YieldCalculator.Calculate(city, grid);

        Assert.Equal(9, result.Science);
    }
}
