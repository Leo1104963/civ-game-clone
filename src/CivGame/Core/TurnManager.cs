using CivGame.Cities;
using CivGame.Tech;
using CivGame.Units;
using CivGame.World;

namespace CivGame.Core;

/// <summary>
/// Orchestrates the end-of-turn sequence: tick production, tick research, reset movement, advance turn counter.
/// </summary>
public sealed class TurnManager
{
    private readonly UnitManager _unitManager;
    private readonly CityManager _cityManager;
    private readonly HexGrid? _grid;
    private readonly ResearchManager? _researchManager;
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
        _researchManager = null;
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
        _researchManager = null;
        PlayerOrder = playerOrder;
        _currentPlayerIndex = 0;
    }

    /// <summary>
    /// Overload that wires in a <see cref="ResearchManager"/> for science ticking each turn.
    /// Parameter order: (unitManager, cityManager, grid, researchManager, playerOrder).
    /// </summary>
    public TurnManager(
        UnitManager unitManager,
        CityManager cityManager,
        HexGrid grid,
        ResearchManager researchManager,
        IReadOnlyList<int> playerOrder)
    {
        _unitManager = unitManager ?? throw new ArgumentNullException(nameof(unitManager));
        _cityManager = cityManager ?? throw new ArgumentNullException(nameof(cityManager));
        if (playerOrder is null || playerOrder.Count == 0)
            throw new ArgumentException("playerOrder must be non-empty", nameof(playerOrder));
        _grid = grid;
        _researchManager = researchManager ?? throw new ArgumentNullException(nameof(researchManager));
        PlayerOrder = playerOrder;
        _currentPlayerIndex = 0;
    }

    /// <summary>
    /// Execute end-of-turn sequence for the current player:
    /// 1. Fire TurnEnding event
    /// 2. Tick production for CurrentPlayerId's cities
    /// 3. Tick research for CurrentPlayerId (if ResearchManager is wired)
    /// 4. Reset movement for CurrentPlayerId's units
    /// 5. Advance to next player; increment CurrentTurn only on full-cycle wrap
    /// 6. Fire TurnEnded event
    /// </summary>
    public void EndTurn()
    {
        TurnEnding?.Invoke(CurrentTurn);

        if (_grid is not null)
            _cityManager.TickProductionFor(CurrentPlayerId, _grid);
        else
            _cityManager.TickProductionFor(CurrentPlayerId);

        if (_researchManager is not null)
        {
            int scienceThisTurn = _grid is not null
                ? _cityManager.CalculateScienceFor(CurrentPlayerId, _grid)
                : 0;
            _researchManager.TickFor(CurrentPlayerId, scienceThisTurn);
        }

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
