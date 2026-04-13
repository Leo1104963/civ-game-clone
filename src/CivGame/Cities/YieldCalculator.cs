using CivGame.Buildings;
using CivGame.World;

namespace CivGame.Cities;

public readonly record struct YieldResult(int Food, int Production, int Science);

public static class YieldCalculator
{
    /// <summary>
    /// Compute the per-turn Food, Production, and Science for a city by summing the terrain
    /// yields of the city's own tile plus the six adjacent tiles (radius 1, no citizen
    /// assignment), plus the ScienceYield from all completed buildings.
    /// Out-of-bounds neighbors contribute zero. A cell with Water terrain contributes its
    /// water yield even though units can't enter Water.
    /// </summary>
    public static YieldResult Calculate(City city, HexGrid grid)
    {
        if (city is null) throw new ArgumentNullException(nameof(city));
        if (grid is null) throw new ArgumentNullException(nameof(grid));

        int food = 0;
        int production = 0;
        int science = 0;

        var centerCell = grid.GetCell(city.Position);
        if (centerCell is not null)
        {
            var c = TerrainYields.Of(centerCell.Terrain);
            food += c.Food;
            production += c.Production;
            science += c.Science;
        }

        foreach (var neighbor in grid.GetNeighbors(city.Position))
        {
            var y = TerrainYields.Of(neighbor.Terrain);
            food += y.Food;
            production += y.Production;
            science += y.Science;
        }

        // Sum science from completed buildings only (not current production).
        foreach (var building in city.CompletedBuildings)
        {
            science += building.ScienceYield;
        }

        return new YieldResult(food, production, science);
    }
}
