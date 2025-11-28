namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Configuration for different display modes
    /// Each mode has its own timing and rendering characteristics
    /// </summary>
    public abstract class DisplayMode
    {
        /// <summary>
        /// Debounce delay in milliseconds (how long to wait before rendering after last update)
        /// </summary>
        public abstract int DebounceMs { get; }
        
        /// <summary>
        /// Minimum delay between renders in milliseconds
        /// </summary>
        public abstract int MinRenderDelayMs { get; }
        
        /// <summary>
        /// Whether to auto-scroll to bottom when new content is added
        /// </summary>
        public abstract bool AutoScroll { get; }
        
        /// <summary>
        /// Whether to clear canvas on major state changes
        /// </summary>
        public abstract bool ClearOnStateChange { get; }
    }
    
    /// <summary>
    /// Standard display mode for menus, inventory, exploration
    /// Fast, responsive rendering
    /// </summary>
    public class StandardDisplayMode : DisplayMode
    {
        public override int DebounceMs => 8; // 120fps equivalent for smooth updates
        public override int MinRenderDelayMs => 0;
        public override bool AutoScroll => true;
        public override bool ClearOnStateChange => true;
    }
    
    /// <summary>
    /// Combat display mode
    /// Slightly slower rendering to batch rapid combat messages
    /// </summary>
    public class CombatDisplayMode : DisplayMode
    {
        public override int DebounceMs => 50; // Batch rapid combat messages
        public override int MinRenderDelayMs => 0; // No minimum delay between turns
        public override bool AutoScroll => true;
        public override bool ClearOnStateChange => false; // Preserve content when transitioning to combat
    }
    
    /// <summary>
    /// Menu display mode
    /// Very fast, instant rendering for responsive menus
    /// </summary>
    public class MenuDisplayMode : DisplayMode
    {
        public override int DebounceMs => 0; // Instant rendering
        public override int MinRenderDelayMs => 0;
        public override bool AutoScroll => false; // Menus don't scroll
        public override bool ClearOnStateChange => true;
    }
}

