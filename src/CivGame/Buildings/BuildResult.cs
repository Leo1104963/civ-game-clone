namespace CivGame.Buildings;

/// <summary>
/// Result of a City.TryStartBuilding call. Success == true means the building was queued.
/// LockedReason is non-null when Success == false and describes why.
/// </summary>
public readonly record struct BuildResult(bool Success, string? LockedReason);
