using CivGame.Buildings;
using CivGame.World;

namespace CivGame.Cities;

/// <summary>
/// A city on the hex grid. Has a name, position, build queue, and completed buildings.
/// </summary>
public sealed class City
{
    private static int _nextId;
    private readonly List<BuildingDefinition> _completedBuildings = new();

    public int Id { get; }
    public string Name { get; }
    public HexCoord Position { get; }
    public int OwnerId { get; }
    public BuildQueueItem? CurrentProduction { get; private set; }
    public IReadOnlyList<BuildingDefinition> CompletedBuildings => _completedBuildings.AsReadOnly();

    public City(string name, HexCoord position, int ownerId = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("City name cannot be empty.", nameof(name));

        Id = Interlocked.Increment(ref _nextId);
        Name = name;
        Position = position;
        OwnerId = ownerId;
    }

    /// <summary>True if the building is already completed or currently in production.</summary>
    public bool HasBuilding(string name)
    {
        if (CurrentProduction is not null &&
            string.Equals(CurrentProduction.Definition.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _completedBuildings.Any(b =>
            string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Start building. Returns false if already building something or building already completed.
    /// </summary>
    public bool StartBuilding(BuildingDefinition building)
    {
        if (building is null) throw new ArgumentNullException(nameof(building));
        if (CurrentProduction is not null) return false;
        if (HasBuilding(building.Name)) return false;

        CurrentProduction = new BuildQueueItem(building);
        return true;
    }

    /// <summary>
    /// Tick production: reduce current build item cost by 1.
    /// If complete, move to completed buildings list and clear current production.
    /// </summary>
    public void TickProduction()
    {
        if (CurrentProduction is null) return;

        CurrentProduction.Tick();

        if (CurrentProduction.IsComplete)
        {
            _completedBuildings.Add(CurrentProduction.Definition);
            CurrentProduction = null;
        }
    }
}
