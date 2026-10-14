using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Items
{
    /// <summary>
    /// Regression tests: weapon Quality (gearPrimaryStatMultiplier) scales damage. Speed comes only from the weapon's
    /// catalog speed and weapon speed points (the same factor combat uses), so quality leaves it unchanged.
    /// </summary>
    public static class WeaponItemQualityMultiplierTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestQualityDoesNotChangeWeaponSpeed(ref run, ref passed, ref failed);
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

        private static void TestQualityDoesNotChangeWeaponSpeed(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestQualityDoesNotChangeWeaponSpeed));

            // Base speed is a time multiplier: 1.0 = baseline, and quality does not move it.
            var w = CreateWeaponWithQuality("Fine", baseDamage: 10, baseSpeed: 1.0, qualityMultiplier: 2.0);
            TestBase.AssertEqual(1.0, Math.Round(w.GetTotalAttackSpeed(), 3), "high quality keeps weapon speed", ref run, ref passed, ref failed);

            var w2 = CreateWeaponWithQuality("Crude", baseDamage: 10, baseSpeed: 1.0, qualityMultiplier: 0.5);
            TestBase.AssertEqual(1.0, Math.Round(w2.GetTotalAttackSpeed(), 3), "low quality keeps weapon speed", ref run, ref passed, ref failed);

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

