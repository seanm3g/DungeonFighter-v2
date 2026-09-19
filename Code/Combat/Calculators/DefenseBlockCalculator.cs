using System;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Hero-only defense: unforced 1d20 vs the attack face. Margin converts armor into per-swing block
    /// with a 75% floor and 150% ceiling. Enemies keep 100% armor. Pierce and non-hero targets do not roll.
    /// </summary>
    public static class DefenseBlockCalculator
    {
        /// <summary>d20 mean is 10.5; used when a defense total exists but the attack has no d20 face (env hazards).</summary>
        public const int NeutralAttackFace = 11;

        public static bool IsHeroDefender(Actor? target) =>
            target is Character character && character is not Enemy;

        /// <summary>
        /// True when a successful hit against this target should roll an unforced 1d20 defense.
        /// </summary>
        public static bool ShouldRoll(Actor? target, Action? action) =>
            IsHeroDefender(target) && !DamageCalculator.IgnoresArmor(target, action);

        /// <summary>
        /// Attack face for the contest: missing or non-positive values use <see cref="NeutralAttackFace"/>.
        /// </summary>
        public static int ResolveAttackFace(int? attackFace)
        {
            if (!attackFace.HasValue || attackFace.Value <= 0)
                return NeutralAttackFace;
            return attackFace.Value;
        }

        /// <summary>Classic opposed: attack face minus defense face.</summary>
        public static int GetMargin(int attackFace, int defenseTotal) =>
            ResolveAttackFace(attackFace) - defenseTotal;

        /// <summary>
        /// Armor-to-block multiplier from opposed margin. Attack-ahead is the 75% floor;
        /// defense-ahead is 150%. Close contests stay at 100%.
        /// </summary>
        public static double GetMultiplierFromMargin(int margin)
        {
            if (margin >= 4)
                return 0.75;
            if (margin >= -3)
                return 1.0;
            return 1.5;
        }

        /// <summary>Armor-to-block multiplier for an attack face vs a 1d20 defense face.</summary>
        public static double GetMultiplier(int attackFace, int defenseTotal) =>
            GetMultiplierFromMargin(GetMargin(attackFace, defenseTotal));

        /// <summary>Signed margin for combat log (<c>+5</c>, <c>0</c>, <c>-3</c>).</summary>
        public static string FormatMarginSigned(int margin) =>
            margin > 0 ? $"+{margin}" : margin.ToString();

        /// <summary>
        /// Flat block for this swing: round(armor × band) away from zero, floored at 0.
        /// </summary>
        public static int ComputeBlock(int armor, int attackFace, int defenseTotal)
        {
            int safeArmor = Math.Max(0, armor);
            double block = safeArmor * GetMultiplier(attackFace, defenseTotal);
            return Math.Max(0, (int)Math.Round(block, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Mitigation subtracted from raw attack. Hero + defense total uses the opposed-margin table;
        /// otherwise returns <see cref="DamageCalculator.ResolveTargetArmor"/> (100% armor).
        /// Missing attack face (env) uses <see cref="NeutralAttackFace"/>.
        /// </summary>
        public static int ResolveMitigation(Actor target, Action? action, int? defenseFace, int? attackFace = null)
        {
            int armor = DamageCalculator.ResolveTargetArmor(target, action);
            if (defenseFace.HasValue && IsHeroDefender(target))
                return ComputeBlock(armor, ResolveAttackFace(attackFace), defenseFace.Value);
            return armor;
        }

        /// <summary>
        /// Rolls one unforced d20 (does not consume attack-scripted d20 queues) and returns the face (1–20).
        /// Returns null when the target should not roll (enemy, pierce, non-hero).
        /// </summary>
        public static int? TryRollDefenseFace(Actor? target, Action? action)
        {
            if (!ShouldRoll(target, action))
                return null;
            return Dice.RollUnforced(20);
        }
    }
}
