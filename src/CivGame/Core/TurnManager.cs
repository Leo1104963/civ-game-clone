using CivGame.Cities;
using CivGame.Units;

namespace CivGame.Core;

/// <summary>
/// Orchestrates the end-of-turn sequence: tick production, reset movement, advance turn counter.
/// </summary>
public sealed class TurnManager
{
    private readonly UnitManager _unitManager;
    private readonly CityManager _cityManager;

    public int CurrentTurn { get; private set; } = 1;

    /// <summary>Fired before end-of-turn processing begins. Parameter is the current (ending) turn number.</summary>
    public event Action<int>? TurnEnding;

    /// <summary>Fired after all end-of-turn processing. Parameter is the new turn number.</summary>
    public event Action<int>? TurnEnded;

    public TurnManager(UnitManager unitManager, CityManager cityManager)
    {
        _unitManager = unitManager ?? throw new ArgumentNullException(nameof(unitManager));
        _cityManager = cityManager ?? throw new ArgumentNullException(nameof(cityManager));
    }

    /// <summary>
    /// Execute end-of-turn sequence:
    /// 1. Fire TurnEnding event
    /// 2. Tick all city build queues
    /// 3. Reset all unit movement
    /// 4. Advance turn counter
    /// 5. Fire TurnEnded event
    /// </summary>
    public void EndTurn()
    {
        TurnEnding?.Invoke(CurrentTurn);

        _cityManager.TickAllProduction();
        _unitManager.ResetAllMovement();

        CurrentTurn++;

        TurnEnded?.Invoke(CurrentTurn);
    }
}
