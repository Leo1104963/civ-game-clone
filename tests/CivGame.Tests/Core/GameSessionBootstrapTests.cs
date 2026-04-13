using CivGame.Cities;
using CivGame.Core;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Tests.Core;

/// <summary>
/// Failing tests for issue #53: Bootstrap v1 game session with generated map
/// and starting settler.
///
/// Covers the new GameSession(int, int, int seed) constructor:
/// - Map is generated (not all-Grass), contains Water
/// - Capital is placed on Grass at grid center
/// - Exactly one Warrior and one Settler, both on passable neighbors of Capital
/// - Deterministic: same seed → identical terrain; different seeds → different terrain
/// - 4-arg constructor regression
/// </summary>
public class GameSessionBootstrapTests
{
    // -----------------------------------------------------------------------
    // Generated map — terrain distribution
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ContainAtLeastOneWaterCell_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        bool hasWater = session.Grid.AllCells()
            .Any(c => c.Terrain == TerrainType.Water);

        Assert.True(hasWater);
    }

    [Fact]
    public void Should_PlaceCapitalOnGrassTerrain_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        var capitalPos = session.Cities.AllCities[0].Position;
        var capitalCell = session.Grid.GetCell(capitalPos);

        Assert.NotNull(capitalCell);
        Assert.Equal(TerrainType.Grass, capitalCell!.Terrain);
    }

    // -----------------------------------------------------------------------
    // Capital city
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_PlaceExactlyOneCity_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        Assert.Single(session.Cities.AllCities);
    }

    [Fact]
    public void Should_NameCityCapital_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        Assert.Equal("Capital", session.Cities.AllCities[0].Name);
    }

    [Fact]
    public void Should_PlaceCapitalAtGridCenter_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        // center = (width/2, height/2) = (5, 4) for a 10×8 grid
        Assert.Equal(new HexCoord(5, 4), session.Cities.AllCities[0].Position);
    }

    // -----------------------------------------------------------------------
    // Starting units
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_PlaceExactlyTwoUnits_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        Assert.Equal(2, session.Units.AllUnits.Count);
    }

    [Fact]
    public void Should_PlaceOneWarrior_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        int warriorCount = session.Units.AllUnits.Count(u => u.UnitType == "Warrior");
        Assert.Equal(1, warriorCount);
    }

    [Fact]
    public void Should_PlaceOneSettler_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        int settlerCount = session.Units.AllUnits.Count(u => u.UnitType == "Settler");
        Assert.Equal(1, settlerCount);
    }

    [Fact]
    public void Should_PlaceBothUnitsAdjacentToCapital_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        var capitalPos = session.Cities.AllCities[0].Position;
        var neighborCoords = session.Grid.GetNeighbors(capitalPos)
            .Select(n => n.Coord)
            .ToHashSet();

        foreach (var unit in session.Units.AllUnits)
        {
            Assert.Contains(unit.Position, neighborCoords);
        }
    }

    [Fact]
    public void Should_PlaceBothUnitsOnPassableTiles_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        foreach (var unit in session.Units.AllUnits)
        {
            var cell = session.Grid.GetCell(unit.Position);
            Assert.NotNull(cell);
            Assert.True(TerrainRules.IsPassable(cell!.Terrain),
                $"Unit {unit.UnitType} at {unit.Position} is on impassable terrain {cell.Terrain}");
        }
    }

    [Fact]
    public void Should_PlaceBothUnitsOnDifferentTiles_When_DefaultSessionCreated()
    {
        var session = new GameSession(10, 8);

        var positions = session.Units.AllUnits.Select(u => u.Position).ToList();
        Assert.Equal(2, positions.Distinct().Count());
    }

    // -----------------------------------------------------------------------
    // Seed parameter — determinism and differentiation
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_ProduceIdenticalTerrain_When_SameSeedUsedTwice()
    {
        var session1 = new GameSession(10, 8, GameSession.DefaultSeed);
        var session2 = new GameSession(10, 8, GameSession.DefaultSeed);

        var cells1 = session1.Grid.AllCells().ToList();
        var cells2 = session2.Grid.AllCells().ToList();

        Assert.Equal(cells1.Count, cells2.Count);
        for (int i = 0; i < cells1.Count; i++)
        {
            Assert.Equal(cells1[i].Terrain, cells2[i].Terrain);
        }
    }

    [Fact]
    public void Should_ProduceDifferentTerrain_When_DifferentSeedsUsed()
    {
        var session1 = new GameSession(10, 8, seed: 1);
        var session2 = new GameSession(10, 8, seed: 2);

        var cells1 = session1.Grid.AllCells().ToList();
        var cells2 = session2.Grid.AllCells().ToList();

        bool anyDifference = cells1.Zip(cells2, (a, b) => a.Terrain != b.Terrain).Any(diff => diff);
        Assert.True(anyDifference);
    }

    [Fact]
    public void Should_ProduceIdenticalTerrain_When_ExplicitSeedMatchesDefault()
    {
        // GameSession(10, 8) and GameSession(10, 8, 12345) must be identical
        var sessionDefault = new GameSession(10, 8);
        var sessionExplicit = new GameSession(10, 8, GameSession.DefaultSeed);

        var cells1 = sessionDefault.Grid.AllCells().ToList();
        var cells2 = sessionExplicit.Grid.AllCells().ToList();

        for (int i = 0; i < cells1.Count; i++)
        {
            Assert.Equal(cells1[i].Terrain, cells2[i].Terrain);
        }
    }

    // -----------------------------------------------------------------------
    // 4-arg constructor — no regression
    // -----------------------------------------------------------------------

    [Fact]
    public void Should_AcceptPreBuiltComponents_When_FourArgConstructorUsed()
    {
        var grid = new HexGrid(5, 5);
        var units = new UnitManager();
        var cities = new CityManager();
        var turns = new TurnManager(units, cities);

        var session = new GameSession(grid, units, cities, turns);

        Assert.Same(grid, session.Grid);
        Assert.Same(units, session.Units);
        Assert.Same(cities, session.Cities);
        Assert.Same(turns, session.Turns);
    }

    [Fact]
    public void Should_NotPlaceAnyUnitsOrCities_When_FourArgConstructorUsedWithEmptyManagers()
    {
        var grid = new HexGrid(5, 5);
        var units = new UnitManager();
        var cities = new CityManager();
        var turns = new TurnManager(units, cities);

        var session = new GameSession(grid, units, cities, turns);

        Assert.Empty(session.Units.AllUnits);
        Assert.Empty(session.Cities.AllCities);
    }
}
