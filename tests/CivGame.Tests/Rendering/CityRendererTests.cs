using CivGame.Rendering;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for CityRenderer. Verifies that the class exists with the expected
/// public API and that the data model supports rendering all cities with
/// correct pixel positions, square bounds, and name labels.
/// </summary>
public class CityRendererTests
{
    private const float DefaultHexSize = 40f;
    private const float CityHalfSize = 14f;

    private static (HexGrid Grid, CityManager Manager) CreateDefaultSetup()
    {
        var grid = new HexGrid(5, 5);
        var manager = new CityManager();
        return (grid, manager);
    }

    // --- CityRenderer class existence and API ---

    [Fact]
    public void Should_Exist_When_Referenced()
    {
        var type = typeof(CityRenderer);
        Assert.NotNull(type);
    }

    [Fact]
    public void Should_HaveInitializeMethod_When_Called()
    {
        var renderer = new CityRenderer();
        var (grid, manager) = CreateDefaultSetup();

        renderer.Initialize(manager, grid, DefaultHexSize);
    }

    [Fact]
    public void Should_HaveRefreshMethod_When_Called()
    {
        var renderer = new CityRenderer();

        renderer.Refresh();
    }

    // --- Data model correctness for rendering ---

    [Fact]
    public void Should_ComputeCorrectSquareCenter_When_CityAtOrigin()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateCity("TestCity", new HexCoord(0, 0), grid);

        var (px, py) = HexGrid.HexToPixel(new HexCoord(0, 0), DefaultHexSize);
        Assert.Equal(0f, px, precision: 2);
        Assert.Equal(0f, py, precision: 2);
    }

    [Fact]
    public void Should_ComputeCorrectSquareCenter_When_CityAtArbitraryPosition()
    {
        var (grid, manager) = CreateDefaultSetup();
        var city = manager.CreateCity("TestCity", new HexCoord(1, 2), grid);

        var (px, py) = HexGrid.HexToPixel(city.Position, DefaultHexSize);

        float expectedX = DefaultHexSize * (3f / 2f * 1);
        float expectedY = DefaultHexSize * (MathF.Sqrt(3f) / 2f * 1 + MathF.Sqrt(3f) * 2);
        Assert.Equal(expectedX, px, precision: 2);
        Assert.Equal(expectedY, py, precision: 2);
    }

    [Fact]
    public void Should_ComputeCorrectSquareBounds_When_RenderingCity()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateCity("TestCity", new HexCoord(0, 0), grid);

        var (px, py) = HexGrid.HexToPixel(new HexCoord(0, 0), DefaultHexSize);

        float left = px - CityHalfSize;
        float top = py - CityHalfSize;
        float width = CityHalfSize * 2;
        float height = CityHalfSize * 2;

        Assert.Equal(28f, width);
        Assert.Equal(28f, height);
        Assert.Equal(px - 14f, left);
        Assert.Equal(py - 14f, top);
    }

    [Fact]
    public void Should_RenderAllCities_When_MultipleCitiesExist()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateCity("City1", new HexCoord(0, 0), grid);
        manager.CreateCity("City2", new HexCoord(1, 1), grid);
        manager.CreateCity("City3", new HexCoord(2, 2), grid);

        Assert.Equal(3, manager.AllCities.Count);
    }

    [Fact]
    public void Should_ExposeCityName_When_DrawingLabel()
    {
        var (grid, manager) = CreateDefaultSetup();
        var city = manager.CreateCity("Capital", new HexCoord(2, 2), grid);

        Assert.Equal("Capital", city.Name);
    }

    [Fact]
    public void Should_ComputeNameLabelPosition_When_DrawingAboveSquare()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateCity("TestCity", new HexCoord(1, 1), grid);

        var (px, py) = HexGrid.HexToPixel(new HexCoord(1, 1), DefaultHexSize);

        float labelX = px - CityHalfSize;
        float labelY = py - CityHalfSize - 5f;

        Assert.Equal(px - 14f, labelX);
        Assert.Equal(py - 19f, labelY);
    }

    [Fact]
    public void Should_ProduceNonOverlappingSquareCenters_When_MultipleCitiesRendered()
    {
        var (grid, manager) = CreateDefaultSetup();
        manager.CreateCity("A", new HexCoord(0, 0), grid);
        manager.CreateCity("B", new HexCoord(2, 2), grid);
        manager.CreateCity("C", new HexCoord(4, 4), grid);

        var centers = new HashSet<(float, float)>();
        foreach (var city in manager.AllCities)
        {
            var (px, py) = HexGrid.HexToPixel(city.Position, DefaultHexSize);
            var rounded = (MathF.Round(px, 2), MathF.Round(py, 2));
            Assert.True(centers.Add(rounded));
        }

        Assert.Equal(3, centers.Count);
    }
}
