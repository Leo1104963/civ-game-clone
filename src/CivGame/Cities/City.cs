using CivGame.Buildings;
using CivGame.Tech;
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
    /// Directly add a completed building to this city. Intended for testing and save-load.
    /// Throws if building is null. Does not validate for duplicates (use HasBuilding to check).
    /// </summary>
    public void AddCompletedBuilding(BuildingDefinition building)
    {
        if (building is null) throw new ArgumentNullException(nameof(building));
        _completedBuildings.Add(building);
    }

    /// <summary>
    /// Start building. Returns false if already building something or building already completed.
    /// Tech gating is not checked; use TryStartBuilding for tech-aware production.
    /// </summary>
    public bool StartBuilding(BuildingDefinition building) =>
        TryStartBuilding(building, null, 0).Success;

    /// <summary>
    /// Attempt to start building with optional tech-unlock enforcement.
    /// Check order: (1) already producing, (2) already built, (3) tech gate.
    /// When <paramref name="unlocks"/> is null, the tech gate is skipped.
    /// LockedReason values: "Already producing", "Already built", "Requires {TechName}".
    /// </summary>
    public BuildResult TryStartBuilding(
        BuildingDefinition building,
        TechUnlockService? unlocks,
        int playerId)
    {
        if (building is null) throw new ArgumentNullException(nameof(building));

        if (CurrentProduction is not null)
            return new BuildResult(false, "Already producing");

        if (_completedBuildings.Any(b =>
                string.Equals(b.Name, building.Name, StringComparison.OrdinalIgnoreCase)))
            return new BuildResult(false, "Already built");

        if (unlocks is not null)
        {
            var tag = $"building:{building.Name}";
            if (!unlocks.IsUnlocked(playerId, tag))
            {
                var techName = unlocks.GatingTechName(tag) ?? "unknown tech";
                return new BuildResult(false, $"Requires {techName}");
            }
        }

        CurrentProduction = new BuildQueueItem(building);
        return new BuildResult(true, null);
    }

    /// <summary>
    /// Tick production: reduce current build item cost by 1.
    /// If complete, move to completed buildings list and clear current production.
    /// </summary>
    public void TickProduction() => TickProduction(1);

    /// <summary>
    /// Apply <paramref name="productionYield"/> production points this turn.
    /// Each point reduces remaining cost by 1. If the building completes mid-loop,
    /// any remaining production is discarded (carry-over is out of scope).
    /// </summary>
    public void TickProduction(int productionYield)
    {
        if (CurrentProduction is null) return;
        if (productionYield <= 0) return;

        for (int i = 0; i < productionYield; i++)
        {
            CurrentProduction.Tick();
            if (CurrentProduction.IsComplete)
            {
                _completedBuildings.Add(CurrentProduction.Definition);
                CurrentProduction = null;
                return;
            }
        }
    }
}
