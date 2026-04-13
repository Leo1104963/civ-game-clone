using CivGame.Units;
using CivGame.World;

namespace CivGame.Combat;

/// <summary>
/// Determines whether a unit can legally initiate a melee attack on a target coord.
/// Mirrors the preconditions of UnitManager.TryAttack (adjacency-based, fog-agnostic).
/// </summary>
public static class CombatTargetResolver
{
    public static bool CanAttackFrom(Unit selectedUnit, HexCoord targetCoord, HexGrid grid, UnitManager units)
    {
        if (selectedUnit is null || grid is null || units is null)
            return false;

        if (selectedUnit.CombatStrength <= 0) return false;
        if (selectedUnit.MovementRemaining <= 0) return false;
        if (selectedUnit.IsDead) return false;
        if (!grid.InBounds(targetCoord)) return false;

        bool adjacent = false;
        foreach (var neighbor in selectedUnit.Position.Neighbors())
        {
            if (neighbor == targetCoord) { adjacent = true; break; }
        }

        if (!adjacent) return false;

        var defender = units.GetUnitAt(targetCoord);
        if (defender is null) return false;
        if (defender.OwnerId == selectedUnit.OwnerId) return false;

        return true;
    }
}
