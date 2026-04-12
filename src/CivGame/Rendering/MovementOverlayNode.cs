using Godot;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Godot Node2D wrapper around MovementOverlay. Draws semi-transparent blue
/// hex highlights over reachable cells.
/// </summary>
public partial class MovementOverlayNode : Node2D
{
    public MovementOverlay Data { get; } = new();

    public void ShowReachable(IReadOnlySet<HexCoord> cells, HexGrid grid, float hexSize)
    {
        Data.ShowReachable(cells, grid, hexSize);
        QueueRedraw();
    }

    public void Clear()
    {
        Data.Clear();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Data.ReachableCells is null || Data.Grid is null) return;

        var (hr, hg, hb, ha) = MovementOverlay.HighlightColor;
        var highlightColor = new Color(hr, hg, hb, ha);
        float overlaySize = Data.HexSize * 0.9f;

        foreach (var coord in Data.ReachableCells)
        {
            var (px, py) = HexGrid.HexToPixel(coord, Data.HexSize);
            var vertices = MovementOverlay.GetHexVertices(px, py, overlaySize);

            var polyVerts = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                polyVerts[i] = new Vector2(vertices[i].X, vertices[i].Y);
            }

            DrawColoredPolygon(polyVerts, highlightColor);
        }
    }
}
