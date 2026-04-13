using Godot;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Godot Node2D wrapper around InputHandler. Handles mouse clicks to select
/// units/cities and issue move commands.
/// </summary>
public partial class InputHandlerNode : Node2D
{
    public InputHandler Data { get; } = new();

    private GameSession? _session;
    private HexGridRenderer? _gridRenderer;
    private UnitRendererNode? _unitRendererNode;
    private MovementOverlayNode? _movementOverlayNode;

    private Unit? _selectedUnit;
    private VisibilityMap? _visibility;
    private int _viewerOwnerId;

    /// <summary>The currently selected unit, or null if none selected.</summary>
    public Unit? SelectedUnit => _selectedUnit;

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
        UnitRendererNode unitRendererNode,
        MovementOverlayNode movementOverlayNode,
        VisibilityMap? visibility = null,
        int viewerOwnerId = 0)
    {
        _session = session;
        _gridRenderer = gridRenderer;
        _unitRendererNode = unitRendererNode;
        _movementOverlayNode = movementOverlayNode;
        _visibility = visibility;
        _viewerOwnerId = viewerOwnerId;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_session is null || _gridRenderer is null) return;

        if (@event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed)
        {
            var hexCoord = _gridRenderer.ScreenToHex(mouseButton.GlobalPosition);
            if (hexCoord is null) return;

            HandleClick(hexCoord.Value);
        }
    }

    private void HandleClick(HexCoord coord)
    {
        if (_session is null) return;

        // If a unit is selected and the click is on a reachable cell, move
        if (_selectedUnit is not null)
        {
            var reachable = _session.Units.GetReachableCells(_selectedUnit, _session.Grid);
            if (reachable.Contains(coord) && coord != _selectedUnit.Position)
            {
                if (_selectedUnit.TryMoveTo(coord, _session.Grid, _session.Units))
                {
                    UnitMoved?.Invoke(_selectedUnit.Id, coord.Q, coord.R);
                    _unitRendererNode?.Refresh();

                    // Update overlay if unit still has movement
                    if (_selectedUnit.CanMove)
                    {
                        var newReachable = _session.Units.GetReachableCells(_selectedUnit, _session.Grid);
                        var visibleNewReachable = FilterUnseen(newReachable);
                        _movementOverlayNode?.ShowReachable(visibleNewReachable, _session.Grid, _gridRenderer!.HexSize);
                    }
                    else
                    {
                        DeselectUnit();
                    }
                    return;
                }
            }

            // Click was not a valid move — deselect
            DeselectUnit();
        }

        // Check for unit at coord
        var unitAtCoord = _session.Units.GetUnitAt(coord);
        if (unitAtCoord is not null)
        {
            SelectUnit(unitAtCoord);
            return;
        }

        // Check for city at coord
        var cityAtCoord = _session.Cities.GetCityAt(coord);
        if (cityAtCoord is not null)
        {
            CitySelected?.Invoke(cityAtCoord.Id);
            return;
        }
    }

    private void SelectUnit(Unit unit)
    {
        _selectedUnit = unit;
        if (_unitRendererNode is not null)
        {
            _unitRendererNode.Data.SelectedUnitId = unit.Id;
            _unitRendererNode.Refresh();
        }

        var reachable = _session!.Units.GetReachableCells(unit, _session.Grid);
        var visibleReachable = FilterUnseen(reachable);
        _movementOverlayNode?.ShowReachable(visibleReachable, _session.Grid, _gridRenderer!.HexSize);

        UnitSelected?.Invoke(unit.Id);
    }

    private IReadOnlySet<HexCoord> FilterUnseen(IReadOnlySet<HexCoord> cells)
    {
        if (_visibility is null) return cells;

        var filtered = new HashSet<HexCoord>();
        foreach (var coord in cells)
        {
            if (_visibility.IsAt(_viewerOwnerId, coord) != VisibilityState.Unseen)
                filtered.Add(coord);
        }
        return filtered;
    }

    private void DeselectUnit()
    {
        _selectedUnit = null;
        if (_unitRendererNode is not null)
        {
            _unitRendererNode.Data.SelectedUnitId = -1;
            _unitRendererNode.Refresh();
        }
        _movementOverlayNode?.Clear();
        UnitDeselected?.Invoke();
    }
}
