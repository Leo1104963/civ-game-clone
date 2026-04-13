namespace CivGame.Rendering;

/// <summary>
/// Returns a per-owner tint color as an (R, G, B) tuple in [0, 1].
/// Player 0 → white (1, 1, 1); player 1 (barbarian) → red-orange; negative ids → neutral grey;
/// other ids → stable palette derived from hash-to-HSV.
/// </summary>
public static class OwnerTintResolver
{
    private static readonly (float R, float G, float B) _barbarianTint = (0.95f, 0.35f, 0.25f);
    private static readonly (float R, float G, float B) _neutralGrey = (0.5f, 0.5f, 0.5f);

    public static (float R, float G, float B) GetTint(int ownerId)
    {
        if (ownerId < 0)
            return _neutralGrey;

        return ownerId switch
        {
            0 => (1f, 1f, 1f),
            1 => _barbarianTint,
            _ => HsvToRgb(((uint)ownerId * 2654435761u) % 360u / 360f, 0.7f, 0.9f),
        };
    }

    // Pure HSV → RGB conversion (H in [0,1), S and V in [0,1]).
    private static (float R, float G, float B) HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - Math.Abs((h * 6f) % 2f - 1f));
        float m = v - c;

        var (r1, g1, b1) = (int)(h * 6) switch
        {
            0 => (c, x, 0f),
            1 => (x, c, 0f),
            2 => (0f, c, x),
            3 => (0f, x, c),
            4 => (x, 0f, c),
            _ => (c, 0f, x),
        };

        return (r1 + m, g1 + m, b1 + m);
    }
}
