namespace CivGame.Tests;

public class GameInfoTests
{
    [Fact]
    public void Should_HaveProjectName()
    {
        Assert.Equal("CivGame", GameInfo.Name);
    }

    [Fact]
    public void Should_HaveVersion()
    {
        Assert.False(string.IsNullOrEmpty(GameInfo.Version));
    }
}
