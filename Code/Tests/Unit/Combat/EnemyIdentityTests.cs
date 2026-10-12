using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Enemy identity mode: level references set power, growth shape sets speed / hit size / HP without
    /// changing threat, shape grows with level, and per-enemy calibration scales damage.
    /// </summary>
    public static class EnemyIdentityTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyIdentity Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestDisabledReturnsNull();
            TestIntensityRisesToFull();
            TestAgileEnemySwingsFasterAndHitsLighter();
            TestShapeGrowsWithLevel();
            TestShapeKeepsThreatNeutral();
            TestCalibrationScalesDamage();
            TestStatsUseReferenceHealth();

            Console.WriteLine($"\nEnemyIdentity: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed.\n");
        }

        private static EnemySystemConfig System(bool enabled = true)
        {
            var sys = new EnemySystemConfig();
            sys.Identity = new EnemyIdentityConfig { Enabled = enabled };
            sys.ArchetypeCombat = new Dictionary<string, ArchetypeCombatConfig>(StringComparer.OrdinalIgnoreCase);
            return sys;
        }

        private static EnemyData Enemy(string name, double str, double agi, string archetype = "Berserker") => new()
        {
            Name = name,
            Archetype = archetype,
            HealthPercent = 100,
            HealthGrowthPercent = 4.29,
            BaseAttributes = new EnemyAttributeSet { Strength = 3, Agility = 3, Technique = 3, Intelligence = 3 },
            GrowthPerLevel = new EnemyAttributeSet { Strength = str, Agility = agi, Technique = 1.5, Intelligence = 1.5 }
        };

        private static void TestDisabledReturnsNull()
        {
            TestBase.AssertNull(EnemyIdentity.Resolve(Enemy("Off", 1.5, 1.5), 10, System(enabled: false)),
                "Identity is null when disabled", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIntensityRisesToFull()
        {
            var config = new EnemyIdentityConfig { IntensityAtLevel1 = 0.5, FullIntensityLevel = 50 };
            TestBase.AssertEqual(0.5, EnemyIdentity.IntensityAt(1, config), "Intensity at L1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, EnemyIdentity.IntensityAt(50, config), "Intensity full at L50", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, EnemyIdentity.IntensityAt(90, config), "Intensity stays full past L50", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAgileEnemySwingsFasterAndHitsLighter()
        {
            var sys = System();
            var agile = EnemyIdentity.Resolve(Enemy("Agile", 0.5, 3.0), 1, sys)!;
            var strong = EnemyIdentity.Resolve(Enemy("Strong", 3.0, 0.5), 1, sys)!;
            TestBase.AssertTrue(agile.AttackTime < strong.AttackTime, "AGI-leaning enemy swings faster at L1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(agile.HitDamage < strong.HitDamage, "AGI-leaning enemy hits lighter at L1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestShapeGrowsWithLevel()
        {
            var sys = System();
            var data = Enemy("Strong", 3.0, 0.5);
            double l1 = EnemyIdentity.Resolve(data, 1, sys)!.AttackTime;
            double l50 = EnemyIdentity.Resolve(data, 50, sys)!.AttackTime;
            TestBase.AssertTrue(l1 > 1.0 && l50 > l1, "Slow enemy is slow at L1 and slower at L50",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestShapeKeepsThreatNeutral()
        {
            var sys = System();
            var p = EnemyIdentity.Resolve(Enemy("Brute", 2.0, 1.0, "Brute"), 30, sys)!;
            TestBase.AssertTrue(p.Health > 1.0, "Brute archetype is tankier", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(p.HitDamage * p.Health / p.AttackTime - 1.0) < 1e-9,
                "Hit damage × HP ÷ swing time is 1 (shape does not change threat)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalibrationScalesDamage()
        {
            var sys = System();
            sys.Identity.Calibration = new Dictionary<string, List<EnemyDamageCurveAnchor>>(StringComparer.OrdinalIgnoreCase)
            {
                ["calibrated"] = new() { new() { Level = 1, Multiplier = 0.5 }, new() { Level = 21, Multiplier = 2.0 } }
            };
            var data = Enemy("Calibrated", 1.5, 1.5);
            TestBase.AssertEqual(0.5, EnemyIdentity.Resolve(data, 1, sys)!.Calibration, "Calibration at first anchor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(EnemyIdentity.Resolve(data, 11, sys)!.Calibration - 1.0) < 1e-9,
                "Calibration interpolates in log space between anchors", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, EnemyIdentity.Resolve(Enemy("Other", 1.5, 1.5), 11, sys)!.Calibration,
                "Uncalibrated enemy uses 1.0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatsUseReferenceHealth()
        {
            var sys = System();
            var low = Enemy("LowGrowth", 1.5, 1.5);
            low.HealthGrowthPercent = 2.0;
            var high = Enemy("HighGrowth", 1.5, 1.5);
            high.HealthGrowthPercent = 4.29;
            int lowHp = EnemyStatCalculator.CalculateStats(low, 1, sys).Health;
            int highHp = EnemyStatCalculator.CalculateStats(high, 1, sys).Health;
            int reference = EnemyStatCalculator.CalculateStats(high, 40, sys).Health;
            TestBase.AssertTrue(lowHp < highHp, "Low HP growth enemy is frailer from L1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(reference > highHp, "HP still grows with level", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
