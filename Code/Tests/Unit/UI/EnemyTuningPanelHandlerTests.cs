using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>Spawn-weight and progression behavior backing EnemyTuningPanelHandler.</summary>
    public static class EnemyTuningPanelHandlerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyTuningPanelHandler Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRawSpawnWeights_PersistWithoutNormalization();
            TestSavePath_NormalizesSpawnWeightsTo100();
            TestProgressionScales_RoundTripOnConfig();

            TestBase.PrintSummary("EnemyTuningPanelHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRawSpawnWeights_PersistWithoutNormalization()
        {
            Console.WriteLine("--- Raw spawn weights stay unchanged until sanitize ---");
            var weights = new EnemySpawnTierWeightsConfig
            {
                CommonPercent = 40,
                UncommonBiomePercent = 20,
                UncommonRegionPercent = 10,
                UncommonLocationPercent = 0,
                RareLocationPercent = 0,
                AnywherePercent = 0
            };

            TestBase.AssertEqual(70, weights.TotalPercent,
                "Raw total reflects entered values",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(40, weights.CommonPercent,
                "Common percent unchanged before save sanitize",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSavePath_NormalizesSpawnWeightsTo100()
        {
            Console.WriteLine("--- Save sanitize normalizes spawn weights ---");
            var sys = GameConfiguration.Instance.EnemySystem ??= new EnemySystemConfig();
            sys.SpawnTierWeightsBySettlement ??= new EnemySpawnTierWeightsBySettlementConfig();
            var rural = sys.SpawnTierWeightsBySettlement.Rural;
            int savedCommon = rural.CommonPercent;
            int savedBiome = rural.UncommonBiomePercent;
            int savedRegion = rural.UncommonRegionPercent;
            int savedLocation = rural.UncommonLocationPercent;
            int savedRare = rural.RareLocationPercent;
            int savedAnywhere = rural.AnywherePercent;

            try
            {
                rural.CommonPercent = 40;
                rural.UncommonBiomePercent = 20;
                rural.UncommonRegionPercent = 10;
                rural.UncommonLocationPercent = 0;
                rural.RareLocationPercent = 0;
                rural.AnywherePercent = 0;

                sys.EnsureSanitizedDefaults();

                TestBase.AssertEqual(100, rural.TotalPercent,
                    "Rural weights total 100 after save sanitize",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                rural.CommonPercent = savedCommon;
                rural.UncommonBiomePercent = savedBiome;
                rural.UncommonRegionPercent = savedRegion;
                rural.UncommonLocationPercent = savedLocation;
                rural.RareLocationPercent = savedRare;
                rural.AnywherePercent = savedAnywhere;
                sys.EnsureSanitizedDefaults();
            }
        }

        private static void TestProgressionScales_RoundTripOnConfig()
        {
            Console.WriteLine("--- Progression scales round-trip on enemy system ---");
            var sys = GameConfiguration.Instance.EnemySystem ??= new EnemySystemConfig();
            sys.ProgressionScales ??= new EnemyProgressionScalesConfig();
            double savedBase = sys.ProgressionScales.BaseHealthScale;
            double savedGrowth = sys.ProgressionScales.HealthGrowthScale;
            double savedAttr = sys.ProgressionScales.AttributeGrowthScale;

            try
            {
                sys.ProgressionScales.BaseHealthScale = 1.25;
                sys.ProgressionScales.HealthGrowthScale = 0.9;
                sys.ProgressionScales.AttributeGrowthScale = 1.1;
                sys.EnsureSanitizedDefaults();

                TestBase.AssertTrue(Math.Abs(sys.ProgressionScales.BaseHealthScale - 1.25) < 0.001,
                    "Base health scale persisted", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(sys.ProgressionScales.HealthGrowthScale - 0.9) < 0.001,
                    "Health growth scale persisted", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(Math.Abs(sys.ProgressionScales.AttributeGrowthScale - 1.1) < 0.001,
                    "Attribute growth scale persisted", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                sys.ProgressionScales.BaseHealthScale = savedBase;
                sys.ProgressionScales.HealthGrowthScale = savedGrowth;
                sys.ProgressionScales.AttributeGrowthScale = savedAttr;
            }
        }
    }
}
