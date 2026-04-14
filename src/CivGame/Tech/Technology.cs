namespace CivGame.Tech;

/// <summary>
/// Immutable data record for a single technology in the tech tree.
/// Plain C# — no Godot types.
/// </summary>
public sealed class Technology
{
    public string Id { get; }
    public string Name { get; }
    public int ScienceCost { get; }
    public IReadOnlyList<string> Prerequisites { get; }
    public IReadOnlyList<string> Unlocks { get; }

    public Technology(
        string id,
        string name,
        int scienceCost,
        IReadOnlyList<string>? prerequisites = null,
        IReadOnlyList<string>? unlocks = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or whitespace.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        if (scienceCost <= 0)
            throw new ArgumentOutOfRangeException(nameof(scienceCost), "ScienceCost must be positive.");

        Id = id;
        Name = name;
        ScienceCost = scienceCost;
        Prerequisites = prerequisites ?? Array.Empty<string>();
        Unlocks = unlocks ?? Array.Empty<string>();
    }
}
