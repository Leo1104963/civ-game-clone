using CivGame.Cities;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Ai;

/// <summary>
/// Simple AI for barbarian units: move toward nearest player unit or city and attack if adjacent.
/// Pure C# — no Godot runtime required.
/// </summary>
public static class BarbarianAi
{
    /// <summary>
    /// Run one turn of AI for every barbarian-owned unit. For each barbarian:
    ///   1. Find the nearest player-owned unit or city (by HexCoord.DistanceTo).
    ///   2. If adjacent and barbarian has CombatStrength > 0 and movement left, TryAttack it.
    ///   3. Else move one step along the shortest reachable path toward it.
    ///   4. If no player target exists, the barbarian does nothing.
    /// Repeats until the barbarian cannot usefully act this turn.
    /// </summary>
    public static void TakeTurn(HexGrid grid, UnitManager units, CityManager cities)
    {
        // Snapshot the list so spawning/removal mid-turn doesn't affect iteration.
        var barbarians = units.UnitsOwnedBy(BarbarianSpawner.BarbarianOwnerId).ToList();

        foreach (var barbarian in barbarians)
        {
            if (barbarian.IsDead) continue;

            // Keep acting until no movement left or no valid action.
            bool acted = true;
            while (acted && barbarian.MovementRemaining > 0 && !barbarian.IsDead)
            {
                acted = false;

                var target = FindNearestTarget(barbarian.Position, units, cities);
                if (target is null) break;

                int distance = barbarian.Position.DistanceTo(target.Value);

                if (distance == 1)
                {
                    // Adjacent — attempt attack if there is a unit there.
                    var defender = units.GetUnitAt(target.Value);
                    if (defender is not null && barbarian.CombatStrength > 0)
                    {
                        units.TryAttack(barbarian, target.Value, grid);
                        acted = true;
                    }
                    // If target is only a city (no unit there), can't attack cities yet — stop.
                    break;
                }
                else
                {
                    // Not adjacent — move one step closer.
                    var step = BestStepToward(barbarian, target.Value, grid, units);
                    if (step is not null)
                    {
                        bool moved = barbarian.TryMoveTo(step.Value, grid, units);
                        if (moved) acted = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the coord of the nearest player-owned unit or city,
    /// or null if none exist.
    /// </summary>
    private static HexCoord? FindNearestTarget(HexCoord from, UnitManager units, CityManager cities)
    {
        HexCoord? nearest = null;
        int bestDist = int.MaxValue;

        foreach (var unit in units.AllUnits)
        {
            if (unit.OwnerId == BarbarianSpawner.BarbarianOwnerId) continue;
            int d = from.DistanceTo(unit.Position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = unit.Position;
            }
        }

        foreach (var city in cities.AllCities)
        {
            if (city.OwnerId == BarbarianSpawner.BarbarianOwnerId) continue;
            int d = from.DistanceTo(city.Position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = city.Position;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Pick the best adjacent neighbor to move to: passable, unoccupied, reachable within
    /// MovementRemaining, and minimizes DistanceTo(targetCoord).
    /// Returns null if no valid step exists.
    /// </summary>
    private static HexCoord? BestStepToward(Unit barbarian, HexCoord targetCoord, HexGrid grid, UnitManager units)
    {
        HexCoord? best = null;
        int bestDist = int.MaxValue;

        foreach (var neighbor in barbarian.Position.Neighbors())
        {
            if (!grid.InBounds(neighbor)) continue;
            var cell = grid.GetCell(neighbor);
            if (cell is null || !cell.IsPassable) continue;
            if (units.IsOccupied(neighbor)) continue;

            int cost = TerrainRules.MovementCost(cell.Terrain);
            if (cost == int.MaxValue || cost > barbarian.MovementRemaining) continue;

            int distToTarget = neighbor.DistanceTo(targetCoord);
            if (distToTarget < bestDist)
            {
                bestDist = distToTarget;
                best = neighbor;
            }
        }

        return best;
    }
}
