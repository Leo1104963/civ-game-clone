using CivGame.Cities;
using CivGame.Combat;
using CivGame.Tech;
using CivGame.World;

namespace CivGame.Units;

/// <summary>
/// Manages all units on the map. Provides creation, lookup, and reachability queries.
/// Reachability uses weighted shortest-path respecting TerrainRules.MovementCost.
/// </summary>
public sealed class UnitManager
{
    private readonly List<Unit> _units = new();
    private readonly Dictionary<HexCoord, Unit> _positionIndex = new();

    public IReadOnlyList<Unit> AllUnits => _units.AsReadOnly();

    /// <summary>
    /// Create a new unit and place it on the grid.
    /// Throws if the cell is out of bounds, impassable, or already occupied.
    /// Known unit types: "Warrior" (movement 2), "Settler" (movement 2).
    /// </summary>
    public Unit CreateUnit(string unitType, HexCoord position, HexGrid grid, int ownerId = 0)
    {
        if (!grid.InBounds(position))
            throw new ArgumentException($"Position {position} is out of bounds.");

        var cell = grid.GetCell(position);
        if (cell is null || !cell.IsPassable)
            throw new ArgumentException($"Position {position} is not passable.");

        if (_positionIndex.ContainsKey(position))
            throw new InvalidOperationException($"Position {position} is already occupied.");

        int movementRange = unitType switch
        {
            "Warrior" => 2,
            "Settler" => 2,
            "Horseman" => 3,
            _ => throw new ArgumentException($"Unknown unit type: {unitType}"),
        };

        var unit = new Unit(unitType, position, movementRange, ownerId);
        _units.Add(unit);
        _positionIndex[position] = unit;
        return unit;
    }

    /// <summary>
    /// Tech-aware unit spawn. Checks the tech gate first (when <paramref name="unlocks"/> is
    /// non-null), then delegates to <see cref="CreateUnit"/>.
    /// Returns a locked result when the unit type is gated and not yet researched.
    /// Throws for invalid input (bad position, occupied cell, unknown unit type) — matching
    /// CreateUnit behaviour.
    /// </summary>
    public UnitSpawnResult TrySpawnUnit(
        string unitType,
        HexCoord position,
        HexGrid grid,
        int ownerId,
        TechUnlockService? unlocks)
    {
        if (unlocks is not null)
        {
            var tag = $"unit:{unitType}";
            if (!unlocks.IsUnlocked(ownerId, tag))
            {
                var techName = unlocks.GatingTechName(tag) ?? "unknown tech";
                return new UnitSpawnResult(null, $"Requires {techName}");
            }
        }

        var unit = CreateUnit(unitType, position, grid, ownerId);
        return new UnitSpawnResult(unit, null);
    }

    /// <summary>Returns all units owned by the given player.</summary>
    public IEnumerable<Unit> UnitsOwnedBy(int ownerId)
    {
        foreach (var u in _units)
        {
            if (u.OwnerId == ownerId) yield return u;
        }
    }

    /// <summary>Reset movement budgets for units owned by the given player only.</summary>
    public void ResetMovementFor(int ownerId)
    {
        foreach (var u in _units)
        {
            if (u.OwnerId == ownerId) u.ResetMovement();
        }
    }

    /// <summary>Returns the unit at the given coordinate, or null.</summary>
    public Unit? GetUnitAt(HexCoord coord) =>
        _positionIndex.TryGetValue(coord, out var unit) ? unit : null;

    /// <summary>True if any unit occupies the given coordinate.</summary>
    public bool IsOccupied(HexCoord coord) => _positionIndex.ContainsKey(coord);

    /// <summary>
    /// All cells reachable by this unit with its remaining movement, using
    /// weighted shortest-path (TerrainRules.MovementCost). Excludes occupied
    /// and impassable cells. Includes current position.
    /// </summary>
    public IReadOnlySet<HexCoord> GetReachableCells(Unit unit, HexGrid grid)
    {
        var best = new Dictionary<HexCoord, int> { [unit.Position] = 0 };
        var frontier = new PriorityQueue<HexCoord, int>();
        frontier.Enqueue(unit.Position, 0);

        while (frontier.TryDequeue(out var current, out var currentCost))
        {
            foreach (var neighborCoord in current.Neighbors())
            {
                if (!grid.InBounds(neighborCoord)) continue;

                var cell = grid.GetCell(neighborCoord);
                if (cell is null || !cell.IsPassable) continue;
                if (IsOccupied(neighborCoord)) continue;

                int stepCost = TerrainRules.MovementCost(cell.Terrain);
                if (stepCost == int.MaxValue) continue;

                int newCost = currentCost + stepCost;
                if (newCost > unit.MovementRemaining) continue;

                if (best.TryGetValue(neighborCoord, out int existing) && existing <= newCost) continue;

                best[neighborCoord] = newCost;
                frontier.Enqueue(neighborCoord, newCost);
            }
        }

        return new HashSet<HexCoord>(best.Keys);
    }

