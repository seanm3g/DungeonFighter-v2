namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Avalonia.Media;
    using Avalonia.Threading;
    using RPGGame.GameCore.Helpers;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Handles dungeon execution, room processing, and enemy encounters.
    /// Extracted from Game.cs to manage the entire dungeon run orchestration.
    /// 
    /// This is the most complex manager - handles:
    /// - Dungeon orchestration
    /// - Room processing
    /// - Enemy encounters
    /// - Combat integration
    /// - Narrative formatting for combat
    /// </summary>
    public class DungeonRunnerManager
    {
        private GameStateManager stateManager;
        private GameNarrativeManager narrativeManager;
        private CombatManager? combatManager;
        private IUIManager? customUIManager;
        private DungeonDisplayManager displayManager;
        private DungeonExitChoiceHandler? exitChoiceHandler;
        private readonly ExplorationManager? explorationManager;
        
        // Delegates for dungeon completion with reward data
        public delegate void OnDungeonCompleted(int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun);
        public delegate void OnShowDeathScreen(Character player);
        public delegate void OnDungeonExitedEarly();
        
        public event OnDungeonCompleted? DungeonCompletedEvent;
        public event OnShowDeathScreen? ShowDeathScreenEvent;
        public event OnDungeonExitedEarly? DungeonExitedEarlyEvent;
        
        // Store last reward data for completion screen
        private int lastXPGained;
        private Item? lastLootReceived;
        private List<LevelUpInfo> lastLevelUpInfos = new List<LevelUpInfo>();
        
        // Track starting inventory to calculate items found during dungeon run
        private List<Item>? startingInventory;

        public DungeonRunnerManager(
            GameStateManager stateManager,
            GameNarrativeManager narrativeManager,
            CombatManager? combatManager,
            IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.narrativeManager = narrativeManager ?? throw new ArgumentNullException(nameof(narrativeManager));
            this.combatManager = combatManager;
            this.customUIManager = customUIManager;
            this.displayManager = new DungeonDisplayManager(narrativeManager, customUIManager);
            this.exitChoiceHandler = new DungeonExitChoiceHandler(stateManager, customUIManager, displayManager);
            this.explorationManager = new ExplorationManager();
        }
        
        /// <summary>
        /// Get the exit choice handler for input routing
        /// </summary>
        public DungeonExitChoiceHandler? GetExitChoiceHandler() => exitChoiceHandler;

        /// <summary>
        /// Run the entire dungeon
        /// </summary>
        public async Task RunDungeon()
        {
            DebugLogger.Log("DungeonRunnerManager", $"CombatManager: {(combatManager != null ? "initialized" : "null")}");
            
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null || combatManager == null)
            {
                DungeonErrorHandler.HandleMissingComponents(stateManager, customUIManager);
                return;
            }
            
            // Validate dungeon has been generated and has rooms
            if (stateManager.CurrentDungeon.Rooms == null || stateManager.CurrentDungeon.Rooms.Count == 0)
            {
                // Try to regenerate the dungeon
                try
                {
                    stateManager.CurrentDungeon.Generate();
                }
                catch (Exception ex)
                {
                    DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, $"Failed to generate dungeon: {ex.Message}");
                    return;
                }
                
                if (stateManager.CurrentDungeon?.Rooms == null || stateManager.CurrentDungeon.Rooms.Count == 0)
                {
                    DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, "Dungeon generation failed - no rooms created.");
                    return;
                }
            }
            
            // Set game state to Dungeon
            stateManager.TransitionToState(GameState.Dungeon);
            
            // Reset combo step to 0 to start at the first action in the sequence
            if (stateManager.CurrentPlayer != null)
            {
                stateManager.CurrentPlayer.ComboStep = 0;
            }
            
            // Capture starting inventory to track items found during the run
            if (stateManager.CurrentPlayer != null)
            {
                startingInventory = new List<Item>(stateManager.CurrentPlayer.Inventory);
            }
            
            // Restore display buffer rendering in case it was suppressed (e.g., from dungeon selection screen)
            // This ensures dungeon exploration and combat screens work correctly
            if (customUIManager is CanvasUICoordinator canvasUIRestore)
            {
                canvasUIRestore.RestoreDisplayBufferRendering();
            }
            
            // Start dungeon using unified display manager
            // This prepares the dungeon data but doesn't add content to buffer yet
            // Content (including dungeon header) will be added in ProcessRoom() for the first room
            if (stateManager.CurrentPlayer == null)
            {
                DungeonErrorHandler.HandleDungeonGenerationError(stateManager, customUIManager, "Cannot start dungeon - no player character available.");
                return;
            }
            displayManager.StartDungeon(stateManager.CurrentDungeon, stateManager.CurrentPlayer);
            
            // Process all rooms
            try
            {
                int roomNumber = 0;
                int totalRooms = stateManager.CurrentDungeon.Rooms.Count;
                bool isFirstRoom = true;
                foreach (Environment room in stateManager.CurrentDungeon.Rooms)
                {
                    roomNumber++;
                    if (!await ProcessRoom(room, roomNumber, totalRooms, isFirstRoom))
                    {
                        // Player died - transition to death screen
                        if (stateManager.CurrentPlayer != null)
                        {
                            stateManager.TransitionToState(GameState.Death);
                            ShowDeathScreenEvent?.Invoke(stateManager.CurrentPlayer);
                        }
                        return;
                    }
                    isFirstRoom = false;
                    
                    // Offer exit choice after every room (except the last room)
                    // Only check if there are more rooms to process
                    if (roomNumber < totalRooms)
                    {
                        // Offer exit choice after room completion
                        if (exitChoiceHandler != null)
                        {
                            bool shouldExit = await exitChoiceHandler.ShowExitChoiceMenu(roomNumber, totalRooms);
                            if (shouldExit)
                            {
                                // Player chose to leave - exit dungeon without rewards
                                // Fully heal the player before leaving
                                int healthRestored = 0;
                                if (stateManager.CurrentPlayer != null)
                                {
                                    int effectiveMaxHealth = stateManager.CurrentPlayer.GetEffectiveMaxHealth();
                                    healthRestored = effectiveMaxHealth - stateManager.CurrentPlayer.CurrentHealth;
                                    if (healthRestored > 0)
                                    {
                                        stateManager.CurrentPlayer.Heal(healthRestored);
                                    }
                                }
                                
                                if (customUIManager is CanvasUICoordinator canvasUIExit)
                                {
                                    displayManager.AddCombatEvent("");
                                    if (healthRestored > 0)
                                    {
                                        displayManager.AddCombatEvent($"You have been fully healed! (+{healthRestored} health)");
                                    }
                                    displayManager.AddCombatEvent("You leave the dungeon safely, but receive no rewards.");
                                    if (stateManager.CurrentPlayer != null && stateManager.CurrentRoom != null)
                                    {
                                        canvasUIExit.RenderRoomEntry(
                                            stateManager.CurrentRoom, 
                                            stateManager.CurrentPlayer, 
                                            stateManager.CurrentDungeon?.Name);
                                    }
                                    if (!RPGGame.MCP.MCPMode.IsActive)
                                    {
                                        await Task.Delay(2000);
                                    }
                                }
                                
                                // Clear dungeon state and return to game loop
                                stateManager.SetCurrentDungeon(null!);
                                stateManager.SetCurrentRoom(null!);
                                stateManager.TransitionToState(GameState.GameLoop);
                                
                                // Trigger event to show game loop screen
                                DungeonExitedEarlyEvent?.Invoke();
                                
                                return; // Exit without calling CompleteDungeon()
                            }
                        }
                    }
                }
                
                // Dungeon completed successfully
                await CompleteDungeon();
            }
            catch (Exception ex)
            {
                DungeonErrorHandler.HandleException(ex, customUIManager);
                throw;
            }
        }

        /// <summary>
        /// Process a single room in the dungeon
        /// </summary>
        private async Task<bool> ProcessRoom(Environment room, int roomNumber, int totalRooms, bool isFirstRoom)
        {
            if (stateManager.CurrentPlayer == null || combatManager == null) return false;
            
            stateManager.SetCurrentRoom(room);
            bool isLastRoom = (roomNumber == totalRooms);
            
            // Show room entry screen (handles entering room, adding to buffer, and rendering)
            if (stateManager.CurrentPlayer != null && customUIManager is CanvasUICoordinator canvasUI)
            {
                // Simplified: one method call handles everything
                displayManager.ShowRoomEntry(room, roomNumber, totalRooms, isFirstRoom);
                
                // Render the room entry screen
                canvasUI.RenderRoomEntry(room, stateManager.CurrentPlayer, stateManager.CurrentDungeon?.Name);
                
                // Delay to show dungeon and room information (skip in MCP mode)
                if (!RPGGame.MCP.MCPMode.IsActive)
                {
                    await Task.Delay(3500);
                }
            }
            
            // Clear temporary effects
            if (stateManager.CurrentPlayer != null)
            {
                stateManager.CurrentPlayer.ClearAllTempEffects();
            }
            
            // Pre-combat exploration
            bool playerGetsFirstAttack = false;
            bool enemyGetsFirstAttack = false;
            bool skipCombat = false;
            
            if (explorationManager != null && stateManager.CurrentPlayer != null)
            {
                var explorationResult = explorationManager.ExploreRoom(room, stateManager.CurrentPlayer, isLastRoom);
                
                // Display exploration result using existing displayManager
                displayManager.AddCombatEvent($"Exploration Roll: {explorationResult.Roll}");
                displayManager.AddCombatEvent(explorationResult.Message);
                
                if (explorationResult.EnvironmentInfo != null)
                {
                    displayManager.AddCombatEvent(explorationResult.EnvironmentInfo);
                }
                
                // Handle environmental hazard
                if (explorationResult.Outcome == ExplorationOutcome.EnvironmentalHazard && explorationResult.Hazard != null)
                {
                    var hazard = explorationResult.Hazard;
                    
                    // Format the initial environmental hazard message with colors
                    // Message format: "Poisonous gas suddenly fills the area! You take 2 damage!"
                    var hazardBuilder = new ColoredTextBuilder();
                    string message = hazard.Message;
                    
                    // Find where "You take" appears in the message
                    int takeIndex = message.IndexOf("You take ");
                    if (takeIndex >= 0 && hazard.Damage > 0)
                    {
                        // Add text before "You take" (the environmental description)
                        string beforeTake = message.Substring(0, takeIndex);
                        hazardBuilder.Add(beforeTake, Colors.White);
                        
                        // Add "You take " in white
                        hazardBuilder.Add("You take ", Colors.White);
                        
                        // Add damage number in red
                        hazardBuilder.Add(hazard.Damage.ToString(), ColorPalette.Damage);
                        
                        // Find where " damage" appears after the damage number
                        int damageWordIndex = message.IndexOf(" damage", takeIndex + 9 + hazard.Damage.ToString().Length);
                        if (damageWordIndex >= 0)
                        {
                            // Add " damage" and everything after it in white
                            string afterDamage = message.Substring(damageWordIndex);
                            hazardBuilder.Add(afterDamage, Colors.White);
                        }
                        else
                        {
                            // Fallback: just add " damage!" if we can't find it
                            hazardBuilder.Add(" damage!", Colors.White);
                        }
                    }
                    else
                    {
                        // Fallback: if we can't parse it, just display as-is
                        hazardBuilder.Add(message, Colors.White);
                    }
                    
                    displayManager.AddCombatEvent(hazardBuilder);
                    
                    if (hazard.SkipToCombat && hazard.Damage > 0)
                    {
                        stateManager.CurrentPlayer.TakeDamage(hazard.Damage);
                        
                        // Format the second damage message with colors
                        var damageBuilder = new ColoredTextBuilder();
                        damageBuilder.Add("You take ", Colors.White);
                        damageBuilder.Add(hazard.Damage.ToString(), ColorPalette.Damage);
                        damageBuilder.Add(" damage from the hazard!", Colors.White);
                        displayManager.AddCombatEvent(damageBuilder);
                    }
                    else if (hazard.SkipToSearch)
                    {
                        skipCombat = true;
                    }
                }
                
                // Set combat order flags
                if (explorationResult.PlayerGetsFirstAttack) playerGetsFirstAttack = true;
                else if (explorationResult.IsSurprised && room.HasLivingEnemies()) enemyGetsFirstAttack = true;
                
                // Re-render and delay
                if (customUIManager is CanvasUICoordinator canvasUIExplore)
                {
                    canvasUIExplore.RenderRoomEntry(room, stateManager.CurrentPlayer, stateManager.CurrentDungeon?.Name);
                    await Task.Delay(2000);
                }
            }
            
            // Check if room is empty (no enemies)
            bool roomWasHostile = room.IsHostile;
            if (!skipCombat)
            {
                if (!room.HasLivingEnemies())
                {
                    // Room is empty - display safe message
                    if (customUIManager is CanvasUICoordinator canvasUISafe && stateManager.CurrentPlayer != null)
                    {
                        // Add blank line before safe message (after room info)
                        displayManager.AddCombatEvent("");
                        displayManager.AddCombatEvent("It appears you are safe... for now.");
                        displayManager.AddCombatEvent(""); // Blank line after safe message
                        // Re-render room entry to show the safe message
                        canvasUISafe.RenderRoomEntry(room, stateManager.CurrentPlayer, stateManager.CurrentDungeon?.Name);
                        if (!RPGGame.MCP.MCPMode.IsActive)
                        {
                            await Task.Delay(2000);
                        }
                    }
                }
                else
                {
                    // Display surprise/advantage status before combat
                    var random = new Random();
                    if (playerGetsFirstAttack)
                    {
                        var advantageMessages = new[]
                        {
                            "You have the advantage! You'll strike first!",
                            "You've caught the enemy off guard! You attack first!",
                            "Your quick reflexes give you the first strike!",
                            "You've gained the upper hand! You'll act first!",
                            "The element of surprise is yours! You strike first!"
                        };
                        displayManager.AddCombatEvent("");
                        displayManager.AddCombatEvent(advantageMessages[random.Next(advantageMessages.Length)]);
                    }
                    else if (enemyGetsFirstAttack)
                    {
                        // Surprise message will be shown after enemy appears in ProcessEnemyEncounter
                    }
                    else
                    {
                        var neutralMessages = new[]
                        {
                            "Combat begins! Both sides are ready.",
                            "The battle commences! Neither side has the advantage.",
                            "Combat starts! Both combatants are prepared.",
                            "The fight begins! It's an even match.",
                            "Combat erupts! Both sides are equally ready."
                        };
                        displayManager.AddCombatEvent("");
                        displayManager.AddCombatEvent(neutralMessages[random.Next(neutralMessages.Length)]);
                    }
                    
                    // Re-render to show advantage message
                    if (customUIManager is CanvasUICoordinator canvasUIAdvantage && stateManager.CurrentPlayer != null)
                    {
                        canvasUIAdvantage.RenderRoomEntry(room, stateManager.CurrentPlayer, stateManager.CurrentDungeon?.Name);
                        await Task.Delay(1500);
                    }
                    
                    // Process all enemies in the room
                    while (room.HasLivingEnemies())
                    {
                        Enemy? currentEnemy = room.GetNextLivingEnemy();
                        if (currentEnemy == null) break;
                        
                        if (!await ProcessEnemyEncounter(currentEnemy, playerGetsFirstAttack, enemyGetsFirstAttack))
                        {
                            return false; // Player died
                        }
                        
                        // Reset flags after first enemy encounter
                        playerGetsFirstAttack = false;
                        enemyGetsFirstAttack = false;
                    }
                }
            }
            
            // Post-combat search (happens FIRST, before room cleared message)
            bool foundLoot = false;
            if (explorationManager != null && stateManager.CurrentPlayer != null && stateManager.CurrentDungeon != null)
            {
                var searchResult = explorationManager.SearchRoom(room, stateManager.CurrentPlayer, 
                    stateManager.CurrentDungeon.MinLevel, isLastRoom);
                
                // Display search result
                displayManager.AddCombatEvent($"Search Roll: {searchResult.Roll}");
                displayManager.AddCombatEvent(searchResult.Message);
                
                // If loot found, add it to inventory
                if (searchResult.FoundLoot && searchResult.LootItem != null)
                {
                    foundLoot = true;
                    stateManager.CurrentPlayer.AddToInventory(searchResult.LootItem);
                    displayManager.AddCombatEvent($"You found: {searchResult.LootItem.Name}");
                }
                
                // Re-render and delay
                if (customUIManager is CanvasUICoordinator canvasUISearch)
                {
                    canvasUISearch.RenderRoomEntry(room, stateManager.CurrentPlayer, stateManager.CurrentDungeon?.Name);
                    await Task.Delay(2000);
                }
            }
            
            // Room completion message (only shown if no loot was found)
            if (roomWasHostile && !skipCombat && !foundLoot && customUIManager is CanvasUICoordinator canvasUI2)
            {
                canvasUI2.AddRoomClearedMessage();
                await Task.Delay(2000);
            }
            
            return true; // Player survived the room
        }

        /// <summary>
        /// Process a single enemy encounter
        /// </summary>
        private async Task<bool> ProcessEnemyEncounter(Enemy enemy, bool playerGetsFirstAttack = false, bool enemyGetsFirstAttack = false)
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
                    ShowDeathScreenEvent?.Invoke(stateManager.CurrentPlayer);
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

        /// <summary>
        /// Complete the dungeon run
        /// </summary>
        private async Task CompleteDungeon()
        {
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null) return;
            
            // Award rewards and get the data
            var dungeonLevel = stateManager.CurrentDungeon.MaxLevel;
            var dungeonManager = new DungeonManagerWithRegistry();
            var (xpGained, lootReceived, levelUpInfos) = dungeonManager.AwardLootAndXPWithReturns(
                stateManager.CurrentPlayer, 
                stateManager.CurrentInventory, 
                new List<Dungeon> { stateManager.CurrentDungeon }
            );
            
            // Calculate all items found during the dungeon run
            // Compare current inventory to starting inventory, excluding the final completion reward
            List<Item> itemsFoundDuringRun = new List<Item>();
            if (stateManager.CurrentPlayer != null && startingInventory != null)
            {
                var currentInventory = stateManager.CurrentPlayer.Inventory;
                // Find all items in current inventory that weren't in starting inventory
                foreach (var item in currentInventory)
                {
                    // Skip the final completion reward - it will be displayed separately
                    if (lootReceived != null && item.Name == lootReceived.Name && item.Type == lootReceived.Type)
                    {
                        continue;
                    }
                    
                    // Check if this item exists in starting inventory
                    // Use a simple comparison based on item identity (name + type should be unique enough)
                    bool foundInStarting = false;
                    foreach (var startingItem in startingInventory)
                    {
                        if (startingItem.Name == item.Name && startingItem.Type == item.Type)
                        {
                            foundInStarting = true;
                            break;
                        }
                    }
                    if (!foundInStarting)
                    {
                        itemsFoundDuringRun.Add(item);
                    }
                }
            }
            
            // Store reward data
            lastXPGained = xpGained;
            lastLootReceived = lootReceived;
            lastLevelUpInfos = levelUpInfos ?? new List<LevelUpInfo>();
            
            // Add a delay to let rewards display if in console
            if (!RPGGame.MCP.MCPMode.IsActive)
            {
                await Task.Delay(1500);
            }
            
            // Transition to completion state
            stateManager.TransitionToState(GameState.DungeonCompletion);
            
            // Trigger event to handle UI display with reward data
            DungeonCompletedEvent?.Invoke(xpGained, lootReceived, lastLevelUpInfos, itemsFoundDuringRun);
            
            // Reset starting inventory for next run
            startingInventory = null;
        }
        
        /// <summary>
        /// Get the last reward data from dungeon completion
        /// </summary>
        public (int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos) GetLastRewardData()
        {
            return (lastXPGained, lastLootReceived, lastLevelUpInfos);
        }
        
        /// <summary>
        /// Check if we're at the halfway point of the dungeon
        /// </summary>
        private bool IsHalfwayPoint(int currentRoom, int totalRooms)
        {
            // Halfway point is when we've completed exactly half the rooms
            // For odd numbers, use integer division (e.g., 5 rooms -> halfway at room 2)
            int halfwayPoint = totalRooms / 2;
            return currentRoom == halfwayPoint;
        }

    }
}

