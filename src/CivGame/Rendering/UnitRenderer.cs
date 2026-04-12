using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Draws all units as colored circles centered on their hex cell.
/// In Godot context, attach as a Node2D child and call Refresh() to trigger redraw.
/// </summary>
public class UnitRenderer
{
    private UnitManager? _unitManager;
    private HexGrid? _grid;
    private float _hexSize;

    public static readonly (float R, float G, float B) UnitColor = (0.8f, 0.2f, 0.2f);         // red
    public static readonly (float R, float G, float B) SelectedUnitColor = (1.0f, 0.9f, 0.2f); // yellow
    public const float UnitRadius = 12f;

    public int SelectedUnitId { get; set; } = -1;

    public UnitManager? Manager => _unitManager;
    public HexGrid? Grid => _grid;
    public float HexSize => _hexSize;

    public void Initialize(UnitManager unitManager, HexGrid grid, float hexSize)
    {
        _unitManager = unitManager;
        _grid = grid;
        _hexSize = hexSize;
    }

    public void Refresh()
    {
        // In Godot context, the Node2D wrapper calls QueueRedraw().
        // In test context, this is a no-op.
    }
}
