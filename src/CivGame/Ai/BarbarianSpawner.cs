using CivGame.Cities;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Ai;

/// <summary>
/// Decides whether and where to spawn a new barbarian warrior each turn.
/// Pure C# — no Godot runtime required.
/// </summary>
public static class BarbarianSpawner
{
    public const int BarbarianOwnerId = 1;
    public const int DefaultMinDistanceFromPlayer = 4;
    public const int DefaultMaxBarbarians = 6;
    public const int DefaultSpawnEveryNTurns = 5;

    /// <summary>
    /// Decide whether to spawn a barbarian this turn and, if so, where.
    /// Returns the chosen HexCoord, or null if no spawn should occur.
    ///
    /// Rules:
    ///   - Spawn iff (currentTurn % spawnEveryNTurns == 0) and current barbarian count &lt; maxBarbarians.
    ///   - Candidate cells: passable, unoccupied, with min distance >= minDistanceFromPlayer from
    ///     every player-owned unit and city.
    ///   - If there are no candidates, returns null.
    /// </summary>
    public static HexCoord? ChooseSpawn(
        int currentTurn,
        HexGrid grid,
        UnitManager units,
        CityManager cities,
        Random rng,
        int minDistanceFromPlayer = DefaultMinDistanceFromPlayer,
        int maxBarbarians = DefaultMaxBarbarians,
        int spawnEveryNTurns = DefaultSpawnEveryNTurns)
    {
        if (currentTurn % spawnEveryNTurns != 0)
            return null;

        int barbarianCount = units.UnitsOwnedBy(BarbarianOwnerId).Count();
        if (barbarianCount >= maxBarbarians)
            return null;

        // Collect player-owned unit positions and city positions.
        var playerPositions = new List<HexCoord>();
        foreach (var unit in units.AllUnits)
        {
            if (unit.OwnerId != BarbarianOwnerId)
                playerPositions.Add(unit.Position);
        }
        foreach (var city in cities.AllCities)
        {
            if (city.OwnerId != BarbarianOwnerId)
                playerPositions.Add(city.Position);
        }

        var candidates = new List<HexCoord>();
        foreach (var cell in grid.AllCells())
        {
            if (!cell.IsPassable) continue;
            if (units.IsOccupied(cell.Coord)) continue;
            if (cities.GetCityAt(cell.Coord) is not null) continue;

            bool farEnough = true;
            foreach (var pos in playerPositions)
            {
                if (cell.Coord.DistanceTo(pos) < minDistanceFromPlayer)
                {
                    farEnough = false;
                    break;
                }
            }
            if (farEnough)
                candidates.Add(cell.Coord);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[rng.Next(candidates.Count)];
    }

    /// <summary>
    /// Convenience: call ChooseSpawn and, if non-null, create the barbarian warrior.
    /// Returns the created Unit or null.
    /// </summary>
    public static Unit? TrySpawn(
        int currentTurn,
        HexGrid grid,
        UnitManager units,
        CityManager cities,
        Random rng,
        int minDistanceFromPlayer = DefaultMinDistanceFromPlayer,
        int maxBarbarians = DefaultMaxBarbarians,
        int spawnEveryNTurns = DefaultSpawnEveryNTurns)
    {
        var coord = ChooseSpawn(currentTurn, grid, units, cities, rng,
            minDistanceFromPlayer, maxBarbarians, spawnEveryNTurns);

        if (coord is null)
            return null;

        return units.CreateUnit("Warrior", coord.Value, grid, ownerId: BarbarianOwnerId);
    }
}
