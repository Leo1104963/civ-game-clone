namespace CivGame.Combat;

public readonly record struct CombatResult(
    int AttackerHpAfter,
    int DefenderHpAfter,
    bool AttackerKilled,
    bool DefenderKilled);
