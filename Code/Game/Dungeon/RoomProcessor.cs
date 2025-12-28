namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using System.IO;
    using System.Text.Json;
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
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "RoomProcessor.cs:ProcessRoom", message = "Entry", data = new { roomNumber, totalRooms, currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
            // #endregion
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
                
                // Apply keyword coloring to exploration message
                // Skip displaying the message for environmental hazards - it will be displayed with proper formatting below
                if (explorationResult.Outcome != ExplorationOutcome.EnvironmentalHazard)
                {
                    var explorationMessageColored = KeywordColorSystem.Colorize(explorationResult.Message);
                    string explorationMessageMarkup = ColoredTextRenderer.RenderAsMarkup(explorationMessageColored);
                    displayManager.AddCombatEvent(explorationMessageMarkup, stateManager.CurrentPlayer);
                }
                
                if (explorationResult.EnvironmentInfo != null)
                {
                    // Apply keyword coloring to environment info
                    var environmentInfoColored = KeywordColorSystem.Colorize(explorationResult.EnvironmentInfo);
                    string environmentInfoMarkup = ColoredTextRenderer.RenderAsMarkup(environmentInfoColored);
                    displayManager.AddCombatEvent(environmentInfoMarkup, stateManager.CurrentPlayer);
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
                    
                    displayManager.AddCombatEvent(hazardBuilder, stateManager.CurrentPlayer);
                    
                    if (hazard.SkipToCombat && hazard.Damage > 0)
                    {
                        stateManager.CurrentPlayer.TakeDamage(hazard.Damage);
                        // Damage message is already included in the hazard message above, no need to display again
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
                        displayManager.AddCombatEvent("", stateManager.CurrentPlayer);
                        var safeMessageBuilder = new ColoredTextBuilder()
                            .Add("It appears you are safe... ", Colors.White)
                            .Add("for now.", ColorPalette.Red);
                        displayManager.AddCombatEvent(safeMessageBuilder, stateManager.CurrentPlayer);
                        displayManager.AddCombatEvent("", stateManager.CurrentPlayer); // Blank line after safe message
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
                        displayManager.AddCombatEvent("", stateManager.CurrentPlayer);
                        var advantageMessage = advantageMessages[random.Next(advantageMessages.Length)];
                        var advantageBuilder = new ColoredTextBuilder()
                            .Add(advantageMessage, ColorPalette.Red);
                        displayManager.AddCombatEvent(advantageBuilder, stateManager.CurrentPlayer);
                    }
                    else if (enemyGetsFirstAttack)
                    {
                        // Surprise message will be shown after enemy appears in ProcessEnemyEncounter
                    }
                    else
                    {
                        // Both combatants are prepared - no message displayed
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
                        
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "RoomProcessor.cs:ProcessRoom", message = "Before ProcessEnemyEncounter", data = new { enemyName = currentEnemy.Name, currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
                        // #endregion
                        if (!await enemyEncounterHandler.ProcessEnemyEncounter(currentEnemy, playerGetsFirstAttack, enemyGetsFirstAttack))
                        {
                            // #region agent log
                            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "RoomProcessor.cs:ProcessRoom", message = "Player died in combat", data = new { currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
                            // #endregion
                            return false; // Player died
                        }
                        // #region agent log
                        try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "RoomProcessor.cs:ProcessRoom", message = "After ProcessEnemyEncounter", data = new { currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
                        // #endregion
                        
                        // Reset flags after first enemy encounter
                        playerGetsFirstAttack = false;
                        enemyGetsFirstAttack = false;
                    }
                }
            }
            
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "RoomProcessor.cs:ProcessRoom", message = "Room processing complete", data = new { roomNumber, currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); } catch { }
            // #endregion
            
            // Post-combat search (happens FIRST, before room cleared message)
            bool foundLoot = false;
            if (explorationManager != null && stateManager.CurrentPlayer != null && stateManager.CurrentDungeon != null)
            {
                var searchResult = explorationManager.SearchRoom(room, stateManager.CurrentPlayer, 
                    stateManager.CurrentDungeon.MinLevel, isLastRoom);
                
                // Display search result with colors
                displayManager.AddCombatEvent("", stateManager.CurrentPlayer); // Blank line before search roll
                var searchRollBuilder = new ColoredTextBuilder()
                    .Add("Search Roll: ", ColorPalette.Info)
                    .Add(searchResult.Roll.ToString(), ColorPalette.Success);
                displayManager.AddCombatEvent(searchRollBuilder, stateManager.CurrentPlayer);
                
                // Apply keyword coloring to search message
                var searchMessageColored = KeywordColorSystem.Colorize(searchResult.Message);
                string searchMessageMarkup = ColoredTextRenderer.RenderAsMarkup(searchMessageColored);
                displayManager.AddCombatEvent(searchMessageMarkup, stateManager.CurrentPlayer);
                
                // If loot found, add it to inventory
                // Note: The search message already contains the loot information, so we don't need to display it again
                if (searchResult.FoundLoot && searchResult.LootItem != null)
                {
                    foundLoot = true;
                    stateManager.CurrentPlayer.AddToInventory(searchResult.LootItem);
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

