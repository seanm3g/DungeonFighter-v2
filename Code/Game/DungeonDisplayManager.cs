namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia.Media;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Unified manager for all dungeon display information.
    /// Handles dungeon header, room info, enemy info, and combat log in a single, consistent system.
    /// 
    /// This manager eliminates the need to manually sync between GameNarrativeManager,
    /// display buffer, and context manager by providing a single source of truth.
    /// </summary>
    public class DungeonDisplayManager
    {
        private readonly GameNarrativeManager narrativeManager;
        private readonly IUIManager? uiManager;
        private readonly CanvasUICoordinator? canvasUI;

        // Current state
        private Dungeon? currentDungeon;
        private Environment? currentRoom;
        private Enemy? currentEnemy;
        private Character? currentPlayer;
        private int currentRoomNumber;
        private int totalRooms;
        private bool dungeonHeaderAddedToBuffer = false; // Track if header has been added to prevent duplicates

        // Display sections (in order of display)
        private readonly List<string> dungeonHeader = new List<string>();
        private readonly List<string> roomInfo = new List<string>();
        private readonly List<string> enemyInfo = new List<string>();
        private readonly List<string> combatLog = new List<string>();

        /// <summary>
        /// Event fired when a combat event is added to the combat log.
        /// Allows subscribers to react immediately to combat log changes without polling.
        /// </summary>
        public event System.Action CombatEventAdded = delegate { };

        /// <summary>
        /// Gets the complete display log (all sections combined in order)
        /// </summary>
        public List<string> CompleteDisplayLog
        {
            get
            {
                var log = new List<string>();
                log.AddRange(dungeonHeader);
                log.AddRange(roomInfo);
                log.AddRange(enemyInfo);
                log.AddRange(combatLog);
                return log;
            }
        }

        /// <summary>
        /// Gets just the combat log (for combat screen rendering)
        /// </summary>
        public List<string> CombatLog => new List<string>(combatLog);

        public DungeonDisplayManager(GameNarrativeManager narrativeManager, IUIManager? uiManager = null)
        {
            this.narrativeManager = narrativeManager ?? throw new ArgumentNullException(nameof(narrativeManager));
            this.uiManager = uiManager;
            this.canvasUI = uiManager as CanvasUICoordinator;
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

            // Clear all previous information
            ClearAll();

            // Clear display buffer to prevent duplicate messages from previous runs
            if (canvasUI != null)
            {
                canvasUI.ClearDisplayBuffer();
            }

            // Reset TextSpacingSystem for new dungeon
            TextSpacingSystem.Reset();
            
            // Reset flag to track if header has been added
            dungeonHeaderAddedToBuffer = false;

            // Store current state
            currentDungeon = dungeon;
            currentPlayer = player;
            currentRoomNumber = 0;
            totalRooms = dungeon.Rooms.Count;

            // Build dungeon header (in memory only - not added to buffer yet)
            BuildDungeonHeader(dungeon);

            // Sync to narrative manager (for internal tracking)
            SyncToNarrativeManager();

            // DO NOT set UI context here - there's no room yet and no content in the buffer
            // Context will be set when ShowRoomEntry() is called for the first room
            // This ensures context matches the actual buffer state
        }

        /// <summary>
        /// Enters a new room. Clears room and enemy info, sets up new room information.
        /// </summary>
        public void EnterRoom(Environment room, int roomNumber, int totalRooms, bool isFirstRoom)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));

            // Clear room-specific info (but keep dungeon header)
            roomInfo.Clear();
            enemyInfo.Clear();
            combatLog.Clear();

            // Store current state
            currentRoom = room;
            currentRoomNumber = roomNumber;
            this.totalRooms = totalRooms;
            currentEnemy = null;

            // Build room info
            BuildRoomInfo(room, roomNumber, totalRooms);

            // Sync to narrative manager
            SyncToNarrativeManager();

            // Set UI context
            SetUIContext();

            // Don't add to display buffer here - let the render methods handle it
            // This prevents duplicate messages when both RenderDungeonStart and RenderRoomEntry are called
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
            roomInfo.Clear();
            enemyInfo.Clear();
            combatLog.Clear();

            // Store current state
            currentRoom = room;
            currentRoomNumber = roomNumber;
            this.totalRooms = totalRooms;
            currentEnemy = null;

            // Build room info
            BuildRoomInfo(room, roomNumber, totalRooms);

            // Sync to narrative manager (but don't set UI context yet)
            SyncToNarrativeManager();

            // Step 2: Add content to display buffer (without auto-rendering)
            // For first room: include dungeon header + room info
            // For subsequent rooms: only room info (dungeon header already shown)
            AddCurrentInfoToDisplayBuffer(
                includeDungeonHeader: isFirstRoom,
                includeRoomInfo: true,
                autoRender: false  // RenderRoomEntry will handle rendering
            );

            // Step 3: NOW set UI context (after buffer is populated, before explicit render)
            // This ensures the context is set for RenderRoomEntry, but doesn't trigger reactive render
            // because the buffer was already added with autoRender: false
            SetUIContext();
        }

        /// <summary>
        /// Starts an enemy encounter. Adds enemy information and prepares for combat.
        /// Integrates with TextSpacingSystem to apply proper spacing between sections.
        /// </summary>
        public void StartEnemyEncounter(Enemy enemy)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));

            // Store current enemy
            currentEnemy = enemy;

            // Clear previous enemy info and combat log
            enemyInfo.Clear();
            combatLog.Clear();

            // Build enemy info
            BuildEnemyInfo(enemy);

            // Sync to narrative manager (combine header + room + enemy for dungeon log)
            SyncToNarrativeManager();

            // Set UI context
            SetUIContext();

            // Add enemy info to display buffer with proper spacing
            // Use batch transaction to ensure proper spacing between sections
            if (canvasUI != null && canvasUI is CanvasUICoordinator coordinator)
            {
                using (var batch = coordinator.StartBatch(autoRender: true))
                {
                    // Apply spacing before enemy appearance (after room info)
                    int spacingBefore = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.EnemyAppearance);
                    for (int i = 0; i < spacingBefore; i++)
                    {
                        batch.Add("");
                    }
                    
                    // Add enemy appearance line (first line of enemyInfo)
                    if (enemyInfo.Count > 0)
                    {
                        batch.Add(enemyInfo[0]);
                        TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnemyAppearance);
                    }
                    
                    // Apply spacing before enemy stats (after enemy appearance)
                    int spacingBeforeStats = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.EnemyStats);
                    for (int i = 0; i < spacingBeforeStats; i++)
                    {
                        batch.Add("");
                    }
                    
                    // Add enemy stats lines (remaining lines of enemyInfo)
                    for (int i = 1; i < enemyInfo.Count; i++)
                    {
                        batch.Add(enemyInfo[i]);
                    }
                    
                    // Record that enemy stats were displayed (after all stats lines)
                    if (enemyInfo.Count > 1)
                    {
                        TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnemyStats);
                    }
                }
            }
            else
            {
                // Fallback to old method if coordinator not available
                if (uiManager != null)
                {
                    // Apply spacing before enemy appearance
                    int spacingBefore = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.EnemyAppearance);
                    for (int i = 0; i < spacingBefore; i++)
                    {
                        uiManager.WriteLine("", UIMessageType.System);
                    }
                    
                    // Add enemy appearance
                    if (enemyInfo.Count > 0)
                    {
                        uiManager.WriteLine(enemyInfo[0], UIMessageType.System);
                        TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnemyAppearance);
                    }
                    
                    // Apply spacing before enemy stats
                    int spacingBeforeStats = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.EnemyStats);
                    for (int i = 0; i < spacingBeforeStats; i++)
                    {
                        uiManager.WriteLine("", UIMessageType.System);
                    }
                    
                    // Add enemy stats
                    for (int i = 1; i < enemyInfo.Count; i++)
                    {
                        uiManager.WriteLine(enemyInfo[i], UIMessageType.System);
                    }
                    
                    // Record enemy stats
                    if (enemyInfo.Count > 1)
                    {
                        TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.EnemyStats);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a combat event to the combat log.
        /// Also adds to display buffer and narrative manager to keep everything in sync.
        /// </summary>
        public void AddCombatEvent(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                combatLog.Add(message);
                
                // Add to narrative manager's dungeon log
                narrativeManager.LogDungeonEvent(message);
                
                // Add to display buffer so it's visible immediately
                if (uiManager != null)
                {
                    uiManager.WriteLine(message, UIMessageType.System);
                }
                
                // Notify subscribers that a combat event was added
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
            combatLog.Clear();
            narrativeManager.ClearDungeonLog();
        }

        /// <summary>
        /// Clears all information. Used when leaving a dungeon.
        /// </summary>
        public void ClearAll()
        {
            dungeonHeader.Clear();
            roomInfo.Clear();
            enemyInfo.Clear();
            combatLog.Clear();

            currentDungeon = null;
            currentRoom = null;
            currentEnemy = null;
            currentPlayer = null;
            currentRoomNumber = 0;
            totalRooms = 0;
            dungeonHeaderAddedToBuffer = false;

            narrativeManager.ResetNarrative();
        }

        /// <summary>
        /// Gets the current dungeon name.
        /// </summary>
        public string? GetDungeonName() => currentDungeon?.Name;

        /// <summary>
        /// Gets the current room name.
        /// </summary>
        public string? GetRoomName() => currentRoom?.Name;

        /// <summary>
        /// Gets the current enemy.
        /// </summary>
        public Enemy? GetCurrentEnemy() => currentEnemy;

        /// <summary>
        /// Gets the current player.
        /// </summary>
        public Character? GetCurrentPlayer() => currentPlayer;

        #region Private Methods

        private void BuildDungeonHeader(Dungeon dungeon)
        {
            dungeonHeader.Clear();

            var headerText = AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnteringDungeonHeader);
            var coloredHeader = new ColoredTextBuilder()
                .Add(headerText, ColorPalette.Gold)
                .Build();
            dungeonHeader.Add(ColoredTextRenderer.RenderAsMarkup(coloredHeader));

            char themeColorCode = DungeonThemeColors.GetThemeColorCode(dungeon.Theme);
            var dungeonNameColor = GetColorFromThemeCode(themeColorCode);

            var dungeonInfo = new ColoredTextBuilder()
                .Add("Dungeon: ", ColorPalette.Warning)
                .Add(dungeon.Name, dungeonNameColor)
                .Build();
            dungeonHeader.Add(ColoredTextRenderer.RenderAsMarkup(dungeonInfo));

            var levelInfo = new ColoredTextBuilder()
                .Add("Level Range: ", ColorPalette.Warning)
                .Add($"{dungeon.MinLevel} - {dungeon.MaxLevel}", ColorPalette.Info)
                .Build();
            dungeonHeader.Add(ColoredTextRenderer.RenderAsMarkup(levelInfo));

            var roomInfo = new ColoredTextBuilder()
                .Add("Total Rooms: ", ColorPalette.Warning)
                .Add(dungeon.Rooms.Count.ToString(), ColorPalette.Info)
                .Build();
            dungeonHeader.Add(ColoredTextRenderer.RenderAsMarkup(roomInfo));
            dungeonHeader.Add("");
        }

        private void BuildRoomInfo(Environment room, int roomNumber, int totalRooms)
        {
            roomInfo.Clear();

            var roomHeaderText = AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnteringRoomHeader);
            var coloredRoomHeader = new ColoredTextBuilder()
                .Add(roomHeaderText, ColorPalette.Gold)
                .Build();
            roomInfo.Add(ColoredTextRenderer.RenderAsMarkup(coloredRoomHeader));

            var roomNumberInfo = new ColoredTextBuilder()
                .Add("Room Number: ", ColorPalette.Info)
                .Add($"{roomNumber} of {totalRooms}", ColorPalette.Warning)
                .Build();
            roomInfo.Add(ColoredTextRenderer.RenderAsMarkup(roomNumberInfo));

            // Get environment color template based on theme
            string themeTemplate = string.IsNullOrEmpty(room.Theme) 
                ? "" 
                : room.Theme.ToLower().Replace(" ", "");
            var environmentNameColored = ColorTemplateLibrary.GetTemplate(themeTemplate, room.Name);
            
            var roomNameInfo = new ColoredTextBuilder()
                .Add("Room: ", ColorPalette.White)
                .AddRange(environmentNameColored)
                .Build();
            roomInfo.Add(ColoredTextRenderer.RenderAsMarkup(roomNameInfo));

            var roomDescription = new ColoredTextBuilder()
                .Add(room.Description, ColorPalette.White)
                .Build();
            roomInfo.Add(ColoredTextRenderer.RenderAsMarkup(roomDescription));
            roomInfo.Add("");
        }

        private void BuildEnemyInfo(Enemy enemy)
        {
            enemyInfo.Clear();

            string enemyWeaponInfo = enemy.Weapon != null
                ? string.Format(AsciiArtAssets.UIText.WeaponSuffix, enemy.Weapon.Name)
                : "";
            string encounteredText = string.Format(AsciiArtAssets.UIText.EncounteredFormat, enemy.Name, enemyWeaponInfo);
            enemyInfo.Add($"{{{{common|{encounteredText}}}}}");

            string statsText = AsciiArtAssets.UIText.FormatEnemyStats(enemy.CurrentHealth, enemy.MaxHealth, enemy.Armor);
            var statsBuilder = new ColoredTextBuilder();
            statsBuilder.Add(statsText, Colors.White);
            enemyInfo.Add(ColoredTextRenderer.RenderAsMarkup(statsBuilder.Build()));

            string attackText = AsciiArtAssets.UIText.FormatEnemyAttack(enemy.Strength, enemy.Agility, enemy.Technique, enemy.Intelligence);
            enemyInfo.Add($"{{{{enhanced_rare|{attackText}}}}}");
            enemyInfo.Add("");
        }

        private void SyncToNarrativeManager()
        {
            // Sync dungeon header
            narrativeManager.SetDungeonHeaderInfo(new List<string>(dungeonHeader));

            // Sync room info
            narrativeManager.SetRoomInfo(new List<string>(roomInfo));

            // Build dungeon log (header + room + enemy + combat log)
            var dungeonLog = new List<string>();
            dungeonLog.AddRange(dungeonHeader);
            dungeonLog.AddRange(roomInfo);
            dungeonLog.AddRange(enemyInfo);
            dungeonLog.AddRange(combatLog);

            // Clear and rebuild narrative manager's dungeon log
            narrativeManager.ClearDungeonLog();
            foreach (var line in dungeonLog)
            {
                narrativeManager.LogDungeonEvent(line);
            }
        }

        private void SetUIContext()
        {
            if (canvasUI == null) return;

            // Set all context information
            canvasUI.SetCharacter(currentPlayer);
            canvasUI.SetDungeonName(GetDungeonName());
            canvasUI.SetRoomName(GetRoomName());

            if (currentEnemy != null)
            {
                canvasUI.SetCurrentEnemy(currentEnemy);
            }

            // Set dungeon context (complete display log)
            canvasUI.SetDungeonContext(CompleteDisplayLog);
        }

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
            if (canvasUI == null || !(canvasUI is CanvasUICoordinator coordinator)) return;

            // Check if dungeon header is already in the buffer to prevent duplicates
            // Use a flag to track if header has been added (more reliable than checking TextSpacingSystem
            // since it gets reset when starting a new dungeon)
            bool dungeonHeaderAlreadyAdded = includeDungeonHeader && dungeonHeaderAddedToBuffer;

            // Use batch transaction with specified autoRender setting
            // When autoRender=false, RenderRoomEntry() will handle the initial render
            // When autoRender=true, reactive system will handle rendering
            using (var batch = coordinator.StartBatch(autoRender: autoRender))
            {
                // Add dungeon header if requested, available, and not already added
                if (includeDungeonHeader && dungeonHeader.Count > 0 && !dungeonHeaderAlreadyAdded)
                {
                    // Apply spacing before dungeon header (if needed)
                    int spacingBefore = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.DungeonHeader);
                    for (int i = 0; i < spacingBefore; i++)
                    {
                        batch.Add("");
                    }
                    
                    // Add dungeon header lines
                    batch.AddRange(dungeonHeader);
                    
                    // Record that dungeon header was displayed
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.DungeonHeader);
                    
                    // Mark that header has been added to prevent duplicates
                    dungeonHeaderAddedToBuffer = true;
                }

                // Add room info if requested and available
                if (includeRoomInfo && roomInfo.Count > 0)
                {
                    // Apply spacing before room header (if needed, e.g., after dungeon header)
                    // Use RoomHeader for spacing calculation since that's what the spacing rules expect
                    int spacingBefore = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.RoomHeader);
                    for (int i = 0; i < spacingBefore; i++)
                    {
                        batch.Add("");
                    }
                    
                    // Add room info lines (includes room header + room details)
                    batch.AddRange(roomInfo);
                    
                    // Record that room info was displayed
                    // Note: roomInfo includes both RoomHeader ("=== ENTERING ROOM ===") and RoomInfo (room details)
                    // We record RoomInfo since that's the main block type for the complete room information
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.RoomInfo);
                }
            }
        }

        /// <summary>
        /// Adds lines to display buffer and triggers render.
        /// For cases where RenderRoomEntry will handle rendering, use AddCurrentInfoToDisplayBuffer instead.
        /// </summary>
        private void AddToDisplayBuffer(List<string> lines)
        {
            if (uiManager == null) return;

            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    uiManager.WriteLine(line, UIMessageType.System);
                }
            }
        }

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

