using CivGame.Cities;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Units.Tests;

/// <summary>
/// Failing tests for issue #50: Settler unit type and found-city action.
/// These tests compile against the expected public API surface; they fail
/// until UnitManager.CanFoundCity and UnitManager.FoundCityWithSettler
/// are implemented by the dev.
/// </summary>
public class SettlerTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// A 10×10 all-Grassland grid with a fresh UnitManager and CityManager.
    /// Large enough that we can place a settler far from any city.
    /// </summary>
    private static (HexGrid Grid, UnitManager Units, CityManager Cities) CreateSetup()
    {
        var grid = new HexGrid(10, 10);
        var units = new UnitManager();
        var cities = new CityManager();
        return (grid, units, cities);
    }

    // -----------------------------------------------------------------------
    // CreateUnit — Settler type
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_CreateSettlerWithCorrectType_When_UnitTypeIsSettler()
    {
        var (grid, units, _) = CreateSetup();
        var pos = new HexCoord(5, 5);

        var settler = units.CreateUnit("Settler", pos, grid);

        Assert.Equal("Settler", settler.UnitType);
    }

    [Fact]
    public void Should_CreateSettlerWithMovementRangeTwo_When_UnitTypeIsSettler()
    {
        var (grid, units, _) = CreateSetup();
        var pos = new HexCoord(5, 5);

        var settler = units.CreateUnit("Settler", pos, grid);

        Assert.Equal(2, settler.MovementRange);
    }

    // -----------------------------------------------------------------------
    // CanFoundCity — true cases
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ReturnTrue_When_FreshSettlerOnEmptyPassableTileFarFromAnyCity()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        bool canFound = units.CanFoundCity(settler, cities, grid);

        Assert.True(canFound);
    }

    [Fact]
    public void Should_ReturnTrue_When_NearestCityIsExactlyCubeDistanceThree()
    {
        var (grid, units, cities) = CreateSetup();
        // Place a city at distance 3 from (5,5).
        // HexCoord (5,5) cube: Q=5, R=5, S=-10
        // HexCoord (5,2) cube: Q=5, R=2, S=-7  => distance = max(|5-5|,|5-2|,|-10+7|) = 3 ✓
        cities.CreateCity("Distant", new HexCoord(5, 2), grid);
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        bool canFound = units.CanFoundCity(settler, cities, grid);

        Assert.True(canFound);
    }

    // -----------------------------------------------------------------------
    // CanFoundCity — false cases
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ReturnFalse_When_UnitIsNotASettler()
    {
        var (grid, units, cities) = CreateSetup();
        var warrior = units.CreateUnit("Warrior", new HexCoord(5, 5), grid);

        bool canFound = units.CanFoundCity(warrior, cities, grid);

        Assert.False(canFound);
    }

    [Fact]
    public void Should_ReturnFalse_When_SettlerHasMovedThisTurn()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);
        // Move one step so MovementRemaining < MovementRange
        settler.TryMoveTo(new HexCoord(6, 5), grid, units);
        Assert.True(settler.MovementRemaining < settler.MovementRange,
            "Pre-condition: settler must have moved.");

        bool canFound = units.CanFoundCity(settler, cities, grid);

        Assert.False(canFound);
    }

    [Fact]
    public void Should_ReturnFalse_When_ExistingCityIsWithinCubeDistanceTwo()
    {
        var (grid, units, cities) = CreateSetup();
        // Distance from (5,5) to (5,6): max(0,1,1) = 1  <=  2
        cities.CreateCity("TooClose", new HexCoord(5, 6), grid);
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        bool canFound = units.CanFoundCity(settler, cities, grid);

        Assert.False(canFound);
    }

    [Fact]
    public void Should_ReturnFalse_When_ExistingCityIsAtCubeDistanceTwo()
    {
        var (grid, units, cities) = CreateSetup();
        // Exactly distance 2: (5,5) → (5,7) — max(0,2,2) = 2  <=  2
        cities.CreateCity("DistanceTwo", new HexCoord(5, 7), grid);
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        bool canFound = units.CanFoundCity(settler, cities, grid);

        Assert.False(canFound);
    }

    [Fact]
    public void Should_ReturnFalse_When_CityAlreadyOccupiesSettlerTile()
    {
        var (grid, units, cities) = CreateSetup();
        var pos = new HexCoord(5, 5);
        cities.CreateCity("SameTile", pos, grid);
        // Place settler elsewhere, then construct the scenario manually via
        // a fresh manager so the settler's position is logically on the city tile.
        // Easiest: create settler at that position on a second grid not used for
        // city lookup, then call CanFoundCity with the real grid.
        // Simpler: use position index trick — the settler IS at pos, city IS at pos.
        var units2 = new UnitManager();
        var settler = units2.CreateUnit("Settler", pos, grid);

        bool canFound = units2.CanFoundCity(settler, cities, grid);

        Assert.False(canFound);
    }

    // -----------------------------------------------------------------------
    // FoundCityWithSettler — success path
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ReturnNewCity_When_FoundCityWithSettlerSucceeds()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        var city = units.FoundCityWithSettler(settler, "Rome", cities, grid);

        Assert.NotNull(city);
        Assert.Equal("Rome", city!.Name);
    }

    [Fact]
    public void Should_PlaceCityAtSettlerPosition_When_FoundCitySucceeds()
    {
        var (grid, units, cities) = CreateSetup();
        var pos = new HexCoord(5, 5);
        var settler = units.CreateUnit("Settler", pos, grid);

        units.FoundCityWithSettler(settler, "Athens", cities, grid);

        Assert.Equal(pos, cities.GetCityAt(pos)!.Position);
    }

    [Fact]
    public void Should_RemoveSettlerFromAllUnits_When_FoundCitySucceeds()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        units.FoundCityWithSettler(settler, "Carthage", cities, grid);

        Assert.DoesNotContain(settler, units.AllUnits);
    }

    [Fact]
    public void Should_ClearUnitOccupancy_When_FoundCitySucceeds()
    {
        var (grid, units, cities) = CreateSetup();
        var pos = new HexCoord(5, 5);
        var settler = units.CreateUnit("Settler", pos, grid);

        units.FoundCityWithSettler(settler, "Sparta", cities, grid);

        // The tile is now a city tile, not a unit tile.
        Assert.False(units.IsOccupied(pos));
    }

    [Fact]
    public void Should_RegisterCityInCityManager_When_FoundCitySucceeds()
    {
        var (grid, units, cities) = CreateSetup();
        var pos = new HexCoord(5, 5);
        var settler = units.CreateUnit("Settler", pos, grid);

        units.FoundCityWithSettler(settler, "Babylon", cities, grid);

        Assert.NotNull(cities.GetCityAt(pos));
    }

    [Fact]
    public void Should_StartWithNullCurrentProduction_When_CityFounded()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        var city = units.FoundCityWithSettler(settler, "Troy", cities, grid);

        Assert.Null(city!.CurrentProduction);
    }

    // -----------------------------------------------------------------------
    // FoundCityWithSettler — failure paths (null return, no state change)
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ReturnNull_When_FoundCityCalledWithEmptyName()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        var city = units.FoundCityWithSettler(settler, "", cities, grid);

        Assert.Null(city);
    }

    [Fact]
    public void Should_ReturnNull_When_FoundCityCalledWithWhitespaceName()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        var city = units.FoundCityWithSettler(settler, "   ", cities, grid);

        Assert.Null(city);
    }

    [Fact]
    public void Should_RetainSettlerInAllUnits_When_FoundCityFailsDueToEmptyName()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        units.FoundCityWithSettler(settler, "", cities, grid);

        Assert.Contains(settler, units.AllUnits);
    }

    [Fact]
    public void Should_ReturnNull_When_FoundCityCalledOnNonSettler()
    {
        var (grid, units, cities) = CreateSetup();
        var warrior = units.CreateUnit("Warrior", new HexCoord(5, 5), grid);

        var city = units.FoundCityWithSettler(warrior, "InvalidCity", cities, grid);

        Assert.Null(city);
    }

    [Fact]
    public void Should_RetainWarriorInAllUnits_When_FoundCityFailsDueToNonSettlerUnit()
    {
        var (grid, units, cities) = CreateSetup();
        var warrior = units.CreateUnit("Warrior", new HexCoord(5, 5), grid);

        units.FoundCityWithSettler(warrior, "InvalidCity", cities, grid);

        Assert.Contains(warrior, units.AllUnits);
    }

    [Fact]
    public void Should_ReturnNull_When_FoundCityCalledAfterSettlerMoved()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);
        settler.TryMoveTo(new HexCoord(6, 5), grid, units);

        var city = units.FoundCityWithSettler(settler, "MovedCity", cities, grid);

        Assert.Null(city);
    }

    [Fact]
    public void Should_RetainSettlerInAllUnits_When_FoundCityFailsDueToSettlerMoved()
    {
        var (grid, units, cities) = CreateSetup();
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);
        settler.TryMoveTo(new HexCoord(6, 5), grid, units);

        units.FoundCityWithSettler(settler, "MovedCity", cities, grid);

        Assert.Contains(settler, units.AllUnits);
    }

    [Fact]
    public void Should_ReturnNull_When_FoundCityCalledWithNearbyExistingCity()
    {
        var (grid, units, cities) = CreateSetup();
        cities.CreateCity("TooClose", new HexCoord(5, 6), grid);
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        var city = units.FoundCityWithSettler(settler, "Blocked", cities, grid);

        Assert.Null(city);
    }

    [Fact]
    public void Should_RetainSettlerInAllUnits_When_FoundCityFailsDueToNearbyCity()
    {
        var (grid, units, cities) = CreateSetup();
        cities.CreateCity("TooClose", new HexCoord(5, 6), grid);
        var settler = units.CreateUnit("Settler", new HexCoord(5, 5), grid);

        units.FoundCityWithSettler(settler, "Blocked", cities, grid);

        Assert.Contains(settler, units.AllUnits);
    }
}
