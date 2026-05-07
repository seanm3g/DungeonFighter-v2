using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Items
{
    /// <summary>
    /// Regression tests: weapon Quality (gearPrimaryStatMultiplier) affects both damage and speed.
    /// Speed is a time multiplier (lower = faster), so quality is applied inversely for speed.
    /// </summary>
    public static class WeaponItemQualityMultiplierTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestQualityAffectsWeaponSpeed_Inversely(ref run, ref passed, ref failed);
            TestQualityAffectsWeaponDamage_Directly(ref run, ref passed, ref failed);

            TestBase.PrintSummary(nameof(WeaponItemQualityMultiplierTests), run, passed, failed);
        }

        private static WeaponItem CreateWeaponWithQuality(string name, int baseDamage, double baseSpeed, double qualityMultiplier)
        {
            var w = new WeaponItem(name, tier: 1, baseDamage: baseDamage, baseAttackSpeed: baseSpeed, weaponType: WeaponType.Sword)
            {
                Rarity = "Common",
                Modifications = new List<Modification>()
            };

            w.Modifications.Add(new Modification
            {
                Name = "Quality",
                PrefixCategory = "Quality",
                Effect = "gearPrimaryStatMultiplier",
                RolledValue = qualityMultiplier
            });

            return w;
        }

        private static void TestQualityAffectsWeaponSpeed_Inversely(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestQualityAffectsWeaponSpeed_Inversely));

            // Base speed is a time multiplier: 1.0 = baseline.
            // Quality 2.0 should make weapon faster => 1.0 / 2.0 = 0.5.
            var w = CreateWeaponWithQuality("Fast", baseDamage: 10, baseSpeed: 1.0, qualityMultiplier: 2.0);
            TestBase.AssertEqual(0.5, Math.Round(w.GetTotalAttackSpeed(), 3), "quality speeds up weapon (inverse)", ref run, ref passed, ref failed);

            // Quality 0.5 should make weapon slower => 1.0 / 0.5 = 2.0.
            var w2 = CreateWeaponWithQuality("Slow", baseDamage: 10, baseSpeed: 1.0, qualityMultiplier: 0.5);
            TestBase.AssertEqual(2.0, Math.Round(w2.GetTotalAttackSpeed(), 3), "low quality slows weapon (inverse)", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestQualityAffectsWeaponDamage_Directly(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestQualityAffectsWeaponDamage_Directly));

            // Damage uses the multiplier directly.
            var w = CreateWeaponWithQuality("Strong", baseDamage: 10, baseSpeed: 1.0, qualityMultiplier: 1.5);
            TestBase.AssertEqual(15, w.GetTotalDamage(), "quality increases weapon damage", ref run, ref passed, ref failed);

            var w2 = CreateWeaponWithQuality("Weak", baseDamage: 10, baseSpeed: 1.0, qualityMultiplier: 0.75);
            TestBase.AssertEqual(8, w2.GetTotalDamage(), "quality reduces weapon damage (round)", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }
    }
}

