namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for CameraClamp and CameraZoom pure helpers (issue #92).
/// All types use primitives — no Godot runtime required.
/// </summary>
public class CameraHelpersTests
{
    // --- CameraZoom.ClampZoom ---

    [Fact]
    public void Should_ReturnMin_When_ZoomBelowMinimum()
    {
        float result = CameraZoom.ClampZoom(0.1f, min: 0.5f, max: 2.0f);
        Assert.Equal(0.5f, result, precision: 4);
    }

    [Fact]
    public void Should_ReturnMax_When_ZoomAboveMaximum()
    {
        float result = CameraZoom.ClampZoom(5.0f, min: 0.5f, max: 2.0f);
        Assert.Equal(2.0f, result, precision: 4);
    }

    [Fact]
    public void Should_ReturnZoom_When_ZoomWithinBounds()
    {
        float result = CameraZoom.ClampZoom(1.0f, min: 0.5f, max: 2.0f);
        Assert.Equal(1.0f, result, precision: 4);
    }

    [Fact]
    public void Should_ReturnMin_When_ZoomEqualsMin()
    {
        float result = CameraZoom.ClampZoom(0.5f, min: 0.5f, max: 2.0f);
        Assert.Equal(0.5f, result, precision: 4);
    }

    [Fact]
    public void Should_ReturnMax_When_ZoomEqualsMax()
    {
        float result = CameraZoom.ClampZoom(2.0f, min: 0.5f, max: 2.0f);
        Assert.Equal(2.0f, result, precision: 4);
    }

    [Fact]
    public void Should_Throw_When_MinIsGreaterThanMax()
    {
        Assert.Throws<ArgumentException>(() => CameraZoom.ClampZoom(1.0f, min: 3.0f, max: 1.0f));
    }

    // --- CameraClamp.ClampPosition ---

    [Fact]
    public void ClampPosition_ThrowsArgumentException_WhenBoundsAreDegenerate()
    {
        var pos = (X: 100f, Y: 100f);
        var bounds = (MinX: 300f, MinY: 0f, MaxX: 100f, MaxY: 400f); // MinX > MaxX
        Assert.Throws<ArgumentException>(() => CameraClamp.ClampPosition(pos, bounds));
    }

    [Fact]
    public void Should_ReturnPosition_When_PositionWithinBounds()
    {
        var pos = (X: 100f, Y: 100f);
        var bounds = (MinX: 0f, MinY: 0f, MaxX: 500f, MaxY: 400f);

        var result = CameraClamp.ClampPosition(pos, bounds);

        Assert.Equal(100f, result.X, precision: 4);
        Assert.Equal(100f, result.Y, precision: 4);
    }

    [Fact]
    public void Should_ClampToMinX_When_XTooSmall()
    {
        var pos = (X: -50f, Y: 100f);
        var bounds = (MinX: 0f, MinY: 0f, MaxX: 500f, MaxY: 400f);

        var result = CameraClamp.ClampPosition(pos, bounds);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(100f, result.Y, precision: 4);
    }

    [Fact]
    public void Should_ClampToMaxX_When_XTooLarge()
    {
        var pos = (X: 600f, Y: 100f);
        var bounds = (MinX: 0f, MinY: 0f, MaxX: 500f, MaxY: 400f);

        var result = CameraClamp.ClampPosition(pos, bounds);

        Assert.Equal(500f, result.X, precision: 4);
    }

    [Fact]
    public void Should_ClampToMinY_When_YTooSmall()
    {
        var pos = (X: 100f, Y: -10f);
        var bounds = (MinX: 0f, MinY: 0f, MaxX: 500f, MaxY: 400f);

        var result = CameraClamp.ClampPosition(pos, bounds);

        Assert.Equal(0f, result.Y, precision: 4);
    }

    [Fact]
    public void Should_ClampToMaxY_When_YTooLarge()
    {
        var pos = (X: 100f, Y: 500f);
        var bounds = (MinX: 0f, MinY: 0f, MaxX: 500f, MaxY: 400f);

        var result = CameraClamp.ClampPosition(pos, bounds);

        Assert.Equal(400f, result.Y, precision: 4);
    }

    [Fact]
    public void Should_ReturnBoundaryPosition_When_PositionExactlyOnBoundary()
    {
        var pos = (X: 0f, Y: 400f);
        var bounds = (MinX: 0f, MinY: 0f, MaxX: 500f, MaxY: 400f);

        var result = CameraClamp.ClampPosition(pos, bounds);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(400f, result.Y, precision: 4);
    }
}
