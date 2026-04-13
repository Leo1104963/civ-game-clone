using CivGame.Ai;
using CivGame.Cities;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Core;

/// <summary>
/// Top-level game state container. Holds grid, managers, and turn state.
/// </summary>
public sealed class GameSession
{
    public const int DefaultSeed = 12345;

    public HexGrid Grid { get; }
    public UnitManager Units { get; }
    public CityManager Cities { get; }
    public TurnManager Turns { get; }
    public VisibilityMap Visibility { get; }

    /// <summary>
    /// Create a fully custom game session (for testing or advanced setup).
    /// Visibility is constructed but NOT auto-recomputed; caller owns setup.
    /// </summary>
    public GameSession(HexGrid grid, UnitManager units, CityManager cities, TurnManager turns)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        Units = units ?? throw new ArgumentNullException(nameof(units));
        Cities = cities ?? throw new ArgumentNullException(nameof(cities));
        Turns = turns ?? throw new ArgumentNullException(nameof(turns));
        Visibility = new VisibilityMap(grid);
        Turns.TurnEnded += _ => Visibility.RecomputeForPlayer(Turns.CurrentPlayerId, Units, Cities);
    }

    /// <summary>
    /// Create a new game session with v2 default setup:
    /// - Grid of the given size populated by MapGenerator.Generate(..., seed)
    /// - One city ("Capital") at (width/2, height/2) (terrain is guaranteed Grass)
    /// - One Warrior on a passable neighbor of the capital
    /// - One Settler on a different passable neighbor of the capital
    /// - PlayerOrder [0, 1]; barbarian AI runs on player 1's TurnEnding,
    ///   barbarian spawning runs on TurnEnded when CurrentPlayerId advances to 0.
    /// </summary>
    public GameSession(int gridWidth, int gridHeight, int seed = DefaultSeed)
    {
        if (gridWidth <= 0) throw new ArgumentOutOfRangeException(nameof(gridWidth));
        if (gridHeight <= 0) throw new ArgumentOutOfRangeException(nameof(gridHeight));

        Grid = MapGenerator.Generate(gridWidth, gridHeight, seed);
        Units = new UnitManager();
        Cities = new CityManager();
        Turns = new TurnManager(Units, Cities, Grid, new[] { 0, 1 });

        var barbarianRng = new Random(seed ^ 0x5A5A5A5A);

        // Place city at grid center (MapGenerator guarantees this is Grass).
        var cityCoord = new HexCoord(gridWidth / 2, gridHeight / 2);
        Cities.CreateCity("Capital", cityCoord, Grid, ownerId: 0);

        // Place Warrior and Settler on two different passable neighbors.
        var passableNeighbors = new List<HexCoord>();
        foreach (var neighbor in Grid.GetNeighbors(cityCoord))
        {
            if (neighbor.IsPassable)
            {
                passableNeighbors.Add(neighbor.Coord);
            }
        }

        if (passableNeighbors.Count >= 1)
        {
            Units.CreateUnit("Warrior", passableNeighbors[0], Grid, ownerId: 0);
        }

        if (passableNeighbors.Count >= 2)
        {
            Units.CreateUnit("Settler", passableNeighbors[1], Grid, ownerId: 0);
        }

        Visibility = new VisibilityMap(Grid);

        // Run barbarian AI before player 1's turn ticks (guard on CurrentPlayerId == 1).
        Turns.TurnEnding += _ =>
        {
            if (Turns.CurrentPlayerId == 1)
                BarbarianAi.TakeTurn(Grid, Units, Cities);
        };

        // Recompute visibility and possibly spawn a barbarian after each turn ends.
        Turns.TurnEnded += _ =>
        {
            Visibility.RecomputeForPlayer(Turns.CurrentPlayerId, Units, Cities);

            // Spawn at the start of each new game turn (when CurrentPlayerId == 0).
            if (Turns.CurrentPlayerId == 0)
                BarbarianSpawner.TrySpawn(Turns.CurrentTurn, Grid, Units, Cities, barbarianRng);
        };

        // Initial recompute for player 0 after all units and cities are placed.
        Visibility.RecomputeForPlayer(0, Units, Cities);
    }
}
