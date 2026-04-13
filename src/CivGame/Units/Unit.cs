using CivGame.World;

namespace CivGame.Units;

/// <summary>
/// A single unit on the hex grid. Tracks position and movement budget.
/// Movement respects per-terrain costs via TerrainRules.
/// </summary>
public sealed class Unit
{
    private static int _nextId;

    public int Id { get; }
    public string UnitType { get; }
    /// <summary>Alias for UnitType; matches the naming convention used by City.Name.</summary>
    public string Name => UnitType;
    public HexCoord Position { get; internal set; }
    public int MovementRange { get; }
    public int MovementRemaining { get; internal set; }
    public bool CanMove => MovementRemaining > 0;
    public int OwnerId { get; }
    public int MaxHp { get; }
    public int Hp { get; internal set; }
    public int CombatStrength { get; }
    public bool IsDead => Hp <= 0;

    public Unit(string unitType, HexCoord position, int movementRange, int ownerId = 0)
    {
        Id = Interlocked.Increment(ref _nextId);
        UnitType = unitType;
        Position = position;
        MovementRange = movementRange;
        MovementRemaining = movementRange;
        OwnerId = ownerId;
        (MaxHp, CombatStrength) = unitType switch
        {
            "Warrior" => (10, 5),
            "Settler" => (5, 0),
            _ => (10, 0),
        };
        Hp = MaxHp;
    }

    /// <summary>
    /// Attempt to move this unit to the target hex.
    /// Uses weighted shortest-path (Dijkstra / uniform-cost search) that respects
    /// terrain movement costs. Returns true if move succeeded.
    /// </summary>
    public bool TryMoveTo(HexCoord target, HexGrid grid, UnitManager manager)
    {
        if (!CanMove) return false;
        if (!grid.InBounds(target)) return false;

        var targetCell = grid.GetCell(target);
        if (targetCell is null || !targetCell.IsPassable) return false;

        if (manager.IsOccupied(target)) return false;

        int cost = FindDistance(Position, target, grid, manager);
        if (cost < 0 || cost > MovementRemaining) return false;

        var oldPosition = Position;
        Position = target;
        MovementRemaining -= cost;

        manager.UpdatePositionIndex(oldPosition, target, this);

        return true;
    }

    /// <summary>Reset movement budget to full range (called at turn start).</summary>
    public void ResetMovement()
    {
        MovementRemaining = MovementRange;
    }

    /// <summary>
    /// Weighted shortest-path cost from start to goal on the hex grid.
    /// Returns -1 if no path exists. The cost of entering the goal cell is included.
    /// Intermediate cells must be passable and unoccupied; the goal occupancy is
    /// checked by the caller.
    /// </summary>
    internal static int FindDistance(HexCoord start, HexCoord goal, HexGrid grid, UnitManager manager)
    {
        if (start == goal) return 0;

        // Uniform-cost search (Dijkstra on small integer costs).
        var best = new Dictionary<HexCoord, int> { [start] = 0 };
        var frontier = new PriorityQueue<HexCoord, int>();
        frontier.Enqueue(start, 0);

        while (frontier.TryDequeue(out var current, out var currentCost))
        {
            if (current == goal) return currentCost;

            foreach (var neighborCoord in current.Neighbors())
            {
                if (!grid.InBounds(neighborCoord)) continue;

                var cell = grid.GetCell(neighborCoord);
                if (cell is null || !cell.IsPassable) continue;

                // Intermediate cells cannot be occupied (goal occupancy checked by caller).
                if (neighborCoord != goal && manager.IsOccupied(neighborCoord)) continue;

                int stepCost = TerrainRules.MovementCost(cell.Terrain);
                if (stepCost == int.MaxValue) continue;

                int newCost = currentCost + stepCost;
                if (best.TryGetValue(neighborCoord, out int existing) && existing <= newCost) continue;

                best[neighborCoord] = newCost;
                frontier.Enqueue(neighborCoord, newCost);
            }
        }

        return -1;
    }
}
