using System;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Damage and Defense scaling from turn-limited statuses (knobs in <see cref="CombatConfig"/>).
    /// Weaken / Focus change the holder's outgoing damage; Vulnerability changes incoming damage;
    /// Harden / Expose add / remove Defense (in units of the DR constant K, so they matter at every level and
    /// Expose can push Defense below zero: the holder then takes extra damage).
    /// Stacks only count while their turns remain and are capped by <see cref="CombatConfig.StatusMaxStacks"/>.
    /// </summary>
    public static class StatusCombatModifiers
    {
        private static int ActiveStacks(int? stacks, int turns)
        {
            if (turns <= 0 || (stacks ?? 0) <= 0)
                return 0;
            int cap = GameConfiguration.Instance.Combat.StatusMaxStacks;
            return cap > 0 ? Math.Min(stacks!.Value, cap) : stacks!.Value;
        }

        /// <summary>Multiplier on damage this actor deals (Weaken lowers it, Focus raises it).</summary>
        public static double OutgoingDamageMultiplier(Actor? attacker)
        {
            if (attacker == null)
                return 1.0;
            var combat = GameConfiguration.Instance.Combat;
            double multiplier = 1.0;
            if (attacker.IsWeakened && attacker.WeakenTurns > 0)
                multiplier *= Math.Clamp(combat.StatusWeakenDamageMultiplier, 0.0, 1.0);
            multiplier *= 1.0 + ActiveStacks(attacker.FocusStacks, attacker.FocusTurns) * combat.StatusFocusDamagePerStack;
            return multiplier;
        }

        /// <summary>Multiplier on damage this actor takes (Vulnerability).</summary>
        public static double IncomingDamageMultiplier(Actor? target)
        {
            if (target == null)
                return 1.0;
            var combat = GameConfiguration.Instance.Combat;
            return 1.0 + ActiveStacks(target.VulnerabilityStacks, target.VulnerabilityTurns) * combat.StatusVulnerabilityDamagePerStack;
        }

        /// <summary>Defense added by Harden (and Fortify, when <see cref="CombatConfig.StatusFortifyDefensePerStack"/> is set) minus Defense removed by Expose, given the DR constant <paramref name="k"/>.</summary>
        public static double DefenseBonus(Actor? defender, double k)
        {
            if (defender == null)
                return 0.0;
            var combat = GameConfiguration.Instance.Combat;
            return k * (ActiveStacks(defender.HardenStacks, defender.HardenTurns) * combat.StatusHardenDefensePerStack
                        + ActiveStacks(defender.FortifyStacks, defender.FortifyTurns) * combat.StatusFortifyDefensePerStack
                        - ActiveStacks(defender.ExposeStacks, defender.ExposeTurns) * combat.StatusExposeDefensePerStack);
        }
    }
}
