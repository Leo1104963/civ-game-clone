using Godot;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Godot Node2D wrapper around UnitRenderer. Draws all units as colored circles,
/// with the fill color chosen by unit type (Warrior = red, Settler = white).
/// Units on non-Visible tiles are skipped when a VisibilityMap is bound.
/// </summary>
public partial class UnitRendererNode : Node2D
{
    public UnitRenderer Data { get; } = new();

    private VisibilityMap? _visibility;
    private int _viewerOwnerId;

    public void Initialize(UnitManager unitManager, HexGrid grid, float hexSize,
        VisibilityMap? visibility = null, int viewerOwnerId = 0)
    {
        Data.Initialize(unitManager, grid, hexSize);
        _visibility = visibility;
        _viewerOwnerId = viewerOwnerId;
    }

    public void Refresh()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Data.Manager is null || Data.Grid is null) return;

        foreach (var unit in Data.Manager.AllUnits)
        {
            // Skip units not visible to the viewer
            if (_visibility is not null &&
                _visibility.IsAt(_viewerOwnerId, unit.Position) != VisibilityState.Visible)
                continue;

            var (px, py) = HexGrid.HexToPixel(unit.Position, Data.HexSize);
            var center = new Vector2(px, py);

            bool isSelected = unit.Id == Data.SelectedUnitId;
            var (r, g, b) = isSelected
                ? UnitRenderer.SelectedUnitColor
                : UnitRenderer.GetUnitColor(unit.UnitType);
            var color = new Color(r, g, b);

            DrawCircle(center, UnitRenderer.UnitRadius, color);
        }
    }
}
