using CivGame.Cities;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Draws all cities as colored squares centered on their hex cell.
/// In Godot context, attach as a Node2D child and call Refresh() to trigger redraw.
/// </summary>
public class CityRenderer
{
    private CityManager? _cityManager;
    private HexGrid? _grid;
    private float _hexSize;

    public static readonly (float R, float G, float B) CityColor = (0.6f, 0.4f, 0.1f);   // brown
    public const float CityHalfSize = 14f;

    public CityManager? Manager => _cityManager;
    public HexGrid? Grid => _grid;
    public float HexSize => _hexSize;

    public void Initialize(CityManager cityManager, HexGrid grid, float hexSize)
    {
        _cityManager = cityManager;
        _grid = grid;
        _hexSize = hexSize;
    }

    public void Refresh()
    {
        // In Godot context, the Node2D wrapper calls QueueRedraw().
        // In test context, this is a no-op.
    }
}
