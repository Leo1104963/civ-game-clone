namespace CivGame.World;

/// <summary>
/// Axial hex coordinate using cube-coordinate math for a flat-top hex grid.
/// Cube coordinates: q, r, s where s = -q - r.
/// </summary>
public readonly record struct HexCoord(int Q, int R)
{
    /// <summary>Derived cube coordinate S = -Q - R.</summary>
    public int S => -Q - R;

    // Cube-coordinate direction vectors for flat-top hex (E, NE, NW, W, SW, SE)
    private static readonly (int Dq, int Dr)[] CubeDirections =
    {
        (+1,  0), (+1, -1), ( 0, -1),
        (-1,  0), (-1, +1), ( 0, +1),
    };

    /// <summary>
    /// Returns the neighbor in the given direction.
    /// 0=East, 1=NE, 2=NW, 3=West, 4=SW, 5=SE.
    /// </summary>
    public HexCoord Neighbor(int direction)
    {
        var (dq, dr) = CubeDirections[((direction % 6) + 6) % 6];
        return new HexCoord(Q + dq, R + dr);
    }

    /// <summary>All six neighbors in order (E, NE, NW, W, SW, SE).</summary>
    public IReadOnlyList<HexCoord> Neighbors()
    {
        var result = new HexCoord[6];
        for (int i = 0; i < 6; i++)
        {
            result[i] = Neighbor(i);
        }
        return result;
    }

    /// <summary>Manhattan distance in cube coordinates.</summary>
    public int DistanceTo(HexCoord other)
    {
        return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
    }

    public override string ToString() => $"({Q}, {R})";
}
