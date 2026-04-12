using System;
using System.Collections.Generic;
using System.Linq;
using CivGame.World;
using Xunit;

namespace CivGame.Tests.World;

public class HexGridTests
{
    // ------------------------------------------------------------------ //
    // HexCoord — value type, S derivation                                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_BeImmutableValueType_When_HexCoordCreated()
    {
        var a = new HexCoord(3, -1);
        var b = new HexCoord(3, -1);

        // Record structs support equality by value
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Should_DeriveSCorrectly_When_HexCoordCreated()
    {
        var coord = new HexCoord(2, 3);
        // S = -Q - R
        Assert.Equal(-2 - 3, coord.S);
    }

    [Fact]
    public void Should_DeriveSAsNegativeSumOfQAndR_When_VariousCoords()
    {
        var cases = new[]
        {
            new HexCoord(0, 0),
            new HexCoord(1, -1),
            new HexCoord(-3, 2),
            new HexCoord(5, 5),
        };

        foreach (var c in cases)
        {
            Assert.Equal(-c.Q - c.R, c.S);
        }
    }

    // ------------------------------------------------------------------ //
    // HexCoord.DistanceTo                                                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnZeroDistance_When_SameCoord()
    {
        var coord = new HexCoord(2, 3);
        Assert.Equal(0, coord.DistanceTo(coord));
    }

    [Fact]
    public void Should_ReturnOne_When_AdjacentCoords()
    {
        var origin = new HexCoord(0, 0);
        // Direct east neighbor: (1, 0)
        Assert.Equal(1, origin.DistanceTo(new HexCoord(1, 0)));
    }

    [Theory]
    [InlineData(0, 0, 3, -1, 3)]
    [InlineData(0, 0, 2, -2, 2)]
    [InlineData(1, 1, 4, -2, 3)]
    [InlineData(-1, 2, 2, -1, 3)]
    public void Should_ReturnCubeManhattanDistance_When_DistanceToOtherCoord(
        int q1, int r1, int q2, int r2, int expected)
    {
        var a = new HexCoord(q1, r1);
        var b = new HexCoord(q2, r2);
        Assert.Equal(expected, a.DistanceTo(b));
        // Symmetric
        Assert.Equal(expected, b.DistanceTo(a));
    }

    // ------------------------------------------------------------------ //
    // HexCoord.Neighbors()                                                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnExactlySixNeighbors_When_NeighborsCalled()
    {
        var coord = new HexCoord(0, 0);
        var neighbors = coord.Neighbors();
        Assert.Equal(6, neighbors.Count);
    }

    [Fact]
    public void Should_ReturnNeighborsAtDistanceOne_When_NeighborsCalled()
    {
        var coord = new HexCoord(2, -1);
        foreach (var neighbor in coord.Neighbors())
        {
            Assert.Equal(1, coord.DistanceTo(neighbor));
        }
    }

    [Fact]
    public void Should_ReturnUniqueNeighbors_When_NeighborsCalled()
    {
        var coord = new HexCoord(0, 0);
        var neighbors = coord.Neighbors();
        var distinct = neighbors.Distinct().ToList();
        Assert.Equal(6, distinct.Count);
    }

    // ------------------------------------------------------------------ //
    // HexCoord.Neighbor(direction)                                         //
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData(0, 1, 0)]   // 0 = East:  q+1, r+0
    [InlineData(1, 1, -1)]   // 1 = NE:    q+1, r-1
    [InlineData(2, 0, -1)]   // 2 = NW:    q+0, r-1
    [InlineData(3, -1, 0)]   // 3 = West:  q-1, r+0
    [InlineData(4, -1, 1)]   // 4 = SW:    q-1, r+1
    [InlineData(5, 0, 1)]   // 5 = SE:    q+0, r+1
    public void Should_ReturnCorrectNeighbor_When_DirectionGiven(int direction, int dq, int dr)
    {
        var origin = new HexCoord(0, 0);
        var expected = new HexCoord(dq, dr);
        Assert.Equal(expected, origin.Neighbor(direction));
    }

