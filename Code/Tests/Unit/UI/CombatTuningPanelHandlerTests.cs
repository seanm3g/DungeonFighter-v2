using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>Config and registry behavior backing CombatTuningPanelHandler.</summary>
    public static class CombatTuningPanelHandlerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatTuningPanelHandler Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestRegistry_LayerCountsMatchAllParameters();
            TestRegistry_ParameterRoundTripsThroughConfig();

            TestBase.PrintSummary("CombatTuningPanelHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRegistry_LayerCountsMatchAllParameters()
        {
            Console.WriteLine("--- Registry layer counts cover all parameters ---");
            int layerTotal = Enum.GetValues<CombatTuningLayer>()
                .Sum(layer => CombatTuningParameterRegistry.GetByLayer(layer).Count);
            TestBase.AssertEqual(CombatTuningParameterRegistry.All.Count, layerTotal,
                "Every registry parameter belongs to exactly one layer",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CombatTuningParameterRegistry.All.Count >= 30,
                "Combat tuning exposes curated parameter set",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRegistry_ParameterRoundTripsThroughConfig()
        {
            Console.WriteLine("--- Handler push target round-trips playerBaseHealth ---");
            var param = CombatTuningParameterRegistry.GetById("playerBaseHealth");
            var cfg = GameConfiguration.Instance;
            int saved = cfg.Character.PlayerBaseHealth;
            try
            {
                TestBase.AssertTrue(param != null, "playerBaseHealth exists", ref _testsRun, ref _testsPassed, ref _testsFailed);
                param!.SetValue(77);
                TestBase.AssertEqual(77, cfg.Character.PlayerBaseHealth,
                    "PushParameterFromControl target updates config",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                param.SetValue(saved);
                TestBase.AssertEqual(saved, param.GetValue(),
                    "ReloadSettings would read saved value",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = saved;
            }
        }
    }
}
