namespace RPGGame.UI.Avalonia.Display.Mode
{
    using RPGGame.UI.Avalonia.Display;

    /// <summary>
    /// Manages display mode switching and state.
    /// </summary>
    public class DisplayModeManager
    {
        private DisplayMode currentMode;
        private readonly DisplayTiming timing;
        
        public DisplayModeManager(DisplayMode initialMode)
        {
            this.currentMode = initialMode ?? new StandardDisplayMode();
            this.timing = new DisplayTiming(currentMode);
        }
        
        /// <summary>
        /// Gets the current display mode
        /// </summary>
        public DisplayMode CurrentMode => currentMode;
        
        /// <summary>
        /// Gets the display timing instance
        /// </summary>
        public DisplayTiming Timing => timing;
        
        /// <summary>
        /// Sets the display mode (Standard, Combat, Menu, etc.)
        /// </summary>
        public void SetMode(DisplayMode mode, System.Action cancelPending)
        {
            if (mode == null) return;
            
            // If switching modes, cancel any pending renders
            if (mode.GetType() != currentMode.GetType())
            {
                cancelPending?.Invoke();
            }
            
            currentMode = mode;
            // Note: Timing is recreated in the caller to ensure it uses the new mode
        }
    }
}

