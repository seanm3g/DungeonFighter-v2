namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Avalonia.Threading;
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
        
        // Delegates for dungeon completion with reward data
        public delegate void OnDungeonCompleted(int xpGained, Item? lootReceived);
        public delegate void OnShowMainMenu();
        public delegate void OnShowDeathScreen(Character player);
        
        public event OnDungeonCompleted? DungeonCompletedEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowDeathScreen? ShowDeathScreenEvent;
        
        // Store last reward data for completion screen
        private int lastXPGained;
        private Item? lastLootReceived;

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
        }

        /// <summary>
        /// Run the entire dungeon
        /// </summary>
        public async Task RunDungeon()
        {
            DebugLogger.Log("DungeonRunnerManager", "RunDungeon called");
            DebugLogger.Log("DungeonRunnerManager", $"CurrentPlayer: {stateManager.CurrentPlayer?.Name ?? "null"}");
            DebugLogger.Log("DungeonRunnerManager", $"CurrentDungeon: {stateManager.CurrentDungeon?.Name ?? "null"}");
            DebugLogger.Log("DungeonRunnerManager", $"CombatManager: {(combatManager != null ? "initialized" : "null")}");
            
            if (stateManager.CurrentPlayer == null || stateManager.CurrentDungeon == null || combatManager == null)
            {
                DebugLogger.Log("DungeonRunnerManager", "ERROR: Cannot run dungeon - missing required components");
                return;
            }
            
            // Set game state to Dungeon
            DebugLogger.Log("DungeonRunnerManager", "Transitioning to Dungeon state");
            stateManager.TransitionToState(GameState.Dungeon);
            
            // Create dungeon header info
            narrativeManager.DungeonHeaderInfo.Clear();
            
            var headerText = AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnteringDungeonHeader);
            var coloredHeader = new ColoredTextBuilder()
                .Add(headerText, ColorPalette.Warning)
                .Build();
            narrativeManager.DungeonHeaderInfo.Add(ColoredTextRenderer.RenderAsPlainText(coloredHeader));
            
            char themeColorCode = DungeonThemeColors.GetThemeColorCode(stateManager.CurrentDungeon.Theme);
            var dungeonNameColor = GetColorFromThemeCode(themeColorCode);
            
            var dungeonInfo = new ColoredTextBuilder()
                .Add("Dungeon: ", ColorPalette.Warning)
                .Add(stateManager.CurrentDungeon.Name, dungeonNameColor)
                .Build();
            narrativeManager.DungeonHeaderInfo.Add(ColoredTextRenderer.RenderAsPlainText(dungeonInfo));
            
            var levelInfo = new ColoredTextBuilder()
                .Add("Level Range: ", ColorPalette.Warning)
                .Add($"{stateManager.CurrentDungeon.MinLevel} - {stateManager.CurrentDungeon.MaxLevel}", ColorPalette.Info)
                .Build();
            narrativeManager.DungeonHeaderInfo.Add(ColoredTextRenderer.RenderAsPlainText(levelInfo));
            
            var roomInfo = new ColoredTextBuilder()
                .Add("Total Rooms: ", ColorPalette.Warning)
                .Add(stateManager.CurrentDungeon.Rooms.Count.ToString(), ColorPalette.Info)
                .Build();
            narrativeManager.DungeonHeaderInfo.Add(ColoredTextRenderer.RenderAsPlainText(roomInfo));
            narrativeManager.DungeonHeaderInfo.Add("");
            
            // Show dungeon start screen with narrative beat (only for first room)
            if (stateManager.CurrentPlayer != null && customUIManager is CanvasUICoordinator canvasUIStart)
            {
                canvasUIStart.RenderDungeonStart(stateManager.CurrentDungeon, stateManager.CurrentPlayer);
                // Brief delay to show dungeon information
                await Task.Delay(1500);
            }
            
            // Process all rooms
            int roomNumber = 0;
            int totalRooms = stateManager.CurrentDungeon.Rooms.Count;
            foreach (Environment room in stateManager.CurrentDungeon.Rooms)
            {
                roomNumber++;
                if (!await ProcessRoom(room, roomNumber, totalRooms))
                {
                    // Player died - transition to death screen
                    if (stateManager.CurrentPlayer != null)
                    {
                        stateManager.TransitionToState(GameState.Death);
                        ShowDeathScreenEvent?.Invoke(stateManager.CurrentPlayer);
                    }
                    return;
                }
            }
            
            // Dungeon completed successfully
            await CompleteDungeon();
        }

        /// <summary>
        /// Process a single room in the dungeon
        /// </summary>
        private async Task<bool> ProcessRoom(Environment room, int roomNumber, int totalRooms)
        {
            if (stateManager.CurrentPlayer == null || combatManager == null) return false;
            
            stateManager.SetCurrentRoom(room);
            
            // Set up room info
            narrativeManager.CurrentRoomInfo.Clear();
            
            var roomHeaderText = AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnteringRoomHeader);
            var coloredRoomHeader = new ColoredTextBuilder()
                .Add(roomHeaderText, ColorPalette.Warning)
                .Build();
            narrativeManager.CurrentRoomInfo.Add(ColoredTextRenderer.RenderAsPlainText(coloredRoomHeader));
            
            // Add room number information (e.g., "Room 1 of 5")
            var roomNumberInfo = new ColoredTextBuilder()
                .Add("Room Number: ", ColorPalette.Info)
                .Add($"{roomNumber} of {totalRooms}", ColorPalette.Warning)
                .Build();
            narrativeManager.CurrentRoomInfo.Add(ColoredTextRenderer.RenderAsPlainText(roomNumberInfo));
            
            var roomNameInfo = new ColoredTextBuilder()
                .Add("Room: ", ColorPalette.White)
                .Add(room.Name, ColorPalette.White)
                .Build();
            narrativeManager.CurrentRoomInfo.Add(ColoredTextRenderer.RenderAsPlainText(roomNameInfo));
            
            var roomDescription = new ColoredTextBuilder()
                .Add(room.Description, ColorPalette.White)
                .Build();
            narrativeManager.CurrentRoomInfo.Add(ColoredTextRenderer.RenderAsPlainText(roomDescription));
            narrativeManager.CurrentRoomInfo.Add("");
            
            // Show room entry screen with narrative beat
            if (customUIManager is CanvasUICoordinator canvasUIRoom)
            {
                canvasUIRoom.RenderRoomEntry(room, stateManager.CurrentPlayer, stateManager.CurrentDungeon?.Name);
                // Delay to show room information (3.5 seconds to reach ~5 seconds total with dungeon start)
                await Task.Delay(3500);
            }
            
            // Clear temporary effects
            stateManager.CurrentPlayer.ClearAllTempEffects();
            
            // Process all enemies in the room
            bool roomWasHostile = room.IsHostile;
            while (room.HasLivingEnemies())
            {
                Enemy? currentEnemy = room.GetNextLivingEnemy();
                if (currentEnemy == null) break;
                
                if (!await ProcessEnemyEncounter(currentEnemy))
                {
                    return false; // Player died
                }
            }
            
            // Room completion message
            if (roomWasHostile && customUIManager is CanvasUICoordinator canvasUI2)
            {
                canvasUI2.AddRoomClearedMessage();
                await Task.Delay(2000);
            }
            return true; // Player survived the room
        }

        /// <summary>
        /// Process a single enemy encounter
        /// </summary>
        private async Task<bool> ProcessEnemyEncounter(Enemy enemy)
        {
            if (stateManager.CurrentPlayer == null || combatManager == null) return false;
            
            // Build context for this encounter
            narrativeManager.DungeonLog.Clear();
            narrativeManager.DungeonLog.AddRange(narrativeManager.DungeonHeaderInfo);
            narrativeManager.DungeonLog.AddRange(narrativeManager.CurrentRoomInfo);
            
            // Set combat context FIRST before adding to display buffer
            // This ensures that when display buffer updates trigger renders, the context is already set
            // This prevents unnecessary full clears when transitioning to combat
            if (customUIManager is CanvasUICoordinator canvasUISetup)
            {
                canvasUISetup.SetDungeonContext(narrativeManager.DungeonLog);
                canvasUISetup.SetCurrentEnemy(enemy);
                canvasUISetup.SetDungeonName(stateManager.CurrentDungeon?.Name);
                canvasUISetup.SetRoomName(stateManager.CurrentRoom?.Name);
                canvasUISetup.SetCharacter(stateManager.CurrentPlayer);
                canvasUISetup.ResetForNewBattle();
            }
            
            // Add dungeon header and room info to display buffer
            // This ensures comprehensive pre-combat information is visible during combat
            // We add it for each encounter to ensure it's always visible (display buffer handles duplicates)
            foreach (var headerLine in narrativeManager.DungeonHeaderInfo)
            {
                if (!string.IsNullOrWhiteSpace(headerLine))
                {
                    UIManager.WriteLine(headerLine, UIMessageType.System);
                }
            }
            
            // Add room info to display buffer
            foreach (var roomLine in narrativeManager.CurrentRoomInfo)
            {
                if (!string.IsNullOrWhiteSpace(roomLine))
                {
                    UIManager.WriteLine(roomLine, UIMessageType.System);
                }
            }
            
            // Add enemy encounter info to dungeon log
            string enemyWeaponInfo = enemy.Weapon != null 
                ? string.Format(AsciiArtAssets.UIText.WeaponSuffix, enemy.Weapon.Name)
                : "";
            string encounteredText = string.Format(AsciiArtAssets.UIText.EncounteredFormat, enemy.Name, enemyWeaponInfo);
            narrativeManager.LogDungeonEvent($"{{{{common|{encounteredText}}}}}");
            string statsText = AsciiArtAssets.UIText.FormatEnemyStats(enemy.CurrentHealth, enemy.MaxHealth, enemy.Armor);
            narrativeManager.LogDungeonEvent($"{{{{enhanced_rare|{statsText}}}}}");
            string attackText = AsciiArtAssets.UIText.FormatEnemyAttack(enemy.Strength, enemy.Agility, enemy.Technique, enemy.Intelligence);
            narrativeManager.LogDungeonEvent($"{{{{enhanced_rare|{attackText}}}}}");
            narrativeManager.LogDungeonEvent("");
            
            // Add encounter info to display buffer so it persists through combat
            // This ensures the pre-combat information stays visible during combat
            UIManager.WriteLine($"{{{{common|{encounteredText}}}}}", UIMessageType.System);
            UIManager.WriteLine($"{{{{enhanced_rare|{statsText}}}}}", UIMessageType.System);
            UIManager.WriteLine($"{{{{enhanced_rare|{attackText}}}}}", UIMessageType.System);
            UIManager.WriteLine("", UIMessageType.System);
            
            // Show enemy encounter
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderEnemyEncounter(enemy, stateManager.CurrentPlayer, narrativeManager.DungeonLog, stateManager.CurrentDungeon?.Name, stateManager.CurrentRoom?.Name);
            }
            
            // Increased delay to 3500ms to give players more time to read encounter info
            await Task.Delay(3500);
            
            // Don't call RenderCombat - it would clear the screen
            // The display buffer already has all pre-combat info, and combat text will be added to it
            // The display will update naturally as combat text is added via UIManager.WriteLine()
            
            // Run combat on background thread to prevent UI blocking, but update UI on UI thread
            var room = stateManager.CurrentRoom;
            var player = stateManager.CurrentPlayer;
            var dungeonLog = narrativeManager.DungeonLog;
            
            // Run combat on background thread to prevent UI blocking
            // Refresh UI only when combat log changes, with debouncing to reduce flickering
            bool playerWon = false;
            int lastLogCount = dungeonLog.Count; // Track combat log size to detect changes
            System.DateTime lastRefreshTime = System.DateTime.MinValue;
            const int minRefreshIntervalMs = 200; // Minimum time between refreshes to reduce flickering
            
            var combatTask = Task.Run(async () =>
            {
                return await combatManager.RunCombat(player, enemy, room!);
            });
            
            // While combat is running, refresh UI only when combat log changes (with debouncing)
            while (!combatTask.IsCompleted)
            {
                var now = System.DateTime.Now;
                var timeSinceLastRefresh = (now - lastRefreshTime).TotalMilliseconds;
                
                // Only refresh if combat log has new entries AND enough time has passed since last refresh
                if (dungeonLog.Count > lastLogCount && timeSinceLastRefresh >= minRefreshIntervalMs)
                {
                    lastLogCount = dungeonLog.Count;
                    lastRefreshTime = now;
                    
                    // Refresh UI on UI thread
                    if (customUIManager is CanvasUICoordinator canvasUI3)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            canvasUI3.RenderCombat(player, enemy, dungeonLog);
                        });
                    }
                }
                
                // Check frequently for new log entries
                await Task.Delay(50);
            }
            
            // Final refresh to show complete combat log
            if (customUIManager is CanvasUICoordinator canvasUI4)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    canvasUI4.RenderCombat(player, enemy, dungeonLog);
                });
            }
            
            // Get the result
            playerWon = await combatTask;
            
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
            
            // Enemy defeated - small delay before next
            await Task.Delay(1000);
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
            var (xpGained, lootReceived) = dungeonManager.AwardLootAndXPWithReturns(
                stateManager.CurrentPlayer, 
                stateManager.CurrentInventory, 
                new List<Dungeon> { stateManager.CurrentDungeon }
            );
            
            // Store reward data
            lastXPGained = xpGained;
            lastLootReceived = lootReceived;
            
            // Add a delay to let rewards display if in console
            await Task.Delay(1500);
            
            // Transition to completion state
            stateManager.TransitionToState(GameState.DungeonCompletion);
            
            // Trigger event to handle UI display with reward data
            DungeonCompletedEvent?.Invoke(xpGained, lootReceived);
        }
        
        /// <summary>
        /// Get the last reward data from dungeon completion
        /// </summary>
        public (int xpGained, Item? lootReceived) GetLastRewardData()
        {
            return (lastXPGained, lastLootReceived);
        }

        /// <summary>
        /// Get color from theme code
        /// </summary>
        private ColorPalette GetColorFromThemeCode(char themeCode)
        {
            return themeCode switch
            {
                'R' => ColorPalette.Error,
                'G' => ColorPalette.Success,
                'B' => ColorPalette.Info,
                'Y' => ColorPalette.Warning,
                _ => ColorPalette.White
            };
        }
    }
}

