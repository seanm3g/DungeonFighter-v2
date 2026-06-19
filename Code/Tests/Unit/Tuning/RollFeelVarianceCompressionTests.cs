using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class RollFeelVarianceCompressionTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== RollFeelVarianceCompression Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRegistry_HasMasterParameter();
            TestApply_Chaotic_Endpoints();
            TestApply_Regular_Endpoints();
            TestApply_Midpoint_Interpolates();
            TestPanelViewModel_MasterSlider_RefreshesLinkedRows();

            TestBase.PrintSummary("RollFeelVarianceCompression Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRegistry_HasMasterParameter()
        {
            Console.WriteLine("--- Master parameter registered ---");
            var master = CombatTuningParameterRegistry.GetById(RollFeelVarianceCompression.MasterParameterId);
            TestBase.AssertTrue(master != null,
                "rollFeelVarianceCompression exists in registry",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(RollFeelVarianceCompression.SubGroupName, master!.SubGroup,
                "Master slider is in Variance Compression subgroup",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            foreach (string id in RollFeelVarianceCompression.DrivenParameterIds)
            {
                var driven = CombatTuningParameterRegistry.GetById(id);
                TestBase.AssertTrue(driven != null,
                    $"{id} exists in registry",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(RollFeelVarianceCompression.SubGroupName, driven!.SubGroup,
                    $"{id} is in Variance Compression subgroup",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(driven.Tab == CombatTuningTab.Core,
                    $"{id} is on Core tab",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApply_Chaotic_Endpoints()
        {
            Console.WriteLine("--- Apply(0) sets chaotic endpoints ---");
            var cfg = GameConfiguration.Instance;
            SaveSnapshot(cfg, out var snap);
            try
            {
                RollFeelVarianceCompression.Apply(0, cfg);
                TestBase.AssertEqual(0, cfg.CombatBalance.RollFeelVarianceCompression,
                    "Stores compression value", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2.5, cfg.CombatBalance.CriticalHitDamageMultiplier,
                    "Chaotic crit multiplier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1.5, cfg.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier,
                    "Chaotic combo multiplier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1.0, cfg.CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier,
                    "Basic roll multiplier stays 1.0", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, cfg.Combat.PlayerBaseArmor,
                    "Chaotic player armor baseline", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0.5, cfg.EnemySystem.GlobalMultipliers.ArmorMultiplier,
                    "Chaotic enemy armor mult", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1500, cfg.Combat.MaximumDamageCap,
                    "Chaotic high damage cap", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, cfg.Combat.MinimumDamage,
                    "Chaotic low damage floor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                RestoreSnapshot(cfg, snap);
            }
        }

        private static void TestApply_Regular_Endpoints()
        {
            Console.WriteLine("--- Apply(1) sets regular endpoints ---");
            var cfg = GameConfiguration.Instance;
            SaveSnapshot(cfg, out var snap);
            try
            {
                RollFeelVarianceCompression.Apply(1, cfg);
                TestBase.AssertEqual(1.2, cfg.CombatBalance.CriticalHitDamageMultiplier,
                    "Regular crit multiplier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1.1, cfg.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier,
                    "Regular combo multiplier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(12, cfg.Combat.PlayerBaseArmor,
                    "Regular player armor baseline", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1.5, cfg.EnemySystem.GlobalMultipliers.ArmorMultiplier,
                    "Regular enemy armor mult", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(400, cfg.Combat.MaximumDamageCap,
                    "Regular lower damage cap", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(4, cfg.Combat.MinimumDamage,
                    "Regular higher damage floor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                RestoreSnapshot(cfg, snap);
            }
        }

        private static void TestApply_Midpoint_Interpolates()
        {
            Console.WriteLine("--- Apply(0.5) interpolates ---");
            var cfg = GameConfiguration.Instance;
            SaveSnapshot(cfg, out var snap);
            try
            {
                RollFeelVarianceCompression.Apply(0.5, cfg);
                TestBase.AssertEqual(1.85, cfg.CombatBalance.CriticalHitDamageMultiplier,
                    "Midpoint crit multiplier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1.3, cfg.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier,
                    "Midpoint combo multiplier", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(6, cfg.Combat.PlayerBaseArmor,
                    "Midpoint player armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                RestoreSnapshot(cfg, snap);
            }
        }

        private static void TestPanelViewModel_MasterSlider_RefreshesLinkedRows()
        {
            Console.WriteLine("--- Master slider refreshes linked UI rows ---");
            var cfg = GameConfiguration.Instance;
            SaveSnapshot(cfg, out var snap);
            try
            {
                var vm = CombatTuningPanelViewModel.FromRegistry();
                var master = vm.GetById(RollFeelVarianceCompression.MasterParameterId);
                TestBase.AssertTrue(master != null, "Master row in panel VM", ref _testsRun, ref _testsPassed, ref _testsFailed);

                master!.Value = 0;
                var critRow = vm.GetById("criticalHitDamageMult");
                TestBase.AssertTrue(critRow != null, "Crit row exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2.5, critRow!.Value,
                    "Crit row reflects master slider apply",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                RestoreSnapshot(cfg, snap);
            }
        }

        private sealed class ConfigSnapshot
        {
            public double Compression { get; init; }
            public double CritMult { get; init; }
            public double ComboMult { get; init; }
            public double BasicMult { get; init; }
            public int PlayerArmor { get; init; }
            public double EnemyArmorMult { get; init; }
            public int DamageCap { get; init; }
            public int MinDamage { get; init; }
            public double ArmorReductionFactor { get; init; }
        }

        private static void SaveSnapshot(GameConfiguration cfg, out ConfigSnapshot snap)
        {
            snap = new ConfigSnapshot
            {
                Compression = cfg.CombatBalance.RollFeelVarianceCompression,
                CritMult = cfg.CombatBalance.CriticalHitDamageMultiplier,
                ComboMult = cfg.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier,
                BasicMult = cfg.CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier,
                PlayerArmor = cfg.Combat.PlayerBaseArmor,
                EnemyArmorMult = cfg.EnemySystem.GlobalMultipliers.ArmorMultiplier,
                DamageCap = cfg.Combat.MaximumDamageCap,
                MinDamage = cfg.Combat.MinimumDamage,
                ArmorReductionFactor = cfg.Combat.ArmorReductionFactor
            };
        }

        private static void RestoreSnapshot(GameConfiguration cfg, ConfigSnapshot snap)
        {
            cfg.CombatBalance.RollFeelVarianceCompression = snap.Compression;
            cfg.CombatBalance.CriticalHitDamageMultiplier = snap.CritMult;
            cfg.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier = snap.ComboMult;
            cfg.CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier = snap.BasicMult;
            cfg.Combat.PlayerBaseArmor = snap.PlayerArmor;
            cfg.EnemySystem.GlobalMultipliers.ArmorMultiplier = snap.EnemyArmorMult;
            cfg.Combat.MaximumDamageCap = snap.DamageCap;
            cfg.Combat.MinimumDamage = snap.MinDamage;
            cfg.Combat.ArmorReductionFactor = snap.ArmorReductionFactor;
        }
    }
}
