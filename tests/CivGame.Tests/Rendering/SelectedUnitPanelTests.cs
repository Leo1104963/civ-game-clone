using System.Reflection;
using CivGame.Cities;
using CivGame.Rendering;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Failing tests for issue #52: Selected-unit panel with Found City button.
///
/// SelectedUnitPanel is a Godot Control and cannot be instantiated without a
/// SceneTree, so tests use reflection to verify the type's shape and test the
/// underlying data logic (UnitManager.CanFoundCity / FoundCityWithSettler)
/// that drives button visibility and GameController wiring.
/// </summary>
public class SelectedUnitPanelTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// A 10×10 all-Grassland grid with a fresh UnitManager and CityManager.
    /// </summary>
    private static (HexGrid Grid, UnitManager Units, CityManager Cities) CreateSetup()
    {
        var grid = new HexGrid(10, 10);
        var units = new UnitManager();
        var cities = new CityManager();
        return (grid, units, cities);
    }

    // -----------------------------------------------------------------------
    // SelectedUnitPanel — type existence and API surface (reflection only)
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_Exist_When_SelectedUnitPanelReferenced()
    {
        var type = typeof(SelectedUnitPanel);
        Assert.NotNull(type);
    }

    [Fact]
    public void Should_InheritFromControl_When_SelectedUnitPanelReferenced()
    {
        Assert.True(typeof(Godot.Control).IsAssignableFrom(typeof(SelectedUnitPanel)));
    }

    [Fact]
    public void Should_HaveShowUnitMethod_When_SelectedUnitPanelReferenced()
    {
        var method = typeof(SelectedUnitPanel).GetMethod(
            "ShowUnit",
            new[] { typeof(Unit), typeof(UnitManager), typeof(CityManager), typeof(HexGrid) });
        Assert.NotNull(method);
    }

    [Fact]
    public void Should_HaveHidePanelMethod_When_SelectedUnitPanelReferenced()
    {
        var method = typeof(SelectedUnitPanel).GetMethod("HidePanel", Type.EmptyTypes);
        Assert.NotNull(method);
    }

    [Fact]
    public void Should_HaveFoundCityPressedSignal_When_SelectedUnitPanelReferenced()
    {
        // Godot signal delegates are named <SignalName>EventHandler by convention.
        var delegateType = typeof(SelectedUnitPanel).GetNestedType(
            "FoundCityPressedEventHandler",
            BindingFlags.Public);
        Assert.NotNull(delegateType);
    }

    // -----------------------------------------------------------------------
    // UnitManager.CanFoundCity — drives FoundCityButton.Visible in ShowUnit
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ReturnFalse_When_CanFoundCityCalledWithWarrior()
    {
        var (grid, units, cities) = CreateSetup();
        var warrior = units.CreateUnit("Warrior", new HexCoord(5, 5), grid);

        bool canFound = units.CanFoundCity(warrior, cities, grid);

        Assert.False(canFound);
    }

    [Fact]
    public void Should_ReturnTrue_When_CanFoundCityCalledWithFreshSettlerOnLegalTile()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        bool canFound = units.CanFoundCity(settler, cities, grid);

        Assert.True(canFound);
    }

    [Fact]
    public void Should_ReturnFalse_When_CanFoundCityCalledWithSettlerThatMoved()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);
        // Move the settler so MovementRemaining < MovementRange
        settler.TryMoveTo(new HexCoord(6, 5), grid, units);
        Assert.True(settler.MovementRemaining < settler.MovementRange,
            "Pre-condition: settler must have spent movement.");

        bool canFound = units.CanFoundCity(settler, cities, grid);

        Assert.False(canFound);
    }

    [Fact]
    public void Should_ReturnFalse_When_CanFoundCityCalledWithSettlerTooCloseToCity()
    {
        var (grid, units, cities) = CreateSetup();
        // City at (5,6) is distance 1 from (5,5) — within the exclusion zone of <= 2
        cities.CreateCity("TooClose", new HexCoord(5, 6), grid);
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        bool canFound = units.CanFoundCity(settler, cities, grid);

        Assert.False(canFound);
    }

    // -----------------------------------------------------------------------
    // UnitManager.FoundCityWithSettler — GameController wiring consequences
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_RemoveSettlerFromAllUnits_When_FoundCityWithSettlerSucceeds()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        units.FoundCityWithSettler(settler, "NewCity", cities, grid);

        Assert.DoesNotContain(settler, units.AllUnits);
    }

    [Fact]
    public void Should_ProduceCityNamedCity2_When_SessionAlreadyHasCapital()
    {
        // Mirrors the GameController handler:
        //   $"City {_session.Cities.AllCities.Count + 1}"
        // GameSession(10,10) seeds "Capital" (AllCities.Count == 1), so the
        // next name should be "City 2".
        var session = new CivGame.Core.GameSession(10, 10);

        // The default session places a Warrior adjacent to Capital at (5,5).
        // We need a Settler far enough from Capital to satisfy CanFoundCity.
        // Capital is at (5,5); place the settler at (0,0) — distance >> 2.
        var settler = session.Units.CreateUnit("Settler", new HexCoord(0, 0), session.Grid);

        string cityName = $"City {session.Cities.AllCities.Count + 1}";
        var newCity = session.Units.FoundCityWithSettler(
            settler, cityName, session.Cities, session.Grid);

        Assert.NotNull(newCity);
        Assert.Equal("City 2", newCity!.Name);
    }
}
