using CivGame.World;

namespace CivGame.Units;

/// <summary>
/// Manages all units on the map. Provides creation, lookup, and reachability queries.
/// </summary>
public sealed class UnitManager
{
    private readonly List<Unit> _units = new();
    private readonly Dictionary<HexCoord, Unit> _positionIndex = new();

    public IReadOnlyList<Unit> AllUnits => _units.AsReadOnly();

    /// <summary>
    /// Create a new unit and place it on the grid.
    /// Throws if the cell is out of bounds, impassable, or already occupied.
    /// </summary>
    public Unit CreateUnit(string unitType, HexCoord position, HexGrid grid)
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
            _ => throw new ArgumentException($"Unknown unit type: {unitType}"),
        };

        var unit = new Unit(unitType, position, movementRange);
        _units.Add(unit);
        _positionIndex[position] = unit;
        return unit;
    }

    /// <summary>Returns the unit at the given coordinate, or null.</summary>
    public Unit? GetUnitAt(HexCoord coord) =>
        _positionIndex.TryGetValue(coord, out var unit) ? unit : null;

    /// <summary>True if any unit occupies the given coordinate.</summary>
    public bool IsOccupied(HexCoord coord) => _positionIndex.ContainsKey(coord);

    /// <summary>
    /// All cells reachable by this unit with its remaining movement, via BFS.
    /// Excludes occupied cells and impassable cells. Includes current position.
    /// </summary>
    public IReadOnlySet<HexCoord> GetReachableCells(Unit unit, HexGrid grid)
    {
        var reachable = new HashSet<HexCoord> { unit.Position };
        var queue = new Queue<(HexCoord Coord, int Cost)>();
        queue.Enqueue((unit.Position, 0));

        while (queue.Count > 0)
        {
            var (current, cost) = queue.Dequeue();

            foreach (var neighborCoord in current.Neighbors())
            {
                if (reachable.Contains(neighborCoord)) continue;
                if (!grid.InBounds(neighborCoord)) continue;

                var cell = grid.GetCell(neighborCoord);
                if (cell is null || !cell.IsPassable) continue;

                int newCost = cost + 1;
                if (newCost > unit.MovementRemaining) continue;

                if (IsOccupied(neighborCoord)) continue;

                reachable.Add(neighborCoord);
                queue.Enqueue((neighborCoord, newCost));
            }
        }

        return reachable;
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
}
