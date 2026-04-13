using CivGame.Cities;

namespace CivGame.Rendering;

/// <summary>
/// Resolves the visual tier of a city based on its completed building count.
/// Outpost: 0 buildings; Town: 1–2 buildings; City: 3+ buildings.
/// </summary>
public static class CityTierResolver
{
    public static CityVisualTier ResolveTier(City city)
    {
        if (city is null) throw new ArgumentNullException(nameof(city));

        return city.CompletedBuildings.Count switch
        {
            0 => CityVisualTier.Outpost,
            1 or 2 => CityVisualTier.Town,
            _ => CityVisualTier.City,
        };
    }
}
