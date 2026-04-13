using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Pure helper for minimap coordinate math. Stub — full implementation in #93.
/// </summary>
public static class MinimapMath
{
    public static (float X, float Y, float W, float H) GridToMinimapRect(
        HexCoord coord,
        (float W, float H) minimapSize,
        int gridW,
        int gridH)
    {
        throw new NotImplementedException("MinimapMath not yet implemented — see issue #93.");
    }
}
