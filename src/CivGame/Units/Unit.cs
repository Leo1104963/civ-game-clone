using CivGame.World;

namespace CivGame.Units;

/// <summary>
/// A single unit on the hex grid. Tracks position and movement budget.
/// </summary>
public sealed class Unit
{
    private static int _nextId;

    public int Id { get; }
    public string UnitType { get; }
    public HexCoord Position { get; private set; }
    public int MovementRange { get; }
    public int MovementRemaining { get; private set; }
    public bool CanMove => MovementRemaining > 0;

    public Unit(string unitType, HexCoord position, int movementRange)
    {
        Id = Interlocked.Increment(ref _nextId);
        UnitType = unitType;
        Position = position;
        MovementRange = movementRange;
        MovementRemaining = movementRange;
    }

    /// <summary>
    /// Attempt to move this unit to the target hex.
    /// Uses BFS to find shortest path and deducts movement cost (1 per hex traversed).
    /// Returns true if move succeeded.
    /// </summary>
    public bool TryMoveTo(HexCoord target, HexGrid grid, UnitManager manager)
    {
        if (!CanMove) return false;
        if (!grid.InBounds(target)) return false;

        var targetCell = grid.GetCell(target);
        if (targetCell is null || !targetCell.IsPassable) return false;

        if (manager.IsOccupied(target)) return false;

        // BFS to find shortest path distance
        int distance = FindDistance(Position, target, grid, manager);
        if (distance < 0 || distance > MovementRemaining) return false;

        var oldPosition = Position;
        Position = target;
        MovementRemaining -= distance;

        manager.UpdatePositionIndex(oldPosition, target, this);

        return true;
    }

    /// <summary>Reset movement budget to full range (called at turn start).</summary>
    public void ResetMovement()
    {
        MovementRemaining = MovementRange;
    }

    /// <summary>
    /// BFS shortest-path distance from start to goal on the hex grid.
    /// Returns -1 if no path exists.
    /// Only traverses passable, unoccupied cells (goal occupancy check done by caller).
    /// </summary>
    internal static int FindDistance(HexCoord start, HexCoord goal, HexGrid grid, UnitManager manager)
    {
        if (start == goal) return 0;

        var visited = new HashSet<HexCoord> { start };
        var queue = new Queue<(HexCoord Coord, int Dist)>();
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();

            foreach (var neighborCoord in current.Neighbors())
            {
                if (visited.Contains(neighborCoord)) continue;
                if (!grid.InBounds(neighborCoord)) continue;

                var cell = grid.GetCell(neighborCoord);
                if (cell is null || !cell.IsPassable) continue;

                if (neighborCoord == goal)
                {
                    return dist + 1;
                }

                // Cannot path through occupied cells (except the goal)
                if (manager.IsOccupied(neighborCoord)) continue;

                visited.Add(neighborCoord);
                queue.Enqueue((neighborCoord, dist + 1));
            }
        }

        return -1; // no path found
    }
}
