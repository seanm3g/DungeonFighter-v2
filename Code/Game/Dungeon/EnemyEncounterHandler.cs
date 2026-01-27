namespace RPGGame
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Avalonia.Media;
    using Avalonia.Threading;
    using RPGGame.GameCore.Helpers;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Handles individual enemy encounters during dungeon runs
    /// Extracted from DungeonRunnerManager to separate enemy encounter logic
    /// </summary>
    public class EnemyEncounterHandler
    {
        private readonly GameStateManager stateManager;
        private readonly CombatManager? combatManager;
        private readonly IUIManager? customUIManager;
        private readonly DungeonDisplayManager displayManager;
        private readonly Action<Character>? onPlayerDeath;

        public EnemyEncounterHandler(
            GameStateManager stateManager,
            CombatManager? combatManager,
            IUIManager? customUIManager,
            DungeonDisplayManager displayManager,
            Action<Character>? onPlayerDeath = null)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.combatManager = combatManager;
            this.customUIManager = customUIManager;
            this.displayManager = displayManager ?? throw new ArgumentNullException(nameof(displayManager));
            this.onPlayerDeath = onPlayerDeath;
        }

        /// <summary>
        /// Process a single enemy encounter
        /// </summary>
        public async Task<bool> ProcessEnemyEncounter(Enemy enemy, bool playerGetsFirstAttack = false, bool enemyGetsFirstAttack = false)
        {
            if (stateManager.CurrentPlayer == null || combatManager == null) return false;
            
            var player = stateManager.CurrentPlayer;
            var room = stateManager.CurrentRoom;
            
            // Check if this character is currently active (for background combat support)
            bool isCharacterActive = IsCharacterActive(player);
            
            // Start enemy encounter using unified display manager
            displayManager.StartEnemyEncounter(enemy);
            
            // Show surprise message after enemy appears (if applicable)
            if (enemyGetsFirstAttack)
            {
                var random = new Random();
                var surpriseMessages = new[]
                {
                    "You've been surprised! The enemy will strike first!",
                    "The enemy catches you off guard! They attack first!",
                    "You're caught unaware! The enemy gains the first strike!",
                    "The enemy has the element of surprise! They act first!",
                    "You're taken by surprise! The enemy strikes first!"
                };
                displayManager.AddCombatEvent("", player);
                displayManager.AddCombatEvent(surpriseMessages[random.Next(surpriseMessages.Length)], player);
                displayManager.AddCombatEvent("", player);
            }
            
            // Only render UI if this character is currently active
            // This allows combat to run in the background without interrupting menus or other character views
            if (isCharacterActive)
            {
                // Reset for new battle
                if (customUIManager is CanvasUICoordinator canvasUISetup)
                {
                    canvasUISetup.ResetForNewBattle();
                }
                
                // Render enemy encounter screen to show enemy information after room info
                // This ensures the enemy encounter information is visible before combat starts
                if (customUIManager is CanvasUICoordinator canvasUIEnemy)
                {
                    canvasUIEnemy.RenderEnemyEncounter(enemy, player, displayManager.DungeonContext, 
                        stateManager.CurrentDungeon?.Name, stateManager.CurrentRoom?.Name);
                    // Brief delay to show enemy encounter information
                    await Task.Delay(2000);
                }
                
                // Initial render of combat screen with structured content
                // This sets up the layout and enables structured combat mode
                if (customUIManager is CanvasUICoordinator canvasUIInitial)
                {
                    canvasUIInitial.RenderCombat(player, enemy, displayManager.CombatLog);
                }
            }
            
            // Create debouncer for UI updates (only if character is active)
            CombatEventDebouncer? debouncer = null;
            if (isCharacterActive && customUIManager is CanvasUICoordinator canvasUI)
            {
                debouncer = new CombatEventDebouncer(200, () =>
                {
                    // Double-check character is still active before rendering
                    bool stillActive = IsCharacterActive(player);
                    if (stillActive)
                    {
                        canvasUI.RenderCombat(player, enemy, displayManager.CombatLog);
                    }
                });
                displayManager.CombatEventAdded += debouncer.TriggerRefresh;
            }
            
            bool playerWon = false;
            try
            {
                playerWon = await Task.Run(async () => await combatManager.RunCombat(player, enemy, room!, playerGetsFirstAttack, enemyGetsFirstAttack));
            }
            finally
            {
                if (debouncer != null)
                {
                    displayManager.CombatEventAdded -= debouncer.TriggerRefresh;
                    debouncer.Dispose();
                }
            }
            
            if (!playerWon)
            {
                // Player died - transition to death screen
                if (stateManager.CurrentPlayer != null)
                {
                    // Delete save file when character dies
                    Character.DeleteSaveFile();
                    
                    stateManager.TransitionToState(GameState.Death);
                    onPlayerDeath?.Invoke(stateManager.CurrentPlayer);
                }
                return false;
            }
            
            // Record enemy defeat in session statistics
            if (player != null)
            {
                player.RecordEnemyDefeat();
                
                // Award XP for enemy kill
                if (enemy != null)
                {
                    Progression.XPRewardSystem.AwardEnemyKillXP(player, enemy);
                }
            }
            
            // Enemy defeated - add victory message with proper spacing BEFORE final render
            // This ensures the victory message is included in the final render and prevents overlapping text
            if (enemy != null)
            {
                // Add blank line for spacing after informational summary
                displayManager.AddCombatEvent("");
                displayManager.AddCombatEvent("");
                var victoryBuilder = new ColoredTextBuilder();
                victoryBuilder.Add(enemy.Name, EntityColorHelper.GetEnemyColor(enemy));
                victoryBuilder.Add(" has been defeated!", ColorPalette.Success);
                displayManager.AddCombatEvent(ColoredTextRenderer.RenderAsMarkup(victoryBuilder.Build()));
                
                // Add remaining health right after defeat message
                if (player != null)
                {
                    string healthMsg = string.Format(AsciiArtAssets.UIText.RemainingHealth, 
                        player.CurrentHealth, player.GetEffectiveMaxHealth());
                    var healthBuilder = new ColoredTextBuilder();
                    healthBuilder.Add(healthMsg, ColorPalette.Gold);
                    displayManager.AddCombatEvent(ColoredTextRenderer.RenderAsMarkup(healthBuilder.Build()));
                }
            }
            
            // Wait for any reactive renders triggered by AddCombatEvent to complete before final render
            // This prevents text from overlapping when multiple renders happen in quick succession
            if (!RPGGame.MCP.MCPMode.IsActive)
            {
                await Task.Delay(250);
            }
            
            // Final refresh to show complete combat log including victory message (use Post to avoid blocking)
            // This single render will replace any previous renders and show the complete state
            // Only render if character is still active
            if (isCharacterActive && customUIManager is CanvasUICoordinator canvasUI4 && enemy != null && player != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // Double-check character is still active before rendering
                    if (IsCharacterActive(player))
                    {
                        canvasUI4.RenderCombat(player, enemy, displayManager.CombatLog);
                    }
                });
                // Small delay to ensure the final render completes before continuing
                if (!RPGGame.MCP.MCPMode.IsActive)
                {
                    await Task.Delay(100);
                }
            }
            
            // Remove the dead enemy from the room's enemy list
            // This prevents dead enemies from persisting and showing up during room transitions
            if (enemy != null && !enemy.IsAlive && room != null)
            {
                room.RemoveDeadEnemies();
            }
            
            // Clear enemy context after combat ends to prevent old enemies from showing during transitions
            if (isCharacterActive && customUIManager is CanvasUICoordinator canvasUIClear)
            {
                canvasUIClear.ClearCurrentEnemy();
            }
            
            // Small delay before next
            if (!RPGGame.MCP.MCPMode.IsActive)
            {
                await Task.Delay(1000);
            }
            return true; // Player survived this encounter
        }
        
        /// <summary>
        /// Checks if the given character is currently active and should display combat.
        /// Returns true if the character is the active character and we're not in a menu state.
        /// </summary>
        private bool IsCharacterActive(Character character)
        {
            if (character == null) return false;
            
            // Check if we're in a menu state - don't show combat in menus
            var currentState = stateManager.CurrentState;
            bool isMenuState = currentState == GameState.MainMenu ||
                             currentState == GameState.Inventory ||
                             currentState == GameState.CharacterInfo ||
                             currentState == GameState.Settings ||
                             currentState == GameState.DeveloperMenu ||
                             currentState == GameState.Testing ||
                             currentState == GameState.DungeonSelection ||
                             currentState == GameState.GameLoop ||
                             currentState == GameState.CharacterCreation ||
                             currentState == GameState.WeaponSelection ||
                             currentState == GameState.DungeonCompletion ||
                             currentState == GameState.Death ||
                             currentState == GameState.BattleStatistics ||
                             currentState == GameState.VariableEditor ||
                             currentState == GameState.TuningParameters ||
                             currentState == GameState.ActionEditor ||
                             currentState == GameState.CreateAction ||
                             currentState == GameState.ViewAction ||
                             currentState == GameState.CharacterSelection;
            
            if (isMenuState)
            {
                return false;
            }
            
            // Check if character matches active character (for multi-character support)
            var activeCharacter = stateManager.GetActiveCharacter();
            bool isActive = activeCharacter == character;
            
            return isActive;
        }
    }
}