    [Fact]
    public void Should_WrapDirectionModulo6_When_DirectionOutOfRange()
    {
        var origin = new HexCoord(0, 0);
        // 6 should be same as 0 (East)
        Assert.Equal(origin.Neighbor(0), origin.Neighbor(6));
        // -1 should be same as 5 (SE)
        Assert.Equal(origin.Neighbor(5), origin.Neighbor(-1));
    }

    [Fact]
    public void Should_MatchNeighborsListOrder_When_NeighborDirectionIndexed()
    {
        var coord = new HexCoord(1, 2);
        var list = coord.Neighbors();
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(list[i], coord.Neighbor(i));
        }
    }

    // ------------------------------------------------------------------ //
    // HexGrid constructor                                                  //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_WidthIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexGrid(0, 5));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_HeightIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexGrid(5, 0));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_WidthIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexGrid(-1, 5));
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_HeightIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexGrid(5, -1));
    }

    [Fact]
    public void Should_StoreWidthAndHeight_When_GridCreated()
    {
        var grid = new HexGrid(4, 7);
        Assert.Equal(4, grid.Width);
        Assert.Equal(7, grid.Height);
    }

    [Fact]
    public void Should_CreateWidthTimesHeightCells_When_GridCreated()
    {
        var grid = new HexGrid(3, 4);
        Assert.Equal(12, grid.AllCells().Count());
    }

    // ------------------------------------------------------------------ //
    // HexGrid — default terrain and passability                            //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_InitializeAllCellsAsGrass_When_GridCreated()
    {
        var grid = new HexGrid(3, 3);
        foreach (var cell in grid.AllCells())
        {
            Assert.Equal(TerrainType.Grass, cell.Terrain);
        }
    }

    [Fact]
    public void Should_InitializeAllCellsAsPassable_When_GridCreated()
    {
        var grid = new HexGrid(3, 3);
        foreach (var cell in grid.AllCells())
        {
            Assert.True(cell.IsPassable);
        }
    }

    // ------------------------------------------------------------------ //
    // HexGrid.GetCell                                                      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnCell_When_CoordIsValid()
    {
        var grid = new HexGrid(5, 5);
        var cell = grid.GetCell(new HexCoord(2, 3));
        Assert.NotNull(cell);
        Assert.Equal(new HexCoord(2, 3), cell.Coord);
    }

    [Fact]
    public void Should_ReturnNull_When_CoordIsOutOfBounds()
    {
        var grid = new HexGrid(5, 5);
        Assert.Null(grid.GetCell(new HexCoord(-1, 0)));
        Assert.Null(grid.GetCell(new HexCoord(5, 0)));
        Assert.Null(grid.GetCell(new HexCoord(0, -1)));
        Assert.Null(grid.GetCell(new HexCoord(0, 5)));
    }

    [Fact]
    public void Should_ReturnCellAtCorners_When_CornerCoordsQueried()
    {
        var grid = new HexGrid(4, 6);
        Assert.NotNull(grid.GetCell(new HexCoord(0, 0)));
        Assert.NotNull(grid.GetCell(new HexCoord(3, 0)));
        Assert.NotNull(grid.GetCell(new HexCoord(0, 5)));
        Assert.NotNull(grid.GetCell(new HexCoord(3, 5)));
    }

    // ------------------------------------------------------------------ //
    // HexGrid.InBounds                                                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnTrue_When_CoordIsInsideGrid()
    {
        var grid = new HexGrid(5, 5);
        Assert.True(grid.InBounds(new HexCoord(0, 0)));
        Assert.True(grid.InBounds(new HexCoord(4, 4)));
        Assert.True(grid.InBounds(new HexCoord(2, 2)));
    }

    [Fact]
    public void Should_ReturnFalse_When_CoordIsOutsideGrid()
    {
        var grid = new HexGrid(5, 5);
        Assert.False(grid.InBounds(new HexCoord(-1, 0)));
        Assert.False(grid.InBounds(new HexCoord(5, 0)));
        Assert.False(grid.InBounds(new HexCoord(0, -1)));
        Assert.False(grid.InBounds(new HexCoord(0, 5)));
    }

    // ------------------------------------------------------------------ //
    // HexGrid.AllCells                                                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnAllCells_When_AllCellsCalled()
    {
        var grid = new HexGrid(4, 3);
        var cells = grid.AllCells().ToList();
        Assert.Equal(12, cells.Count);
    }

    [Fact]
    public void Should_ReturnCellsWithCorrectCoords_When_AllCellsCalled()
    {
        var grid = new HexGrid(3, 3);
        var coords = grid.AllCells().Select(c => c.Coord).ToHashSet();

        for (int q = 0; q < 3; q++)
            for (int r = 0; r < 3; r++)
                Assert.Contains(new HexCoord(q, r), coords);
    }

    // ------------------------------------------------------------------ //
    // HexGrid.GetNeighbors                                                 //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_ReturnSixNeighbors_When_CellIsInterior()
    {
        var grid = new HexGrid(5, 5);
        // Cell (2,2) is fully interior — all 6 neighbors should be in bounds
        var neighbors = grid.GetNeighbors(new HexCoord(2, 2));
        Assert.Equal(6, neighbors.Count);
    }

    [Fact]
    public void Should_ReturnFewerThanSixNeighbors_When_CellIsCorner()
    {
        var grid = new HexGrid(5, 5);
        var neighbors = grid.GetNeighbors(new HexCoord(0, 0));
        Assert.True(neighbors.Count < 6);
    }

    [Fact]
    public void Should_ReturnOnlyInBoundsNeighbors_When_GetNeighborsCalled()
    {
        var grid = new HexGrid(3, 3);
        foreach (var cell in grid.AllCells())
        {
            var neighbors = grid.GetNeighbors(cell.Coord);
            foreach (var neighbor in neighbors)
            {
                Assert.True(grid.InBounds(neighbor.Coord));
            }
        }
    }

    [Fact]
    public void Should_ReturnNeighborCellsAtDistanceOne_When_GetNeighborsCalled()
    {
        var grid = new HexGrid(5, 5);
        var center = new HexCoord(2, 2);
        foreach (var neighbor in grid.GetNeighbors(center))
        {
            Assert.Equal(1, center.DistanceTo(neighbor.Coord));
        }
    }

    // ------------------------------------------------------------------ //
    // HexGrid.HexToPixel / PixelToHex round-trip                          //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_RoundTripCoord_When_HexToPixelAndPixelToHexUsed()
    {
        const float hexSize = 64f;
        var grid = new HexGrid(8, 8);

        foreach (var cell in grid.AllCells())
        {
            var (x, y) = HexGrid.HexToPixel(cell.Coord, hexSize);
            var roundTripped = HexGrid.PixelToHex(x, y, hexSize);
            Assert.Equal(cell.Coord, roundTripped);
        }
    }

    [Fact]
    public void Should_ReturnOriginPixel_When_OriginCoordUsed()
    {
        var (x, y) = HexGrid.HexToPixel(new HexCoord(0, 0), 1f);
        Assert.Equal(0f, x, precision: 4);
        Assert.Equal(0f, y, precision: 4);
    }

    [Fact]
    public void Should_ReturnDifferentPixels_When_DifferentCoordsUsed()
    {
        const float hexSize = 32f;
        var a = HexGrid.HexToPixel(new HexCoord(1, 0), hexSize);
        var b = HexGrid.HexToPixel(new HexCoord(0, 1), hexSize);
        // Different coords must map to different pixel positions
        Assert.False(a.X == b.X && a.Y == b.Y);
    }

    // ------------------------------------------------------------------ //
    // HexCell                                                               //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_StoreCoord_When_CellRetrievedFromGrid()
    {
        var grid = new HexGrid(5, 5);
        var coord = new HexCoord(1, 3);
        var cell = grid.GetCell(coord);
        Assert.NotNull(cell);
        Assert.Equal(coord, cell!.Coord);
    }

    [Fact]
    public void Should_AllowTerrainChange_When_TerrainSet()
    {
        var grid = new HexGrid(3, 3);
        var cell = grid.GetCell(new HexCoord(1, 1))!;
        cell.Terrain = TerrainType.Grass; // same for now, but must be settable
        Assert.Equal(TerrainType.Grass, cell.Terrain);
    }

    [Fact]
    public void Should_AllowPassabilityChange_When_IsPassableSet()
    {
        var grid = new HexGrid(3, 3);
        var cell = grid.GetCell(new HexCoord(1, 1))!;
        cell.IsPassable = false;
        Assert.False(cell.IsPassable);
    }
}
