using System;
using System.Collections.Generic;
using RPGGame.Display.Dungeon;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;
using RPGGame.GameCore.Display.Helpers;

namespace RPGGame.Display.Dungeon
{
    /// <summary>
    /// Coordinates adding dungeon and room information to the display buffer
    /// Extracted from DungeonDisplayManager to separate buffer management logic
    /// </summary>
    public class DungeonDisplayBufferCoordinator
    {
        private readonly DungeonDisplayBuffer displayBuffer;
        private readonly CanvasUICoordinator? canvasUI;
        private bool dungeonHeaderAddedToBuffer = false;

        public DungeonDisplayBufferCoordinator(DungeonDisplayBuffer displayBuffer, CanvasUICoordinator? canvasUI)
        {
            this.displayBuffer = displayBuffer ?? throw new ArgumentNullException(nameof(displayBuffer));
            this.canvasUI = canvasUI;
        }

        /// <summary>
        /// Resets the flag indicating whether the dungeon header has been added to the buffer
        /// </summary>
        public void ResetDungeonHeaderFlag()
        {
            dungeonHeaderAddedToBuffer = false;
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
                var dungeonHeader = displayBuffer.DungeonHeader;
                if (includeDungeonHeader && dungeonHeader.Count > 0 && !dungeonHeaderAlreadyAdded)
                {
                    SpacingApplier.ApplySpacingBefore(batch, TextSpacingSystem.BlockType.DungeonHeader);
                    batch.AddRange(dungeonHeader);
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.DungeonHeader);
                    dungeonHeaderAddedToBuffer = true;
                }

                var roomInfo = displayBuffer.RoomInfo;
                if (includeRoomInfo && roomInfo.Count > 0)
                {
                    SpacingApplier.ApplySpacingBefore(batch, TextSpacingSystem.BlockType.RoomHeader);
                    batch.AddRange(roomInfo);
                    TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.RoomInfo);
                }
            }
        }
    }
}

