using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for MinimapMath.GridToMinimapRect (issue #93).
/// All types use primitives — no Godot runtime required.
/// </summary>
public class MinimapMathTests
{
    // --- Corner cells ---

    [Fact]
    public void Should_MapToTopLeft_When_CoordIsOrigin()
    {
        var minimap = (W: 200f, H: 160f);
        var rect = MinimapMath.GridToMinimapRect(new HexCoord(0, 0), minimap, gridW: 10, gridH: 8);

        Assert.Equal(0f, rect.X, precision: 4);
        Assert.Equal(0f, rect.Y, precision: 4);
    }

    [Fact]
    public void Should_MapToBottomRight_When_CoordIsLastCell()
    {
        var minimap = (W: 200f, H: 160f);
        int gridW = 10, gridH = 8;
        var rect = MinimapMath.GridToMinimapRect(new HexCoord(gridW - 1, gridH - 1), minimap, gridW, gridH);

        float cellW = minimap.W / gridW;
        float cellH = minimap.H / gridH;
        float expectedX = (gridW - 1) * cellW;
        float expectedY = (gridH - 1) * cellH;

        Assert.Equal(expectedX, rect.X, precision: 4);
        Assert.Equal(expectedY, rect.Y, precision: 4);
        Assert.Equal(cellW, rect.W, precision: 4);
        Assert.Equal(cellH, rect.H, precision: 4);
    }

    // --- Cell size derivation ---

    [Fact]
    public void Should_ComputeCellSizeFromMinimapDividedByGrid()
    {
        var minimap = (W: 200f, H: 160f);
        var rect = MinimapMath.GridToMinimapRect(new HexCoord(0, 0), minimap, gridW: 10, gridH: 8);

        Assert.Equal(20f, rect.W, precision: 4);
        Assert.Equal(20f, rect.H, precision: 4);
    }

    [Fact]
    public void Should_FillFullMinimapArea_When_GridIsSingleCell()
    {
        var minimap = (W: 200f, H: 160f);
        var rect = MinimapMath.GridToMinimapRect(new HexCoord(0, 0), minimap, gridW: 1, gridH: 1);

        Assert.Equal(0f, rect.X, precision: 4);
        Assert.Equal(0f, rect.Y, precision: 4);
        Assert.Equal(200f, rect.W, precision: 4);
        Assert.Equal(160f, rect.H, precision: 4);
    }

    // --- Arbitrary interior cell ---

    [Fact]
    public void Should_ComputeCorrectOrigin_When_CoordIsInterior()
    {
        var minimap = (W: 100f, H: 80f);
        int gridW = 10, gridH = 8;
        var rect = MinimapMath.GridToMinimapRect(new HexCoord(3, 2), minimap, gridW, gridH);

        float expectedX = 3 * (100f / 10);
        float expectedY = 2 * (80f / 8);

        Assert.Equal(expectedX, rect.X, precision: 4);
        Assert.Equal(expectedY, rect.Y, precision: 4);
    }

    // --- Out-of-bounds coord: clamp to nearest edge (no throw) ---

    [Fact]
    public void Should_ClampToOrigin_When_CoordIsNegative()
    {
        var minimap = (W: 200f, H: 160f);
        var rect = MinimapMath.GridToMinimapRect(new HexCoord(-1, 0), minimap, gridW: 10, gridH: 8);

        Assert.Equal(0f, rect.X, precision: 4);
    }

    [Fact]
    public void Should_ClampToMaxCell_When_CoordExceedsGridBounds()
    {
        var minimap = (W: 200f, H: 160f);
        int gridW = 10, gridH = 8;
        var rect = MinimapMath.GridToMinimapRect(new HexCoord(99, 0), minimap, gridW, gridH);

        float maxX = (gridW - 1) * (minimap.W / gridW);
        Assert.Equal(maxX, rect.X, precision: 4);
    }

    // --- Guard: zero minimap size ---

    [Fact]
    public void Should_ThrowArgumentException_When_MinimapSizeIsZero()
    {
        var zeroSize = (W: 0f, H: 0f);
        Assert.Throws<ArgumentException>(() =>
            MinimapMath.GridToMinimapRect(new HexCoord(0, 0), zeroSize, gridW: 10, gridH: 8));
    }

    // --- MinimapMath.MinimapToGrid ---

    [Fact]
    public void Should_ReturnOrigin_When_ClickIsTopLeft()
    {
        var minimap = (W: 200f, H: 160f);
        var coord = MinimapMath.MinimapToGrid((X: 0f, Y: 0f), minimap, gridW: 10, gridH: 8);
        Assert.Equal(new HexCoord(0, 0), coord);
    }

    [Fact]
    public void Should_ReturnLastCell_When_ClickIsBottomRight()
    {
        var minimap = (W: 200f, H: 160f);
        int gridW = 10, gridH = 8;
        var coord = MinimapMath.MinimapToGrid((X: minimap.W - 1f, Y: minimap.H - 1f), minimap, gridW, gridH);
        Assert.Equal(new HexCoord(gridW - 1, gridH - 1), coord);
    }

    [Fact]
    public void Should_RoundTrip_When_ClickIsCenterOfCell()
    {
        var minimap = (W: 200f, H: 160f);
        int gridW = 10, gridH = 8;
        var origin = new HexCoord(3, 2);
        var rect = MinimapMath.GridToMinimapRect(origin, minimap, gridW, gridH);
        var center = (X: rect.X + rect.W / 2f, Y: rect.Y + rect.H / 2f);

        var result = MinimapMath.MinimapToGrid(center, minimap, gridW, gridH);
        Assert.Equal(origin, result);
    }

    [Fact]
    public void Should_ClampToValidCoord_When_ClickIsOutOfMinimapBounds()
    {
        var minimap = (W: 200f, H: 160f);
        int gridW = 10, gridH = 8;

        // Click well outside minimap bounds — must not throw, must return a valid coord
        var coord = MinimapMath.MinimapToGrid((X: 999f, Y: 999f), minimap, gridW, gridH);
        Assert.InRange(coord.Q, 0, gridW - 1);
        Assert.InRange(coord.R, 0, gridH - 1);
    }
}
