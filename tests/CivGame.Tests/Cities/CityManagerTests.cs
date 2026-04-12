using CivGame.Buildings;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Cities.Tests;

public class CityManagerTests
{
    private static HexGrid CreateGrid(int width = 10, int height = 10)
    {
        return new HexGrid(width, height);
    }

    [Fact]
    public void Should_HaveNoCities_When_Created()
    {
        var manager = new CityManager();

        Assert.Empty(manager.AllCities);
    }

    [Fact]
    public void Should_CreateCityAndAddToList_When_ValidPosition()
    {
        var manager = new CityManager();
        var grid = CreateGrid();

        var city = manager.CreateCity("Rome", new HexCoord(0, 0), grid);

        Assert.NotNull(city);
        Assert.Equal("Rome", city.Name);
        Assert.Equal(new HexCoord(0, 0), city.Position);
        Assert.Single(manager.AllCities);
    }

    [Fact]
    public void Should_CreateMultipleCities_When_DifferentPositions()
    {
        var manager = new CityManager();
        var grid = CreateGrid();

        manager.CreateCity("Rome", new HexCoord(0, 0), grid);
        manager.CreateCity("Athens", new HexCoord(1, 1), grid);

        Assert.Equal(2, manager.AllCities.Count);
    }

    [Fact]
    public void Should_ThrowArgumentException_When_PositionOutOfBounds()
    {
        var manager = new CityManager();
        var grid = CreateGrid(5, 5);

        Assert.Throws<ArgumentException>(() =>
            manager.CreateCity("Rome", new HexCoord(10, 10), grid));
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_PositionAlreadyOccupied()
    {
        var manager = new CityManager();
        var grid = CreateGrid();
        var pos = new HexCoord(2, 3);

        manager.CreateCity("Rome", pos, grid);

        Assert.Throws<InvalidOperationException>(() =>
            manager.CreateCity("Athens", pos, grid));
    }

    [Fact]
    public void Should_ReturnCity_When_GetCityAtOccupiedCoord()
    {
        var manager = new CityManager();
        var grid = CreateGrid();
        var pos = new HexCoord(3, 4);

        var created = manager.CreateCity("Rome", pos, grid);
        var found = manager.GetCityAt(pos);

        Assert.NotNull(found);
        Assert.Same(created, found);
    }

    [Fact]
    public void Should_ReturnNull_When_GetCityAtEmptyCoord()
    {
        var manager = new CityManager();

        var found = manager.GetCityAt(new HexCoord(5, 5));

        Assert.Null(found);
    }

    [Fact]
    public void Should_TickAllCities_When_TickAllProduction()
    {
        var manager = new CityManager();
        var grid = CreateGrid();

        var rome = manager.CreateCity("Rome", new HexCoord(0, 0), grid);
        var athens = manager.CreateCity("Athens", new HexCoord(1, 1), grid);

        rome.StartBuilding(BuildingCatalog.Granary);
        athens.StartBuilding(BuildingCatalog.Granary);

        manager.TickAllProduction();

        Assert.Equal(4, rome.CurrentProduction!.TurnsRemaining);
        Assert.Equal(4, athens.CurrentProduction!.TurnsRemaining);
    }

    [Fact]
    public void Should_CompleteBuildings_When_TickAllProductionEnoughTimes()
    {
        var manager = new CityManager();
        var grid = CreateGrid();

        var city = manager.CreateCity("Rome", new HexCoord(0, 0), grid);
        city.StartBuilding(BuildingCatalog.Granary);

        for (int i = 0; i < 5; i++)
        {
            manager.TickAllProduction();
        }

        Assert.Null(city.CurrentProduction);
        Assert.Single(city.CompletedBuildings);
        Assert.Equal("Granary", city.CompletedBuildings[0].Name);
    }
}
