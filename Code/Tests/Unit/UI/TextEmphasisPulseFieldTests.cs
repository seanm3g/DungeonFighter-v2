using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Effects;

namespace RPGGame.Tests.Unit.UI
{
    public static class TextEmphasisPulseFieldTests
    {
        private static int _testsRun, _testsPassed, _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== TextEmphasisPulseField Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            bool prevDistortion = DeveloperModeState.AreDistortionEffectsEnabled;
            try
            {
                DeveloperModeState.SetDistortionEffectsEnabled(true);

                TestTriggerMakesFieldActive();
                TestExcludeRectSamplesZero();
                TestNeighborsHaveOutwardOffset();
                TestReformsToRest();
                TestDisabledNoOp();
                TestRuntimeDistortionOffNoOp();
            }
            finally
            {
                DeveloperModeState.SetDistortionEffectsEnabled(prevDistortion);
            }

            TestBase.PrintSummary("TextEmphasisPulseField Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static TextEmphasisPulseConfig BaseCfg() => new()
        {
            Enabled = true,
            ImpulseRadiusCells = 8,
            MaxOffsetFraction = 0.85,
            OutSeconds = 0.18,
            ReformSeconds = 0.35,
            VerticalScale = 1.0,
            ChromaticVelocitySeconds = 0.08
        };

        private static void TriggerNearOrigin(TextEmphasisPulseField field, double now = 0)
        {
            // Exclude rect covers grid cell (5,2) glyph 0 roughly — center ~55x, 40y with 10x16 cells
            field.SetTestClockSeconds(now);
            field.Trigger(
                originX: 55,
                originY: 40,
                excludeMinX: 50,
                excludeMaxX: 90,
                excludeMinY: 32,
                excludeMaxY: 48,
                charWidth: 10,
                charHeight: 16,
                nowOverrideSeconds: now);
        }

        private static void TestTriggerMakesFieldActive()
        {
            Console.WriteLine("--- Trigger makes field active ---");
            var field = new TextEmphasisPulseField();
            field.ApplyConfig(BaseCfg());
            TriggerNearOrigin(field);
            TestBase.AssertTrue(field.IsActive, "active after trigger", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(field.IsAliveForTests(), "alive after trigger", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExcludeRectSamplesZero()
        {
            Console.WriteLine("\n--- Exclude rect samples zero ---");
            var field = new TextEmphasisPulseField();
            field.ApplyConfig(BaseCfg());
            TriggerNearOrigin(field);
            field.TickForTests(0.09); // mid out-phase
            // Glyph at grid 5,2 index 0 → center (55, 40) inside exclude
            var (ox, oy) = field.SampleOffset(5, 2, 0, 10, 16);
            TestBase.AssertTrue(Math.Abs(ox) < 1e-6 && Math.Abs(oy) < 1e-6,
                $"excluded glyph should be rigid (ox={ox}, oy={oy})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNeighborsHaveOutwardOffset()
        {
            Console.WriteLine("\n--- Neighbors have outward offset ---");
            var field = new TextEmphasisPulseField();
            field.ApplyConfig(BaseCfg());
            TriggerNearOrigin(field);
            field.TickForTests(0.09);
            // Glyph to the right of exclude (grid 10 → center x=105)
            var (ox, oy) = field.SampleOffset(10, 2, 0, 10, 16);
            TestBase.AssertTrue(ox > 0.5,
                $"neighbor should push outward (ox={ox})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(oy) < Math.Abs(ox) + 1,
                "vertical scale keeps oy reasonable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReformsToRest()
        {
            Console.WriteLine("\n--- Reforms to rest ---");
            var field = new TextEmphasisPulseField();
            field.ApplyConfig(BaseCfg());
            TriggerNearOrigin(field);
            field.TickForTests(0.09);
            var (oxMid, _) = field.SampleOffset(10, 2, 0, 10, 16);
            TestBase.AssertTrue(oxMid > 0.5, "mid pulse has offset", ref _testsRun, ref _testsPassed, ref _testsFailed);

            field.TickForTests(0.60); // past out+reform
            TestBase.AssertTrue(!field.IsAliveForTests(),
                "inactive after reform",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var (oxEnd, oyEnd) = field.SampleOffset(10, 2, 0, 10, 16);
            TestBase.AssertTrue(Math.Abs(oxEnd) < 1e-6 && Math.Abs(oyEnd) < 1e-6,
                $"rest pose after reform (ox={oxEnd})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDisabledNoOp()
        {
            Console.WriteLine("\n--- Disabled ignores trigger ---");
            var field = new TextEmphasisPulseField();
            var cfg = BaseCfg();
            cfg.Enabled = false;
            field.ApplyConfig(cfg);
            TriggerNearOrigin(field);
            TestBase.AssertTrue(!field.IsActive,
                "disabled field stays inactive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRuntimeDistortionOffNoOp()
        {
            Console.WriteLine("\n--- F6 off ignores trigger ---");
            bool prev = DeveloperModeState.AreDistortionEffectsEnabled;
            try
            {
                DeveloperModeState.SetDistortionEffectsEnabled(false);
                var field = new TextEmphasisPulseField();
                field.ApplyConfig(BaseCfg());
                TriggerNearOrigin(field);
                TestBase.AssertTrue(!field.IsActive,
                    "F6-off field stays inactive",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetDistortionEffectsEnabled(prev);
            }
        }
    }
}
