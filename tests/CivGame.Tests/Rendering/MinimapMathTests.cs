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

    // --- Guard: out-of-bounds coord ---

    [Fact]
    public void Should_ThrowOrClamp_When_CoordIsOutOfBounds()
    {
        var minimap = (W: 200f, H: 160f);

        // The spec says either throw or clamp — enforce that it does not silently
        // produce a negative rect origin by wrapping the call
        bool threw = false;
        (float X, float Y, float W, float H) result = default;

        try
        {
            result = MinimapMath.GridToMinimapRect(new HexCoord(-1, 0), minimap, gridW: 10, gridH: 8);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        // Either it threw, or it clamped to a non-negative X
        if (!threw)
            Assert.True(result.X >= 0f, "Out-of-bounds coord must not produce a negative rect origin");
    }

    // --- Guard: zero minimap size would cause divide-by-zero ---

    [Fact]
    public void Should_ThrowArgumentException_When_MinimapSizeIsZero()
    {
        var zeroSize = (W: 0f, H: 0f);
        Assert.Throws<ArgumentException>(() =>
            MinimapMath.GridToMinimapRect(new HexCoord(0, 0), zeroSize, gridW: 10, gridH: 8));
    }
}