    /// <summary>Reset all units' movement budgets (called at turn start).</summary>
    public void ResetAllMovement()
    {
        foreach (var unit in _units)
        {
            unit.ResetMovement();
        }
    }

    /// <summary>
    /// Update the position index when a unit moves from one cell to another.
    /// Called internally by Unit.TryMoveTo.
    /// </summary>
    internal void UpdatePositionIndex(HexCoord oldPosition, HexCoord newPosition, Unit unit)
    {
        _positionIndex.Remove(oldPosition);
        _positionIndex[newPosition] = unit;
    }

    /// <summary>
    /// Attempt a melee attack from attacker's current position against a target coord.
    /// Preconditions: target is an adjacent occupied hex whose unit has a different OwnerId,
    ///                attacker has MovementRemaining > 0 and CombatStrength > 0.
    /// On success: CombatResolver is invoked, both units' Hp updated, any dead unit is
    /// removed from the manager. Attacker spends 1 MovementRemaining regardless of outcome.
    /// Returns the CombatResult, or null if preconditions fail.
    /// </summary>
    public CombatResult? TryAttack(Unit attacker, HexCoord targetCoord, HexGrid grid)
    {
        if (attacker is null) return null;
        if (grid is null) return null;
        if (attacker.CombatStrength <= 0) return null;
        if (attacker.MovementRemaining <= 0) return null;
        if (attacker.IsDead) return null;

        // Target must be an adjacent hex.
        bool adjacent = false;
        foreach (var n in attacker.Position.Neighbors())
        {
            if (n == targetCoord) { adjacent = true; break; }
        }
        if (!adjacent) return null;

        if (!grid.InBounds(targetCoord)) return null;
        var cell = grid.GetCell(targetCoord);
        if (cell is null) return null;

        var defender = GetUnitAt(targetCoord);
        if (defender is null) return null;
        if (defender.OwnerId == attacker.OwnerId) return null;

        var result = CombatResolver.Resolve(attacker, defender, cell.Terrain);

        attacker.Hp = result.AttackerHpAfter;
        defender.Hp = result.DefenderHpAfter;

        // Attacker always pays 1 movement for the attack itself, clamped to 0.
        attacker.MovementRemaining = Math.Max(0, attacker.MovementRemaining - 1);

        if (result.AttackerKilled) RemoveUnit(attacker);
        if (result.DefenderKilled) RemoveUnit(defender);

        return result;
    }

    /// <summary>
    /// Remove a unit from the manager (e.g. a settler consumed by founding a city).
    /// </summary>
    internal void RemoveUnit(Unit unit)
    {
        _units.Remove(unit);
        _positionIndex.Remove(unit.Position);
    }

    /// <summary>
    /// True if founding a city at this settler's current position is currently legal.
    /// See FoundCityWithSettler for the rule set.
    /// </summary>
    public bool CanFoundCity(Unit settler, CityManager cityManager, HexGrid grid)
    {
        if (settler is null) return false;
        if (cityManager is null) return false;
        if (grid is null) return false;

        if (!string.Equals(settler.UnitType, "Settler", StringComparison.Ordinal)) return false;
        if (settler.MovementRemaining != settler.MovementRange) return false;

        if (!grid.InBounds(settler.Position)) return false;
        var cell = grid.GetCell(settler.Position);
        if (cell is null || !cell.IsPassable) return false;

        if (cityManager.GetCityAt(settler.Position) is not null) return false;

        foreach (var city in cityManager.AllCities)
        {
            if (settler.Position.DistanceTo(city.Position) <= 2) return false;
        }

        return true;
    }

    /// <summary>
    /// Found a new city at the settler's position, consuming the settler.
    /// Returns the new City on success, or null if rules fail.
    /// </summary>
    public City? FoundCityWithSettler(Unit settler, string cityName, CityManager cityManager, HexGrid grid)
    {
        if (!CanFoundCity(settler, cityManager, grid)) return null;
        if (string.IsNullOrWhiteSpace(cityName)) return null;

        var position = settler.Position;
        RemoveUnit(settler);

        try
        {
            return cityManager.CreateCity(cityName, position, grid);
        }
        catch
        {
            // If creation fails unexpectedly, restore the settler so state stays consistent.
            _units.Add(settler);
            _positionIndex[position] = settler;
            throw;
        }
    }
}
