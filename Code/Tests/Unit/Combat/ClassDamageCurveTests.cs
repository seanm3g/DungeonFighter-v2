using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>classBalance.&lt;class&gt;.damageCurve: hero-level curve on top of the class damage multiplier.</summary>
    public static class ClassDamageCurveTests
    {
        private static int _run, _pass, _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Class Damage Curve Tests ===\n");
            _run = _pass = _fail = 0;

            TestDisabledCurveIsNeutral();
            TestCurveScalesByLevel();
            TestNoLevelSkipsCurve();

            TestBase.PrintSummary("Class Damage Curve Tests", _run, _pass, _fail);
        }

        private static ClassBalanceConfig Balance(EnemyDamageCurveConfig wizardCurve) => new()
        {
            Barbarian = new ClassMultipliers { HealthMultiplier = 1, DamageMultiplier = 1.35, SpeedMultiplier = 1 },
            Warrior = new ClassMultipliers { HealthMultiplier = 1, DamageMultiplier = 1.55, SpeedMultiplier = 1 },
            Rogue = new ClassMultipliers { HealthMultiplier = 1, DamageMultiplier = 1.5, SpeedMultiplier = 1 },
            Wizard = new ClassMultipliers { HealthMultiplier = 1, DamageMultiplier = 1.0, SpeedMultiplier = 1, DamageCurve = wizardCurve }
        };

        private static double WizardMult(ClassBalanceConfig balance, int level)
        {
            var saved = GameConfiguration.Instance.ClassBalance;
            GameConfiguration.Instance.ClassBalance = balance;
            try { return ClassBalanceHelper.GetDamageMultiplier(WeaponType.Wand, level); }
            finally { GameConfiguration.Instance.ClassBalance = saved; }
        }

        private static void TestDisabledCurveIsNeutral()
        {
            Console.WriteLine("--- disabled curve keeps the flat multiplier ---");
            var balance = Balance(new EnemyDamageCurveConfig
            {
                Enabled = false,
                Anchors = new List<EnemyDamageCurveAnchor> { new() { Level = 10, Multiplier = 2.0 } }
            });
            TestBase.AssertEqual(1.0, WizardMult(balance, 30), "disabled curve -> 1.0 at L30", ref _run, ref _pass, ref _fail);
        }

        private static void TestCurveScalesByLevel()
        {
            Console.WriteLine("--- anchors scale the class multiplier by hero level ---");
            var balance = Balance(new EnemyDamageCurveConfig
            {
                Enabled = true,
                Anchors = new List<EnemyDamageCurveAnchor> { new() { Level = 20, Multiplier = 0.8 }, new() { Level = 60, Multiplier = 1.25 } }
            });
            TestBase.AssertEqual(0.8, WizardMult(balance, 5), "below first anchor holds 0.8", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1.25, WizardMult(balance, 75), "past last anchor holds 1.25", ref _run, ref _pass, ref _fail);
            double mid = WizardMult(balance, 40);
            TestBase.AssertTrue(mid > 0.8 && mid < 1.25, $"L40 between anchors ({mid:0.###})", ref _run, ref _pass, ref _fail);
        }

        private static void TestNoLevelSkipsCurve()
        {
            Console.WriteLine("--- level 0 (no hero level) skips the curve ---");
            var balance = Balance(new EnemyDamageCurveConfig
            {
                Enabled = true,
                Anchors = new List<EnemyDamageCurveAnchor> { new() { Level = 1, Multiplier = 2.0 } }
            });
            TestBase.AssertEqual(1.0, WizardMult(balance, 0), "level 0 -> flat multiplier only", ref _run, ref _pass, ref _fail);
        }
    }
}
