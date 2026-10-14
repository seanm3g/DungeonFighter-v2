using System;
using RPGGame.Items;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Every swing-time input, resolved once. Combat, tooltips and item screens all read this so the numbers agree.
    /// <para>Swing seconds = max(floor, base × AGI factor × weapon factor × archetype × tempo).</para>
    /// </summary>
    public sealed class SpeedBreakdown
    {
        /// <summary>Fixed base attack time (<see cref="CombatConfig.BaseAttackTime"/>, 8s).</summary>
        public double BaseSeconds { get; init; }
        /// <summary>Agility used for the curve (effective AGI; enemy identity uses its reference AGI).</summary>
        public int Agility { get; init; }
        /// <summary>AGI curve × overflow: 1.0 at <see cref="CombatConfig.AgilityMin"/>, lower is faster.</summary>
        public double AgilityFactor { get; init; }
        /// <summary>Weapon catalog <c>attackSpeed</c> after spread and clamp (1.0 when unarmed).</summary>
        public double WeaponBaseFactor { get; init; }
        /// <summary>Weapon speed points: weapon tier bonus + gear <c>AttackSpeed</c> + WEAPON_SPEED actions.</summary>
        public double WeaponSpeedPoints { get; init; }
        /// <summary>Weapon base ÷ class/mod multipliers ÷ (1 + points × % per point). Lower is faster.</summary>
        public double WeaponFactor { get; init; }
        /// <summary>Enemy archetype / identity attack-time multiplier (1.0 for heroes).</summary>
        public double ArchetypeFactor { get; init; } = 1.0;
        /// <summary>Slow debuff time multiplier (1.0 when not slowed).</summary>
        public double SlowFactor { get; init; } = 1.0;
        /// <summary>Standing set-bonus speed % (animal ladder), applied as ÷ (1 + %).</summary>
        public double SetSpeedPercent { get; init; }
        /// <summary>Slow and set bonuses combined (the tempo layer on the swing).</summary>
        public double TempoFactor => SlowFactor / (1.0 + SetSpeedPercent / 100.0);
        /// <summary>Swing before the floor.</summary>
        public double RawSeconds { get; init; }
        public double FloorSeconds { get; init; }
        public double FinalSeconds => Math.Max(FloorSeconds, RawSeconds);
    }

    /// <summary>
    /// Attack time in seconds. Core ingredients are fixed base time, agility and weapon speed;
    /// slow and set bonuses form a tempo layer on top. Per-action length and SPEED_MOD apply in <see cref="ActionSpeedSystem"/>.
    /// </summary>
    public static class SpeedCalculator
    {
        /// <summary>
        /// Weapon sheet <c>attackSpeed</c> / <see cref="WeaponItem.BaseAttackSpeed"/>: 1 = baseline cadence;
        /// values above 1 lengthen swing spacing (slower); below 1 shorten it (faster).
        /// </summary>
        public static double GetWeaponAttackTimeMultiplier(WeaponItem w)
        {
            double m = w.BaseAttackSpeed;
            if (m <= 0 || double.IsNaN(m) || double.IsInfinity(m))
                m = 1.0;
            var combat = GameConfiguration.Instance.Combat;
            m = 1.0 + (m - 1.0) * Math.Max(0.0, combat.WeaponSpeedSpread);
            double clampMin = combat.WeaponAttackTimeClampMin > 0 ? combat.WeaponAttackTimeClampMin : 0.5;
            double clampMax = combat.WeaponAttackTimeClampMax > clampMin ? combat.WeaponAttackTimeClampMax : 1.5;
            return Math.Max(clampMin, Math.Min(clampMax, m));
        }

        /// <summary>Time divisor for weapon speed points: 1 + points × % per point (floored at 0.1 for negative points).</summary>
        public static double GetWeaponSpeedPointDivisor(double points)
        {
            double pct = GameConfiguration.Instance.Combat.GetWeaponSpeedPercentPerPoint();
            return Math.Max(0.1, 1.0 + points * pct / 100.0);
        }

        /// <summary>
        /// The weapon on its own (no wielder): catalog speed ÷ its own speed points. Used by item screens and value models.
        /// </summary>
        public static double GetWeaponItemTimeFactor(WeaponItem w) =>
            GetWeaponAttackTimeMultiplier(w) / GetWeaponSpeedPointDivisor(EquipmentBonusCalculator.GetItemWeaponSpeedPoints(w));

        /// <summary>
        /// Agility time multiplier: <see cref="CombatConfig.AgilityMinSpeedMultiplier"/> at <see cref="CombatConfig.AgilityMin"/>
        /// down to <see cref="CombatConfig.AgilityMaxSpeedMultiplier"/> at <see cref="CombatConfig.AgilityMax"/>, shaped by
        /// <see cref="CombatConfig.AgilitySpeedCurveExponent"/>.
        /// </summary>
        public static double GetAgilitySpeedMultiplier(int agility)
        {
            var combat = GameConfiguration.Instance.Combat;
            int agilityMin = combat.AgilityMin;
            int agilityMax = combat.AgilityMax;
            agility = Math.Max(agilityMin, Math.Min(agilityMax, agility));
            double agilityRange = agilityMax - agilityMin;
            double normalizedProgress = agilityRange > 0 ? (agility - agilityMin) / agilityRange : 0.0;
            double exponent = combat.AgilitySpeedCurveExponent > 0 ? combat.AgilitySpeedCurveExponent : 0.5;
            double curvedProgress = Math.Pow(normalizedProgress, exponent);
            return combat.AgilityMinSpeedMultiplier + (combat.AgilityMaxSpeedMultiplier - combat.AgilityMinSpeedMultiplier) * curvedProgress;
        }

        /// <summary>
        /// Calculates attack speed for any Actor (shared logic)
        /// </summary>
        /// <param name="actor">The Actor to calculate attack speed for</param>
        /// <returns>Attack speed in seconds</returns>
        public static double CalculateAttackSpeed(Actor actor) => GetBreakdown(actor).FinalSeconds;

        /// <summary>
        /// Overflow damage multiplier for a hero whose raw swing is faster than <see cref="CombatConfig.MinimumAttackTime"/>:
        /// 1 + <see cref="CombatConfig.SpeedOverflowDamageEfficiency"/> × (minimum / raw − 1). 1.0 when off, for enemies,
        /// or when the swing is above the floor.
        /// </summary>
        public static double GetSpeedOverflowDamageMultiplier(Actor actor)
        {
            var combat = GameConfiguration.Instance.Combat;
            if (actor is not Character || actor is Enemy || combat.SpeedOverflowDamageEfficiency <= 0)
                return 1.0;
            var b = GetBreakdown(actor);
            double raw = b.RawSeconds;
            if (raw <= 0 || raw >= b.FloorSeconds)
                return 1.0;
            return 1.0 + combat.SpeedOverflowDamageEfficiency * (b.FloorSeconds / raw - 1.0);
        }

        /// <summary>
        /// Swing for <paramref name="hero"/> as if wielding <paramref name="weapon"/> instead of the equipped one
        /// (null = unarmed). Item screens use this to show a weapon as seconds for the current character.
        /// </summary>
        public static SpeedBreakdown GetBreakdownWithWeapon(Character hero, WeaponItem? weapon)
        {
            var combat = GameConfiguration.Instance.Combat;
            return HeroBreakdown(hero, combat.BaseAttackTime, Math.Max(0.01, combat.MinimumAttackTime), weapon, overrideWeapon: true);
        }

        /// <summary>Resolves every swing-time input for <paramref name="actor"/>.</summary>
        public static SpeedBreakdown GetBreakdown(Actor actor)
        {
            var combat = GameConfiguration.Instance.Combat;
            double baseSeconds = combat.BaseAttackTime;
            double floor = Math.Max(0.01, combat.MinimumAttackTime);

            // Enemy : Character, so enemies must be matched first or the archetype branch never runs.
            if (actor is Enemy enemy)
                return EnemyBreakdown(enemy, baseSeconds, floor);
            if (actor is Character hero)
                return HeroBreakdown(hero, baseSeconds, floor, null, overrideWeapon: false);

            return new SpeedBreakdown
            {
                BaseSeconds = baseSeconds,
                AgilityFactor = 1.0,
                WeaponBaseFactor = 1.0,
                WeaponFactor = 1.0,
                RawSeconds = baseSeconds,
                FloorSeconds = floor
            };
        }

        private static SpeedBreakdown HeroBreakdown(Character hero, double baseSeconds, double floor, WeaponItem? weaponOverride, bool overrideWeapon)
        {
            var combat = GameConfiguration.Instance.Combat;

            // Agility: curve, then overflow past the max keeps making the raw swing faster (feeds overflow damage).
            int agi = hero.GetEffectiveAgility();
            double agilityFactor = GetAgilitySpeedMultiplier(agi);
            double perPoint = combat.AgilityOverflowSpeedPerPoint;
            if (perPoint > 0 && agi > combat.AgilityMax)
                agilityFactor /= 1.0 + (agi - combat.AgilityMax) * perPoint;

            // Weapon: catalog speed ÷ weapon-type class multiplier ÷ item speed mods ÷ weapon speed points
            // (points from every equipped item + WEAPON_SPEED actions).
            double weaponBase = 1.0;
            double points = hero.GetEquipmentAttackSpeedBonus() + hero.Effects.ConsumedWeaponSpeedFlat;
            WeaponItem? wielded = hero.Weapon as WeaponItem;
            if (overrideWeapon)
            {
                if (wielded != null)
                    points -= EquipmentBonusCalculator.GetItemWeaponSpeedPoints(wielded);
                wielded = weaponOverride;
                if (wielded != null)
                    points += EquipmentBonusCalculator.GetItemWeaponSpeedPoints(wielded);
            }
            double classSpeed = 1.0;
            if (wielded is WeaponItem w)
            {
                weaponBase = GetWeaponAttackTimeMultiplier(w);
                double cs = ClassBalanceHelper.GetSpeedMultiplier(w.WeaponType);
                if (cs > 0)
                    classSpeed = cs;
            }
            double modSpeed = Math.Max(0.0001, hero.GetModificationSpeedMultiplier());
            double weaponFactor = weaponBase / classSpeed / modSpeed / GetWeaponSpeedPointDivisor(points);

            // Tempo: slow debuff and standing set-bonus speed %.
            double slowFactor = hero.SlowTurns > 0 ? 1.0 / Math.Max(0.1, hero.SlowMultiplier) : 1.0;
            double setPct = Math.Max(0, AnimalSetController.GetGeneralSpeedPct(hero));

            double raw = baseSeconds * agilityFactor * weaponFactor * slowFactor / (1.0 + setPct / 100.0);
            return new SpeedBreakdown
            {
                BaseSeconds = baseSeconds,
                Agility = agi,
                AgilityFactor = agilityFactor,
                WeaponBaseFactor = weaponBase,
                WeaponSpeedPoints = points,
                WeaponFactor = weaponFactor,
                SlowFactor = slowFactor,
                SetSpeedPercent = setPct,
                RawSeconds = Math.Max(0.001, raw),
                FloorSeconds = floor
            };
        }

        private static SpeedBreakdown EnemyBreakdown(Enemy enemy, double baseSeconds, double floor)
        {
            int agi = enemy.Identity is { } identity ? identity.ReferenceAgility : enemy.GetEffectiveAgility();
            double agilityFactor = GetAgilitySpeedMultiplier(agi);

            double weaponBase = enemy.Weapon is WeaponItem w ? GetWeaponAttackTimeMultiplier(w) : 1.0;
            double points = enemy.Effects.ConsumedWeaponSpeedFlat;
            double weaponFactor = weaponBase / GetWeaponSpeedPointDivisor(points);

            double archetype = enemy.Identity?.AttackTime
                ?? ArchetypeManager.GetArchetypeAttackTimeMultiplier(enemy.AttackProfile);
            double slowFactor = enemy.SlowTurns > 0 ? 1.0 / Math.Max(0.1, enemy.SlowMultiplier) : 1.0;

            double raw = baseSeconds * agilityFactor * weaponFactor * archetype * slowFactor;
            return new SpeedBreakdown
            {
                BaseSeconds = baseSeconds,
                Agility = agi,
                AgilityFactor = agilityFactor,
                WeaponBaseFactor = weaponBase,
                WeaponSpeedPoints = points,
                WeaponFactor = weaponFactor,
                ArchetypeFactor = archetype,
                SlowFactor = slowFactor,
                RawSeconds = Math.Max(0.001, raw),
                FloorSeconds = floor
            };
        }
    }
}
