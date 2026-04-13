using CivGame.Cities;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Core;

/// <summary>
/// Orchestrates the end-of-turn sequence: tick production, reset movement, advance turn counter.
/// </summary>
public sealed class TurnManager
{
    private readonly UnitManager _unitManager;
    private readonly CityManager _cityManager;
    private readonly HexGrid? _grid;
    private int _currentPlayerIndex;

    public int CurrentTurn { get; private set; } = 1;
    public IReadOnlyList<int> PlayerOrder { get; }
    public int CurrentPlayerId => PlayerOrder[_currentPlayerIndex];

    /// <summary>Fired before end-of-turn processing begins. Parameter is the current (ending) turn number.</summary>
    public event Action<int>? TurnEnding;

    /// <summary>Fired after all end-of-turn processing. Parameter is the new (or current) turn number.</summary>
    public event Action<int>? TurnEnded;

    public TurnManager(UnitManager unitManager, CityManager cityManager)
        : this(unitManager, cityManager, new[] { 0 }) { }

    public TurnManager(UnitManager unitManager, CityManager cityManager, IReadOnlyList<int> playerOrder)
    {
        _unitManager = unitManager ?? throw new ArgumentNullException(nameof(unitManager));
        _cityManager = cityManager ?? throw new ArgumentNullException(nameof(cityManager));
        if (playerOrder is null || playerOrder.Count == 0)
            throw new ArgumentException("playerOrder must be non-empty", nameof(playerOrder));
        _grid = null;
        PlayerOrder = playerOrder;
        _currentPlayerIndex = 0;
    }

    public TurnManager(UnitManager unitManager, CityManager cityManager, HexGrid grid)
        : this(unitManager, cityManager, grid, new[] { 0 }) { }

    public TurnManager(UnitManager unitManager, CityManager cityManager, HexGrid grid, IReadOnlyList<int> playerOrder)
    {
        _unitManager = unitManager ?? throw new ArgumentNullException(nameof(unitManager));
        _cityManager = cityManager ?? throw new ArgumentNullException(nameof(cityManager));
        if (playerOrder is null || playerOrder.Count == 0)
            throw new ArgumentException("playerOrder must be non-empty", nameof(playerOrder));
        _grid = grid;
        PlayerOrder = playerOrder;
        _currentPlayerIndex = 0;
    }

    /// <summary>
    /// Execute end-of-turn sequence for the current player:
    /// 1. Fire TurnEnding event
    /// 2. Tick production for CurrentPlayerId's cities
    /// 3. Reset movement for CurrentPlayerId's units
    /// 4. Advance to next player; increment CurrentTurn only on full-cycle wrap
    /// 5. Fire TurnEnded event
    /// </summary>
    public void EndTurn()
    {
        TurnEnding?.Invoke(CurrentTurn);

        if (_grid is not null)
            _cityManager.TickProductionFor(CurrentPlayerId, _grid);
        else
            _cityManager.TickProductionFor(CurrentPlayerId);

        _unitManager.ResetMovementFor(CurrentPlayerId);

        _currentPlayerIndex++;
        if (_currentPlayerIndex >= PlayerOrder.Count)
        {
            _currentPlayerIndex = 0;
            CurrentTurn++;
        }

        TurnEnded?.Invoke(CurrentTurn);
    }
}
