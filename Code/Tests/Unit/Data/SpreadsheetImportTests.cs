using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for spreadsheet-imported actions
    /// Tests format detection, loading, conversion, and game mechanics compatibility
    /// </summary>
    public static class SpreadsheetImportTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all spreadsheet import tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Spreadsheet Import Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestFormatDetection();
            TestLoadingFromSpreadsheetFormat();
            TestConversionAccuracy();
            TestPropertyValidation();
            TestActionAttackBonuses();
            TestGameMechanicsCompatibility();

            TestBase.PrintSummary("Spreadsheet Import Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Format Detection Tests

        private static void TestFormatDetection()
        {
            Console.WriteLine("--- Testing Format Detection ---");

            // Find Actions.json file
            var possiblePaths = GameConstants.GetPossibleGameDataFilePaths(GameConstants.ActionsJson);
            string? filePath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    filePath = path;
                    break;
                }
            }

            if (filePath == null)
            {
                TestBase.AssertTrue(false,
                    "Actions.json file not found - cannot test format detection",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            // Read JSON content
            string jsonContent = File.ReadAllText(filePath);
            
            // Test detection logic
            bool isSpreadsheetFormat = DetectSpreadsheetFormat(jsonContent);
            
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(jsonContent),
                "JSON content should not be empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify format is detected (should be spreadsheet format if we just imported)
            Console.WriteLine($"  Detected format: {(isSpreadsheetFormat ? "Spreadsheet" : "Legacy")}");
            
            // Test that we can load regardless of format
            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();
            
            TestBase.AssertTrue(allActions.Count > 0,
                $"Actions should load successfully, got {allActions.Count} actions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static bool DetectSpreadsheetFormat(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return false;
            
            try
            {
                using (var doc = JsonDocument.Parse(jsonContent))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var firstElement = doc.RootElement[0];
                        if (firstElement.ValueKind == JsonValueKind.Object)
                        {
                            if (firstElement.TryGetProperty("action", out _))
                            {
                                return true;
                            }
                            if (firstElement.TryGetProperty("name", out _))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            
            return false;
        }

        #endregion

        #region Loading Tests

        private static void TestLoadingFromSpreadsheetFormat()
        {
            Console.WriteLine("\n--- Testing Loading from Spreadsheet Format ---");

            // Clear and reload actions
            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();

            TestBase.AssertTrue(allActions.Count > 0,
                $"Actions should be loaded, got {allActions.Count} actions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify expected actions from spreadsheet are present
            var expectedActions = new[] { "JAB", "PUNCH HARD", "PUNCH HARDER", "PUNCH HARDEST", "TAUNT", 
                "CONCENTRATE", "STUN", "CRITICAL ATTACK", "FLURRY", "AMPLIFY ACCURACY" };
            
            int foundCount = 0;
            foreach (var expectedAction in expectedActions)
            {
                if (ActionLoader.HasAction(expectedAction))
                {
                    foundCount++;
                }
            }

            TestBase.AssertTrue(foundCount >= 5,
                $"At least 5 expected actions should be found, found {foundCount}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine($"  Loaded {allActions.Count} actions total");
            Console.WriteLine($"  Found {foundCount}/{expectedActions.Length} expected actions");
        }

        #endregion

        #region Conversion Tests

        private static void TestConversionAccuracy()
        {
            Console.WriteLine("\n--- Testing Conversion Accuracy ---");

            // Test specific actions that should have been converted correctly
            var testActions = new[]
            {
                ("JAB", 0.33, 0.5), // Damage: 33%, Speed: 0.50
                ("PUNCH HARD", 1.0, 1.0), // Damage: 100%, Speed: 1.00
                ("PUNCH HARDER", 1.3, 1.0), // Damage: 130%, Speed: 1.00
            };

            int conversionSuccessCount = 0;
            foreach (var (actionName, expectedDamage, expectedSpeed) in testActions)
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    // Check damage multiplier (with tolerance for percentage conversion)
                    double damageDiff = Math.Abs(action.DamageMultiplier - expectedDamage);
                    bool damageCorrect = damageDiff < 0.01; // Allow small tolerance
                    
                    // Check speed/length
                    double speedDiff = Math.Abs(action.Length - expectedSpeed);
                    bool speedCorrect = speedDiff < 0.01;
                    
                    if (damageCorrect && speedCorrect)
                    {
                        conversionSuccessCount++;
                        Console.WriteLine($"  ✓ {actionName}: Damage={action.DamageMultiplier:F2}, Speed={action.Length:F2}");
                    }
                    else
                    {
                        Console.WriteLine($"  ✗ {actionName}: Expected Damage={expectedDamage:F2}, Speed={expectedSpeed:F2}, Got Damage={action.DamageMultiplier:F2}, Speed={action.Length:F2}");
                    }
                }
            }

            TestBase.AssertTrue(conversionSuccessCount >= 2,
                $"At least 2 actions should convert correctly, {conversionSuccessCount}/{testActions.Length} passed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Property Validation Tests

        private static void TestPropertyValidation()
        {
            Console.WriteLine("\n--- Testing Property Validation ---");

            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();

            int validActions = 0;
            int actionsWithNames = 0;
            int actionsWithValidDamage = 0;
            int actionsWithValidSpeed = 0;
            int actionsWithStatusEffects = 0;
            int actionsWithBonuses = 0;

            foreach (var action in allActions)
            {
                bool isValid = true;

                // Check name
                if (!string.IsNullOrEmpty(action.Name))
                {
                    actionsWithNames++;
                }
                else
                {
                    isValid = false;
                }

                // Check damage multiplier (should be >= 0)
                if (action.DamageMultiplier >= 0)
                {
                    actionsWithValidDamage++;
                }
                else
                {
                    isValid = false;
                }

                // Check speed/length (should be > 0)
                if (action.Length > 0)
                {
                    actionsWithValidSpeed++;
                }
                else
                {
                    isValid = false;
                }

                // Check status effects
                if (action.CausesStun || action.CausesPoison || action.CausesBurn || 
                    action.CausesBleed || action.CausesWeaken || action.CausesSlow ||
                    action.CausesVulnerability || action.CausesHarden || action.CausesExpose)
                {
                    actionsWithStatusEffects++;
                }

                // Check ACTION/ATTACK bonuses
                if (action.ActionAttackBonuses != null && 
                    action.ActionAttackBonuses.BonusGroups != null &&
                    action.ActionAttackBonuses.BonusGroups.Count > 0)
                {
                    actionsWithBonuses++;
                }

                if (isValid)
                {
                    validActions++;
                }
            }

            TestBase.AssertTrue(actionsWithNames == allActions.Count,
                $"All actions should have names, {actionsWithNames}/{allActions.Count} have names",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(actionsWithValidDamage == allActions.Count,
                $"All actions should have valid damage multipliers, {actionsWithValidDamage}/{allActions.Count} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(actionsWithValidSpeed == allActions.Count,
                $"All actions should have valid speed values, {actionsWithValidSpeed}/{allActions.Count} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine($"  Actions with names: {actionsWithNames}/{allActions.Count}");
            Console.WriteLine($"  Actions with valid damage: {actionsWithValidDamage}/{allActions.Count}");
            Console.WriteLine($"  Actions with valid speed: {actionsWithValidSpeed}/{allActions.Count}");
            Console.WriteLine($"  Actions with status effects: {actionsWithStatusEffects}");
            Console.WriteLine($"  Actions with ACTION/ATTACK bonuses: {actionsWithBonuses}");
        }

        #endregion

        #region ACTION/ATTACK Bonuses Tests

        private static void TestActionAttackBonuses()
        {
            Console.WriteLine("\n--- Testing ACTION/ATTACK Bonuses ---");

            // Test actions that should have bonuses from spreadsheet
            var bonusTestActions = new[]
            {
                "CONCENTRATE",
                "GRUNT",
                "AMPLIFY ACCURACY",
                "TRUE STRIKE",
                "AVENGE"
            };

            int actionsWithBonuses = 0;
            int bonusGroupsFound = 0;

            foreach (var actionName in bonusTestActions)
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    if (action.ActionAttackBonuses != null && 
                        action.ActionAttackBonuses.BonusGroups != null &&
                        action.ActionAttackBonuses.BonusGroups.Count > 0)
                    {
                        actionsWithBonuses++;
                        bonusGroupsFound += action.ActionAttackBonuses.BonusGroups.Count;
                        
                        // Log bonus details
                        foreach (var group in action.ActionAttackBonuses.BonusGroups)
                        {
                            Console.WriteLine($"  ✓ {actionName}: {group.Keyword} keyword, {group.Count} count, {group.Bonuses.Count} bonuses");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ✗ {actionName}: No bonuses found (may be expected if not in spreadsheet)");
                    }
                }
            }

            TestBase.AssertTrue(actionsWithBonuses >= 1,
                $"At least 1 action should have ACTION/ATTACK bonuses, found {actionsWithBonuses}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine($"  Actions with bonuses: {actionsWithBonuses}/{bonusTestActions.Length}");
            Console.WriteLine($"  Total bonus groups: {bonusGroupsFound}");
        }

        #endregion

        #region Game Mechanics Tests

        private static void TestGameMechanicsCompatibility()
        {
            Console.WriteLine("\n--- Testing Game Mechanics Compatibility ---");

            ActionLoader.LoadActions();
            
            // Test that actions can be retrieved
            var jabAction = ActionLoader.GetAction("JAB");
            TestBase.AssertNotNull(jabAction,
                "JAB action should be retrievable for game mechanics",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (jabAction != null)
            {
                // Test that action has required properties for execution
                TestBase.AssertTrue(jabAction.DamageMultiplier >= 0,
                    "JAB should have valid damage multiplier",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(jabAction.Length > 0,
                    "JAB should have valid length/speed",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test that action type is valid
                TestBase.AssertTrue(Enum.IsDefined(typeof(ActionType), jabAction.Type),
                    $"JAB should have valid ActionType: {jabAction.Type}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test that target type is valid
                TestBase.AssertTrue(Enum.IsDefined(typeof(TargetType), jabAction.Target),
                    $"JAB should have valid TargetType: {jabAction.Target}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test multiple actions for compatibility
            var testActionNames = new[] { "JAB", "PUNCH HARD", "STUN", "FLURRY" };
            int compatibleActions = 0;

            foreach (var actionName in testActionNames)
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null && 
                    action.DamageMultiplier >= 0 && 
                    action.Length > 0 &&
                    Enum.IsDefined(typeof(ActionType), action.Type) &&
                    Enum.IsDefined(typeof(TargetType), action.Target))
                {
                    compatibleActions++;
                }
            }

            TestBase.AssertTrue(compatibleActions >= 3,
                $"At least 3 actions should be compatible with game mechanics, {compatibleActions}/{testActionNames.Length} are compatible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine($"  Compatible actions: {compatibleActions}/{testActionNames.Length}");
        }

        #endregion
    }
}
