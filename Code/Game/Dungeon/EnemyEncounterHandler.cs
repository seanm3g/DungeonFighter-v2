namespace RPGGame
{
    using System;
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
                displayManager.AddCombatEvent("");
                displayManager.AddCombatEvent(surpriseMessages[random.Next(surpriseMessages.Length)]);
            }
            
            // Reset for new battle
            if (customUIManager is CanvasUICoordinator canvasUISetup)
            {
                canvasUISetup.ResetForNewBattle();
            }
            
            // Render enemy encounter screen to show enemy information after room info
            // This ensures the enemy encounter information is visible before combat starts
            if (customUIManager is CanvasUICoordinator canvasUIEnemy)
            {
                canvasUIEnemy.RenderEnemyEncounter(enemy, stateManager.CurrentPlayer, displayManager.CompleteDisplayLog, 
                    stateManager.CurrentDungeon?.Name, stateManager.CurrentRoom?.Name);
                // Brief delay to show enemy encounter information
                await Task.Delay(2000);
            }
            
            // Initial render of combat screen with structured content
            // This sets up the layout and enables structured combat mode
            if (customUIManager is CanvasUICoordinator canvasUIInitial)
            {
                canvasUIInitial.RenderCombat(stateManager.CurrentPlayer, enemy, displayManager.CompleteDisplayLog);
            }
            
            var room = stateManager.CurrentRoom;
            var player = stateManager.CurrentPlayer;
            
            // Create debouncer for UI updates
            CombatEventDebouncer? debouncer = null;
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                debouncer = new CombatEventDebouncer(200, () =>
                    canvasUI.RenderCombat(player, enemy, displayManager.CompleteDisplayLog));
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
            }
            
            // Enemy defeated - add victory message with proper spacing BEFORE final render
            // This ensures the victory message is included in the final render and prevents overlapping text
            if (enemy != null)
            {
                // Add blank line for spacing after informational summary
                displayManager.AddCombatEvent("");
                displayManager.AddCombatEvent("");
                var victoryBuilder = new ColoredTextBuilder();
                victoryBuilder.Add(enemy.Name, ColorPalette.Enemy);
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
            if (customUIManager is CanvasUICoordinator canvasUI4 && enemy != null && player != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    canvasUI4.RenderCombat(player, enemy, displayManager.CompleteDisplayLog);
                });
                // Small delay to ensure the final render completes before continuing
                if (!RPGGame.MCP.MCPMode.IsActive)
                {
                    await Task.Delay(100);
                }
            }
            
            // Small delay before next
            if (!RPGGame.MCP.MCPMode.IsActive)
            {
                await Task.Delay(1000);
            }
            return true; // Player survived this encounter
        }
    }
}

