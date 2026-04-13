using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Pure helpers for terrain texture variant selection.
/// Variant index is chosen deterministically by coord hash so re-renders are stable.
/// </summary>
public static class TerrainTextureSet
{
    /// <summary>
    /// Returns a deterministic variant index in [0, variantCount) for the given terrain and coord.
    /// Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="variantCount"/> is not positive.
    /// </summary>
    public static int PickVariantIndex(TerrainType terrain, HexCoord coord, int variantCount)
    {
        if (variantCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(variantCount), "variantCount must be positive.");

        int hash = HashCode.Combine((int)terrain, coord.Q, coord.R);
        return Math.Abs(hash) % variantCount;
    }
}
