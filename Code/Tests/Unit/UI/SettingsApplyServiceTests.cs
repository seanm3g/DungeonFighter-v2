using System;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for SettingsApplyService. Verifies that ApplyAfterSave correctly triggers
    /// RefreshCurrentPlayerActionPool when actions were saved (Component 2).
    /// </summary>
    public static class SettingsApplyServiceTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SettingsApplyService Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplyAfterSave_WhenSuccessFalse_DoesNotThrow();
            TestApplyAfterSave_WhenActionsSavedFalse_DoesNotThrow();
            TestApplyAfterSave_WhenGameStateManagerNull_DoesNotThrow();
            TestApplyAfterSave_WhenCurrentPlayerNull_DoesNotThrow();
            TestApplyAfterSave_WhenActionsSaved_ResetsPlayerComboStep();

            TestBase.PrintSummary("SettingsApplyService Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestApplyAfterSave_WhenSuccessFalse_DoesNotThrow()
        {
            Console.WriteLine("--- ApplyAfterSave when Success is false does not throw ---");
            try
            {
                var result = new SettingsSaveResult(false, true, false);
                SettingsApplyService.ApplyAfterSave(result, new GameStateManager());
                TestBase.AssertTrue(true, "ApplyAfterSave with Success=false does not throw", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"ApplyAfterSave with Success=false threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyAfterSave_WhenActionsSavedFalse_DoesNotThrow()
        {
            Console.WriteLine("--- ApplyAfterSave when ActionsSaved is false does not throw ---");
            try
            {
                var result = new SettingsSaveResult(true, false, false);
                var stateManager = new GameStateManager();
                stateManager.SetCurrentPlayer(new Character("Test", 1));
                SettingsApplyService.ApplyAfterSave(result, stateManager);
                TestBase.AssertTrue(true, "ApplyAfterSave with ActionsSaved=false does not throw", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"ApplyAfterSave with ActionsSaved=false threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyAfterSave_WhenGameStateManagerNull_DoesNotThrow()
        {
            Console.WriteLine("--- ApplyAfterSave when GameStateManager is null does not throw ---");
            try
            {
                var result = new SettingsSaveResult(true, true, false);
                SettingsApplyService.ApplyAfterSave(result, null);
                TestBase.AssertTrue(true, "ApplyAfterSave with null GameStateManager does not throw", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"ApplyAfterSave with null GameStateManager threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyAfterSave_WhenCurrentPlayerNull_DoesNotThrow()
        {
            Console.WriteLine("--- ApplyAfterSave when CurrentPlayer is null does not throw ---");
            try
            {
                var result = new SettingsSaveResult(true, true, false);
                var stateManager = new GameStateManager();
                // CurrentPlayer not set, so null
                SettingsApplyService.ApplyAfterSave(result, stateManager);
                TestBase.AssertTrue(true, "ApplyAfterSave with null CurrentPlayer does not throw", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"ApplyAfterSave with null CurrentPlayer threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestApplyAfterSave_WhenActionsSaved_ResetsPlayerComboStep()
        {
            Console.WriteLine("--- ApplyAfterSave when ActionsSaved resets player ComboStep ---");
            try
            {
                var stateManager = new GameStateManager();
                var character = new Character("ApplyTest", 1);
                stateManager.SetCurrentPlayer(character);
                character.ComboStep = 5;

                var result = new SettingsSaveResult(true, true, false);
                SettingsApplyService.ApplyAfterSave(result, stateManager);

                TestBase.AssertEqual(0, character.ComboStep, "ComboStep is reset to 0 after ApplyAfterSave", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(character.ActionPool != null, "ActionPool is not null after refresh", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"ApplyAfterSave with ActionsSaved threw: {ex.Message}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
