using CivGame.Cities;
using CivGame.World;

namespace CivGame.Cities.Tests;

public class YieldCalculatorTests
{
    /// <summary>
    /// Build a uniform-terrain grid and place a city at the given position.
    /// The HexGrid constructor defaults all cells to Grass; we override all cells here.
    /// </summary>
    private static (HexGrid grid, City city) CreateUniformGrid(
        int width, int height, TerrainType terrain, int cityQ = 3, int cityR = 3)
    {
        var grid = new HexGrid(width, height);
        foreach (var cell in grid.AllCells())
        {
            cell.Terrain = terrain;
        }

        var city = new City("Test", new HexCoord(cityQ, cityR));
        return (grid, city);
    }

    // ------------------------------------------------------------------ //
    // All-Grass grid: 1 center + 6 neighbors = 7 tiles × (Food:2, Prod:0) //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFood14Production0_When_AllGrassGridAndCityCentered()
    {
        var (grid, city) = CreateUniformGrid(10, 10, TerrainType.Grass);

        var result = YieldCalculator.Calculate(city, grid);

        Assert.Equal(14, result.Food);
        Assert.Equal(0, result.Production);
    }

    // ------------------------------------------------------------------ //
    // All-Forest grid: 7 tiles × (Food:0, Prod:2)                         //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnFood0Production14_When_AllForestGridAndCityCentered()
    {
        var (grid, city) = CreateUniformGrid(10, 10, TerrainType.Forest);

        var result = YieldCalculator.Calculate(city, grid);

        Assert.Equal(0, result.Food);
        Assert.Equal(14, result.Production);
    }

    // ------------------------------------------------------------------ //
    // Edge city: some neighbors out-of-bounds, only in-bounds cells sum    //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_SumOnlyInBoundsCells_When_CityAtCorner()
    {
        // City at (0,0) — some or all neighbors may be OOB.
        // We rely on HexGrid.GetNeighbors returning only in-bounds cells.
        var grid = new HexGrid(10, 10);
        foreach (var cell in grid.AllCells())
        {
            cell.Terrain = TerrainType.Grass;
        }

        var city = new City("Edge", new HexCoord(0, 0));

        var result = YieldCalculator.Calculate(city, grid);

        // Center cell is always counted if in-bounds (it is: (0,0) exists).
        // Neighbor count at corner is less than 6.
        // Each Grass cell contributes Food:2, so total food = (1 + inBoundsNeighborCount) * 2.
        var centerCell = grid.GetCell(new HexCoord(0, 0));
        Assert.NotNull(centerCell);

        var inBoundsNeighborCount = grid.GetNeighbors(new HexCoord(0, 0)).Count;
        int expectedFood = (1 + inBoundsNeighborCount) * 2;

        Assert.Equal(expectedFood, result.Food);
        Assert.Equal(0, result.Production);
    }

    [Fact]
    public void Should_ReturnLessThan14Food_When_CityAtEdge()
    {
        // At an edge position fewer than 6 neighbors are in-bounds, so total < 14.
        var (grid, city) = CreateUniformGrid(10, 10, TerrainType.Grass, cityQ: 0, cityR: 0);

        var result = YieldCalculator.Calculate(city, grid);

        Assert.True(result.Food < 14,
            $"Expected food < 14 for edge city, got {result.Food}");
    }

    // ------------------------------------------------------------------ //
    // Purity: repeated calls return identical results                      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnSameResult_When_CalledTwiceWithUnchangedInputs()
    {
        var (grid, city) = CreateUniformGrid(10, 10, TerrainType.Plains);

        var first = YieldCalculator.Calculate(city, grid);
        var second = YieldCalculator.Calculate(city, grid);

        Assert.Equal(first.Food, second.Food);
        Assert.Equal(first.Production, second.Production);
    }
}
