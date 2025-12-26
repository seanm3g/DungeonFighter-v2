using System;
using System.Threading;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Display;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages crit line animations (undulation and brightness mask effects).
    /// Extracted from CanvasAnimationManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CritLineAnimationManager
    {
        private GameStateManager? stateManager;
        private System.Action? critLineReRenderCallback;
        
        // Animation timers
        private Timer? critUndulationTimer;
        private Timer? critBrightnessMaskTimer;
        
        // Animation configuration
        private int undulationInterval;
        private int brightnessMaskInterval;
        private readonly UIConfiguration uiConfig;
        
        // Render throttling to prevent excessive renders
        private DateTime lastRenderTime = DateTime.MinValue;
        private readonly int minRenderIntervalMs = 42; // ~24fps max render rate
        private readonly object renderThrottleLock = new object();

        public CritLineAnimationManager(GameStateManager? stateManager = null)
        {
            this.stateManager = stateManager;
            
            // Load animation intervals from UIConfiguration
            this.uiConfig = UIConfiguration.LoadFromFile();
            var animConfig = this.uiConfig.DungeonSelectionAnimation;
            
            this.undulationInterval = animConfig.UndulationIntervalMs;
            this.brightnessMaskInterval = animConfig.BrightnessMask.UpdateIntervalMs;
            
            // Start animation timers
            StartAnimationTimers();
        }

        /// <summary>
        /// Starts the crit line animation timers
        /// </summary>
        private void StartAnimationTimers()
        {
            // Start crit animation timers (always running for crit lines)
            critUndulationTimer = new Timer(UpdateCritUndulation, null, undulationInterval, undulationInterval);
            var dungeonAnimConfig = uiConfig.DungeonSelectionAnimation;
            if (dungeonAnimConfig?.BrightnessMask?.Enabled == true)
            {
                critBrightnessMaskTimer = new Timer(UpdateCritBrightnessMask, null, brightnessMaskInterval, brightnessMaskInterval);
            }
        }

        /// <summary>
        /// Handles state change events to pause crit line animations during menu states
        /// </summary>
        public void OnStateChanged(GameState previousState, GameState newState)
        {
            // Pause crit line animations during menu states to prevent them from triggering renders
            // that interfere with menu rendering
            bool previousWasMenu = Display.DisplayStateCoordinator.IsMenuState(previousState);
            bool currentIsMenu = Display.DisplayStateCoordinator.IsMenuState(newState);
            
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
        /// Pauses crit line animations
        /// </summary>
        public void PauseAnimations()
        {
            critUndulationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            critBrightnessMaskTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
        
        /// <summary>
        /// Resumes crit line animations
        /// </summary>
        public void ResumeAnimations()
        {
            critUndulationTimer?.Change(undulationInterval, undulationInterval);
            var dungeonAnimConfig = uiConfig.DungeonSelectionAnimation;
            if (dungeonAnimConfig?.BrightnessMask?.Enabled == true)
            {
                critBrightnessMaskTimer?.Change(brightnessMaskInterval, brightnessMaskInterval);
            }
        }

        /// <summary>
        /// Sets the callback for re-rendering the display buffer when crit lines are animated
        /// </summary>
        public void SetCritLineReRenderCallback(System.Action? callback)
        {
            this.critLineReRenderCallback = callback;
        }

        /// <summary>
        /// Updates animation configuration intervals
        /// </summary>
        public void UpdateIntervals(int newUndulationInterval, int newBrightnessMaskInterval)
        {
            if (newUndulationInterval != undulationInterval)
            {
                undulationInterval = newUndulationInterval;
                critUndulationTimer?.Change(newUndulationInterval, newUndulationInterval);
            }
            
            if (newBrightnessMaskInterval != brightnessMaskInterval)
            {
                brightnessMaskInterval = newBrightnessMaskInterval;
                critBrightnessMaskTimer?.Change(newBrightnessMaskInterval, newBrightnessMaskInterval);
            }
        }

        /// <summary>
        /// Sets the state manager for menu state detection
        /// </summary>
        public void SetStateManager(GameStateManager? stateManager)
        {
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Reloads animation configuration from UIConfiguration and updates timers
        /// </summary>
        public void ReloadConfiguration()
        {
            var uiConfig = UIConfiguration.LoadFromFile();
            var animConfig = uiConfig.DungeonSelectionAnimation;
            
            // Update intervals
            int newUndulationInterval = animConfig.UndulationIntervalMs;
            int newBrightnessMaskInterval = animConfig.BrightnessMask.UpdateIntervalMs;
            
            UpdateIntervals(newUndulationInterval, newBrightnessMaskInterval);
            
            // Reload configuration in crit animation state
            var critState = Managers.CritAnimationState.Instance;
            critState.ReloadConfiguration();
        }

        /// <summary>
        /// Disposes of the animation manager and cleans up resources
        /// </summary>
        public void Dispose()
        {
            // Stop animation timers
            critUndulationTimer?.Dispose();
            critBrightnessMaskTimer?.Dispose();
        }
    }
}

