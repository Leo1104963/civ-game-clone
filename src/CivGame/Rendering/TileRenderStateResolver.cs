using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Pure static helper that maps a VisibilityState to a TileRenderState.
/// No Godot dependency — safe for headless unit tests.
/// </summary>
public static class TileRenderStateResolver
{
    public const float DefaultDimFactor = FogOfWarConstants.DefaultDimFactor;

    public static TileRenderState Resolve(VisibilityState state, bool fogEnabled = true)
    {
        if (!fogEnabled)
            return TileRenderState.Full;

        return state switch
        {
            VisibilityState.Visible  => TileRenderState.Full,
            VisibilityState.Explored => TileRenderState.Dim,
            _                        => TileRenderState.Hidden,
        };
    }
}
