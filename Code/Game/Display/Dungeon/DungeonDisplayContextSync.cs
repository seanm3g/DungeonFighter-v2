using System;
using System.Collections.Generic;
using RPGGame.Display.Dungeon;
using RPGGame.UI.Avalonia;

namespace RPGGame.Display.Dungeon
{
    /// <summary>
    /// Handles synchronization between DungeonDisplayManager and other systems
    /// Extracted from DungeonDisplayManager to separate context synchronization logic
    /// </summary>
    public class DungeonDisplayContextSync
    {
        private readonly GameNarrativeManager narrativeManager;
        private readonly DungeonDisplayBuffer displayBuffer;
        private readonly CanvasUICoordinator? canvasUI;

        public DungeonDisplayContextSync(
            GameNarrativeManager narrativeManager,
            DungeonDisplayBuffer displayBuffer,
            CanvasUICoordinator? canvasUI)
        {
            this.narrativeManager = narrativeManager ?? throw new ArgumentNullException(nameof(narrativeManager));
            this.displayBuffer = displayBuffer ?? throw new ArgumentNullException(nameof(displayBuffer));
            this.canvasUI = canvasUI;
        }

        /// <summary>
        /// Syncs the display buffer state to the narrative manager
        /// </summary>
        public void SyncToNarrativeManager()
        {
            // Sync dungeon header
            narrativeManager.SetDungeonHeaderInfo(displayBuffer.DungeonHeader);

            // Sync room info
            narrativeManager.SetRoomInfo(displayBuffer.RoomInfo);

            // Build dungeon log (header + room + enemy + combat log)
            var dungeonLog = displayBuffer.CompleteDisplayLog;

            // Clear and rebuild narrative manager's dungeon log
            narrativeManager.ClearDungeonLog();
            foreach (var line in dungeonLog)
            {
                narrativeManager.LogDungeonEvent(line);
            }
        }

        /// <summary>
        /// Sets the UI context based on the current display buffer state
        /// </summary>
        public void SetUIContext(Character? currentPlayer, string? dungeonName, string? roomName, Enemy? currentEnemy)
        {
            if (canvasUI == null) return;

            // Set all context information
            canvasUI.SetCharacter(currentPlayer);
            canvasUI.SetDungeonName(dungeonName);
            canvasUI.SetRoomName(roomName);

            if (currentEnemy != null)
            {
                canvasUI.SetCurrentEnemy(currentEnemy);
            }

            // Set dungeon context (complete display log)
            canvasUI.SetDungeonContext(displayBuffer.CompleteDisplayLog);
        }
    }
}

