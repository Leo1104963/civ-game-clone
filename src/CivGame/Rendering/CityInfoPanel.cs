using Godot;
using CivGame.Cities;

namespace CivGame.Rendering;

/// <summary>
/// Panel showing selected city info: name, current production, completed buildings.
/// Has a "Build Granary" button if city has no production and no Granary.
/// </summary>
public partial class CityInfoPanel : Control
{
    private Label? _cityNameLabel;
    private Label? _productionLabel;
    private Label? _buildingsLabel;
    private Button? _buildGranaryButton;
    private City? _currentCity;

    [Signal]
    public delegate void BuildGranaryPressedEventHandler(int cityId);

    public override void _Ready()
    {
        _cityNameLabel = GetNode<Label>("CityNameLabel");
        _productionLabel = GetNode<Label>("ProductionLabel");
        _buildingsLabel = GetNode<Label>("BuildingsLabel");
        _buildGranaryButton = GetNode<Button>("BuildGranaryButton");

        _buildGranaryButton.Pressed += OnBuildGranaryPressed;
        Visible = false;
    }

    public void ShowCity(City city)
    {
        _currentCity = city;
        Visible = true;

        _cityNameLabel!.Text = city.Name;

        if (city.CurrentProduction is not null)
        {
            _productionLabel!.Text = $"Building: {city.CurrentProduction.Definition.Name} ({city.CurrentProduction.TurnsRemaining} turns)";
        }
        else
        {
            _productionLabel!.Text = "Idle";
        }

        if (city.CompletedBuildings.Count > 0)
        {
            _buildingsLabel!.Text = "Buildings: " + string.Join(", ", city.CompletedBuildings.Select(b => b.Name));
        }
        else
        {
            _buildingsLabel!.Text = "Buildings: none";
        }

        // Show build button only if not building anything and Granary not yet built
        _buildGranaryButton!.Visible = city.CurrentProduction is null && !city.HasBuilding("Granary");
    }

    public void HidePanel()
    {
        Visible = false;
        _currentCity = null;
    }

    private void OnBuildGranaryPressed()
    {
        if (_currentCity is not null)
        {
            EmitSignal(SignalName.BuildGranaryPressed, _currentCity.Id);
        }
    }
}
