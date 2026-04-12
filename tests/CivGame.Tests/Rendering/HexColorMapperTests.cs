using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for terrain-to-color mapping used by HexGridRenderer.
/// Each terrain type should map to a specific, distinct color.
/// </summary>
public class HexColorMapperTests
{
    [Fact]
    public void Should_ReturnGreenColor_When_TerrainIsGrass()
    {
        var (r, g, b) = HexColorMapper.GetTerrainColor(TerrainType.Grass);

        // Grass should be green: G component should be dominant
        Assert.True(g > r, "Green component should be greater than red for grass");
        Assert.True(g > b, "Green component should be greater than blue for grass");
    }

    [Fact]
    public void Should_ReturnValidColor_When_AnyTerrainType()
    {
        foreach (TerrainType terrain in Enum.GetValues<TerrainType>())
        {
            var (r, g, b) = HexColorMapper.GetTerrainColor(terrain);

            Assert.InRange(r, 0f, 1f);
            Assert.InRange(g, 0f, 1f);
            Assert.InRange(b, 0f, 1f);
        }
    }

    [Fact]
    public void Should_ReturnDarkerGreen_When_GettingGridLineColor()
    {
        var (fillR, fillG, fillB) = HexColorMapper.GetTerrainColor(TerrainType.Grass);
        var (lineR, lineG, lineB) = HexColorMapper.GetGridLineColor();

        // Grid line color should be darker than fill
        float fillBrightness = fillR + fillG + fillB;
        float lineBrightness = lineR + lineG + lineB;
        Assert.True(lineBrightness < fillBrightness,
            "Grid line should be darker than terrain fill color");
    }
}
