namespace CivGame.Buildings;

/// <summary>
/// Static registry of all building definitions. v0 contains only Granary.
/// </summary>
public static class BuildingCatalog
{
    public static BuildingDefinition Granary { get; } = new("Granary", 5);

    private static readonly Dictionary<string, BuildingDefinition> _buildings = new(StringComparer.OrdinalIgnoreCase)
    {
        [Granary.Name] = Granary,
    };

    /// <summary>Look up a building definition by name (case-insensitive). Returns null if not found.</summary>
    public static BuildingDefinition? GetByName(string name) =>
        _buildings.TryGetValue(name, out var def) ? def : null;
}
