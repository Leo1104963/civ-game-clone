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
    public City CreateCity(string name, HexCoord position, HexGrid grid)
    {
        if (!grid.InBounds(position))
            throw new ArgumentException($"Position {position} is out of bounds.");

        if (_positionIndex.ContainsKey(position))
            throw new InvalidOperationException($"Position {position} already has a city.");

        var city = new City(name, position);
        _cities.Add(city);
        _positionIndex[position] = city;
        return city;
    }

    /// <summary>Returns the city at the given coordinate, or null.</summary>
    public City? GetCityAt(HexCoord coord) =>
        _positionIndex.TryGetValue(coord, out var city) ? city : null;

    /// <summary>Tick production for all cities.</summary>
    public void TickAllProduction()
    {
        foreach (var city in _cities)
        {
            city.TickProduction();
        }
    }
}
