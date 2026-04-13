namespace CivGame.Tech;

/// <summary>
/// Static registry of all v4-starter technologies. Plain C# — no Godot types.
/// </summary>
public static class TechCatalog
{
    // ------------------------------------------------------------------ //
    // Individual tech properties                                          //
    // ------------------------------------------------------------------ //

    public static Technology Pottery { get; } = new(
        "pottery", "Pottery", 40, unlocks: new[] { "Granary" });

    public static Technology BronzeWorking { get; } = new(
        "bronze-working", "Bronze Working", 40, unlocks: new[] { "Spearman" });

    public static Technology AnimalHusbandry { get; } = new(
        "animal-husbandry", "Animal Husbandry", 40, unlocks: new[] { "Scout" });

    public static Technology Masonry { get; } = new(
        "masonry", "Masonry", 40, unlocks: new[] { "Walls" });

    public static Technology Currency { get; } = new(
        "currency", "Currency", 60,
        prerequisites: new[] { "bronze-working" },
        unlocks: new[] { "Market" });

    public static Technology Sailing { get; } = new(
        "sailing", "Sailing", 40, unlocks: new[] { "Galley" });

    public static Technology Mathematics { get; } = new(
        "mathematics", "Mathematics", 80,
        prerequisites: new[] { "currency", "masonry" },
        unlocks: new[] { "Catapult" });

    // ------------------------------------------------------------------ //
    // AllTechs                                                            //
    // ------------------------------------------------------------------ //

    public static IReadOnlyList<Technology> AllTechs { get; } = new[]
    {
        Pottery,
        BronzeWorking,
        AnimalHusbandry,
        Masonry,
        Currency,
        Sailing,
        Mathematics,
    };

    // ------------------------------------------------------------------ //
    // Lookup                                                              //
    // ------------------------------------------------------------------ //

    private static readonly Dictionary<string, Technology> _byId =
        new(StringComparer.OrdinalIgnoreCase);

    static TechCatalog()
    {
        foreach (var tech in AllTechs)
        {
            _byId[tech.Id] = tech;
        }
    }

    /// <summary>Case-insensitive lookup by id. Returns null if not found.</summary>
    public static Technology? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _byId.TryGetValue(id, out var tech) ? tech : null;
    }

    // ------------------------------------------------------------------ //
    // Validate                                                            //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Validate the hardcoded catalog. Returns a list of error messages.
    /// An empty list means the catalog is self-consistent.
    /// </summary>
    public static IReadOnlyList<string> Validate() => Validate(AllTechs);

    /// <summary>
    /// Validate an arbitrary collection of technologies against itself.
    /// Detects: self-prerequisites, dangling prerequisite ids, and cycles.
    /// </summary>
    public static IReadOnlyList<string> Validate(IEnumerable<Technology> techs)
    {
        var techList = techs.ToList();
        var knownIds = new HashSet<string>(techList.Select(t => t.Id), StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        foreach (var tech in techList)
        {
            foreach (var prereqId in tech.Prerequisites)
            {
                // Self-prerequisite check
                if (string.Equals(prereqId, tech.Id, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Tech '{tech.Id}' lists itself as a prerequisite.");
                    continue;
                }

                // Dangling reference check
                if (!knownIds.Contains(prereqId))
                {
                    errors.Add(
                        $"Tech '{tech.Id}' has prerequisite '{prereqId}' which does not exist in the catalog.");
                }
            }
        }

        // Cycle detection via DFS
        var idMap = techList.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tech in techList)
        {
            if (!visited.Contains(tech.Id))
            {
                DetectCycle(tech.Id, idMap, visited, inStack, errors);
            }
        }

        return errors.AsReadOnly();
    }

    private static void DetectCycle(
        string id,
        Dictionary<string, Technology> idMap,
        HashSet<string> visited,
        HashSet<string> inStack,
        List<string> errors)
    {
        visited.Add(id);
        inStack.Add(id);

        if (idMap.TryGetValue(id, out var tech))
        {
            foreach (var prereqId in tech.Prerequisites)
            {
                if (!idMap.ContainsKey(prereqId)) continue; // already reported as dangling

                if (!visited.Contains(prereqId))
                {
                    DetectCycle(prereqId, idMap, visited, inStack, errors);
                }
                else if (inStack.Contains(prereqId))
                {
                    errors.Add($"Cycle detected involving tech '{id}' and '{prereqId}'.");
                }
            }
        }

        inStack.Remove(id);
    }
}
