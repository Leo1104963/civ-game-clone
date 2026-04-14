namespace CivGame.Units;

/// <summary>
/// Result of a UnitManager.TrySpawnUnit call. Unit is non-null on success.
/// LockedReason is non-null when Unit == null due to a tech gate.
/// Invalid input (bad position, unknown unit type) throws rather than returning a result.
/// </summary>
public readonly record struct UnitSpawnResult(Unit? Unit, string? LockedReason);
