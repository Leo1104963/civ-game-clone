using Godot;
using CivGame.Core;
using CivGame.Buildings;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Root controller. Creates the GameSession, initializes all renderers and UI,
/// and connects signals. This is the entry point for the game scene.
/// </summary>
public partial class GameController : Node2D
{
    private GameSession? _session;

    [Export] public int GridWidth { get; set; } = 10;
    [Export] public int GridHeight { get; set; } = 8;

    public override void _Ready()
    {
        _session = new GameSession(GridWidth, GridHeight);

        // Get child nodes
        var gridRenderer = GetNode<HexGridRenderer>("HexGridRenderer");
        var unitRenderer = GetNode<UnitRendererNode>("UnitRenderer");
        var cityRenderer = GetNode<CityRendererNode>("CityRenderer");
        var movementOverlay = GetNode<MovementOverlayNode>("MovementOverlay");
        var inputHandler = GetNode<InputHandlerNode>("InputHandler");
        var turnHud = GetNode<TurnHud>("CanvasLayer/TurnHud");
        var cityInfoPanel = GetNode<CityInfoPanel>("CanvasLayer/CityInfoPanel");
        var selectedUnitPanel = GetNode<SelectedUnitPanel>("CanvasLayer/SelectedUnitPanel");

        // Initialize renderers
        float hexSize = gridRenderer.HexSize;
        gridRenderer.Initialize(_session.Grid);
        unitRenderer.Initialize(_session.Units, _session.Grid, hexSize);
        cityRenderer.Initialize(_session.Cities, _session.Grid, hexSize);
        inputHandler.Initialize(_session, gridRenderer, unitRenderer, movementOverlay);
        turnHud.Initialize(_session.Turns);

        // Initial draw
        unitRenderer.Refresh();
        cityRenderer.Refresh();

        // Connect city selection to info panel
        inputHandler.CitySelected += (cityId) =>
        {
            var city = _session.Cities.AllCities.FirstOrDefault(c => c.Id == cityId);
            if (city is not null)
            {
                cityInfoPanel.ShowCity(city);
            }
        };

        inputHandler.UnitSelected += (unitId) =>
        {
            cityInfoPanel.HidePanel();
            var unit = _session.Units.AllUnits.FirstOrDefault(u => u.Id == unitId);
            if (unit is not null)
            {
                selectedUnitPanel.ShowUnit(unit, _session.Units, _session.Cities, _session.Grid);
            }
        };

        inputHandler.UnitDeselected += () =>
        {
            cityInfoPanel.HidePanel();
            selectedUnitPanel.HidePanel();
        };

        // Connect build button
        cityInfoPanel.BuildGranaryPressed += (cityId) =>
        {
            var city = _session.Cities.AllCities.FirstOrDefault(c => c.Id == cityId);
            if (city is not null)
            {
                city.StartBuilding(BuildingCatalog.Granary);
                cityInfoPanel.ShowCity(city); // refresh display
            }
        };

        // Connect Found City button
        selectedUnitPanel.FoundCityPressed += (unitId) =>
        {
            var unit = _session.Units.AllUnits.FirstOrDefault(u => u.Id == unitId);
            if (unit is null) return;

            var newCity = _session.Units.FoundCityWithSettler(
                unit,
                $"City {_session.Cities.AllCities.Count + 1}",
                _session.Cities,
                _session.Grid);

            if (newCity is not null)
            {
                selectedUnitPanel.HidePanel();
                unitRenderer.Refresh();
                cityRenderer.Refresh();
            }
        };

        // Connect turn end to refresh
        _session.Turns.TurnEnded += (newTurn) =>
        {
            unitRenderer.Refresh();
            cityRenderer.Refresh();
            turnHud.UpdateTurnDisplay(newTurn);
        };
    }
}
