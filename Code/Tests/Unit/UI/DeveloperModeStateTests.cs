using System;
using RPGGame;
using RPGGame.Config;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Verifies runtime combat speed state and the center-panel fast-pacing tint.
    /// </summary>
    public static class DeveloperModeStateTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DeveloperModeState Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestCombatSpeedLadder();
            TestDelayScaling();
            TestTutorialCombatSlowPacing();
            TestCenterPanelTintChangesWithCombatSpeed();
            TestSimulationPacingFastMode();
            TestNarrativeCombatLogDefaultOff();
            TestNarrativeCombatLogToggle();
            TestNarrativeVideoFeedDefaultOff();
            TestNarrativeVideoFeedToggle();
            TestDistortionEffectsDefaultOn();
            TestDistortionEffectsToggle();
            TestZeroDelayDevModeDefaultOff();
            TestZeroDelayDevModeSkipsDelays();

            TestBase.PrintSummary("DeveloperModeState Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestDistortionEffectsDefaultOn()
        {
            Console.WriteLine("--- F6 distortion effects default on ---");
            bool prev = DeveloperModeState.AreDistortionEffectsEnabled;
            try
            {
                DeveloperModeState.SetDistortionEffectsEnabled(true);
                TestBase.AssertTrue(DeveloperModeState.AreDistortionEffectsEnabled,
                    "distortion defaults / resets on", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetDistortionEffectsEnabled(prev);
            }
        }

        private static void TestDistortionEffectsToggle()
        {
            Console.WriteLine("--- F6 distortion effects toggles runtime flag ---");
            bool prev = DeveloperModeState.AreDistortionEffectsEnabled;
            try
            {
                DeveloperModeState.SetDistortionEffectsEnabled(true);
                TestBase.AssertTrue(DeveloperModeState.AreDistortionEffectsEnabled,
                    "starts on", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!DeveloperModeState.ToggleDistortionEffects(),
                    "toggle turns off", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!DeveloperModeState.AreDistortionEffectsEnabled,
                    "flag is off", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.ToggleDistortionEffects(),
                    "toggle turns on", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetDistortionEffectsEnabled(prev);
            }
        }

        private static void TestNarrativeVideoFeedDefaultOff()
        {
            Console.WriteLine("--- F5 narrative video feed default off ---");
            bool prev = DeveloperModeState.IsNarrativeVideoFeedEnabled;
            try
            {
                DeveloperModeState.SetNarrativeVideoFeedEnabled(false);
                TestBase.AssertTrue(!DeveloperModeState.IsNarrativeVideoFeedEnabled,
                    "video feed defaults / resets off", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeVideoFeedEnabled(prev);
            }
        }

        private static void TestNarrativeVideoFeedToggle()
        {
            Console.WriteLine("--- F5 narrative video feed toggles runtime flag ---");
            bool prev = DeveloperModeState.IsNarrativeVideoFeedEnabled;
            try
            {
                DeveloperModeState.SetNarrativeVideoFeedEnabled(false);
                TestBase.AssertTrue(!DeveloperModeState.IsNarrativeVideoFeedEnabled,
                    "starts off", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.ToggleNarrativeVideoFeed(),
                    "toggle turns on", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.IsNarrativeVideoFeedEnabled,
                    "flag is on", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!DeveloperModeState.ToggleNarrativeVideoFeed(),
                    "toggle turns off", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeVideoFeedEnabled(prev);
            }
        }

        private static void TestNarrativeCombatLogDefaultOff()
        {
            Console.WriteLine("--- F7 narrative combat log default off ---");
            bool prev = DeveloperModeState.IsNarrativeCombatLog;
            try
            {
                DeveloperModeState.SetNarrativeCombatLog(false);
                TestBase.AssertTrue(!DeveloperModeState.IsNarrativeCombatLog,
                    "narrative combat log defaults / resets off", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prev);
            }
        }

        private static void TestNarrativeCombatLogToggle()
        {
            Console.WriteLine("--- F7 narrative combat log toggles runtime flag ---");
            bool prev = DeveloperModeState.IsNarrativeCombatLog;
            try
            {
                DeveloperModeState.SetNarrativeCombatLog(false);
                TestBase.AssertTrue(!DeveloperModeState.IsNarrativeCombatLog,
                    "starts off", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.ToggleNarrativeCombatLog(),
                    "toggle turns on", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.IsNarrativeCombatLog,
                    "flag is on", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!DeveloperModeState.ToggleNarrativeCombatLog(),
                    "toggle turns off", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prev);
            }
        }

        private static void TestCombatSpeedLadder()
        {
            Console.WriteLine("--- Combat speed ladder steps through supported values ---");

            double prevSpeed = DeveloperModeState.CombatSpeedMultiplier;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetCombatSpeedMultiplier(1);

                TestBase.AssertEqual(1, DeveloperModeState.CombatSpeedMultiplier,
                    "combat speed starts at 1x when set to the first ladder step",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up step moves 1x to 2x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(5, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up step moves 2x to 5x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(20, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up step moves 5x to 20x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(20, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up stays capped at 20x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(5, DeveloperModeState.DecreaseCombatSpeed(),
                    "Page Down step moves 20x to 5x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                DeveloperModeState.SetCombatSpeedMultiplier(1);
                TestBase.AssertEqual(0.5, DeveloperModeState.DecreaseCombatSpeed(),
                    "Page Down step moves 1x to 0.5x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0.25, DeveloperModeState.DecreaseCombatSpeed(),
                    "Page Down step moves 0.5x to 0.25x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0.0, DeveloperModeState.DecreaseCombatSpeed(),
                    "Page Down step moves 0.25x to pause",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.IsCombatPaused,
                    "bottom step pauses combat",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0.0, DeveloperModeState.DecreaseCombatSpeed(),
                    "Page Down stays at pause",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0.25, DeveloperModeState.IncreaseCombatSpeed(),
                    "Page Up resumes from pause at 0.25x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                DeveloperModeState.SetCombatSpeedMultiplier(0.5);
                TestBase.AssertEqual(6000, DeveloperModeState.ScaleDelayMs(3000),
                    "0.5x speed (on the 1x baseline) plays a 3000 ms delay at 2x length",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                DeveloperModeState.SetCombatSpeedMultiplier(prevSpeed);
            }
        }

        private static void TestDelayScaling()
        {
            Console.WriteLine("--- Combat speed scales delays without making them instant ---");

            double prevSpeed = DeveloperModeState.CombatSpeedMultiplier;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetCombatSpeedMultiplier(5);

                TestBase.AssertEqual(200, DeveloperModeState.ScaleDelayMs(1000),
                    "5x speed (on the 1x baseline) scales 1000 ms to 200 ms",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, DeveloperModeState.ScaleDelayMs(1),
                    "non-zero scaled delays keep a one millisecond floor",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                DeveloperModeState.SetCombatLogInstant(true);
                TestBase.AssertEqual(0, DeveloperModeState.ScaleDelayMs(1000),
                    "legacy instant mode still zeroes delays when explicitly enabled",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                DeveloperModeState.SetCombatSpeedMultiplier(prevSpeed);
            }
        }

        private static void TestTutorialCombatSlowPacing()
        {
            Console.WriteLine("--- Tutorial combat pacing doubles default delay length ---");

            double prevSpeed = DeveloperModeState.CombatSpeedMultiplier;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevTutorialPacing = PreWeaponTrainingFlow.IsTutorialCombatSlowPacingActive;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetCombatSpeedMultiplier(1);
                PreWeaponTrainingFlow.SetTutorialCombatSlowPacing(false);

                TestBase.AssertEqual(3000, DeveloperModeState.ScaleDelayMs(3000),
                    "default 1x speed plays a 3000 ms combat action delay at 3000 ms",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                PreWeaponTrainingFlow.SetTutorialCombatSlowPacing(true);

                int expectedTutorial = (int)Math.Ceiling(3000 * TextDelayConfiguration.GetTutorialCombatDelayMultiplier());
                TestBase.AssertEqual(expectedTutorial, DeveloperModeState.ScaleDelayMs(3000),
                    "tutorial combat pacing applies the configured delay multiplier",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                PreWeaponTrainingFlow.SetTutorialCombatSlowPacing(prevTutorialPacing);
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                DeveloperModeState.SetCombatSpeedMultiplier(prevSpeed);
            }
        }

        private static void TestCenterPanelTintChangesWithCombatSpeed()
        {
            Console.WriteLine("--- Center panel tint changes when combat speed is accelerated ---");

            double prevSpeed = DeveloperModeState.CombatSpeedMultiplier;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetCombatSpeedMultiplier(1);
                var normal = CenterPanelModeTint.GetBackgroundColor();

                DeveloperModeState.SetCombatSpeedMultiplier(2);
                var fast = CenterPanelModeTint.GetBackgroundColor();

                TestBase.AssertTrue(!normal.Equals(fast),
                    "center panel background should change when combat speed is above 1x",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(fast.B > fast.R,
                    "accelerated combat tint should shift the dark panel toward blue/purple",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                DeveloperModeState.SetCombatSpeedMultiplier(prevSpeed);
            }
        }

        private static void TestSimulationPacingFastMode()
        {
            Console.WriteLine("--- SimulationPacing disables combat delays ---");

            bool prevUiDelays = UIManager.EnableDelays;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevFastCombat = GameSettings.Instance.FastCombat;
            bool prevFastLatch = SimulationPacing.IsFastModeEnabled;
            bool prevMute = CombatManager.DisableCombatUIOutput;
            bool prevMcp = RPGGame.MCP.MCPMode.IsActive;
            bool prevTextDelays = GameSettings.Instance.EnableTextDisplayDelays;
            double prevCombatSpeed = GameSettings.Instance.CombatSpeed;
            try
            {
                UIManager.EnableDelays = true;
                DeveloperModeState.SetCombatLogInstant(false);
                GameSettings.Instance.FastCombat = false;
                SimulationPacing.ResetFastModeLatchForTests();
                CombatManager.DisableCombatUIOutput = false;
                RPGGame.MCP.MCPMode.IsActive = false;

                TestBase.AssertTrue(!SimulationPacing.ShouldSkipDelays,
                    "delays active before fast mode", ref _testsRun, ref _testsPassed, ref _testsFailed);

                SimulationPacing.EnableFastMode();

                TestBase.AssertTrue(SimulationPacing.ShouldSkipDelays,
                    "fast mode skips delays", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.IsCombatLogInstant,
                    "fast mode enables instant combat log", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                UIManager.EnableDelays = prevUiDelays;
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                GameSettings.Instance.FastCombat = prevFastCombat;
                GameSettings.Instance.EnableTextDisplayDelays = prevTextDelays;
                GameSettings.Instance.CombatSpeed = prevCombatSpeed;
                CombatManager.DisableCombatUIOutput = prevMute;
                RPGGame.MCP.MCPMode.IsActive = prevMcp;
                if (!prevFastLatch)
                    SimulationPacing.ResetFastModeLatchForTests();
            }
        }

        private static void TestZeroDelayDevModeDefaultOff()
        {
            Console.WriteLine("--- F10 zero-delay dev mode default off ---");
            bool prev = DeveloperModeState.IsZeroDelayDevMode;
            try
            {
                DeveloperModeState.SetZeroDelayDevMode(false);
                TestBase.AssertTrue(!DeveloperModeState.IsZeroDelayDevMode,
                    "zero-delay dev mode defaults / resets off", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetZeroDelayDevMode(prev);
            }
        }

        private static void TestZeroDelayDevModeSkipsDelays()
        {
            Console.WriteLine("--- F10 zero-delay dev mode skips gameplay delays ---");
            bool prevDev = DeveloperModeState.IsZeroDelayDevMode;
            DeveloperModeState.SetZeroDelayDevMode(false);
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            double prevSpeed = DeveloperModeState.CombatSpeedMultiplier;
            bool prevTutorial = PreWeaponTrainingFlow.IsTutorialCombatSlowPacingActive;
            bool prevUiDelays = UIManager.EnableDelays;
            bool prevFastCombat = GameSettings.Instance.FastCombat;
            bool prevMute = CombatManager.DisableCombatUIOutput;
            bool prevMcp = RPGGame.MCP.MCPMode.IsActive;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetCombatSpeedMultiplier(1);
                PreWeaponTrainingFlow.SetTutorialCombatSlowPacing(false);
                UIManager.EnableDelays = true;
                GameSettings.Instance.FastCombat = false;
                CombatManager.DisableCombatUIOutput = false;
                RPGGame.MCP.MCPMode.IsActive = false;

                TestBase.AssertTrue(DeveloperModeState.ToggleZeroDelayDevMode(),
                    "toggle turns dev mode on", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.IsZeroDelayDevMode,
                    "flag is on", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(DeveloperModeState.IsCombatLogInstant,
                    "dev mode reports instant combat log", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, DeveloperModeState.ScaleDelayMs(3000),
                    "dev mode zeroes a 3000 ms delay", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(SimulationPacing.ShouldSkipDelays,
                    "room and reward pacing skips while dev mode is on", ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(!DeveloperModeState.ToggleZeroDelayDevMode(),
                    "toggle turns dev mode off", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!DeveloperModeState.IsCombatLogInstant,
                    "instant latch stays off after dev mode", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(3000, DeveloperModeState.ScaleDelayMs(3000),
                    "delays return at 1x after dev mode", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                DeveloperModeState.SetZeroDelayDevMode(prevDev);
                DeveloperModeState.SetCombatSpeedMultiplier(prevSpeed);
                PreWeaponTrainingFlow.SetTutorialCombatSlowPacing(prevTutorial);
                UIManager.EnableDelays = prevUiDelays;
                GameSettings.Instance.FastCombat = prevFastCombat;
                CombatManager.DisableCombatUIOutput = prevMute;
                RPGGame.MCP.MCPMode.IsActive = prevMcp;
            }
        }
    }
}
