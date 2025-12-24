namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using Avalonia.Media;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Handles processing of individual rooms in a dungeon
    /// Extracted from DungeonRunnerManager to separate room processing logic
    /// </summary>
    public class RoomProcessor
    {
        private readonly GameStateManager stateManager;
        private readonly CombatManager? combatManager;
        private readonly IUIManager? customUIManager;
        private readonly DungeonDisplayManager displayManager;
        private readonly ExplorationManager? explorationManager;
        private readonly EnemyEncounterHandler enemyEncounterHandler;

        public RoomProcessor(
            GameStateManager stateManager,
            CombatManager? combatManager,
            IUIManager? customUIManager,
            DungeonDisplayManager displayManager,
            ExplorationManager? explorationManager,
            EnemyEncounterHandler enemyEncounterHandler)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.combatManager = combatManager;
            this.customUIManager = customUIManager;
            this.displayManager = displayManager ?? throw new ArgumentNullException(nameof(displayManager));
            this.explorationManager = explorationManager;
            this.enemyEncounterHandler = enemyEncounterHandler ?? throw new ArgumentNullException(nameof(enemyEncounterHandler));
        }

        /// <summary>
        /// Process a single room in the dungeon
        /// </summary>
        public async Task<bool> ProcessRoom(Environment room, int roomNumber, int totalRooms, bool isFirstRoom)
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
                        
                        if (!await enemyEncounterHandler.ProcessEnemyEncounter(currentEnemy, playerGetsFirstAttack, enemyGetsFirstAttack))
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
    }
}

