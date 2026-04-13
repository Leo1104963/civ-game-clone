using CivGame.World;

namespace CivGame.Cities;

/// <summary>
/// Manages all cities. Provides creation, lookup, and bulk production ticking.
/// </summary>
public sealed class CityManager
{
    private readonly List<City> _cities = new();
    private readonly Dictionary<HexCoord, City> _positionIndex = new();

    public IReadOnlyList<City> AllCities => _cities.AsReadOnly();

    /// <summary>
    /// Create a city at the given position. Throws if out of bounds or already occupied by a city.
    /// </summary>
    public City CreateCity(string name, HexCoord position, HexGrid grid, int ownerId = 0)
    {
        if (!grid.InBounds(position))
            throw new ArgumentException($"Position {position} is out of bounds.");

        if (_positionIndex.ContainsKey(position))
            throw new InvalidOperationException($"Position {position} already has a city.");

        var city = new City(name, position, ownerId);
        _cities.Add(city);
        _positionIndex[position] = city;
        return city;
    }

    /// <summary>Returns the city at the given coordinate, or null.</summary>
    public City? GetCityAt(HexCoord coord) =>
        _positionIndex.TryGetValue(coord, out var city) ? city : null;

    /// <summary>Returns all cities owned by the given player.</summary>
    public IEnumerable<City> CitiesOwnedBy(int ownerId)
    {
        foreach (var c in _cities)
        {
            if (c.OwnerId == ownerId) yield return c;
        }
    }

    /// <summary>Tick production for cities owned by the given player only.</summary>
    public void TickProductionFor(int ownerId)
    {
        foreach (var c in _cities)
        {
            if (c.OwnerId == ownerId) c.TickProduction();
        }
    }

    /// <summary>Tick yield-driven production for cities owned by the given player only.</summary>
    public void TickProductionFor(int ownerId, HexGrid grid)
    {
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        foreach (var c in _cities)
        {
            if (c.OwnerId != ownerId) continue;
            var y = YieldCalculator.Calculate(c, grid);
            c.TickProduction(y.Production);
        }
    }

    /// <summary>Tick production for all cities.</summary>
    public void TickAllProduction()
    {
        foreach (var city in _cities)
        {
            city.TickProduction();
        }
    }

    /// <summary>Tick yield-driven production for all cities.</summary>
    public void TickAllProduction(HexGrid grid)
    {
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        foreach (var city in _cities)
        {
            var y = YieldCalculator.Calculate(city, grid);
            city.TickProduction(y.Production);
        }
    }
}
