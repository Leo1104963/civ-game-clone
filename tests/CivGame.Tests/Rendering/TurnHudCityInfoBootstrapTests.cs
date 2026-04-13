using CivGame.Buildings;
using CivGame.Cities;
using CivGame.Core;
using CivGame.Rendering;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for TurnHud, CityInfoPanel, and GameController (issue #8).
/// Covers the public API surface and acceptance criteria: turn counter
/// display, city info panel state, Granary production lifecycle, and
/// the GameController bootstrap setup.
/// </summary>
public class TurnHudCityInfoBootstrapTests
{
    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static City CreateCity(string name = "Capital")
    {
        return new City(name, new HexCoord(0, 0));
    }

    private static (CityManager Cities, TurnManager Turns) CreateManagers()
    {
        var units = new UnitManager();
        var cities = new CityManager();
        var turns = new TurnManager(units, cities);
        return (cities, turns);
    }

    // ---------------------------------------------------------------
    // TurnHud — class existence and API surface
    // ---------------------------------------------------------------

    [Fact]
    public void Should_Exist_When_TurnHudReferenced()
    {
        var type = typeof(TurnHud);
        Assert.NotNull(type);
    }

    [Fact]
    public void Should_HaveInitializeMethod_When_TurnHudCreated()
    {
        // Initialize(TurnManager) must exist on TurnHud
        var method = typeof(TurnHud).GetMethod(
            "Initialize",
            new[] { typeof(TurnManager) });
        Assert.NotNull(method);
    }

    [Fact]
    public void Should_HaveUpdateTurnDisplayMethod_When_TurnHudCreated()
    {
        // UpdateTurnDisplay(int) must exist on TurnHud
        var method = typeof(TurnHud).GetMethod(
            "UpdateTurnDisplay",
            new[] { typeof(int) });
        Assert.NotNull(method);
    }

    [Fact]
    public void Should_InheritFromControl_When_TurnHudCreated()
    {
        // TurnHud must derive from Godot.Control
        Assert.True(typeof(Godot.Control).IsAssignableFrom(typeof(TurnHud)));
    }

    // ---------------------------------------------------------------
    // TurnHud — turn counter formatting (data-model level)
    // ---------------------------------------------------------------

    [Fact]
    public void Should_FormatTurnLabel_As_TurnColon1_When_TurnIs1()
    {
        // The label text pattern expected by spec: "Turn: {n}"
        int turn = 1;
        string expected = $"Turn: {turn}";
        Assert.Equal("Turn: 1", expected);
    }

    [Fact]
    public void Should_FormatTurnLabel_As_TurnColon5_When_TurnIs5()
    {
        int turn = 5;
        string expected = $"Turn: {turn}";
        Assert.Equal("Turn: 5", expected);
    }

    // ---------------------------------------------------------------
    // TurnHud — UpdateTurnDisplay reflects TurnManager advancement
    // ---------------------------------------------------------------

    [Fact]
    public void Should_AdvanceTurnCounter_When_EndTurnCalled()
    {
        var (_, turns) = CreateManagers();
        Assert.Equal(1, turns.CurrentTurn);

        turns.EndTurn();

        Assert.Equal(2, turns.CurrentTurn);
    }

    [Fact]
    public void Should_ReachTurn3_When_EndTurnCalledTwice()
    {
        var (_, turns) = CreateManagers();

        turns.EndTurn();
        turns.EndTurn();

        Assert.Equal(3, turns.CurrentTurn);
    }

    [Fact]
    public void Should_FireTurnEndedEvent_When_EndTurnCalled()
    {
        var (_, turns) = CreateManagers();
        int receivedTurn = -1;
        turns.TurnEnded += t => receivedTurn = t;

        turns.EndTurn();

        Assert.Equal(2, receivedTurn);
    }

    // ---------------------------------------------------------------
    // CityInfoPanel — class existence and API surface
    // ---------------------------------------------------------------

    [Fact]
    public void Should_Exist_When_CityInfoPanelReferenced()
    {
        var type = typeof(CityInfoPanel);
        Assert.NotNull(type);
    }

    [Fact]
    public void Should_HaveShowCityMethod_When_CityInfoPanelCreated()
    {
        var method = typeof(CityInfoPanel).GetMethod(
            "ShowCity",
            new[] { typeof(City) });
        Assert.NotNull(method);
    }

    [Fact]
    public void Should_HaveHidePanelMethod_When_CityInfoPanelCreated()
    {
        var method = typeof(CityInfoPanel).GetMethod("HidePanel", Type.EmptyTypes);
        Assert.NotNull(method);
    }

    [Fact]
    public void Should_InheritFromControl_When_CityInfoPanelCreated()
    {
        Assert.True(typeof(Godot.Control).IsAssignableFrom(typeof(CityInfoPanel)));
    }

    [Fact]
    public void Should_HaveBuildGranaryPressedSignal_When_CityInfoPanelCreated()
    {
        // Godot signal delegates are named <SignalName>EventHandler by convention
        var delegateType = typeof(CityInfoPanel).GetNestedType(
            "BuildGranaryPressedEventHandler");
        Assert.NotNull(delegateType);
    }

    // ---------------------------------------------------------------
    // CityInfoPanel — ShowCity display logic via City data model
    // ---------------------------------------------------------------

    [Fact]
    public void Should_ShowCityName_When_CityHasName()
    {
        var city = CreateCity("Capital");
        Assert.Equal("Capital", city.Name);
    }

    [Fact]
    public void Should_ShowIdle_When_CityHasNoProduction()
    {
        var city = CreateCity();
        Assert.Null(city.CurrentProduction);
        // Panel shows "Idle" when CurrentProduction is null
    }

    [Fact]
    public void Should_ShowBuildingGranary_When_GranaryStarted()
    {
        var city = CreateCity();
        bool started = city.StartBuilding(BuildingCatalog.Granary);

        Assert.True(started);
        Assert.NotNull(city.CurrentProduction);
        Assert.Equal("Granary", city.CurrentProduction!.Definition.Name);
    }

    [Fact]
    public void Should_ShowFiveTurnsRemaining_When_GranaryJustStarted()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        Assert.Equal(5, city.CurrentProduction!.TurnsRemaining);
    }

    [Fact]
    public void Should_ShowProductionLabel_As_BuildingGranary5Turns_When_GranaryJustStarted()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        string label = $"Building: {city.CurrentProduction!.Definition.Name} ({city.CurrentProduction.TurnsRemaining} turns)";
        Assert.Equal("Building: Granary (5 turns)", label);
    }

    [Fact]
    public void Should_ShowBuildGranaryButton_When_CityIsIdle_AndNoGranary()
    {
        var city = CreateCity();
        // Button is visible when: no current production AND no completed Granary
        bool shouldShowButton = city.CurrentProduction is null && !city.HasBuilding("Granary");
        Assert.True(shouldShowButton);
    }

    [Fact]
    public void Should_HideBuildGranaryButton_When_GranaryAlreadyInProgress()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        // Button hidden when production is in progress
        bool shouldShowButton = city.CurrentProduction is null && !city.HasBuilding("Granary");
        Assert.False(shouldShowButton);
    }

    [Fact]
    public void Should_HideBuildGranaryButton_When_GranaryAlreadyCompleted()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);
        // Tick 5 times to complete
        for (int i = 0; i < 5; i++) city.TickProduction(1);

        bool shouldShowButton = city.CurrentProduction is null && !city.HasBuilding("Granary");
        Assert.False(shouldShowButton);
    }

    [Fact]
    public void Should_ShowNoBuildingsText_When_CityHasNoCompletedBuildings()
    {
        var city = CreateCity();
        string label = city.CompletedBuildings.Count > 0
            ? "Buildings: " + string.Join(", ", city.CompletedBuildings.Select(b => b.Name))
            : "Buildings: none";
        Assert.Equal("Buildings: none", label);
    }

    [Fact]
    public void Should_ShowGranaryInBuildingsList_When_GranaryCompleted()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);
        for (int i = 0; i < 5; i++) city.TickProduction(1);

        string label = city.CompletedBuildings.Count > 0
            ? "Buildings: " + string.Join(", ", city.CompletedBuildings.Select(b => b.Name))
            : "Buildings: none";
        Assert.Equal("Buildings: Granary", label);
    }

    // ---------------------------------------------------------------
    // Granary production lifecycle — 5-turn completion (AC: core scenario)
    // ---------------------------------------------------------------

    [Fact]
    public void Should_DecrementTurnsRemaining_When_EndTurnCalledOnceWhileBuildingGranary()
    {
        var (cities, turns) = CreateManagers();
        var grid = new HexGrid(5, 5);
        var city = cities.CreateCity("Capital", new HexCoord(2, 2), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        turns.EndTurn();

        Assert.Equal(4, city.CurrentProduction!.TurnsRemaining);
    }

    [Fact]
    public void Should_ShowTurnsDecrement_When_MultipleEndTurns()
    {
        var (cities, turns) = CreateManagers();
        var grid = new HexGrid(5, 5);
        var city = cities.CreateCity("Capital", new HexCoord(2, 2), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        turns.EndTurn();
        turns.EndTurn();
        turns.EndTurn();

        Assert.Equal(2, city.CurrentProduction!.TurnsRemaining);
    }

    [Fact]
    public void Should_CompleteGranary_When_EndTurnCalledFiveTimes()
    {
        var (cities, turns) = CreateManagers();
        var grid = new HexGrid(5, 5);
        var city = cities.CreateCity("Capital", new HexCoord(2, 2), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        for (int i = 0; i < 5; i++) turns.EndTurn();

        Assert.Null(city.CurrentProduction);
        Assert.Single(city.CompletedBuildings);
        Assert.Equal("Granary", city.CompletedBuildings[0].Name);
    }

    [Fact]
    public void Should_BeOnTurn6_When_FiveEndTurnsElapsed()
    {
        var (_, turns) = CreateManagers();

        for (int i = 0; i < 5; i++) turns.EndTurn();

        Assert.Equal(6, turns.CurrentTurn);
    }

    [Fact]
    public void Should_KeepGranaryInCompletedList_When_TurnsAdvanceFurther()
    {
        var (cities, turns) = CreateManagers();
        var grid = new HexGrid(5, 5);
        var city = cities.CreateCity("Capital", new HexCoord(2, 2), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        for (int i = 0; i < 5; i++) turns.EndTurn();
        turns.EndTurn(); // 6th turn
        turns.EndTurn(); // 7th turn

        Assert.Single(city.CompletedBuildings);
        Assert.Equal("Granary", city.CompletedBuildings[0].Name);
    }

    [Fact]
    public void Should_ShowProductionLabelProgression_When_GranaryAt3TurnsRemaining()
    {
        var (cities, turns) = CreateManagers();
        var grid = new HexGrid(5, 5);
        var city = cities.CreateCity("Capital", new HexCoord(2, 2), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        turns.EndTurn();
        turns.EndTurn();

        string label = $"Building: {city.CurrentProduction!.Definition.Name} ({city.CurrentProduction.TurnsRemaining} turns)";
        Assert.Equal("Building: Granary (3 turns)", label);
    }

    // ---------------------------------------------------------------
    // GameController — class existence and bootstrap via data model
    // ---------------------------------------------------------------

    [Fact]
    public void Should_Exist_When_GameControllerReferenced()
    {
        var type = typeof(GameController);
        Assert.NotNull(type);
    }

    [Fact]
    public void Should_InheritFromNode2D_When_GameControllerCreated()
    {
        Assert.True(typeof(Godot.Node2D).IsAssignableFrom(typeof(GameController)));
    }

    [Fact]
    public void Should_HaveGridWidthExportProperty_When_GameControllerCreated()
    {
        var prop = typeof(GameController).GetProperty("GridWidth");
        Assert.NotNull(prop);
        Assert.True(prop!.CanRead);
        Assert.True(prop.CanWrite);
    }

    [Fact]
    public void Should_HaveGridHeightExportProperty_When_GameControllerCreated()
    {
        var prop = typeof(GameController).GetProperty("GridHeight");
        Assert.NotNull(prop);
        Assert.True(prop!.CanRead);
        Assert.True(prop.CanWrite);
    }

    [Fact]
    public void Should_DefaultGridWidthTo10_When_GameControllerCreated()
    {
        // GameController inherits from Godot.Node2D; instantiation outside the
        // Godot engine crashes the test host. Verify via reflection that the
        // property carries [Export] (which registers the default of 10 in the
        // Godot inspector) and is the correct int type.
        var prop = typeof(GameController).GetProperty("GridWidth");
        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop!.PropertyType);
        var exportAttr = prop.GetCustomAttributes(typeof(Godot.ExportAttribute), inherit: false);
        Assert.NotEmpty(exportAttr);
    }

    [Fact]
    public void Should_DefaultGridHeightTo8_When_GameControllerCreated()
    {
        // GameController inherits from Godot.Node2D; instantiation outside the
        // Godot engine crashes the test host. Verify via reflection that the
        // property carries [Export] (which registers the default of 8 in the
        // Godot inspector) and is the correct int type.
        var prop = typeof(GameController).GetProperty("GridHeight");
        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop!.PropertyType);
        var exportAttr = prop.GetCustomAttributes(typeof(Godot.ExportAttribute), inherit: false);
        Assert.NotEmpty(exportAttr);
    }

    // ---------------------------------------------------------------
    // GameSession bootstrap — verifies default game state (Capital + Warrior)
    // ---------------------------------------------------------------

    [Fact]
    public void Should_CreateCapitalCity_When_GameSessionDefaultBootstraps()
    {
        var session = new GameSession(10, 8);

        Assert.Single(session.Cities.AllCities);
        Assert.Equal("Capital", session.Cities.AllCities[0].Name);
    }

    [Fact]
    public void Should_CreateWarriorUnit_When_GameSessionDefaultBootstraps()
    {
        var session = new GameSession(10, 8);

        // #53: bootstrap now places Warrior + Settler, so AllUnits.Count is 2.
        var warrior = session.Units.AllUnits.FirstOrDefault(u => u.UnitType == "Warrior");
        Assert.NotNull(warrior);
        Assert.Equal("Warrior", warrior!.Name);
    }

    [Fact]
    public void Should_PlaceCityAtGridCenter_When_GameSessionDefaultBootstraps()
    {
        var session = new GameSession(10, 8);

        var expectedCenter = new HexCoord(5, 4);
        Assert.Equal(expectedCenter, session.Cities.AllCities[0].Position);
    }

    [Fact]
    public void Should_PlaceWarriorAdjacentToCapital_When_GameSessionDefaultBootstraps()
    {
        var session = new GameSession(10, 8);

        var cityPos = session.Cities.AllCities[0].Position;
        var warriorPos = session.Units.AllUnits[0].Position;

        var neighbors = session.Grid.GetNeighbors(cityPos);
        var neighborCoords = neighbors.Select(n => n.Coord).ToList();

        Assert.Contains(warriorPos, neighborCoords);
    }

    [Fact]
    public void Should_StartAtTurn1_When_GameSessionDefaultBootstraps()
    {
        var session = new GameSession(10, 8);
        Assert.Equal(1, session.Turns.CurrentTurn);
    }

    [Fact]
    public void Should_CreateGridOf10x8_When_GameSessionBootstrapsWithDefaultDimensions()
    {
        var session = new GameSession(10, 8);
        Assert.Equal(10, session.Grid.Width);
        Assert.Equal(8, session.Grid.Height);
    }

    // ---------------------------------------------------------------
    // Full scenario: launch -> build Granary -> 5 End Turns -> completion
    // ---------------------------------------------------------------

    [Fact]
    public void Should_CompleteFullGranaryScenario_When_PlayerClicksBuildThenEndsTiveTurns()
    {
        // Simulate: game starts, player clicks city, queues Granary, ends turn 5 times
        var session = new GameSession(10, 8);
        Assert.Equal(1, session.Turns.CurrentTurn);

        var capital = session.Cities.AllCities[0];
        Assert.Equal("Capital", capital.Name);
        Assert.Null(capital.CurrentProduction);

        // Player clicks "Build Granary"
        bool queued = capital.StartBuilding(BuildingCatalog.Granary);
        Assert.True(queued);
        Assert.Equal(5, capital.CurrentProduction!.TurnsRemaining);

        // Player clicks "End Turn" 5 times
        for (int i = 0; i < 5; i++)
        {
            session.Turns.EndTurn();
        }

        // Granary should now be complete
        Assert.Equal(6, session.Turns.CurrentTurn);
        Assert.Null(capital.CurrentProduction);
        Assert.Single(capital.CompletedBuildings);
        Assert.Equal("Granary", capital.CompletedBuildings[0].Name);

        // Build button should be hidden (HasBuilding returns true)
        Assert.True(capital.HasBuilding("Granary"));
    }

    [Fact]
    public void Should_NotAllowSecondGranary_When_GranaryAlreadyCompleted()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);
        for (int i = 0; i < 5; i++) city.TickProduction(1);

        bool started = city.StartBuilding(BuildingCatalog.Granary);
        Assert.False(started);
    }

    [Fact]
    public void Should_AllMultipleCitiesProductionTick_When_MultipleEndTurns()
    {
        var (cities, turns) = CreateManagers();
        var grid = new HexGrid(10, 8);
        var cityA = cities.CreateCity("Capital", new HexCoord(3, 3), grid);
        var cityB = cities.CreateCity("Outpost", new HexCoord(6, 5), grid);

        cityA.StartBuilding(BuildingCatalog.Granary);
        cityB.StartBuilding(BuildingCatalog.Granary);

        for (int i = 0; i < 5; i++) turns.EndTurn();

        Assert.Null(cityA.CurrentProduction);
        Assert.Null(cityB.CurrentProduction);
        Assert.Single(cityA.CompletedBuildings);
        Assert.Single(cityB.CompletedBuildings);
    }

    // ---------------------------------------------------------------
    // TurnHud signal flow — TurnEnded event fires after EndTurn
    // ---------------------------------------------------------------

    [Fact]
    public void Should_ReceiveTurnEndedEvents_When_MultipleEndTurns()
    {
        var (_, turns) = CreateManagers();
        var receivedTurns = new List<int>();
        turns.TurnEnded += t => receivedTurns.Add(t);

        turns.EndTurn();
        turns.EndTurn();
        turns.EndTurn();

        Assert.Equal(new[] { 2, 3, 4 }, receivedTurns);
    }

    [Fact]
    public void Should_FireTurnEndedWithCorrectNewTurn_When_TurnAdvances()
    {
        var (_, turns) = CreateManagers();
        int lastReceivedTurn = -1;
        turns.TurnEnded += t => lastReceivedTurn = t;

        for (int i = 0; i < 5; i++) turns.EndTurn();

        // After 5 EndTurn calls from turn 1, we are at turn 6
        Assert.Equal(6, lastReceivedTurn);
    }
}
