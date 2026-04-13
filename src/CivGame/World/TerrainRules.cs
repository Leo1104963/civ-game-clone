namespace CivGame.World;

/// <summary>
/// Static rules governing terrain passability and movement cost.
/// </summary>
public static class TerrainRules
{
    /// <summary>True if units can enter this terrain.</summary>
    public static bool IsPassable(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Grass => true,
            TerrainType.Plains => true,
            TerrainType.Forest => true,
            TerrainType.Water => false,
            _ => false,
        };
    }

    /// <summary>
    /// Movement points required to enter a hex of this terrain.
    /// Returns int.MaxValue for impassable terrain.
    /// </summary>
    public static int MovementCost(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Grass => 1,
            TerrainType.Plains => 1,
            TerrainType.Forest => 2,
            TerrainType.Water => int.MaxValue,
            _ => int.MaxValue,
        };
    }
}
