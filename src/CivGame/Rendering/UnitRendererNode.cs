using Godot;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Godot Node2D wrapper around UnitRenderer. Draws all units as colored circles.
/// </summary>
public partial class UnitRendererNode : Node2D
{
    public UnitRenderer Data { get; } = new();

    public void Initialize(UnitManager unitManager, HexGrid grid, float hexSize)
    {
        Data.Initialize(unitManager, grid, hexSize);
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
            var (px, py) = HexGrid.HexToPixel(unit.Position, Data.HexSize);
            var center = new Vector2(px, py);

            bool isSelected = unit.Id == Data.SelectedUnitId;
            var (r, g, b) = isSelected ? UnitRenderer.SelectedUnitColor : UnitRenderer.UnitColor;
            var color = new Color(r, g, b);

            DrawCircle(center, UnitRenderer.UnitRadius, color);
        }
    }
}
