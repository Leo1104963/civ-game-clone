using Godot;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Draws a HexGrid as colored flat-top hexagons, with optional fog-of-war.
/// Attach this as a child of a Node2D in the scene tree.
/// </summary>
public partial class HexGridRenderer : Node2D
{
    private HexGrid? _grid;
    private VisibilityMap? _visibility;
    private int _viewerOwnerId;

    [Export]
    public float HexSize { get; set; } = 40f;

    [Export]
    public bool FogOfWarEnabled { get; set; } = true;

    /// <summary>Bind to a HexGrid data model and trigger redraw.</summary>
    public void Initialize(HexGrid grid, VisibilityMap? visibility = null, int viewerOwnerId = 0)
    {
        _grid = grid;
        _visibility = visibility;
        _viewerOwnerId = viewerOwnerId;
        QueueRedraw();
    }

    /// <summary>
    /// Returns the render state for a given coord. Returns Hidden for out-of-bounds.
    /// When no VisibilityMap is bound or FogOfWarEnabled is false, returns Full for all in-bounds coords.
    /// </summary>
    public TileRenderState GetTileRenderState(HexCoord coord)
    {
        if (_grid is null || !_grid.InBounds(coord))
            return TileRenderState.Hidden;

        if (_visibility is null || !FogOfWarEnabled)
            return TileRenderState.Full;

        return TileRenderStateResolver.Resolve(_visibility.IsAt(_viewerOwnerId, coord), FogOfWarEnabled);
    }

    public override void _Draw()
    {
        if (_grid is null) return;

        foreach (var cell in _grid.AllCells())
        {
            var (px, py) = HexGrid.HexToPixel(cell.Coord, HexSize);
            var vertices = HexVertexCalculator.GetHexVertices(px, py, HexSize);

            var polyVerts = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                polyVerts[i] = new Vector2(vertices[i].X, vertices[i].Y);

            var renderState = GetTileRenderState(cell.Coord);

            if (renderState == TileRenderState.Hidden)
            {
                var (fr, fg, fb) = FogOfWarConstants.FogOfWarColor;
                DrawColoredPolygon(polyVerts, new Color(fr, fg, fb));
                // Skip grid line for hidden tiles
                continue;
            }

            var (r, g, b) = HexColorMapper.GetTerrainColor(cell.Terrain);
            if (renderState == TileRenderState.Dim)
                (r, g, b) = HexColorMapper.Dim((r, g, b), FogOfWarConstants.DefaultDimFactor);

            DrawColoredPolygon(polyVerts, new Color(r, g, b));

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

        var localPos = ToLocal(screenPos);
        var coord = HexGrid.PixelToHex(localPos.X, localPos.Y, HexSize);

        return _grid.InBounds(coord) ? coord : null;
    }
}
