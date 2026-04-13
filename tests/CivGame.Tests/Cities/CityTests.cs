using CivGame.Buildings;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Cities.Tests;

public class CityTests
{
    private static City CreateCity(string name = "TestCity", int q = 0, int r = 0)
    {
        return new City(name, new HexCoord(q, r));
    }

    [Fact]
    public void Should_HaveNameAndPosition_When_Created()
    {
        var pos = new HexCoord(3, 4);
        var city = new City("Rome", pos);

        Assert.Equal("Rome", city.Name);
        Assert.Equal(pos, city.Position);
    }

    [Fact]
    public void Should_HaveUniqueId_When_Created()
    {
        var city1 = CreateCity("A");
        var city2 = CreateCity("B");

        Assert.NotEqual(city1.Id, city2.Id);
    }

    [Fact]
    public void Should_HaveNoCurrentProduction_When_Created()
    {
        var city = CreateCity();

        Assert.Null(city.CurrentProduction);
    }

    [Fact]
    public void Should_HaveEmptyCompletedBuildings_When_Created()
    {
        var city = CreateCity();

        Assert.Empty(city.CompletedBuildings);
    }

    [Fact]
    public void Should_ThrowArgumentException_When_NameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new City("", new HexCoord(0, 0)));
    }

    [Fact]
    public void Should_ThrowArgumentException_When_NameIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new City("   ", new HexCoord(0, 0)));
    }

    [Fact]
    public void Should_ReturnTrueAndSetProduction_When_StartBuildingSucceeds()
    {
        var city = CreateCity();

        bool result = city.StartBuilding(BuildingCatalog.Granary);

        Assert.True(result);
        Assert.NotNull(city.CurrentProduction);
        Assert.Equal("Granary", city.CurrentProduction!.Definition.Name);
    }

    [Fact]
    public void Should_ReturnFalse_When_AlreadyBuildingSomething()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        var otherBuilding = new BuildingDefinition("Library", 8);
        bool result = city.StartBuilding(otherBuilding);

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_BuildingAlreadyCompleted()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        // Complete the granary
        for (int i = 0; i < 5; i++)
        {
            city.TickProduction(1);
        }

        // Try to build another granary
        bool result = city.StartBuilding(BuildingCatalog.Granary);

        Assert.False(result);
    }

    [Fact]
    public void Should_DecrementTurnsRemaining_When_TickProduction()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        city.TickProduction(1);

        Assert.Equal(4, city.CurrentProduction!.TurnsRemaining);
    }

    [Fact]
    public void Should_MoveToCompletedAndClearProduction_When_BuildingFinishes()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        for (int i = 0; i < 5; i++)
        {
            city.TickProduction(1);
        }

        Assert.Null(city.CurrentProduction);
        Assert.Single(city.CompletedBuildings);
        Assert.Equal("Granary", city.CompletedBuildings[0].Name);
    }

    [Fact]
    public void Should_DoNothing_When_TickProductionWithNoProduction()
    {
        var city = CreateCity();

        city.TickProduction(1); // should not throw

        Assert.Null(city.CurrentProduction);
        Assert.Empty(city.CompletedBuildings);
    }

    [Fact]
    public void Should_ReturnTrue_When_HasBuildingForInProgressBuilding()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        Assert.True(city.HasBuilding("Granary"));
    }

    [Fact]
    public void Should_ReturnTrue_When_HasBuildingForCompletedBuilding()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        for (int i = 0; i < 5; i++)
        {
            city.TickProduction(1);
        }

        Assert.True(city.HasBuilding("Granary"));
    }

    [Fact]
    public void Should_ReturnFalse_When_HasBuildingForUnknownBuilding()
    {
        var city = CreateCity();

        Assert.False(city.HasBuilding("Library"));
    }

    [Fact]
    public void Should_ReturnTrueForHasBuilding_When_CaseInsensitive()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        Assert.True(city.HasBuilding("granary"));
        Assert.True(city.HasBuilding("GRANARY"));
    }

    [Fact]
    public void Should_CompleteGranaryInExactlyFiveTicks()
    {
        var city = CreateCity();
        city.StartBuilding(BuildingCatalog.Granary);

        for (int i = 0; i < 4; i++)
        {
            city.TickProduction(1);
            Assert.NotNull(city.CurrentProduction);
        }

        city.TickProduction(1); // 5th tick
        Assert.Null(city.CurrentProduction);
        Assert.Single(city.CompletedBuildings);
    }
}
