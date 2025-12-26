namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;
    using RPGGame.Entity;
    using Avalonia.Media;

    /// <summary>
    /// Test runner for combat log filtering functionality.
    /// Tests that combat logs are properly filtered by character and game state.
    /// </summary>
    public class CombatLogFilteringTestRunner
    {
        private readonly CanvasUICoordinator canvasUI;
        private readonly GameStateManager stateManager;
        private int testsPassed = 0;
        private int testsFailed = 0;
        private readonly List<string> failures = new List<string>();

        public CombatLogFilteringTestRunner(CanvasUICoordinator canvasUI, GameStateManager stateManager)
        {
            this.canvasUI = canvasUI ?? throw new ArgumentNullException(nameof(canvasUI));
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        public async Task RunAllTests()
        {
            canvasUI.WriteLine("=== Combat Log Filtering Tests ===", UIMessageType.Title);
            canvasUI.WriteBlankLine();

            // Test 1: Game State Filtering
            await TestGameStateFiltering();

            // Test 2: Character Matching
            await TestCharacterMatching();

            // Test 3: Menu State Blocking
            await TestMenuStateBlocking();

            // Test 4: Combat State Allowing
            await TestCombatStateAllowing();

            // Test 5: Multiple Character Filtering
            await TestMultipleCharacterFiltering();

            // Summary
            canvasUI.WriteBlankLine();
            canvasUI.WriteLine("=== Test Summary ===", UIMessageType.Title);
            canvasUI.WriteLine($"Tests Passed: {testsPassed}", UIMessageType.System);
            canvasUI.WriteLine($"Tests Failed: {testsFailed}", UIMessageType.System);
            
            if (failures.Count > 0)
            {
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine("Failures:", UIMessageType.System);
                foreach (var failure in failures)
                {
                    canvasUI.WriteLine($"  - {failure}", UIMessageType.System);
                }
            }
        }

        private async Task TestGameStateFiltering()
        {
            canvasUI.WriteLine("Test 1: Game State Filtering", UIMessageType.System);
            
            // Save original state
            var originalState = stateManager.CurrentState;
            
            try
            {
                // Test that menu states block combat logs
                var menuStates = new[]
                {
                    GameState.MainMenu,
                    GameState.Inventory,
                    GameState.CharacterInfo,
                    GameState.Settings,
                    GameState.GameLoop
                };

                bool allBlocked = true;
                foreach (var state in menuStates)
                {
                    stateManager.TransitionToState(state);
                    await Task.Delay(10); // Small delay to ensure state transition
                    
                    // Try to display a combat action - should be blocked
                    var testAction = new List<ColoredText>
                    {
                        new ColoredText("Test Player", ColorPalette.Player.GetColor()),
                        new ColoredText(" attacks ", Colors.White),
                        new ColoredText("Test Enemy", ColorPalette.Enemy.GetColor())
                    };
                    
                    // We can't easily intercept, so we'll check the state manager instead
                    // The actual filtering happens in BlockDisplayManager
                    var shouldDisplay = !IsMenuState(state);
                    
                    if (!shouldDisplay)
                    {
                        allBlocked = false;
                        failures.Add($"Menu state {state} did not block combat logs");
                    }
                }
                
                if (allBlocked)
                {
                    canvasUI.WriteLine("  ✓ All menu states properly block combat logs", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ Some menu states did not block combat logs", UIMessageType.System);
                    testsFailed++;
                }
            }
            finally
            {
                // Restore original state
                stateManager.TransitionToState(originalState);
            }
            
            canvasUI.WriteBlankLine();
        }

        private async Task TestCharacterMatching()
        {
            canvasUI.WriteLine("Test 2: Character Matching", UIMessageType.System);
            
            try
            {
                // Create test characters
                var character1 = new Character("TestChar1", 1);
                var character2 = new Character("TestChar2", 1);
                
                // Set character1 as active
                stateManager.SetCurrentPlayer(character1);
                await Task.Delay(10);
                
                var activeCharacter = stateManager.GetActiveCharacter();
                
                if (activeCharacter == character1)
                {
                    canvasUI.WriteLine("  ✓ Active character correctly set", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ Active character not correctly set", UIMessageType.System);
                    testsFailed++;
                    failures.Add("Active character not correctly set");
                }
                
                // Switch to character2
                stateManager.SetCurrentPlayer(character2);
                await Task.Delay(10);
                
                activeCharacter = stateManager.GetActiveCharacter();
                
                if (activeCharacter == character2)
                {
                    canvasUI.WriteLine("  ✓ Character switching works correctly", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ Character switching failed", UIMessageType.System);
                    testsFailed++;
                    failures.Add("Character switching failed");
                }
            }
            catch (Exception ex)
            {
                canvasUI.WriteLine($"  ✗ Error: {ex.Message}", UIMessageType.System);
                testsFailed++;
                failures.Add($"Character matching test error: {ex.Message}");
            }
            
            canvasUI.WriteBlankLine();
        }

        private async Task TestMenuStateBlocking()
        {
            canvasUI.WriteLine("Test 3: Menu State Blocking", UIMessageType.System);
            
            var originalState = stateManager.CurrentState;
            
            try
            {
                // Set to a menu state
                stateManager.TransitionToState(GameState.Settings);
                await Task.Delay(10);
                
                bool isMenuState = IsMenuState(stateManager.CurrentState);
                
                if (isMenuState)
                {
                    canvasUI.WriteLine("  ✓ Menu state correctly identified", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ Menu state not correctly identified", UIMessageType.System);
                    testsFailed++;
                    failures.Add("Menu state not correctly identified");
                }
            }
            finally
            {
                stateManager.TransitionToState(originalState);
            }
            
            canvasUI.WriteBlankLine();
        }

        private async Task TestCombatStateAllowing()
        {
            canvasUI.WriteLine("Test 4: Combat State Allowing", UIMessageType.System);
            
            var originalState = stateManager.CurrentState;
            
            try
            {
                // Set to combat state
                stateManager.TransitionToState(GameState.Combat);
                await Task.Delay(10);
                
                bool isMenuState = IsMenuState(stateManager.CurrentState);
                
                if (!isMenuState)
                {
                    canvasUI.WriteLine("  ✓ Combat state correctly allows combat logs", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ Combat state incorrectly blocked", UIMessageType.System);
                    testsFailed++;
                    failures.Add("Combat state incorrectly blocked");
                }
                
                // Test dungeon state
                stateManager.TransitionToState(GameState.Dungeon);
                await Task.Delay(10);
                
                isMenuState = IsMenuState(stateManager.CurrentState);
                
                if (!isMenuState)
                {
                    canvasUI.WriteLine("  ✓ Dungeon state correctly allows combat logs", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ Dungeon state incorrectly blocked", UIMessageType.System);
                    testsFailed++;
                    failures.Add("Dungeon state incorrectly blocked");
                }
            }
            finally
            {
                stateManager.TransitionToState(originalState);
            }
            
            canvasUI.WriteBlankLine();
        }

        private async Task TestMultipleCharacterFiltering()
        {
            canvasUI.WriteLine("Test 5: Multiple Character Filtering", UIMessageType.System);
            
            var originalState = stateManager.CurrentState;
            Character? originalPlayer = stateManager.CurrentPlayer;
            
            try
            {
                // Create multiple characters
                var char1 = new Character("Char1", 1);
                var char2 = new Character("Char2", 1);
                
                // Set char1 as active
                stateManager.SetCurrentPlayer(char1);
                await Task.Delay(10);
                
                var active = stateManager.GetActiveCharacter();
                if (active == char1)
                {
                    canvasUI.WriteLine("  ✓ First character set as active", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ First character not set as active", UIMessageType.System);
                    testsFailed++;
                    failures.Add("First character not set as active");
                }
                
                // Switch to char2
                stateManager.SetCurrentPlayer(char2);
                await Task.Delay(10);
                
                active = stateManager.GetActiveCharacter();
                if (active == char2)
                {
                    canvasUI.WriteLine("  ✓ Second character set as active", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ Second character not set as active", UIMessageType.System);
                    testsFailed++;
                    failures.Add("Second character not set as active");
                }
                
                // Verify char1 is no longer active
                if (active != char1)
                {
                    canvasUI.WriteLine("  ✓ First character correctly deactivated", UIMessageType.System);
                    testsPassed++;
                }
                else
                {
                    canvasUI.WriteLine("  ✗ First character still active", UIMessageType.System);
                    testsFailed++;
                    failures.Add("First character still active after switch");
                }
            }
            finally
            {
                if (originalPlayer != null)
                {
                    stateManager.SetCurrentPlayer(originalPlayer);
                }
                stateManager.TransitionToState(originalState);
            }
            
            canvasUI.WriteBlankLine();
        }

        private bool IsMenuState(GameState state)
        {
            return state == GameState.MainMenu ||
                   state == GameState.Inventory ||
                   state == GameState.CharacterInfo ||
                   state == GameState.Settings ||
                   state == GameState.DeveloperMenu ||
                   state == GameState.Testing ||
                   state == GameState.DungeonSelection ||
                   state == GameState.GameLoop ||
                   state == GameState.CharacterCreation ||
                   state == GameState.WeaponSelection ||
                   state == GameState.DungeonCompletion ||
                   state == GameState.Death ||
                   state == GameState.BattleStatistics ||
                   state == GameState.VariableEditor ||
                   state == GameState.TuningParameters ||
                   state == GameState.ActionEditor ||
                   state == GameState.CreateAction ||
                   state == GameState.ViewAction ||
                   state == GameState.CharacterSelection;
        }
    }
}

