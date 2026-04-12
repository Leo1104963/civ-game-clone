namespace CivGame.World;

/// <summary>
/// Rectangular hex grid using axial coordinates. Flat-top hex layout.
/// Q ranges from 0 to Width-1, R ranges from 0 to Height-1.
/// </summary>
public sealed class HexGrid
{
    private readonly HexCell[,] _cells;

    public int Width { get; }
    public int Height { get; }

    public HexGrid(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        _cells = new HexCell[width, height];

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                _cells[q, r] = new HexCell(new HexCoord(q, r));
            }
        }
    }

    /// <summary>Returns the cell at the given coordinate, or null if out of bounds.</summary>
    public HexCell? GetCell(HexCoord coord)
    {
        if (!InBounds(coord)) return null;
        return _cells[coord.Q, coord.R];
    }

    /// <summary>True if the coordinate is within grid bounds.</summary>
    public bool InBounds(HexCoord coord)
    {
        return coord.Q >= 0 && coord.Q < Width && coord.R >= 0 && coord.R < Height;
    }

    /// <summary>All cells in the grid, row by row.</summary>
    public IEnumerable<HexCell> AllCells()
    {
        for (int q = 0; q < Width; q++)
        {
            for (int r = 0; r < Height; r++)
            {
                yield return _cells[q, r];
            }
        }
    }

    /// <summary>Returns all in-bounds neighbors of the given coordinate.</summary>
    public IReadOnlyList<HexCell> GetNeighbors(HexCoord coord)
    {
        var neighbors = new List<HexCell>(6);
        foreach (var neighbor in coord.Neighbors())
        {
            var cell = GetCell(neighbor);
            if (cell is not null)
            {
                neighbors.Add(cell);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// Convert axial coordinate to 2D pixel position (center of hex).
    /// Uses flat-top hex layout.
    /// </summary>
    public static (float X, float Y) HexToPixel(HexCoord coord, float hexSize)
    {
        float x = hexSize * (3f / 2f * coord.Q);
        float y = hexSize * (MathF.Sqrt(3f) / 2f * coord.Q + MathF.Sqrt(3f) * coord.R);
        return (x, y);
    }

    /// <summary>
    /// Convert pixel position to nearest axial coordinate.
    /// Uses flat-top hex layout.
    /// </summary>
    public static HexCoord PixelToHex(float x, float y, float hexSize)
    {
        float q = (2f / 3f * x) / hexSize;
        float r = (-1f / 3f * x + MathF.Sqrt(3f) / 3f * y) / hexSize;

        float s = -q - r;
        int roundQ = (int)MathF.Round(q);
        int roundR = (int)MathF.Round(r);
        int roundS = (int)MathF.Round(s);

        float qDiff = MathF.Abs(roundQ - q);
        float rDiff = MathF.Abs(roundR - r);
        float sDiff = MathF.Abs(roundS - s);

        if (qDiff > rDiff && qDiff > sDiff)
        {
            roundQ = -roundR - roundS;
        }
        else if (rDiff > sDiff)
        {
            roundR = -roundQ - roundS;
        }

        return new HexCoord(roundQ, roundR);
    }
}
