using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for terrain-per-type and unit-per-type rendering colors introduced in issue #51.
/// These tests define the executable spec for HexColorMapper.GetTerrainColor (all terrain types)
/// and UnitRenderer.GetUnitColor.
/// </summary>
public class TerrainRenderingTests
{
    // -------------------------------------------------------------------------
    // HexColorMapper — per-terrain-type colors
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_ReturnGrassColor_When_TerrainIsGrass()
    {
        var (r, g, b) = HexColorMapper.GetTerrainColor(TerrainType.Grass);

        Assert.Equal(0.30f, r, precision: 2);
        Assert.Equal(0.70f, g, precision: 2);
        Assert.Equal(0.20f, b, precision: 2);
    }

    [Fact]
    public void Should_ReturnPlainsColor_When_TerrainIsPlains()
    {
        var (r, g, b) = HexColorMapper.GetTerrainColor(TerrainType.Plains);

        Assert.Equal(0.80f, r, precision: 2);
        Assert.Equal(0.75f, g, precision: 2);
        Assert.Equal(0.35f, b, precision: 2);
    }

    [Fact]
    public void Should_ReturnForestColor_When_TerrainIsForest()
    {
        var (r, g, b) = HexColorMapper.GetTerrainColor(TerrainType.Forest);

        Assert.Equal(0.15f, r, precision: 2);
        Assert.Equal(0.45f, g, precision: 2);
        Assert.Equal(0.15f, b, precision: 2);
    }

    [Fact]
    public void Should_ReturnWaterColor_When_TerrainIsWater()
    {
        var (r, g, b) = HexColorMapper.GetTerrainColor(TerrainType.Water);

        Assert.Equal(0.20f, r, precision: 2);
        Assert.Equal(0.45f, g, precision: 2);
        Assert.Equal(0.85f, b, precision: 2);
    }

    [Fact]
    public void Should_ReturnDistinctColors_When_AllTerrainTypesCompared()
    {
        // No two terrain types should share the same (R, G, B) tuple.
        var types = Enum.GetValues<TerrainType>();
        var colors = types.Select(t => HexColorMapper.GetTerrainColor(t)).ToList();

        // Using string representation for equality because tuples of floats compare by value
        var distinct = colors
            .Select(c => $"{c.R:F2},{c.G:F2},{c.B:F2}")
            .Distinct()
            .Count();

        Assert.Equal(types.Length, distinct);
    }

    // -------------------------------------------------------------------------
    // UnitRenderer.GetUnitColor — per-unit-type colors
    // -------------------------------------------------------------------------

    [Fact]
    public void Should_ReturnWarriorColor_When_UnitTypeIsWarrior()
    {
        var (r, g, b) = UnitRenderer.GetUnitColor("Warrior");

        Assert.Equal(0.80f, r, precision: 2);
        Assert.Equal(0.20f, g, precision: 2);
        Assert.Equal(0.20f, b, precision: 2);
    }

    [Fact]
    public void Should_ReturnSettlerColor_When_UnitTypeIsSettler()
    {
        var (r, g, b) = UnitRenderer.GetUnitColor("Settler");

        Assert.Equal(0.95f, r, precision: 2);
        Assert.Equal(0.95f, g, precision: 2);
        Assert.Equal(0.95f, b, precision: 2);
    }

    [Fact]
    public void Should_ReturnFallbackColor_When_UnitTypeIsUnknown()
    {
        // Must not throw; must return a valid grey fallback
        var (r, g, b) = UnitRenderer.GetUnitColor("Unknown");

        Assert.InRange(r, 0f, 1f);
        Assert.InRange(g, 0f, 1f);
        Assert.InRange(b, 0f, 1f);
    }

    [Fact]
    public void Should_ReturnFallbackColor_When_UnitTypeIsEmpty()
    {
        // Edge case: empty string should not throw
        var (r, g, b) = UnitRenderer.GetUnitColor(string.Empty);

        Assert.InRange(r, 0f, 1f);
        Assert.InRange(g, 0f, 1f);
        Assert.InRange(b, 0f, 1f);
    }

    [Fact]
    public void Should_ReturnSelectedUnitColor_When_SelectedColorQueried()
    {
        // SelectedUnitColor must be yellow as per spec
        var (r, g, b) = UnitRenderer.SelectedUnitColor;

        Assert.Equal(1.0f, r, precision: 2);
        Assert.Equal(0.9f, g, precision: 2);
        Assert.Equal(0.2f, b, precision: 2);
    }
}
