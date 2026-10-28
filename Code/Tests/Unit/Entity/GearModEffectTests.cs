using System;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Regression: every adjective mod effect in Modifications.json must change the hero.
    /// DAMAGE, MAGIC FIND, SPEED and AMP used to be display-only (their names never matched the bonus lookups).
    /// </summary>
    public static class GearModEffectTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Gear Mod Effect Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestDamageModAddsFlatDamage();
            TestMagicFindModAddsMagicFind();
            TestSpeedModIsPercentFaster();
            TestAmpModRaisesBaseAmp();

            TestBase.PrintSummary("Gear Mod Effect Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static Character BuildHero(string? effect, double value)
        {
            var hero = TestDataBuilders.Character().WithName("ModHero").WithLevel(5).WithStats(10, 10, 10, 10).Build();
            hero.EquipItem(new WeaponItem("Mod Sword", 1, 5, 1.0, WeaponType.Sword), "weapon");
            var boots = new FeetItem("Mod Boots", 1, 1);
            if (effect != null)
                boots.Modifications.Add(new Modification { Name = "Mod", PrefixCategory = "ADJECTIVE", Effect = effect, RolledValue = value });
            hero.EquipItem(boots, "feet");
            return hero;
        }

        private static void TestDamageModAddsFlatDamage()
        {
            Console.WriteLine("--- DAMAGE (Brutal +10) ---");
            TestBase.AssertEqual(0, BuildHero(null, 0).GetModificationDamageBonus(),
                "No mod: no damage bonus", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(10, BuildHero("DAMAGE", 10).GetModificationDamageBonus(),
                "DAMAGE 10: +10 flat damage", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, BuildHero("damage", 5).GetModificationDamageBonus(),
                "Legacy lower-case 'damage' still matches", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMagicFindModAddsMagicFind()
        {
            Console.WriteLine("\n--- MAGIC FIND (Blessed +2) ---");
            int baseline = BuildHero(null, 0).GetMagicFind();
            TestBase.AssertEqual(baseline + 2, BuildHero("MAGIC FIND", 2).GetMagicFind(),
                "MAGIC FIND 2: +2 magic find", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSpeedModIsPercentFaster()
        {
            Console.WriteLine("\n--- SPEED (Featherweight 0.5) ---");
            var plain = BuildHero(null, 0);
            var fast = BuildHero("SPEED", 0.5);
            TestBase.AssertTrue(Math.Abs(fast.GetModificationSpeedMultiplier() - 1.5) < 1e-9,
                "SPEED 0.5: speed multiplier ×1.5", ref _testsRun, ref _testsPassed, ref _testsFailed);
            double before = SpeedCalculator.CalculateAttackSpeed(plain);
            double after = SpeedCalculator.CalculateAttackSpeed(fast);
            TestBase.AssertTrue(after < before,
                $"SPEED 0.5: attack time drops ({before:0.###}s → {after:0.###}s)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAmpModRaisesBaseAmp()
        {
            Console.WriteLine("\n--- AMP (refined 0.1) ---");
            double baseAmp = BuildHero(null, 0).GetComboAmplifier();
            double modAmp = BuildHero("AMP", 0.1).GetComboAmplifier();
            TestBase.AssertTrue(Math.Abs(modAmp - baseAmp * 1.1) < 1e-9,
                $"AMP 0.1: base per-step AMP ×1.1 ({baseAmp:0.###} → {modAmp:0.###})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
