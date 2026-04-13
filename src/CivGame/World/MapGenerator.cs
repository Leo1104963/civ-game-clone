namespace CivGame.World;

/// <summary>
/// Deterministic map generator. Populates a HexGrid with a mixed-terrain
/// distribution from a seeded pseudo-random stream.
/// </summary>
public static class MapGenerator
{
    // Cumulative thresholds over a uniform [0, 1) roll.
    // Adjusting these tweaks the distribution.
    private const double GrassCutoff = 0.55;  // [0.00, 0.55) -> Grass
    private const double PlainsCutoff = 0.75; // [0.55, 0.75) -> Plains
    private const double ForestCutoff = 0.90; // [0.75, 0.90) -> Forest
                                              // [0.90, 1.00) -> Water

    /// <summary>
    /// Generate a HexGrid of the given size, populating each cell with a terrain
    /// chosen from a seeded distribution. The same (width, height, seed) always
    /// yields an identical grid.
    /// </summary>
    public static HexGrid Generate(int width, int height, int seed)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        var grid = new HexGrid(width, height);
        var rng = new Random(seed);

        foreach (var cell in grid.AllCells())
        {
            double roll = rng.NextDouble();
            cell.Terrain = PickTerrain(roll);
        }

        // Guarantee a passable capital spawn and surrounding ring.
        var capital = new HexCoord(width / 2, height / 2);
        var capitalCell = grid.GetCell(capital);
        if (capitalCell is not null)
        {
            capitalCell.Terrain = TerrainType.Grass;
        }

        foreach (var neighbor in grid.GetNeighbors(capital))
        {
            if (!TerrainRules.IsPassable(neighbor.Terrain))
            {
                neighbor.Terrain = TerrainType.Grass;
            }
        }

        return grid;
    }

    private static TerrainType PickTerrain(double roll)
    {
        if (roll < GrassCutoff) return TerrainType.Grass;
        if (roll < PlainsCutoff) return TerrainType.Plains;
        if (roll < ForestCutoff) return TerrainType.Forest;
        return TerrainType.Water;
    }
}
