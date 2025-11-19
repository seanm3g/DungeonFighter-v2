namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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
        
        // Delegates
        public delegate void OnDungeonCompleted();
        public delegate void OnShowMainMenu();
        
        public event OnDungeonCompleted? DungeonCompletedEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;

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
            
            // Process all rooms
            foreach (Environment room in stateManager.CurrentDungeon.Rooms)
            {
                if (!await ProcessRoom(room))
                {
                    // Player died
                    stateManager.TransitionToState(GameState.MainMenu);
                    ShowMainMenuEvent?.Invoke();
                    return;
                }
            }
            
            // Dungeon completed successfully
            CompleteDungeon();
        }

        /// <summary>
        /// Process a single room in the dungeon
        /// </summary>
        private async Task<bool> ProcessRoom(Environment room)
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
            
            // Add enemy encounter info
            string enemyWeaponInfo = enemy.Weapon != null 
                ? string.Format(AsciiArtAssets.UIText.WeaponSuffix, enemy.Weapon.Name)
                : "";
            narrativeManager.LogDungeonEvent("&Y" + string.Format(AsciiArtAssets.UIText.EncounteredFormat, enemy.Name, enemyWeaponInfo));
            narrativeManager.LogDungeonEvent("&C" + AsciiArtAssets.UIText.FormatEnemyStats(enemy.CurrentHealth, enemy.MaxHealth, enemy.Armor));
            narrativeManager.LogDungeonEvent("&C" + AsciiArtAssets.UIText.FormatEnemyAttack(enemy.Strength, enemy.Agility, enemy.Technique, enemy.Intelligence));
            narrativeManager.LogDungeonEvent("");
            
            // Show enemy encounter
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RenderEnemyEncounter(enemy, stateManager.CurrentPlayer, narrativeManager.DungeonLog, stateManager.CurrentDungeon?.Name, stateManager.CurrentRoom?.Name);
            }
            
            await Task.Delay(1500);
            
            // Prepare for combat
            if (customUIManager is CanvasUICoordinator canvasUI2)
            {
                canvasUI2.SetDungeonContext(narrativeManager.DungeonLog);
                canvasUI2.SetCurrentEnemy(enemy);
                canvasUI2.SetDungeonName(stateManager.CurrentDungeon?.Name);
                canvasUI2.SetRoomName(stateManager.CurrentRoom?.Name);
                canvasUI2.ResetForNewBattle();
                canvasUI2.SetCharacter(stateManager.CurrentPlayer);
                canvasUI2.RenderCombat(stateManager.CurrentPlayer, enemy, narrativeManager.DungeonLog);
            }
            
            // Run combat
            var room = stateManager.CurrentRoom;
            bool playerWon = combatManager.RunCombat(stateManager.CurrentPlayer, enemy, room!);
            
            if (!playerWon)
            {
                // Player died - return to main menu
                return false;
            }
            
            // Enemy defeated - small delay before next
            await Task.Delay(1000);
            return true; // Player survived this encounter
        }

        /// <summary>
        /// Complete the dungeon run
        /// </summary>
        private void CompleteDungeon()
        {
            // Transition to completion state
            stateManager.TransitionToState(GameState.DungeonCompletion);
            
            // Trigger event to handle UI display
            DungeonCompletedEvent?.Invoke();
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

