using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Draws semi-transparent highlight hexagons over cells a selected unit can reach.
/// In Godot context, attach as a Node2D child — ShowReachable/Clear trigger redraw.
/// </summary>
public class MovementOverlay
{
    private IReadOnlySet<HexCoord>? _reachableCells;
    private HexGrid? _grid;
    private float _hexSize;

    public static readonly (float R, float G, float B, float A) HighlightColor = (0.2f, 0.5f, 0.9f, 0.3f);

    public IReadOnlySet<HexCoord>? ReachableCells => _reachableCells;
    public HexGrid? Grid => _grid;
    public float HexSize => _hexSize;

    public void ShowReachable(IReadOnlySet<HexCoord> cells, HexGrid grid, float hexSize)
    {
        _reachableCells = cells;
        _grid = grid;
        _hexSize = hexSize;
    }

    public void Clear()
    {
        _reachableCells = null;
    }

    /// <summary>
    /// Compute hex vertices for a highlight overlay (inset by 10%).
    /// </summary>
    public static (float X, float Y)[] GetHexVertices(float centerX, float centerY, float size)
    {
        var vertices = new (float X, float Y)[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i;
            float angleRad = angleDeg * MathF.PI / 180f;
            vertices[i] = (
                centerX + size * MathF.Cos(angleRad),
                centerY + size * MathF.Sin(angleRad)
            );
        }
        return vertices;
    }
}
