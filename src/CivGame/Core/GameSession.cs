using CivGame.Cities;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Core;

/// <summary>
/// Top-level game state container. Holds grid, managers, and turn state.
/// </summary>
public sealed class GameSession
{
    public HexGrid Grid { get; }
    public UnitManager Units { get; }
    public CityManager Cities { get; }
    public TurnManager Turns { get; }

    /// <summary>
    /// Create a fully custom game session (for testing or advanced setup).
    /// </summary>
    public GameSession(HexGrid grid, UnitManager units, CityManager cities, TurnManager turns)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        Units = units ?? throw new ArgumentNullException(nameof(units));
        Cities = cities ?? throw new ArgumentNullException(nameof(cities));
        Turns = turns ?? throw new ArgumentNullException(nameof(turns));
    }

    /// <summary>
    /// Create a new game session with default setup:
    /// - Grid of the given size
    /// - One city ("Capital") at the center of the grid
    /// - One unit ("Warrior") adjacent to the city
    /// </summary>
    public GameSession(int gridWidth, int gridHeight)
    {
        if (gridWidth <= 0) throw new ArgumentOutOfRangeException(nameof(gridWidth));
        if (gridHeight <= 0) throw new ArgumentOutOfRangeException(nameof(gridHeight));

        Grid = new HexGrid(gridWidth, gridHeight);
        Units = new UnitManager();
        Cities = new CityManager();
        Turns = new TurnManager(Units, Cities);

        // Place city at grid center
        var cityCoord = new HexCoord(gridWidth / 2, gridHeight / 2);
        Cities.CreateCity("Capital", cityCoord, Grid);

        // Place warrior adjacent to city
        var neighbors = Grid.GetNeighbors(cityCoord);
        if (neighbors.Count > 0)
        {
            Units.CreateUnit("Warrior", neighbors[0].Coord, Grid);
        }
    }
}
