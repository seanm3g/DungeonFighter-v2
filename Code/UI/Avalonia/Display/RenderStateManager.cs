using RPGGame;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Manages render state to determine when rendering is needed
    /// Separates state tracking from rendering logic
    /// </summary>
    public class RenderStateManager
    {
        // Cached render state
        private Character? lastRenderedCharacter;
        private Enemy? lastRenderedEnemy;
        private string? lastRenderedDungeonName;
        private string? lastRenderedRoomName;
        private bool layoutInitialized = false;
        private int lastBufferCount = 0;
        private int lastScrollOffset = 0;
        private bool lastIsManualScrolling = false;
        
        /// <summary>
        /// Determines if a render is needed based on current state
        /// </summary>
        public RenderState GetRenderState(DisplayBuffer buffer, ICanvasContextManager contextManager, GameStateManager? stateManager = null)
        {
            var currentCharacter = contextManager.GetCurrentCharacter();
            var currentEnemy = contextManager.GetCurrentEnemy();
            var dungeonName = contextManager.GetDungeonName();
            var roomName = contextManager.GetRoomName();
            
            // CRITICAL: If there's an enemy but the character is not active, clear the enemy
            // This prevents background combat enemies from being included in render state
            if (currentEnemy != null && stateManager != null && currentCharacter != null)
            {
                var activeCharacter = stateManager.GetActiveCharacter();
                if (currentCharacter != activeCharacter)
                {
                    // Character is not active - this enemy is from background combat, don't include it
                    currentEnemy = null;
                }
            }
            
            // Check if we need to re-render the full layout
            bool needsFullRender = !layoutInitialized ||
                !ReferenceEquals(currentCharacter, lastRenderedCharacter) ||
                !ReferenceEquals(currentEnemy, lastRenderedEnemy) ||
                dungeonName != lastRenderedDungeonName ||
                roomName != lastRenderedRoomName;
            
            // Check if buffer changed
            bool bufferChanged = buffer.Count != lastBufferCount;
            
            // Check if scroll state changed (scroll offset or manual scrolling flag)
            bool scrollChanged = buffer.ManualScrollOffset != lastScrollOffset || 
                                 buffer.IsManualScrolling != lastIsManualScrolling;
            
            // Determine if this is a major state change
            bool isMajorStateChange = !layoutInitialized ||
                !ReferenceEquals(currentCharacter, lastRenderedCharacter) ||
                dungeonName != lastRenderedDungeonName ||
                roomName != lastRenderedRoomName;
            
            return new RenderState
            {
                NeedsRender = needsFullRender || bufferChanged || scrollChanged,
                NeedsFullLayout = needsFullRender,
                IsMajorStateChange = isMajorStateChange,
                CurrentCharacter = currentCharacter,
                CurrentEnemy = currentEnemy,
                DungeonName = dungeonName,
                RoomName = roomName
            };
        }
        
        /// <summary>
        /// Records that a render has been performed
        /// Updates cached state to match current state
        /// </summary>
        public void RecordRender(DisplayBuffer buffer, ICanvasContextManager contextManager)
        {
            lastRenderedCharacter = contextManager.GetCurrentCharacter();
            lastRenderedEnemy = contextManager.GetCurrentEnemy();
            lastRenderedDungeonName = contextManager.GetDungeonName();
            lastRenderedRoomName = contextManager.GetRoomName();
            lastBufferCount = buffer.Count;
            lastScrollOffset = buffer.ManualScrollOffset;
            lastIsManualScrolling = buffer.IsManualScrolling;
            layoutInitialized = true;
        }
        
        /// <summary>
        /// Resets the render state (used when clearing display)
        /// </summary>
        public void Reset()
        {
            layoutInitialized = false;
            lastBufferCount = 0;
            lastScrollOffset = 0;
            lastIsManualScrolling = false;
            lastRenderedCharacter = null;
            lastRenderedEnemy = null;
            lastRenderedDungeonName = null;
            lastRenderedRoomName = null;
        }
        
        /// <summary>
        /// Determines if canvas should be cleared based on state change
        /// Only clears on major state changes (new dungeon, new character), not on room changes within same dungeon
        /// </summary>
        public bool ShouldClearCanvas(RenderState state, DisplayMode mode)
        {
            if (!mode.ClearOnStateChange) return false;
            
            // Only clear on truly major state changes:
            // - New character (different reference)
            // - New dungeon (different dungeon name)
            // - Not initialized yet
            // Do NOT clear on room changes within the same dungeon
            bool isTrulyMajorChange = !layoutInitialized ||
                !ReferenceEquals(state.CurrentCharacter, lastRenderedCharacter) ||
                state.DungeonName != lastRenderedDungeonName;
            
            return isTrulyMajorChange;
        }
    }
    
    /// <summary>
    /// Represents the current render state
    /// </summary>
    public class RenderState
    {
        public bool NeedsRender { get; set; }
        public bool NeedsFullLayout { get; set; }
        public bool IsMajorStateChange { get; set; }
        public Character? CurrentCharacter { get; set; }
        public Enemy? CurrentEnemy { get; set; }
        public string? DungeonName { get; set; }
        public string? RoomName { get; set; }
    }
}

