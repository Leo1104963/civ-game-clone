namespace CivGame.World;

/// <summary>
/// A single cell in the hex grid. Passability is derived from terrain via TerrainRules.
/// </summary>
public sealed class HexCell
{
    public HexCoord Coord { get; }
    public TerrainType Terrain { get; set; }

    /// <summary>True if the cell's terrain is passable. Derived from TerrainRules.</summary>
    public bool IsPassable => TerrainRules.IsPassable(Terrain);

    public HexCell(HexCoord coord, TerrainType terrain = TerrainType.Grass)
    {
        Coord = coord;
        Terrain = terrain;
    }
}
