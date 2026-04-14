using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Maps terrain types to display colors and provides grid-line colors.
/// Colors are returned as (R, G, B) tuples with components in [0, 1].
/// </summary>
public static class HexColorMapper
{
    /// <summary>
    /// Returns the fill color for the given terrain type.
    /// </summary>
    public static (float R, float G, float B) GetTerrainColor(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Grass => (0.30f, 0.70f, 0.20f),
            TerrainType.Plains => (0.80f, 0.75f, 0.35f),
            TerrainType.Forest => (0.15f, 0.45f, 0.15f),
            TerrainType.Water => (0.20f, 0.45f, 0.85f),
            _ => (0.50f, 0.50f, 0.50f),
        };
    }

    /// <summary>
    /// Returns the color used for hex grid outlines.
    /// </summary>
    public static (float R, float G, float B) GetGridLineColor()
    {
        return (0.2f, 0.5f, 0.15f);
    }

    /// <summary>
    /// Multiplies each channel by <paramref name="factor"/>. Pure helper, no Godot dependency.
    /// </summary>
    public static (float R, float G, float B) Dim((float R, float G, float B) color, float factor)
    {
        return (color.R * factor, color.G * factor, color.B * factor);
    }
}
