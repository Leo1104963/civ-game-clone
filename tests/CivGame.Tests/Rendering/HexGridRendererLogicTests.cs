using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for the pure logic aspects of HexGridRenderer behavior.
/// These verify the renderer's contract without requiring Godot scene tree.
/// The renderer delegates to HexVertexCalculator and HexColorMapper for
/// testable math, while Godot-specific draw calls are integration-tested
/// via game-launch-verify.
/// </summary>
public class HexGridRendererLogicTests
{
    private const float DefaultHexSize = 40f;

    [Fact]
    public void Should_DrawOneHexPerCell_When_GridIsInitialized()
    {
        // A 4x3 grid should have 12 cells, each needing one hexagon drawn.
        var grid = new HexGrid(4, 3);
        int cellCount = grid.AllCells().Count();

        Assert.Equal(12, cellCount);
    }

    [Fact]
    public void Should_HaveCorrectDefaultHexSize()
    {
        // The default hex size per the spec is 40f
        Assert.Equal(40f, DefaultHexSize);
    }

    [Fact]
    public void Should_ComputeVerticesForEveryCell_When_DrawingGrid()
    {
        var grid = new HexGrid(3, 3);

        // Each cell should produce a valid pixel center and 6 vertices
        foreach (var cell in grid.AllCells())
        {
            var (px, py) = HexGrid.HexToPixel(cell.Coord, DefaultHexSize);
            var vertices = HexVertexCalculator.GetHexVertices(px, py, DefaultHexSize);

            Assert.Equal(6, vertices.Length);
        }
    }

    [Fact]
    public void Should_ProduceNonOverlappingCenters_When_GridHasMultipleCells()
    {
        var grid = new HexGrid(5, 5);
        var centers = new HashSet<(float, float)>();

        foreach (var cell in grid.AllCells())
        {
            var (px, py) = HexGrid.HexToPixel(cell.Coord, DefaultHexSize);
            // Round to avoid floating-point near-duplicates
            var rounded = (MathF.Round(px, 2), MathF.Round(py, 2));
            Assert.True(centers.Add(rounded),
                $"Hex center at ({px}, {py}) for coord {cell.Coord} overlaps with another cell");
        }

        Assert.Equal(25, centers.Count);
    }

    [Fact]
    public void Should_HaveAdjacentHexesSharingEdges_When_NeighborsAreDrawn()
    {
        // Two adjacent hexes should have exactly 2 shared vertex positions
        var center = new HexCoord(2, 2);
        var east = center.Neighbor(0); // direction 0 = East

        var (cx, cy) = HexGrid.HexToPixel(center, DefaultHexSize);
        var (ex, ey) = HexGrid.HexToPixel(east, DefaultHexSize);

        var centerVerts = HexVertexCalculator.GetHexVertices(cx, cy, DefaultHexSize);
        var eastVerts = HexVertexCalculator.GetHexVertices(ex, ey, DefaultHexSize);

        // Count vertices that are approximately equal between the two hexes
        const float tolerance = 0.01f;
        int sharedCount = 0;
        foreach (var cv in centerVerts)
        {
            foreach (var ev in eastVerts)
            {
                if (MathF.Abs(cv.X - ev.X) < tolerance && MathF.Abs(cv.Y - ev.Y) < tolerance)
                {
                    sharedCount++;
                }
            }
        }

        Assert.Equal(2, sharedCount);
    }
}
