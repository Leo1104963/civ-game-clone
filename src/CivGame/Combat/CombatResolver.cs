using CivGame.Units;
using CivGame.World;

namespace CivGame.Combat;

public static class CombatResolver
{
    private const double ForestDefenderMultiplier = 1.25;
    private const int BaseDamage = 5;

    /// <summary>
    /// Deterministic melee resolution. Forest defender receives +25% effective strength.
    /// Formula:
    ///   effectiveAttackerStr = attacker.CombatStrength
    ///   effectiveDefenderStr = defender.CombatStrength * (defenderTerrain == Forest ? 1.25 : 1.0)
    ///   attackerDamage = max(1, round(5 * effectiveDefenderStr / effectiveAttackerStr))
    ///   defenderDamage = max(1, round(5 * effectiveAttackerStr / effectiveDefenderStr))
    /// Damage caps at the receiver's current Hp.
    /// If a defender has CombatStrength == 0 (e.g. Settler), defenderDamage = defender.Hp (one-shot),
    /// attackerDamage = 0.
    /// </summary>
    public static CombatResult Resolve(Unit attacker, Unit defender, TerrainType defenderTerrain)
    {
        if (attacker is null) throw new ArgumentNullException(nameof(attacker));
        if (defender is null) throw new ArgumentNullException(nameof(defender));

        double attackerStr = attacker.CombatStrength;
        double defenderStr = defender.CombatStrength;
        if (defenderTerrain == TerrainType.Forest && defenderStr > 0)
        {
            defenderStr *= ForestDefenderMultiplier;
        }

        int attackerDamage;
        int defenderDamage;
        if (defenderStr <= 0)
        {
            // Non-combat defender (e.g., Settler): one-shot kill, attacker untouched.
            defenderDamage = defender.Hp;
            attackerDamage = 0;
        }
        else if (attackerStr <= 0)
        {
            // Non-combat attacker cannot meaningfully attack: no damage either way.
            attackerDamage = 0;
            defenderDamage = 0;
        }
        else
        {
            attackerDamage = Math.Max(1, (int)Math.Round(BaseDamage * defenderStr / attackerStr));
            defenderDamage = Math.Max(1, (int)Math.Round(BaseDamage * attackerStr / defenderStr));
        }

        int attackerHpAfter = Math.Max(0, attacker.Hp - attackerDamage);
        int defenderHpAfter = Math.Max(0, defender.Hp - defenderDamage);

        return new CombatResult(
            attackerHpAfter,
            defenderHpAfter,
            AttackerKilled: attackerHpAfter <= 0,
            DefenderKilled: defenderHpAfter <= 0);
    }
}
