using CivGame.Units;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Determines whether a unit can legally initiate a melee attack on a target coord.
/// Stub — full implementation in #94.
/// </summary>
public static class CombatTargetResolver
{
    public static bool CanAttackFrom(Unit selectedUnit, HexCoord targetCoord, HexGrid grid, UnitManager units)
    {
        throw new NotImplementedException("CombatTargetResolver not yet implemented — see issue #94.");
    }
}
