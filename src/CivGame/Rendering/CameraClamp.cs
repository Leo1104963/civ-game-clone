namespace CivGame.Rendering;

/// <summary>
/// Pure helper for clamping camera position to bounds. Stub — full implementation in #92.
/// </summary>
public static class CameraClamp
{
    public static (float X, float Y) ClampPosition(
        (float X, float Y) position,
        (float MinX, float MinY, float MaxX, float MaxY) bounds)
    {
        throw new NotImplementedException("CameraClamp not yet implemented — see issue #92.");
    }
}
