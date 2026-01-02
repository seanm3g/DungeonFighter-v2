namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Avalonia.Media;
    using RPGGame.Display.Dungeon;
    using RPGGame.GameCore.Display.Helpers;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Display;
    using RPGGame.UI.Avalonia.Managers;
    using RPGGame.UI.ColorSystem;
    using RPGGame.UI.Services;

    /// <summary>
    /// Unified manager for all dungeon display information.
    /// Handles dungeon header, room info, enemy info, and combat log in a single, consistent system.
    /// 
    /// This manager eliminates the need to manually sync between GameNarrativeManager,
    /// display buffer, and context manager by providing a single source of truth.
    /// Refactored to use extracted builders, display buffer, and specialized coordinators.
    /// </summary>
    public class DungeonDisplayManager
    {
        private readonly GameNarrativeManager narrativeManager;
        private readonly IUIManager? uiManager;
        private readonly CanvasUICoordinator? canvasUI;
        private readonly RPGGame.Display.Dungeon.DungeonDisplayBuffer displayBuffer;
        private readonly GameStateManager? stateManager;
        private MessageRouter? messageRouter;
        
        // Specialized coordinators
        private readonly DungeonDisplayBufferCoordinator bufferCoordinator;
        private readonly DungeonDisplayContextSync contextSync;
        
        // Extracted state management and message routing
        private readonly DungeonDisplayState state;
        private readonly DungeonMessageRouter messageRouterService;

        /// <summary>
        /// Event fired when a combat event is added to the combat log.
        /// Allows subscribers to react immediately to combat log changes without polling.
        /// </summary>
        public event System.Action CombatEventAdded = delegate { };

        /// <summary>
        /// Gets the complete display log (all sections combined in order)
        /// </summary>
        public List<string> CompleteDisplayLog => displayBuffer.CompleteDisplayLog;

        /// <summary>
        /// Gets just the combat log (for combat screen rendering)
        /// </summary>
        public List<string> CombatLog => displayBuffer.CombatLog;

        /// <summary>
        /// Gets the dungeon context (header + room + enemy info, without combat log)
        /// Used for rendering the header section in combat screens
        /// </summary>
        public List<string> DungeonContext => displayBuffer.DungeonContext;

        public DungeonDisplayManager(GameNarrativeManager narrativeManager, IUIManager? uiManager = null, GameStateManager? stateManager = null)
        {
            this.narrativeManager = narrativeManager ?? throw new ArgumentNullException(nameof(narrativeManager));
            this.uiManager = uiManager;
            this.canvasUI = uiManager as CanvasUICoordinator;
            this.stateManager = stateManager;
            this.displayBuffer = new DungeonDisplayBuffer();
            
            // Initialize state management
            this.state = new DungeonDisplayState(stateManager);
            
            // Initialize MessageRouter with available components
            // Pass CanvasTextManager if available for per-character display manager support
            CanvasTextManager? canvasTextManager = null;
            if (canvasUI != null)
            {
                // Get CanvasTextManager from CanvasUICoordinator
                // This allows MessageRouter to route to per-character display managers
                var textManager = canvasUI.GetTextManager();
                canvasTextManager = textManager as CanvasTextManager;
            }
            this.messageRouter = new MessageRouter(null, uiManager, stateManager, null, canvasTextManager);
            
            // Initialize message router service
            this.messageRouterService = new DungeonMessageRouter(state, messageRouter, uiManager, stateManager);
            
            // Initialize specialized coordinators
            this.bufferCoordinator = new DungeonDisplayBufferCoordinator(displayBuffer, canvasUI);
            this.contextSync = new DungeonDisplayContextSync(narrativeManager, displayBuffer, canvasUI);
            
            // Subscribe to character switch events to clear combat log when switching characters
            // This prevents combat log from character A from persisting when switching to character B
            if (stateManager != null)
            {
                stateManager.CharacterSwitched += OnCharacterSwitched;
            }
        }

        /// <summary>
        /// Starts a new dungeon run. Clears all previous information and sets up dungeon header.
        /// This method only prepares the data - it does NOT add content to the buffer or set UI context.
        /// Content is added to the buffer when ShowRoomEntry() is called for the first room.
        /// </summary>
        public void StartDungeon(Dungeon dungeon, Character player)
        {
            if (dungeon == null) throw new ArgumentNullException(nameof(dungeon));
            if (player == null) throw new ArgumentNullException(nameof(player));

            // CRITICAL: Only update currentPlayer if this character is the active character
            // This prevents overwriting currentPlayer when another character's dungeon is still running
            // If another character's dungeon is active, we should not start a new dungeon for this character
            var activeCharacter = stateManager?.GetActiveCharacter();
            if (activeCharacter != null && player != activeCharacter)
            {
                // Character is not active - don't start dungeon (another character's dungeon is still running)
                return;
            }

            // Clear all previous information
            ClearAll();
            
            // Explicitly clear combat log when starting a new dungeon
            // This ensures the combat log from the previous dungeon is not visible
            ClearCombatLog();

            // Clear display buffer to prevent duplicate messages from previous runs
            if (canvasUI != null)
            {
                canvasUI.ClearDisplayBuffer();
            }

            // Reset TextSpacingSystem for new dungeon
            TextSpacingSystem.Reset();
            
            // Reset buffer coordinator flag
            bufferCoordinator.ResetDungeonHeaderFlag();

            // Store current state using state manager
            state.SetDungeon(dungeon, player);

            // Build dungeon header (in memory only - not added to buffer yet)
            var header = DungeonHeaderBuilder.BuildDungeonHeader(dungeon);
            displayBuffer.SetDungeonHeader(header);

            // Sync to narrative manager (for internal tracking)
            contextSync.SyncToNarrativeManager();

            // DO NOT set UI context here - there's no room yet and no content in the buffer
            // Context will be set when ShowRoomEntry() is called for the first room
            // This ensures context matches the actual buffer state
        }


        /// <summary>
        /// Simplified method to show room entry screen.
        /// Handles entering the room, adding content to buffer, and triggering render.
        /// This is the recommended way to display room entry - it handles all the complexity internally.
        /// </summary>
        /// <param name="room">The room to enter</param>
        /// <param name="roomNumber">Room number (1-based)</param>
        /// <param name="totalRooms">Total number of rooms in dungeon</param>
        /// <param name="isFirstRoom">Whether this is the first room (includes dungeon header)</param>
        public void ShowRoomEntry(Environment room, int roomNumber, int totalRooms, bool isFirstRoom)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));

            // Step 1: Set up room state (but don't set UI context yet - that triggers reactive rendering)
            // Clear room-specific info (but keep dungeon header)
            displayBuffer.ClearRoomInfo();
            displayBuffer.ClearEnemyInfo();
            displayBuffer.ClearCombatLog();

            // Store current state using state manager
            state.SetRoom(room, roomNumber, totalRooms);

            // Build room info
            var roomInfo = RoomInfoBuilder.BuildRoomInfo(room, roomNumber, totalRooms);
            displayBuffer.SetRoomInfo(roomInfo);

            // Sync to narrative manager (but don't set UI context yet)
            contextSync.SyncToNarrativeManager();

            // Step 2: Add content to display buffer (without auto-rendering)
            // For first room: include dungeon header + room info
            // For subsequent rooms: only room info (dungeon header already shown)
            bufferCoordinator.AddCurrentInfoToDisplayBuffer(
                includeDungeonHeader: isFirstRoom,
                includeRoomInfo: true,
                autoRender: false  // RenderRoomEntry will handle rendering
            );

            // Step 3: NOW set UI context (after buffer is populated, before explicit render)
            // This ensures the context is set for RenderRoomEntry, but doesn't trigger reactive render
            // because the buffer was already added with autoRender: false
            contextSync.SetUIContext(state.CurrentPlayer, state.GetDungeonName(), state.GetRoomName(), state.CurrentEnemy);
        }

        /// <summary>
        /// Starts an enemy encounter. Adds enemy information and prepares for combat.
        /// Integrates with TextSpacingSystem to apply proper spacing between sections.
        /// </summary>
        public void StartEnemyEncounter(Enemy enemy)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));

            // Store current enemy using state manager
            state.SetEnemy(enemy);

            // Clear previous enemy info and combat log
            displayBuffer.ClearEnemyInfo();
            displayBuffer.ClearCombatLog();

            // Build enemy info
            var enemyInfo = EnemyInfoBuilder.BuildEnemyInfo(enemy);
            displayBuffer.SetEnemyInfo(enemyInfo);

            // Sync to narrative manager (combine header + room + enemy for dungeon log)
            contextSync.SyncToNarrativeManager();

            // Set UI context
            contextSync.SetUIContext(state.CurrentPlayer, state.GetDungeonName(), state.GetRoomName(), state.CurrentEnemy);

            var enemyInfoList = displayBuffer.EnemyInfo;
            
            // CRITICAL: Only add enemy info to display buffer if this character is currently active
            // This prevents background combat from adding enemy info to the display buffer
            bool shouldAddToDisplayBuffer = true;
            if (canvasUI != null && stateManager != null && state.CurrentPlayer != null)
            {
                var activeCharacter = stateManager.GetActiveCharacter();
                shouldAddToDisplayBuffer = (state.CurrentPlayer == activeCharacter);
            }
            
            if (shouldAddToDisplayBuffer && canvasUI != null && canvasUI is CanvasUICoordinator coordinator)
            {
                using (var batch = coordinator.StartBatch(autoRender: true))
                {
                    if (enemyInfoList.Count > 0)
                    {
                        SpacingApplier.ApplySpacingBefore(batch, TextSpacingSystem.BlockType.EnemyAppearance);
                        batch.Add(enemyInfoList[0]);
                        TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnemyAppearance);
                        
                        if (enemyInfoList.Count > 1)
                        {
                            SpacingApplier.ApplySpacingBefore(batch, TextSpacingSystem.BlockType.EnemyStats);
                            for (int i = 1; i < enemyInfoList.Count; i++)
                                batch.Add(enemyInfoList[i]);
                            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnemyStats);
                        }
                    }
                }
            }
            else if (shouldAddToDisplayBuffer && uiManager != null && enemyInfoList.Count > 0)
            {
                SpacingApplier.ApplySpacingBefore(uiManager, TextSpacingSystem.BlockType.EnemyAppearance);
                uiManager.WriteLine(enemyInfoList[0], UIMessageType.System);
                TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnemyAppearance);
                
                if (enemyInfoList.Count > 1)
                {
                    SpacingApplier.ApplySpacingBefore(uiManager, TextSpacingSystem.BlockType.EnemyStats);
                    for (int i = 1; i < enemyInfoList.Count; i++)
                        uiManager.WriteLine(enemyInfoList[i], UIMessageType.System);
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnemyStats);
                }
            }
        }

        /// <summary>
        /// Adds a combat event to the combat log.
        /// Also adds to display buffer and narrative manager to keep everything in sync.
        /// </summary>
        /// <param name="message">The combat event message</param>
        /// <param name="sourceCharacter">The character this combat event belongs to (optional, will be inferred if not provided)</param>
        public void AddCombatEvent(string message, Character? sourceCharacter = null)
        {
            // Allow empty strings (for blank lines) but filter out null or whitespace-only strings
            if (message == null)
                return;
            
            // Route message using message router service
            // message is already checked for null above, so it's safe here
            bool shouldUpdateUI = messageRouterService.RouteCombatEvent(
                message!,
                sourceCharacter,
                displayBuffer,
                narrativeManager);
            
            // Notify subscribers that a combat event was added (only if character is active)
            if (shouldUpdateUI)
            {
                CombatEventAdded?.Invoke();
            }
        }

        /// <summary>
        /// Adds a combat event to the combat log using colored text.
        /// Also adds to display buffer and narrative manager to keep everything in sync.
        /// Only updates UI if the character is currently active (for background dungeon support).
        /// </summary>
        /// <param name="builder">The colored text builder for the combat event</param>
        /// <param name="sourceCharacter">The character this combat event belongs to (optional, will be inferred if not provided)</param>
        public void AddCombatEvent(ColoredTextBuilder builder, Character? sourceCharacter = null)
        {
            if (builder == null)
                return;
            
            // Route message using message router service
            bool shouldUpdateUI = messageRouterService.RouteCombatEvent(
                builder,
                sourceCharacter,
                displayBuffer,
                narrativeManager);
            
            // Notify subscribers that a combat event was added (only if character is active)
            if (shouldUpdateUI)
            {
                CombatEventAdded?.Invoke();
            }
        }

        /// <summary>
        /// Adds multiple combat events to the combat log.
        /// </summary>
        public void AddCombatEvents(IEnumerable<string> messages)
        {
            if (messages == null) return;

            foreach (var message in messages)
            {
                AddCombatEvent(message);
            }
        }

        /// <summary>
        /// Clears the combat log (but keeps dungeon header, room info, and enemy info).
        /// Used when starting a new combat encounter.
        /// </summary>
        public void ClearCombatLog()
        {
            displayBuffer.ClearCombatLog();
            narrativeManager.ClearDungeonLog();
        }

        /// <summary>
        /// Clears all information. Used when leaving a dungeon.
        /// </summary>
        public void ClearAll()
        {
            displayBuffer.ClearAll();
            state.ClearAll();
            bufferCoordinator.ResetDungeonHeaderFlag();
            narrativeManager.ResetNarrative();
        }

        /// <summary>
        /// Handles character switch events to clear combat log and update current player when switching between characters.
        /// This prevents combat log from one character from persisting and influencing the display for another character.
        /// Also updates currentPlayer so that filtering works correctly for background combat.
        /// </summary>
        private void OnCharacterSwitched(object? sender, CharacterSwitchedEventArgs e)
        {
            // Update state for character switch
            state.OnCharacterSwitched();
            
            // Clear all dungeon display data when switching characters
            // This ensures that character A's dungeon info doesn't show for character B
            // We clear everything (not just combat log) because each character should have their own dungeon context
            ClearAll();
        }

        /// <summary>
        /// Gets the current dungeon name.
        /// </summary>
        public string? GetDungeonName() => state.GetDungeonName();

        /// <summary>
        /// Gets the current room name.
        /// </summary>
        public string? GetRoomName() => state.GetRoomName();

        /// <summary>
        /// Gets the current enemy.
        /// </summary>
        public Enemy? GetCurrentEnemy() => state.CurrentEnemy;

        /// <summary>
        /// Gets the current player.
        /// </summary>
        public Character? GetCurrentPlayer() => state.CurrentPlayer;

        /// <summary>
        /// Adds the current dungeon and room info to the display buffer using a batch transaction.
        /// Integrates with TextSpacingSystem to apply proper spacing between sections.
        /// 
        /// Note: For room entry, use ShowRoomEntry() instead - it handles everything automatically.
        /// This method is for advanced use cases where you need fine-grained control.
        /// </summary>
        /// <param name="includeDungeonHeader">Whether to include the dungeon header (typically only for first room)</param>
        /// <param name="includeRoomInfo">Whether to include room info (default: true)</param>
        /// <param name="autoRender">If true, reactive system will auto-render immediately. If false, caller must render explicitly (e.g., via RenderRoomEntry). Default: false to prevent duplicate rendering.</param>
        public void AddCurrentInfoToDisplayBuffer(bool includeDungeonHeader, bool includeRoomInfo = true, bool autoRender = false)
        {
            bufferCoordinator.AddCurrentInfoToDisplayBuffer(includeDungeonHeader, includeRoomInfo, autoRender);
        }

        #region Private Methods

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

        #endregion
    }
}
