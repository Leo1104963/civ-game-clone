namespace CivGame.Buildings;

/// <summary>
/// An item currently being produced in a city's build queue.
/// </summary>
public sealed class BuildQueueItem
{
    public BuildingDefinition Definition { get; }
    public int TurnsRemaining { get; private set; }
    public bool IsComplete => TurnsRemaining <= 0;

    public BuildQueueItem(BuildingDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        TurnsRemaining = definition.BuildCost;
    }

    /// <summary>Reduce remaining turns by 1. Does nothing if already complete.</summary>
    public void Tick()
    {
        if (!IsComplete)
        {
            TurnsRemaining--;
        }
    }
}
