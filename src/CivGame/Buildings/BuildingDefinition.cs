namespace CivGame.Buildings;

/// <summary>
/// Immutable definition of a building type. Defines name, build cost, and optional science yield.
/// </summary>
public sealed class BuildingDefinition
{
    public string Name { get; }
    public int BuildCost { get; }
    public int ScienceYield { get; }

    public BuildingDefinition(string name, int buildCost, int scienceYield = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (buildCost <= 0)
            throw new ArgumentOutOfRangeException(nameof(buildCost), "Build cost must be positive.");
        if (scienceYield < 0)
            throw new ArgumentOutOfRangeException(nameof(scienceYield), "ScienceYield cannot be negative.");

        Name = name;
        BuildCost = buildCost;
        ScienceYield = scienceYield;
    }
}
