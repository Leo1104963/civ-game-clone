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
            TerrainType.Grass => (0.3f, 0.7f, 0.2f),
            _ => (0.5f, 0.5f, 0.5f),
        };
    }

    /// <summary>
    /// Returns the color used for hex grid outlines.
    /// </summary>
    public static (float R, float G, float B) GetGridLineColor()
    {
        return (0.2f, 0.5f, 0.15f);
    }
}
