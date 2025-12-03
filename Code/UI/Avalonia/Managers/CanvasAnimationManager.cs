using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;
using RPGGame;
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
        
        // Animation state
        private bool isDungeonSelectionActive = false;
        private Character? dungeonSelectionPlayer = null;
        private List<Dungeon>? dungeonSelectionList = null;
        
        // Animation timers
        private Timer? undulationTimer = null;
        private Timer? brightnessMaskTimer = null;
        
        // Animation configuration
        private readonly int undulationInterval;
        private readonly int brightnessMaskInterval;
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
        }
        
        /// <summary>
        /// Starts the dungeon selection animation
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="dungeons">List of available dungeons</param>
        public void StartDungeonSelectionAnimation(Character player, List<Dungeon> dungeons)
        {
            ScrollDebugLogger.Log($"[ANIMATION] StartDungeonSelectionAnimation called - player: {player != null}, dungeons: {dungeons?.Count ?? 0}");
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
                    ScrollDebugLogger.Log($"[ANIMATION] ThrottledRender skipped - only {timeSinceLastRender}ms since last render");
                    return;
                }
                
                lastRenderTime = now;
            }
            
            // Perform the actual render
            ScrollDebugLogger.Log($"[ANIMATION] ThrottledRender - isDungeonSelectionActive: {isDungeonSelectionActive}, player: {dungeonSelectionPlayer != null}, dungeons: {dungeonSelectionList != null}, callback: {reRenderCallback != null}");
            if (isDungeonSelectionActive && dungeonSelectionPlayer != null && dungeonSelectionList != null && reRenderCallback != null)
            {
                // Capture local copies to avoid race conditions
                var player = dungeonSelectionPlayer;
                var dungeons = dungeonSelectionList;
                var callback = reRenderCallback;
                
                // Re-render the dungeon selection on UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    // Double-check that we still have valid data before rendering
                    if (player != null && dungeons != null && callback != null)
                    {
                        ScrollDebugLogger.Log($"[ANIMATION] Calling re-render callback");
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
            if (isDungeonSelectionActive)
            {
                // Update centralized animation state
                var animationState = Managers.DungeonSelectionAnimationState.Instance;
                animationState.AdvanceUndulation();
                
                // Trigger throttled render
                ThrottledRender();
            }
        }
        
        /// <summary>
        /// Update callback for brightness mask animation (separate from undulation)
        /// </summary>
        private void UpdateBrightnessMask(object? state)
        {
            if (isDungeonSelectionActive)
            {
                // Update centralized animation state
                var animationState = Managers.DungeonSelectionAnimationState.Instance;
                int oldOffset = animationState.BrightnessMaskOffset;
                animationState.AdvanceBrightnessMask();
                ScrollDebugLogger.Log($"[ANIMATION] Brightness mask offset: {oldOffset} -> {animationState.BrightnessMaskOffset}");
                
                // Trigger throttled render
                ThrottledRender();
            }
        }
        
        /// <summary>
        /// Pauses all animations
        /// </summary>
        public void PauseAnimations()
        {
            undulationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            brightnessMaskTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
        
        /// <summary>
        /// Resumes all animations
        /// </summary>
        public void ResumeAnimations()
        {
            undulationTimer?.Change(undulationInterval, undulationInterval);
            brightnessMaskTimer?.Change(brightnessMaskInterval, brightnessMaskInterval);
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
        /// Sets up the animation manager with proper renderer and callback
        /// This method should be called after the UI coordinator is fully initialized
        /// </summary>
        public void SetupAnimationManager(DungeonRenderer dungeonRenderer, Action<Character, List<Dungeon>> reRenderCallback)
        {
            this.dungeonRenderer = dungeonRenderer;
            this.reRenderCallback = reRenderCallback;
        }
        
        /// <summary>
        /// Disposes of the animation manager and cleans up resources
        /// </summary>
        public void Dispose()
        {
            // Stop animation timers
            undulationTimer?.Dispose();
            brightnessMaskTimer?.Dispose();
            
            // Clear animation state
            isDungeonSelectionActive = false;
            dungeonSelectionPlayer = null;
            dungeonSelectionList = null;
        }
    }
}
