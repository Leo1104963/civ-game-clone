using CivGame.Core;
using CivGame.Rendering;
using CivGame.Tech;

namespace CivGame.Rendering.Tests;

/// <summary>
/// Reflection-only tests for TurnHud research additions per issue #101.
/// Matches the existing TurnHudCityInfoBootstrapTests.cs pattern — no scene graph instantiation.
/// Tests compile but fail until the new TurnHud overload and signal are implemented.
/// </summary>
public class TurnHudResearchTests
{
    // ------------------------------------------------------------------ //
    // Regression — existing Initialize(TurnManager) must still exist      //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_StillHaveExistingInitializeMethod_When_TurnHudReferenced()
    {
        // Regression: the new overload must NOT have removed the existing single-arg one.
        var method = typeof(TurnHud).GetMethod(
            "Initialize",
            new[] { typeof(TurnManager) });

        Assert.NotNull(method);
    }

    // ------------------------------------------------------------------ //
    // New Initialize overload — exact parameter types                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveNewInitializeOverload_When_TurnHudReferenced()
    {
        var method = typeof(TurnHud).GetMethod(
            "Initialize",
            new[] { typeof(TurnManager), typeof(ResearchManager), typeof(Func<int>), typeof(int) });

        Assert.NotNull(method);
    }

    // ------------------------------------------------------------------ //
    // RefreshScience method                                               //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveRefreshScienceMethod_When_TurnHudReferenced()
    {
        var method = typeof(TurnHud).GetMethod("RefreshScience", Type.EmptyTypes);

        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ------------------------------------------------------------------ //
    // ResearchPressed signal delegate                                     //
    // ------------------------------------------------------------------ //

    [Fact]
    public void Should_HaveResearchPressedEventHandlerDelegate_When_TurnHudReferenced()
    {
        var delegateType = typeof(TurnHud).GetNestedType("ResearchPressedEventHandler");

        Assert.NotNull(delegateType);
    }

    [Fact]
    public void Should_HaveResearchPressedEventHandlerWithNoParams_When_TurnHudReferenced()
    {
        var delegateType = typeof(TurnHud).GetNestedType("ResearchPressedEventHandler");
        Assert.NotNull(delegateType);

        var invokeMethod = delegateType!.GetMethod("Invoke");
        Assert.NotNull(invokeMethod);

        Assert.Empty(invokeMethod!.GetParameters());
    }
}
