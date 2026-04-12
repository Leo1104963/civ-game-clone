namespace CivGame.Rendering;

/// <summary>
/// Pure math helper that computes the vertices of a flat-top hexagon.
/// First vertex at 0 degrees (east), then every 60 degrees counter-clockwise.
/// </summary>
public static class HexVertexCalculator
{
    /// <summary>
    /// Returns the 6 vertices of a flat-top hexagon centered at (cx, cy) with
    /// the given size (center-to-vertex distance).
    /// </summary>
    public static (float X, float Y)[] GetHexVertices(float cx, float cy, float size)
    {
        var vertices = new (float X, float Y)[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i;
            float angleRad = angleDeg * MathF.PI / 180f;
            vertices[i] = (
                cx + size * MathF.Cos(angleRad),
                cy + size * MathF.Sin(angleRad)
            );
        }
        return vertices;
    }
}
