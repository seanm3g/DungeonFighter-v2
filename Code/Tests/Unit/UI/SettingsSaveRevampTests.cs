using System;
using System.Reflection;
using RPGGame;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for the settings save revamp: ensure save persists values and post-save UI shows saved state.
    /// Verifies SettingsSaveResult, in-memory consistency, and that the single GameSettings write contract is testable.
    /// </summary>
    public static class SettingsSaveRevampTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Settings Save Revamp Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSettingsSaveResult_SuccessReflectsConstructor();
            TestSettingsSaveResult_DefaultIsFailure();
            TestSettingsSaveResult_ActionsAndTextDelaysFlags();
            TestGameSettings_InMemoryUpdate_ReflectsInSameInstance();
            TestGameSettings_ValidateAndFix_DoesNotLoseValues();
            TestGameSettings_ActionStripFlashClamped();
            TestHandlerSaveOrder_ItemGenerationBeforeClasses();
            TestBalanceHandlerTags_CommitBeforeGeneralHandlers();
            TestEstimatePlayerMaxHealth_DoesNotMutateZeroConfig();

            TestBase.PrintSummary("Settings Save Revamp Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestSettingsSaveResult_SuccessReflectsConstructor()
        {
            Console.WriteLine("--- SettingsSaveResult.Success reflects constructor ---");
            var ok = new SettingsSaveResult(true, false, false);
            var fail = new SettingsSaveResult(false);
            TestBase.AssertTrue(ok.Success, "Success=true when constructed with true", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(fail.Success, "Success=false when constructed with false", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSettingsSaveResult_DefaultIsFailure()
        {
            Console.WriteLine("--- SettingsSaveResult default is failure ---");
            var result = default(SettingsSaveResult);
            TestBase.AssertFalse(result.Success, "default(SettingsSaveResult).Success is false", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSettingsSaveResult_ActionsAndTextDelaysFlags()
        {
            Console.WriteLine("--- SettingsSaveResult ActionsSaved and TextDelaysSaved ---");
            var result = new SettingsSaveResult(true, true, true);
            TestBase.AssertTrue(result.ActionsSaved, "ActionsSaved=true", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(result.TextDelaysSaved, "TextDelaysSaved=true", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGameSettings_InMemoryUpdate_ReflectsInSameInstance()
        {
            Console.WriteLine("--- GameSettings in-memory update reflects in same instance (no repopulation) ---");
            var settings = new GameSettings();
            settings.FastCombat = true;
            settings.ShowHealthBars = false;
            TestBase.AssertTrue(settings.FastCombat, "FastCombat remains true after set", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(settings.ShowHealthBars, "ShowHealthBars remains false after set", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGameSettings_ValidateAndFix_DoesNotLoseValues()
        {
            Console.WriteLine("--- GameSettings ValidateAndFix does not lose valid values ---");
            var settings = new GameSettings();
            settings.FastCombat = true;
            settings.ShowComboProgress = true;
            settings.ValidateAndFix();
            TestBase.AssertTrue(settings.FastCombat, "FastCombat still true after ValidateAndFix", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(settings.ShowComboProgress, "ShowComboProgress still true after ValidateAndFix", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGameSettings_ActionStripFlashClamped()
        {
            Console.WriteLine("--- GameSettings action strip flash durations clamped ---");
            var settings = new GameSettings
            {
                ActionStripMissFlashDurationMs = 50,
                ActionStripSuccessFlashDurationMs = 100,
                ActionStripSuccessFlashPulseHalfPeriodMs = 10
            };
            settings.ValidateAndFix();
            TestBase.AssertTrue(settings.ActionStripMissFlashDurationMs >= 200, "Miss flash clamped to min", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(settings.ActionStripSuccessFlashDurationMs >= 500, "Success total clamped to min", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(settings.ActionStripSuccessFlashPulseHalfPeriodMs >= 50, "Pulse half clamped to min", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Regression: ClassesPanelHandler calls <see cref="GameConfiguration.SaveToFile"/>; ItemGeneration must run first so
        /// <c>lootSystem.comboSequenceBaseMax</c> is applied from the Item Generation tab before that write.
        /// </summary>
        private static void TestHandlerSaveOrder_ItemGenerationBeforeClasses()
        {
            Console.WriteLine("--- SettingsSaveOrchestrator: ItemGeneration runs before Classes ---");
            var field = typeof(SettingsSaveOrchestrator).GetField(
                "HandlerSaveCategoryTags",
                BindingFlags.NonPublic | BindingFlags.Static);
            TestBase.AssertNotNull(field, "HandlerSaveCategoryTags field", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (field == null)
                return;
            var tags = (string[]?)field.GetValue(null);
            TestBase.AssertNotNull(tags, "handler tag list", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (tags == null)
                return;
            int ig = Array.IndexOf(tags, "ItemGeneration");
            int cl = Array.IndexOf(tags, "Classes");
            TestBase.AssertTrue(ig >= 0, "ItemGeneration present", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(cl >= 0, "Classes present", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ig < cl, "ItemGeneration before Classes", ref _testsRun, ref _testsPassed, ref _testsFailed);
            int au = Array.IndexOf(tags, "Audio");
            TestBase.AssertTrue(au >= 0, "Audio handler included so AudioConfig.json saves with settings", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBalanceHandlerTags_CommitBeforeGeneralHandlers()
        {
            Console.WriteLine("--- SettingsSaveOrchestrator: balance handlers have dedicated early-save pass ---");
            var balanceField = typeof(SettingsSaveOrchestrator).GetField(
                "BalanceHandlerTags",
                BindingFlags.NonPublic | BindingFlags.Static);
            TestBase.AssertNotNull(balanceField, "BalanceHandlerTags field", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (balanceField == null)
                return;

            var balanceTags = (string[]?)balanceField.GetValue(null);
            TestBase.AssertNotNull(balanceTags, "balance tag list", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (balanceTags == null)
                return;

            TestBase.AssertTrue(Array.IndexOf(balanceTags, "CombatTuning") >= 0,
                "CombatTuning in balance pass", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Array.IndexOf(balanceTags, "ItemGeneration") >= 0,
                "ItemGeneration in balance pass", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Array.IndexOf(balanceTags, "Classes") >= 0,
                "Classes in balance pass", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int ig = Array.IndexOf(balanceTags, "ItemGeneration");
            int cl = Array.IndexOf(balanceTags, "Classes");
            TestBase.AssertTrue(ig < cl, "balance pass: ItemGeneration before Classes", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEstimatePlayerMaxHealth_DoesNotMutateZeroConfig()
        {
            Console.WriteLine("--- EstimatePlayerMaxHealth does not rewrite zero config to 60 ---");
            var cfg = GameConfiguration.Instance;
            int saved = cfg.Character.PlayerBaseHealth;
            try
            {
                cfg.Character.PlayerBaseHealth = 0;
                int estimate = EnemyProgressionCurveEvaluator.EstimatePlayerMaxHealth(1);
                TestBase.AssertEqual(0, cfg.Character.PlayerBaseHealth,
                    "preview estimate must not mutate stored PlayerBaseHealth", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(CharacterConfig.RuntimeFallbackBaseHealth, estimate,
                    "preview uses read-only runtime fallback", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.Character.PlayerBaseHealth = saved;
            }
        }
    }
}
