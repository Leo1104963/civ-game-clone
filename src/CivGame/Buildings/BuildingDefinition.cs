namespace CivGame.Buildings;

/// <summary>
/// Immutable definition of a building type. Defines name and build cost.
/// </summary>
public sealed class BuildingDefinition
{
    public string Name { get; }
    public int BuildCost { get; }

    public BuildingDefinition(string name, int buildCost)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (buildCost <= 0)
            throw new ArgumentOutOfRangeException(nameof(buildCost), "Build cost must be positive.");

        Name = name;
        BuildCost = buildCost;
    }
}
