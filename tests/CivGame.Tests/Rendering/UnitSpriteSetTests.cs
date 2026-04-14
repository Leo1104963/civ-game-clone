namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for UnitSpriteSet.GetTexturePath (issue #90).
/// Verifies known unit types return documented paths,
/// unknown types return the fallback path (no throw).
/// </summary>
public class UnitSpriteSetTests
{
    [Fact]
    public void Should_ReturnWarriorPath_When_UnitTypeIsWarrior()
    {
        var path = UnitSpriteSet.GetTexturePath("Warrior");
        Assert.Equal("res://assets/units/warrior.png", path);
    }

    [Fact]
    public void Should_ReturnSettlerPath_When_UnitTypeIsSettler()
    {
        var path = UnitSpriteSet.GetTexturePath("Settler");
        Assert.Equal("res://assets/units/settler.png", path);
    }

    [Fact]
    public void Should_ReturnFallbackPath_When_UnitTypeIsUnknown()
    {
        var path = UnitSpriteSet.GetTexturePath("Catapult");
        Assert.Equal("res://assets/units/fallback.png", path);
    }

    [Fact]
    public void Should_ReturnFallbackPath_When_UnitTypeIsEmpty()
    {
        var path = UnitSpriteSet.GetTexturePath(string.Empty);
        Assert.Equal("res://assets/units/fallback.png", path);
    }

    [Fact]
    public void Should_NotThrow_When_UnitTypeIsNull()
    {
        // Null should map to fallback, not throw
        var path = UnitSpriteSet.GetTexturePath(null!);
        Assert.Equal("res://assets/units/fallback.png", path);
    }

    [Fact]
    public void Should_BeCaseInsensitive_When_UnitTypeUsesLowerCase()
    {
        // The spec notes ToLowerInvariant() in the implementation
        var path = UnitSpriteSet.GetTexturePath("warrior");
        Assert.Equal("res://assets/units/warrior.png", path);
    }
}
