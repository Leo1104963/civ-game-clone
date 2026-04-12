using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for screen-to-hex coordinate conversion logic used by HexGridRenderer.ScreenToHex.
/// These test the pure math without requiring a Godot scene tree.
/// </summary>
public class HexScreenConversionTests
{
    private const float DefaultHexSize = 40f;

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 4)]
    [InlineData(9, 9)]
    public void Should_RoundTripCoordinate_When_ConvertingHexToPixelAndBack(int q, int r)
    {
        // Convert hex to pixel center, then back to hex. Should get the same coord.
        var coord = new HexCoord(q, r);
        var (px, py) = HexGrid.HexToPixel(coord, DefaultHexSize);
        var result = HexGrid.PixelToHex(px, py, DefaultHexSize);

        Assert.Equal(coord, result);
    }

    [Fact]
    public void Should_ReturnNull_When_ScreenPositionIsOutsideGrid()
    {
        var grid = new HexGrid(10, 10);

        // A pixel position far outside the grid bounds
        var coord = HexGrid.PixelToHex(-1000f, -1000f, DefaultHexSize);
        bool inBounds = grid.InBounds(coord);

        Assert.False(inBounds, "Position far from grid should be out of bounds");
    }

    [Fact]
    public void Should_ReturnValidCoord_When_ScreenPositionIsInsideGrid()
    {
        var grid = new HexGrid(10, 10);

        // Pixel position at the center of hex (0,0) should be in bounds
        var (px, py) = HexGrid.HexToPixel(new HexCoord(0, 0), DefaultHexSize);
        var coord = HexGrid.PixelToHex(px, py, DefaultHexSize);

        Assert.True(grid.InBounds(coord), "Center of hex (0,0) should be in bounds");
        Assert.Equal(0, coord.Q);
        Assert.Equal(0, coord.R);
    }

    [Fact]
    public void Should_ReturnCorrectHex_When_ScreenPositionIsSlightlyOffCenter()
    {
        // Pixel position slightly offset from center of hex (5,5) should still resolve to (5,5)
        var target = new HexCoord(5, 5);
        var (px, py) = HexGrid.HexToPixel(target, DefaultHexSize);

        // Small offset that stays within the hex
        var coord = HexGrid.PixelToHex(px + 1f, py + 1f, DefaultHexSize);

        Assert.Equal(target, coord);
    }

    [Fact]
    public void Should_ReturnNullEquivalent_When_NegativeCoordinatesAreOutOfBounds()
    {
        var grid = new HexGrid(10, 10);

        // Pixel that maps to negative hex coords
        var (px, py) = HexGrid.HexToPixel(new HexCoord(-1, -1), DefaultHexSize);
        var coord = HexGrid.PixelToHex(px, py, DefaultHexSize);

        Assert.False(grid.InBounds(coord), "Negative coordinates should be out of bounds");
    }

    [Fact]
    public void Should_ReturnNullEquivalent_When_CoordinatesExceedGridDimensions()
    {
        var grid = new HexGrid(10, 10);

        // Pixel that maps to coords beyond grid size
        var (px, py) = HexGrid.HexToPixel(new HexCoord(10, 10), DefaultHexSize);
        var coord = HexGrid.PixelToHex(px, py, DefaultHexSize);

        Assert.False(grid.InBounds(coord), "Coordinates at grid dimension should be out of bounds");
    }

    [Theory]
    [InlineData(20f)]
    [InlineData(40f)]
    [InlineData(64f)]
    public void Should_RoundTripCorrectly_When_HexSizeVaries(float hexSize)
    {
        var coord = new HexCoord(3, 4);
        var (px, py) = HexGrid.HexToPixel(coord, hexSize);
        var result = HexGrid.PixelToHex(px, py, hexSize);

        Assert.Equal(coord, result);
    }

    [Fact]
    public void Should_ProduceDistinctPixelPositions_When_AdjacentHexes()
    {
        var center = new HexCoord(5, 5);
        var east = new HexCoord(6, 5);

        var (cx, cy) = HexGrid.HexToPixel(center, DefaultHexSize);
        var (ex, ey) = HexGrid.HexToPixel(east, DefaultHexSize);

        // Adjacent hexes should have distinct pixel positions
        Assert.NotEqual(cx, ex);
    }

    [Fact]
    public void Should_PlaceOriginHexAtZero_When_CoordIsZeroZero()
    {
        var (px, py) = HexGrid.HexToPixel(new HexCoord(0, 0), DefaultHexSize);

        Assert.Equal(0f, px);
        Assert.Equal(0f, py);
    }
}
