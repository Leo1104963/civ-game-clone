namespace CivGame.Rendering;

/// <summary>
/// Returns res:// asset paths for unit sprite textures.
/// Unknown or null unit types return the documented fallback path without throwing.
/// </summary>
public static class UnitSpriteSet
{
    private const string FallbackPath = "res://assets/units/fallback.png";

    public static string GetTexturePath(string? unitType)
    {
        return unitType?.ToLowerInvariant() switch
        {
            "warrior" => "res://assets/units/warrior.png",
            "settler" => "res://assets/units/settler.png",
            _ => FallbackPath,
        };
    }
}
