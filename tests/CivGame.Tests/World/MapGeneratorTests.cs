using System;
using System.Collections.Generic;
using System.Linq;
using CivGame.World;
using Xunit;

namespace CivGame.Tests.World;

public class MapGeneratorTests
{
    // ------------------------------------------------------------------ //
    // Generate — dimensions                                                //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnGridOfCorrectDimensions_When_GenerateCalled()
    {
        var grid = MapGenerator.Generate(10, 8, seed: 42);
        Assert.Equal(10, grid.Width);
        Assert.Equal(8, grid.Height);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(20, 20)]
    [InlineData(5, 15)]
    public void Should_ReturnExactWidthTimesHeightCells_When_GenerateCalled(int w, int h)
    {
        var grid = MapGenerator.Generate(w, h, seed: 0);
        Assert.Equal(w * h, grid.AllCells().Count());
    }

    // ------------------------------------------------------------------ //
    // Generate — invalid args                                              //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_WidthIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapGenerator.Generate(0, 5, 1));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_HeightIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapGenerator.Generate(5, 0, 1));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_WidthIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapGenerator.Generate(-1, 5, 1));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_HeightIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapGenerator.Generate(5, -1, 1));
    }

    // ------------------------------------------------------------------ //
    // Generate — determinism                                               //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ProduceIdenticalTerrainForEveryCell_When_SameSeedUsedTwice()
    {
        var grid1 = MapGenerator.Generate(20, 20, seed: 12345);
        var grid2 = MapGenerator.Generate(20, 20, seed: 12345);

        var cells1 = grid1.AllCells().ToList();
        var cells2 = grid2.AllCells().ToList();

        int count1 = cells1.Count;
        int count2 = cells2.Count;
        Assert.Equal(count1, count2);
        for (int i = 0; i < count1; i++)
        {
            Assert.Equal(cells1[i].Coord, cells2[i].Coord);
            Assert.Equal(cells1[i].Terrain, cells2[i].Terrain);
        }
    }

    [Fact]
    public void Should_ProduceDifferentGrids_When_DifferentSeedsUsed()
    {
        var grid1 = MapGenerator.Generate(20, 20, seed: 1);
        var grid2 = MapGenerator.Generate(20, 20, seed: 2);

        var cells1 = grid1.AllCells().ToList();
        var cells2 = grid2.AllCells().ToList();

        bool anyDifferent = false;
        for (int i = 0; i < cells1.Count; i++)
        {
            if (cells1[i].Terrain != cells2[i].Terrain)
            {
                anyDifferent = true;
                break;
            }
        }
        Assert.True(anyDifferent, "Two different seeds produced identical grids on a 20x20 map.");
    }

    // ------------------------------------------------------------------ //
    // Generate — capital spawn guarantees                                  //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData(10, 10, 42)]
    [InlineData(20, 20, 0)]
    [InlineData(7, 3, 999)]
    public void Should_HaveGrassAtCapitalSpawn_When_GenerateCalled(int w, int h, int seed)
    {
        var grid = MapGenerator.Generate(w, h, seed);
        var capital = grid.GetCell(new HexCoord(w / 2, h / 2));

        Assert.NotNull(capital);
        Assert.Equal(TerrainType.Grass, capital!.Terrain);
    }

    [Theory]
    [InlineData(10, 10, 42)]
    [InlineData(20, 20, 0)]
    [InlineData(7, 3, 999)]
    public void Should_HaveAllInBoundsNeighborsPassable_When_CheckingCapitalSpawn(int w, int h, int seed)
    {
        var grid = MapGenerator.Generate(w, h, seed);
        var capitalCoord = new HexCoord(w / 2, h / 2);
        var neighbors = grid.GetNeighbors(capitalCoord);

        foreach (var neighbor in neighbors)
        {
            Assert.True(
                TerrainRules.IsPassable(neighbor.Terrain),
                $"Neighbor at {neighbor.Coord} has impassable terrain {neighbor.Terrain}.");
        }
    }

    // ------------------------------------------------------------------ //
    // Generate — terrain diversity                                         //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ContainAtLeastOneOfEachTerrainType_When_MapIs10x10()
    {
        // With enough cells and varied seeds, a 10x10 map should always
        // produce all four terrain types.
        // We try a small number of seeds; any single one is sufficient.
        var terrainsSeen = new HashSet<TerrainType>();
        for (int seed = 0; seed < 20; seed++)
        {
            var grid = MapGenerator.Generate(10, 10, seed);
            foreach (var cell in grid.AllCells())
                terrainsSeen.Add(cell.Terrain);

            if (terrainsSeen.Count == 4) break;
        }

        Assert.Contains(TerrainType.Grass, terrainsSeen);
        Assert.Contains(TerrainType.Plains, terrainsSeen);
        Assert.Contains(TerrainType.Forest, terrainsSeen);
        Assert.Contains(TerrainType.Water, terrainsSeen);
    }

    // ------------------------------------------------------------------ //
    // Generate — distribution sanity (1000-map sampling)                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ProduceRoughlyExpectedTerrainDistribution_When_Sampling1000Maps()
    {
        // Across 1000 random-seed 20x20 maps:
        //   Grass  : roughly 45-65% of cells
        //   Forest : roughly  5-20% of cells
        //   Water  : roughly  5-15% of cells
        const int mapCount = 1000;
        const int w = 20, h = 20;
        int totalCells = 0;
        int grassCount = 0, forestCount = 0, waterCount = 0;

        for (int seed = 0; seed < mapCount; seed++)
        {
            var grid = MapGenerator.Generate(w, h, seed);
            foreach (var cell in grid.AllCells())
            {
                totalCells++;
                switch (cell.Terrain)
                {
                    case TerrainType.Grass: grassCount++; break;
                    case TerrainType.Forest: forestCount++; break;
                    case TerrainType.Water: waterCount++; break;
                }
            }
        }

        double grassPct = (double)grassCount / totalCells;
        double forestPct = (double)forestCount / totalCells;
        double waterPct = (double)waterCount / totalCells;

        Assert.True(grassPct >= 0.45 && grassPct <= 0.65,
            $"Grass %={grassPct:P1} is outside expected 45-65%.");
        Assert.True(forestPct >= 0.05 && forestPct <= 0.20,
            $"Forest %={forestPct:P1} is outside expected 5-20%.");
        Assert.True(waterPct >= 0.05 && waterPct <= 0.15,
            $"Water %={waterPct:P1} is outside expected 5-15%.");
    }
}
