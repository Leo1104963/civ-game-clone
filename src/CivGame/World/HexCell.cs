namespace CivGame.World;

/// <summary>
/// A single cell in the hex grid. Holds terrain and passability.
/// </summary>
public sealed class HexCell
{
    public HexCoord Coord { get; }
    public TerrainType Terrain { get; set; }
    public bool IsPassable { get; set; }

    public HexCell(HexCoord coord, TerrainType terrain = TerrainType.Grass, bool isPassable = true)
    {
        Coord = coord;
        Terrain = terrain;
        IsPassable = isPassable;
    }
}
