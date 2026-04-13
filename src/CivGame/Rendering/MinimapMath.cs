using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Pure helper for minimap coordinate math.
/// </summary>
public static class MinimapMath
{
    public static (float X, float Y, float W, float H) GridToMinimapRect(
        HexCoord coord,
        (float W, float H) minimapSize,
        int gridW,
        int gridH)
    {
        if (minimapSize.W <= 0f || minimapSize.H <= 0f)
            throw new ArgumentException("minimapSize must have positive width and height.");

        float cellW = minimapSize.W / gridW;
        float cellH = minimapSize.H / gridH;

        int clampedQ = Math.Clamp(coord.Q, 0, gridW - 1);
        int clampedR = Math.Clamp(coord.R, 0, gridH - 1);

        return (clampedQ * cellW, clampedR * cellH, cellW, cellH);
    }
}
