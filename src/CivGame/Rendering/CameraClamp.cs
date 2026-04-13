namespace CivGame.Rendering;

/// <summary>
/// Pure helper for clamping camera position to map bounds.
/// </summary>
public static class CameraClamp
{
    public static (float X, float Y) ClampPosition(
        (float X, float Y) position,
        (float MinX, float MinY, float MaxX, float MaxY) bounds)
    {
        if (bounds.MinX > bounds.MaxX || bounds.MinY > bounds.MaxY)
            throw new ArgumentException("bounds min values must be <= max values.");

        return (
            Math.Clamp(position.X, bounds.MinX, bounds.MaxX),
            Math.Clamp(position.Y, bounds.MinY, bounds.MaxY)
        );
    }
}
