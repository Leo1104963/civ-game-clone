using CivGame.World;

namespace CivGame.Cities;

public static class TerrainYields
{
    public static YieldResult Of(TerrainType terrain) => terrain switch
    {
        TerrainType.Grass => new YieldResult(Food: 2, Production: 0, Science: 0),
        TerrainType.Plains => new YieldResult(Food: 1, Production: 1, Science: 1),
        TerrainType.Forest => new YieldResult(Food: 0, Production: 2, Science: 0),
        TerrainType.Water => new YieldResult(Food: 1, Production: 0, Science: 0),
        _ => new YieldResult(0, 0, 0),
    };
}
