using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for fog-of-war rendering logic (issue #88).
/// Covers TileRenderStateResolver, HexColorMapper.Dim, and FogOfWarConstants.
/// All tests are headless — no Godot node instantiation.
/// </summary>
public class FogOfWarRenderingTests
{
    // --- TileRenderState enum ---

    [Fact]
    public void Should_ExposeHiddenDimFullValues_When_EnumDefined()
    {
        var values = Enum.GetValues<TileRenderState>();
        Assert.Contains(TileRenderState.Hidden, values);
        Assert.Contains(TileRenderState.Dim, values);
        Assert.Contains(TileRenderState.Full, values);
    }

    // --- TileRenderStateResolver.Resolve — full truth table ---

    [Fact]
    public void Should_ReturnFull_When_VisibilityIsVisible()
    {
        var result = TileRenderStateResolver.Resolve(VisibilityState.Visible);
        Assert.Equal(TileRenderState.Full, result);
    }

    [Fact]
    public void Should_ReturnDim_When_VisibilityIsExplored()
    {
        var result = TileRenderStateResolver.Resolve(VisibilityState.Explored);
        Assert.Equal(TileRenderState.Dim, result);
    }

    [Fact]
    public void Should_ReturnHidden_When_VisibilityIsUnseen()
    {
        var result = TileRenderStateResolver.Resolve(VisibilityState.Unseen);
        Assert.Equal(TileRenderState.Hidden, result);
    }

    [Fact]
    public void Should_ReturnFull_When_FogDisabledAndVisibilityIsUnseen()
    {
        var result = TileRenderStateResolver.Resolve(VisibilityState.Unseen, fogEnabled: false);
        Assert.Equal(TileRenderState.Full, result);
    }

    [Fact]
    public void Should_ReturnFull_When_FogDisabledAndVisibilityIsExplored()
    {
        var result = TileRenderStateResolver.Resolve(VisibilityState.Explored, fogEnabled: false);
        Assert.Equal(TileRenderState.Full, result);
    }

    [Fact]
    public void Should_ReturnFull_When_FogDisabledAndVisibilityIsVisible()
    {
        var result = TileRenderStateResolver.Resolve(VisibilityState.Visible, fogEnabled: false);
        Assert.Equal(TileRenderState.Full, result);
    }

    // --- FogOfWarConstants ---

    [Fact]
    public void Should_HaveDefaultDimFactorOfPointFive()
    {
        Assert.Equal(0.5f, FogOfWarConstants.DefaultDimFactor);
    }

    [Fact]
    public void Should_HaveFogOfWarColorWithLowBrightness()
    {
        var (r, g, b) = FogOfWarConstants.FogOfWarColor;
        Assert.Equal(0.05f, r, precision: 4);
        Assert.Equal(0.05f, g, precision: 4);
        Assert.Equal(0.08f, b, precision: 4);
    }

    // --- HexColorMapper.Dim ---

    [Fact]
    public void Should_HalveAllChannels_When_DimFactorIsPointFive()
    {
        var color = (R: 0.8f, G: 0.6f, B: 0.4f);
        (float R, float G, float B) dimmed = HexColorMapper.Dim(color, 0.5f);

        Assert.Equal(0.4f, dimmed.R, precision: 4);
        Assert.Equal(0.3f, dimmed.G, precision: 4);
        Assert.Equal(0.2f, dimmed.B, precision: 4);
    }

    [Fact]
    public void Should_ReturnOriginalColor_When_DimFactorIsOne()
    {
        var color = (R: 0.5f, G: 0.7f, B: 0.3f);
        (float R, float G, float B) dimmed = HexColorMapper.Dim(color, 1.0f);

        Assert.Equal(0.5f, dimmed.R, precision: 4);
        Assert.Equal(0.7f, dimmed.G, precision: 4);
        Assert.Equal(0.3f, dimmed.B, precision: 4);
    }

    [Fact]
    public void Should_ReturnBlack_When_DimFactorIsZero()
    {
        var color = (R: 1.0f, G: 1.0f, B: 1.0f);
        (float R, float G, float B) dimmed = HexColorMapper.Dim(color, 0.0f);

        Assert.Equal(0f, dimmed.R, precision: 4);
        Assert.Equal(0f, dimmed.G, precision: 4);
        Assert.Equal(0f, dimmed.B, precision: 4);
    }

    [Fact]
    public void Should_ProduceChannelsInValidRange_When_DimAppliedToTerrainColor()
    {
        foreach (var terrain in Enum.GetValues<TerrainType>())
        {
            var color = HexColorMapper.GetTerrainColor(terrain);
            (float R, float G, float B) dimmed = HexColorMapper.Dim(color, FogOfWarConstants.DefaultDimFactor);

            Assert.InRange(dimmed.R, 0f, 1f);
            Assert.InRange(dimmed.G, 0f, 1f);
            Assert.InRange(dimmed.B, 0f, 1f);
        }
    }

    [Fact]
    public void Should_ReturnDimmerColor_When_DefaultDimFactorApplied()
    {
        var color = (R: 0.6f, G: 0.7f, B: 0.5f);
        var dimmed = HexColorMapper.Dim(color, FogOfWarConstants.DefaultDimFactor);

        Assert.True(dimmed.R < color.R);
        Assert.True(dimmed.G < color.G);
        Assert.True(dimmed.B < color.B);
    }

    // --- VisibilityMap integration: resolver honours map state ---

    [Fact]
    public void Should_ReturnHidden_When_MapNeverRecomputedForViewer()
    {
        var grid = new HexGrid(3, 3);
        var map = new VisibilityMap(grid);

        // No Recompute called — all tiles default to Unseen
        var state = TileRenderStateResolver.Resolve(map.IsAt(0, new HexCoord(0, 0)));
        Assert.Equal(TileRenderState.Hidden, state);
    }

    [Fact]
    public void Should_ReturnFull_When_OnlyTileIsVisible()
    {
        var grid = new HexGrid(3, 3);
        var map = new VisibilityMap(grid);
        map.Recompute(0, new[] { new HexCoord(0, 0) }, sightRadius: 0);

        var state = TileRenderStateResolver.Resolve(map.IsAt(0, new HexCoord(0, 0)));
        Assert.Equal(TileRenderState.Full, state);
    }

    [Fact]
    public void Should_ReturnHidden_When_OutOfBoundsCoordQueried()
    {
        var grid = new HexGrid(3, 3);
        var map = new VisibilityMap(grid);

        // VisibilityMap.IsAt returns Unseen for out-of-bounds — resolver must return Hidden, never throw
        var visibility = map.IsAt(0, new HexCoord(99, 99));
        var state = TileRenderStateResolver.Resolve(visibility);
        Assert.Equal(TileRenderState.Hidden, state);
    }
}
