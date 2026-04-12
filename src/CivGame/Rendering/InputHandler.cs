using CivGame.Core;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Handles mouse input: click to select unit, click to move, click to select city.
/// In Godot context, attach as a Node2D child to receive _UnhandledInput events.
/// </summary>
public class InputHandler
{
    private GameSession? _session;
    private HexGridRenderer? _gridRenderer;
    private UnitRenderer? _unitRenderer;
    private MovementOverlay? _movementOverlay;

    private Unit? _selectedUnit;

    /// <summary>Fired when a unit is selected. Parameter is the unit ID.</summary>
    public event Action<int>? UnitSelected;

    /// <summary>Fired when the current unit is deselected.</summary>
    public event Action? UnitDeselected;

    /// <summary>Fired when a city is selected. Parameter is the city ID.</summary>
    public event Action<int>? CitySelected;

    /// <summary>Fired when a unit moves. Parameters are (unitId, toQ, toR).</summary>
    public event Action<int, int, int>? UnitMoved;

    public void Initialize(
        GameSession session,
        HexGridRenderer gridRenderer,
        UnitRenderer unitRenderer,
        MovementOverlay movementOverlay)
    {
        _session = session;
        _gridRenderer = gridRenderer;
        _unitRenderer = unitRenderer;
        _movementOverlay = movementOverlay;
    }
}
