using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages animations for the canvas UI, including undulation and brightness mask effects
    /// </summary>
    public class CanvasAnimationManager : ICanvasAnimationManager, IDisposable
    {
        private readonly GameCanvasControl canvas;
        private DungeonRenderer? dungeonRenderer;
        private Action<Character, List<Dungeon>>? reRenderCallback;
        private System.Action? critLineReRenderCallback = null; // Callback to re-render display buffer for crit lines
        private GameStateManager? stateManager = null; // Reference to state manager for event subscription
        
        // Animation state
        private bool isDungeonSelectionActive = false;
        private Character? dungeonSelectionPlayer = null;
        private List<Dungeon>? dungeonSelectionList = null;
        
        // Animation timers
        private Timer? undulationTimer = null;
        private Timer? brightnessMaskTimer = null;
        private Timer? critUndulationTimer = null;
        private Timer? critBrightnessMaskTimer = null;
        
        // Animation configuration
        private int undulationInterval;
        private int brightnessMaskInterval;
        private readonly UIConfiguration uiConfig;
        
        // Render throttling to prevent excessive renders
        private DateTime lastRenderTime = DateTime.MinValue;
        private readonly int minRenderIntervalMs = 42; // ~24fps max render rate (1/24th of a second ≈ 41.67ms, rounded to 42ms)
        private readonly object renderThrottleLock = new object();
        
        public CanvasAnimationManager(GameCanvasControl canvas, DungeonRenderer? dungeonRenderer, Action<Character, List<Dungeon>>? reRenderCallback)
        {
            this.canvas = canvas;
            this.dungeonRenderer = dungeonRenderer;
            this.reRenderCallback = reRenderCallback;
            
            // Load animation intervals from UIConfiguration
            this.uiConfig = UIConfiguration.LoadFromFile();
            var animConfig = this.uiConfig.DungeonSelectionAnimation;
            
            this.undulationInterval = animConfig.UndulationIntervalMs;
            this.brightnessMaskInterval = animConfig.BrightnessMask.UpdateIntervalMs;
            
            // Start animation timers
            StartAnimationTimers();
        }
        
        /// <summary>
        /// Starts the animation timers
        /// </summary>
        private void StartAnimationTimers()
        {
            // Start undulation animation timer (always enabled for dungeon selection)
            undulationTimer = new Timer(UpdateUndulation, null, undulationInterval, undulationInterval);
            
            // Start brightness mask timer only if enabled in configuration
            var dungeonAnimConfig = uiConfig.DungeonSelectionAnimation;
            if (dungeonAnimConfig?.BrightnessMask?.Enabled == true)
            {
                brightnessMaskTimer = new Timer(UpdateBrightnessMask, null, brightnessMaskInterval, brightnessMaskInterval);
            }
            
            // Start crit animation timers (always running for crit lines)
            critUndulationTimer = new Timer(UpdateCritUndulation, null, undulationInterval, undulationInterval);
            if (dungeonAnimConfig?.BrightnessMask?.Enabled == true)
            {
                critBrightnessMaskTimer = new Timer(UpdateCritBrightnessMask, null, brightnessMaskInterval, brightnessMaskInterval);
            }
        }
        
        /// <summary>
        /// Starts the dungeon selection animation
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="dungeons">List of available dungeons</param>
        public void StartDungeonSelectionAnimation(Character player, List<Dungeon> dungeons)
        {
            isDungeonSelectionActive = true;
            dungeonSelectionPlayer = player;
            dungeonSelectionList = dungeons;
        }
        
        /// <summary>
        /// Stops the dungeon selection animation
        /// </summary>
        public void StopDungeonSelectionAnimation()
        {
            isDungeonSelectionActive = false;
            dungeonSelectionPlayer = null;
            dungeonSelectionList = null;
        }
        
        /// <summary>
        /// Handles state change events to automatically stop animation when leaving dungeon selection
        /// and pause crit line animations during menu states
        /// </summary>
        private void OnStateChanged(object? sender, StateChangedEventArgs e)
        {
            // If we're leaving dungeon selection state, stop the animation
            if (e.PreviousState == GameState.DungeonSelection && e.NewState != GameState.DungeonSelection)
            {
                StopDungeonSelectionAnimation();
            }
            
            // Pause crit line animations during menu states to prevent them from triggering renders
            // that interfere with menu rendering
            bool previousWasMenu = IsMenuState(e.PreviousState);
            bool currentIsMenu = IsMenuState(e.NewState);
            
            if (!previousWasMenu && currentIsMenu)
            {
                // Entering menu state - pause crit line animations
                critUndulationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                critBrightnessMaskTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else if (previousWasMenu && !currentIsMenu)
            {
                // Leaving menu state - resume crit line animations
                critUndulationTimer?.Change(undulationInterval, undulationInterval);
                var dungeonAnimConfig = uiConfig.DungeonSelectionAnimation;
                if (dungeonAnimConfig?.BrightnessMask?.Enabled == true)
                {
                    critBrightnessMaskTimer?.Change(brightnessMaskInterval, brightnessMaskInterval);
                }
            }
        }
        
        /// <summary>
        /// Checks if a game state is a menu state (where crit line animations should be paused)
        /// </summary>
        private bool IsMenuState(GameState state)
        {
            return Display.DisplayStateCoordinator.IsMenuState(state);
        }
        
        /// <summary>
        /// Checks if dungeon selection animation is currently active
        /// </summary>
        public bool IsDungeonSelectionActive => isDungeonSelectionActive;
        
        /// <summary>
        /// Gets the current dungeon selection player
        /// </summary>
        public Character? DungeonSelectionPlayer => dungeonSelectionPlayer;
        
        /// <summary>
        /// Gets the current dungeon selection list
        /// </summary>
        public List<Dungeon>? DungeonSelectionList => dungeonSelectionList;
        
        /// <summary>
        /// Throttled render method that prevents excessive renders
        /// Only renders if enough time has passed since last render
        /// </summary>
        private void ThrottledRender()
        {
            lock (renderThrottleLock)
            {
                var now = DateTime.Now;
                var timeSinceLastRender = (now - lastRenderTime).TotalMilliseconds;
                
                // If we rendered recently, skip this render
                if (timeSinceLastRender < minRenderIntervalMs)
                {
                    return;
                }
                
                lastRenderTime = now;
            }
            
            // Perform the actual render
            // Double-check animation is still active before rendering
            // State changes will automatically stop the animation via event handler
            if (!isDungeonSelectionActive)
            {
                return;
            }
            
            // Additional safety check: verify we're still in dungeon selection state
            // This handles edge cases where events might not fire immediately
            if (stateManager != null && stateManager.CurrentState != GameState.DungeonSelection)
            {
                StopDungeonSelectionAnimation();
                return;
            }
            
            if (dungeonSelectionPlayer != null && dungeonSelectionList != null && reRenderCallback != null)
            {
                // Capture local copies to avoid race conditions
                var player = dungeonSelectionPlayer;
                var dungeons = dungeonSelectionList;
                var callback = reRenderCallback;
                
                // Re-render the dungeon selection on UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    // Final check: animation state and data validity before rendering
                    if (!isDungeonSelectionActive)
                    {
                        return;
                    }
                    
                    // Final state check on UI thread
                    if (stateManager != null && stateManager.CurrentState != GameState.DungeonSelection)
                    {
                        return;
                    }
                    
                    if (player != null && dungeons != null && callback != null)
                    {
                        callback(player, dungeons);
                    }
                }, DispatcherPriority.Background);
            }
        }
        
        /// <summary>
        /// Update callback for undulation animation
        /// </summary>
        private void UpdateUndulation(object? state)
        {
            // Update centralized animation state (used by both dungeon selection and room entry)
            var animationState = Managers.DungeonSelectionAnimationState.Instance;
            animationState.AdvanceUndulation();
            
            if (isDungeonSelectionActive)
            {
                // Trigger throttled render for dungeon selection
                ThrottledRender();
            }
            
            // Also trigger re-render for display buffer (for "ENTERING DUNGEON" lines)
            ThrottledCritLineRender();
        }
        
        /// <summary>
        /// Update callback for brightness mask animation (separate from undulation)
        /// </summary>
        private void UpdateBrightnessMask(object? state)
        {
            // Update centralized animation state (used by both dungeon selection and room entry)
            var animationState = Managers.DungeonSelectionAnimationState.Instance;
            animationState.AdvanceBrightnessMask();
            
            if (isDungeonSelectionActive)
            {
                // Trigger throttled render for dungeon selection
                ThrottledRender();
            }
            
            // Also trigger re-render for display buffer (for "ENTERING DUNGEON" lines)
            ThrottledCritLineRender();
        }
        
        /// <summary>
        /// Update callback for crit line undulation animation
        /// </summary>
        private void UpdateCritUndulation(object? state)
        {
            // Always update crit animation state (no condition needed - animation runs continuously)
            var critAnimationState = Managers.CritAnimationState.Instance;
            critAnimationState.AdvanceUndulation();
            
            // Trigger throttled re-render of display buffer to show animation
            ThrottledCritLineRender();
        }
        
        /// <summary>
        /// Update callback for crit line brightness mask animation
        /// </summary>
        private void UpdateCritBrightnessMask(object? state)
        {
            // Always update crit animation state (no condition needed - animation runs continuously)
            var critAnimationState = Managers.CritAnimationState.Instance;
            critAnimationState.AdvanceBrightnessMask();
            
            // Trigger throttled re-render of display buffer to show animation
            ThrottledCritLineRender();
        }
        
        /// <summary>
        /// Throttled render method for crit lines that prevents excessive renders
        /// Only renders if enough time has passed since last render
        /// </summary>
        private void ThrottledCritLineRender()
        {
            if (critLineReRenderCallback == null)
                return;
            
            // CRITICAL: Don't trigger display buffer renders during menu states or when stateManager is null
            // Menu screens and title screen handle their own rendering and don't use the display buffer
            // This prevents the center panel from being cleared when menus/title screen are displayed
            if (DisplayStateCoordinator.ShouldSuppressRendering(stateManager?.CurrentState, stateManager))
            {
                return;
            }
            
            lock (renderThrottleLock)
            {
                var now = DateTime.Now;
                var timeSinceLastRender = (now - lastRenderTime).TotalMilliseconds;
                
                // If we rendered recently, skip this render
                if (timeSinceLastRender < minRenderIntervalMs)
                {
                    return;
                }
                
                lastRenderTime = now;
            }
            
            // Re-render the display buffer on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                critLineReRenderCallback?.Invoke();
            }, DispatcherPriority.Background);
        }
        
        /// <summary>
        /// Pauses all animations
        /// </summary>
        public void PauseAnimations()
        {
            undulationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            brightnessMaskTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            critUndulationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            critBrightnessMaskTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
        
        /// <summary>
        /// Resumes all animations
        /// </summary>
        public void ResumeAnimations()
        {
            undulationTimer?.Change(undulationInterval, undulationInterval);
            brightnessMaskTimer?.Change(brightnessMaskInterval, brightnessMaskInterval);
            critUndulationTimer?.Change(undulationInterval, undulationInterval);
            critBrightnessMaskTimer?.Change(brightnessMaskInterval, brightnessMaskInterval);
        }
        
        /// <summary>
        /// Updates animation configuration
        /// </summary>
        /// <param name="newUndulationInterval">New undulation interval in milliseconds</param>
        /// <param name="newBrightnessMaskInterval">New brightness mask interval in milliseconds</param>
        public void UpdateAnimationConfiguration(int newUndulationInterval, int newBrightnessMaskInterval)
        {
            // Stop current timers
            undulationTimer?.Dispose();
            brightnessMaskTimer?.Dispose();
            
            // Create new timers with updated intervals
            undulationTimer = new Timer(UpdateUndulation, null, newUndulationInterval, newUndulationInterval);
            brightnessMaskTimer = new Timer(UpdateBrightnessMask, null, newBrightnessMaskInterval, newBrightnessMaskInterval);
        }
        
        /// <summary>
        /// Gets the current animation configuration
        /// </summary>
        /// <returns>Tuple of (undulationInterval, brightnessMaskInterval)</returns>
        public (int undulationInterval, int brightnessMaskInterval) GetAnimationConfiguration()
        {
            return (undulationInterval, brightnessMaskInterval);
        }
        
        /// <summary>
        /// Reloads animation configuration from UIConfiguration and updates timers
        /// Called when settings are changed to apply new values in real-time
        /// </summary>
        public void ReloadAnimationConfiguration()
        {
            var uiConfig = UIConfiguration.LoadFromFile();
            var animConfig = uiConfig.DungeonSelectionAnimation;
            
            // Update intervals
            int newUndulationInterval = animConfig.UndulationIntervalMs;
            int newBrightnessMaskInterval = animConfig.BrightnessMask.UpdateIntervalMs;
            
            // Update timer intervals if they changed
            if (newUndulationInterval != undulationInterval)
            {
                undulationInterval = newUndulationInterval;
                undulationTimer?.Change(newUndulationInterval, newUndulationInterval);
                critUndulationTimer?.Change(newUndulationInterval, newUndulationInterval);
            }
            
            if (newBrightnessMaskInterval != brightnessMaskInterval)
            {
                brightnessMaskInterval = newBrightnessMaskInterval;
                brightnessMaskTimer?.Change(newBrightnessMaskInterval, newBrightnessMaskInterval);
                critBrightnessMaskTimer?.Change(newBrightnessMaskInterval, newBrightnessMaskInterval);
            }
            
            // Reload configuration in animation states
            var dungeonState = Managers.DungeonSelectionAnimationState.Instance;
            var critState = Managers.CritAnimationState.Instance;
            
            dungeonState.ReloadConfiguration();
            critState.ReloadConfiguration();
        }
        
        /// <summary>
        /// Sets up the animation manager with proper renderer and callback
        /// This method should be called after the UI coordinator is fully initialized
        /// </summary>
        public void SetupAnimationManager(DungeonRenderer dungeonRenderer, Action<Character, List<Dungeon>> reRenderCallback, GameStateManager? stateManager = null)
        {
            this.dungeonRenderer = dungeonRenderer;
            this.reRenderCallback = reRenderCallback;
            
            // Unsubscribe from previous state manager if any
            if (this.stateManager != null)
            {
                this.stateManager.StateChanged -= OnStateChanged;
            }
            
            // Subscribe to new state manager
            this.stateManager = stateManager;
            if (this.stateManager != null)
            {
                this.stateManager.StateChanged += OnStateChanged;
            }
        }
        
        /// <summary>
        /// Sets the callback for re-rendering the display buffer when crit lines are animated
        /// This allows the animation to be visible by triggering periodic re-renders
        /// </summary>
        public void SetCritLineReRenderCallback(System.Action? callback)
        {
            this.critLineReRenderCallback = callback;
        }
        
        /// <summary>
        /// Disposes of the animation manager and cleans up resources
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe from state manager
            if (stateManager != null)
            {
                stateManager.StateChanged -= OnStateChanged;
                stateManager = null;
            }
            
            // Stop animation timers
            undulationTimer?.Dispose();
            brightnessMaskTimer?.Dispose();
            critUndulationTimer?.Dispose();
            critBrightnessMaskTimer?.Dispose();
            
            // Clear animation state
            isDungeonSelectionActive = false;
            dungeonSelectionPlayer = null;
            dungeonSelectionList = null;
        }
    }
}
