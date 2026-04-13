using Godot;
using CivGame.Cities;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Panel showing the currently selected unit: type, position, and movement.
/// For a Settler on a legal founding tile, a "Found City" button is shown.
/// Clicking it emits FoundCityPressed with the unit's id.
/// </summary>
public partial class SelectedUnitPanel : Control
{
    private Label? _unitNameLabel;
    private Label? _unitDetailsLabel;
    private Button? _foundCityButton;

    private Unit? _currentUnit;

    [Signal]
    public delegate void FoundCityPressedEventHandler(int unitId);

    public override void _Ready()
    {
        _unitNameLabel = GetNode<Label>("UnitNameLabel");
        _unitDetailsLabel = GetNode<Label>("UnitDetailsLabel");
        _foundCityButton = GetNode<Button>("FoundCityButton");

        _foundCityButton.Pressed += OnFoundCityPressed;
        Visible = false;
    }

    /// <summary>Show the panel for the given unit.</summary>
    public void ShowUnit(Unit unit, UnitManager unitManager, CityManager cityManager, HexGrid grid)
    {
        _currentUnit = unit;
        Visible = true;

        _unitNameLabel!.Text = unit.Name;
        _unitDetailsLabel!.Text =
            $"Position: {unit.Position}\n" +
            $"Movement: {unit.MovementRemaining}/{unit.MovementRange}";

        bool canFound =
            string.Equals(unit.UnitType, "Settler", StringComparison.Ordinal)
            && unitManager.CanFoundCity(unit, cityManager, grid);

        _foundCityButton!.Visible = canFound;
    }

    /// <summary>Hide the panel and clear the current unit reference.</summary>
    public void HidePanel()
    {
        Visible = false;
        _currentUnit = null;
    }

    private void OnFoundCityPressed()
    {
        if (_currentUnit is not null)
        {
            EmitSignal(SignalName.FoundCityPressed, _currentUnit.Id);
        }
    }
}
