namespace CivGame.Rendering;

/// <summary>
/// Pure helper for clamping camera zoom level.
/// </summary>
public static class CameraZoom
{
    public static float ClampZoom(float zoom, float min, float max)
    {
        if (min > max)
            throw new ArgumentException($"min ({min}) must be <= max ({max}).");

        return Math.Clamp(zoom, min, max);
    }
}
