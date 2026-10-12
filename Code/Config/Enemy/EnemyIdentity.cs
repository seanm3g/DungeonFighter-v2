using System;
using RPGGame.World.Tags;

namespace RPGGame
{
    /// <summary>
    /// Per-enemy combat shape for <see cref="EnemyIdentityConfig"/> mode. Damage base, swing time and HP come
    /// from level references; the multipliers here only redistribute threat between speed, hit size and HP.
    /// </summary>
    public sealed class EnemyIdentityProfile
    {
        /// <summary>Swing time × this (below 1 = faster). Includes the archetype attack time.</summary>
        public double AttackTime { get; init; } = 1.0;
        /// <summary>Max HP × this.</summary>
        public double Health { get; init; } = 1.0;
        /// <summary>Damage per hit × this (AttackTime ÷ Health, so speed and HP shape do not change threat).</summary>
        public double HitDamage { get; init; } = 1.0;
        /// <summary>Per-enemy calibration from <see cref="EnemyIdentityConfig.Calibration"/> at this level.</summary>
        public double Calibration { get; init; } = 1.0;
        /// <summary>Attribute damage bonus of a reference enemy at this level (replaces STR + primary).</summary>
        public int ReferenceAttributeDamage { get; init; }
        /// <summary>Agility of a reference enemy at this level (drives the shared agility speed curve).</summary>
        public int ReferenceAgility { get; init; }
    }

    public static class EnemyIdentity
    {
        /// <summary>Shape strength at <paramref name="level"/>: <see cref="EnemyIdentityConfig.IntensityAtLevel1"/> at L1, 1 from the full level on.</summary>
        public static double IntensityAt(int level, EnemyIdentityConfig config)
        {
            double start = Math.Clamp(config.IntensityAtLevel1, 0.0, 1.0);
            int full = Math.Max(2, config.FullIntensityLevel);
            double t = Math.Clamp((level - 1) / (double)(full - 1), 0.0, 1.0);
            return start + (1.0 - start) * t;
        }

        /// <summary>Reference attribute value at <paramref name="level"/> (L1 base + growth budget ÷ 4 per level).</summary>
        public static double ReferenceAttributeAt(int level, EnemySystemConfig enemySystem)
        {
            var prog = enemySystem.ProgressionScales ?? new EnemyProgressionScalesConfig();
            double budget = enemySystem.AttributeGrowthBudgetPerLevel > 0
                ? enemySystem.AttributeGrowthBudgetPerLevel
                : EnemyStatCalculator.EnemyAttributeGrowthBudgetPerLevel;
            return enemySystem.Identity.ReferenceBaseAttribute + Math.Max(0, level - 1) * budget / 4.0 * prog.AttributeGrowthScale;
        }

        /// <summary>Null when identity mode is off.</summary>
        public static EnemyIdentityProfile? Resolve(EnemyData data, int level, EnemySystemConfig enemySystem)
        {
            var config = enemySystem.Identity;
            if (config == null || !config.Enabled)
                return null;

            double intensity = IntensityAt(level, config);

            var (str, agi, _, _) = EnemyStatCalculator.ComputeNormalizedAttributeGrowthPerLevel(
                data.GrowthPerLevel, enemySystem.AttributeGrowthBudgetPerLevel);
            double refRatio = config.ReferenceAgilityStrengthRatio > 0 ? config.ReferenceAgilityStrengthRatio : 1.0;
            double ratio = Math.Clamp((agi + 0.5) / (str + 0.5) / refRatio, 0.25, 4.0);
            double attackTime = Math.Pow(ratio, -config.SpeedExponent * intensity);

            TagDefinitions.TryParseEnemyArchetype(data.Archetype, out var archetype);
            var profile = ArchetypeManager.GetArchetypeProfile(archetype);
            attackTime *= Math.Pow(ArchetypeManager.GetArchetypeAttackTimeMultiplier(profile), intensity);

            double refGrowth = config.ReferenceHealthGrowthPercent > 0 ? config.ReferenceHealthGrowthPercent : 1.0;
            double growth = data.HealthGrowthPercent ?? refGrowth;
            double healthPercent = (data.HealthPercent ?? 100.0) / 100.0;
            double healthShape = Math.Max(0.1, profile.HealthMultiplier * healthPercent * growth / refGrowth);
            double health = Math.Pow(healthShape, intensity);

            double calibration = 1.0;
            if (config.Calibration != null && config.Calibration.TryGetValue(data.Name, out var anchors) && anchors?.Count > 0)
                calibration = new EnemyDamageCurveConfig { Enabled = true, Anchors = anchors }.MultiplierAt(level);

            double refAttr = ReferenceAttributeAt(level, enemySystem);
            double scale = GameConfiguration.Instance.Combat.AttributeDamageScale;
            if (scale <= 0)
                scale = 0.05;

            return new EnemyIdentityProfile
            {
                AttackTime = attackTime,
                Health = health,
                HitDamage = attackTime / health,
                Calibration = calibration,
                ReferenceAttributeDamage = (int)Math.Floor(2.0 * refAttr * scale),
                ReferenceAgility = (int)Math.Round(refAttr * enemySystem.GlobalMultipliers.SpeedMultiplier)
            };
        }
    }
}
