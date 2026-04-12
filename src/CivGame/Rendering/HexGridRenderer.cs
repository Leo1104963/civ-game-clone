using Godot;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Draws a HexGrid as colored flat-top hexagons.
/// Attach this as a child of a Node2D in the scene tree.
/// </summary>
public partial class HexGridRenderer : Node2D
{
    private HexGrid? _grid;

    [Export]
    public float HexSize { get; set; } = 40f;

    private static readonly Color GrassColor = new(0.3f, 0.7f, 0.2f);
    private static readonly Color GridLineColor = new(0.2f, 0.5f, 0.15f);

    /// <summary>Bind to a HexGrid data model and trigger redraw.</summary>
    public void Initialize(HexGrid grid)
    {
        _grid = grid;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_grid is null) return;

        foreach (var cell in _grid.AllCells())
        {
            var (px, py) = HexGrid.HexToPixel(cell.Coord, HexSize);
            var center = new Vector2(px, py);
            var vertices = HexVertexCalculator.GetHexVertices(px, py, HexSize);

            // Build a Vector2 array for the polygon
            var polyVerts = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                polyVerts[i] = new Vector2(vertices[i].X, vertices[i].Y);
            }

            // Determine fill color from terrain
            var (r, g, b) = HexColorMapper.GetTerrainColor(cell.Terrain);
            var fillColor = new Color(r, g, b);

            // Fill
            DrawColoredPolygon(polyVerts, fillColor);

            // Outline
            var (lr, lg, lb) = HexColorMapper.GetGridLineColor();
            var lineColor = new Color(lr, lg, lb);
            for (int i = 0; i < polyVerts.Length; i++)
            {
                int next = (i + 1) % polyVerts.Length;
                DrawLine(polyVerts[i], polyVerts[next], lineColor, 1.5f);
            }
        }
    }

    /// <summary>Convert a screen position to the nearest hex coordinate, or null if out of bounds.</summary>
    public HexCoord? ScreenToHex(Vector2 screenPos)
    {
        if (_grid is null) return null;

        // Convert screen position to local position (accounts for camera/transform)
        var localPos = ToLocal(screenPos);
        var coord = HexGrid.PixelToHex(localPos.X, localPos.Y, HexSize);

        return _grid.InBounds(coord) ? coord : null;
    }
}
