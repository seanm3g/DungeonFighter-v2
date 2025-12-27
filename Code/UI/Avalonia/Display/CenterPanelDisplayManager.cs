using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display.Helpers;
using RPGGame.UI.Avalonia.Display.Mode;
using RPGGame.UI.Avalonia.Display.Render;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Services;
using RPGGame.Utils;
using static RPGGame.Utils.GameConstants;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Unified display manager for center panel content
    /// Single rendering path for all game modes (combat, menus, exploration, etc.)
    /// </summary>
    public class CenterPanelDisplayManager
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly ICanvasContextManager contextManager;
        private readonly PersistentLayoutManager layoutManager;
        private GameStateManager? stateManager;
        private readonly MessageFilterService filterService = new MessageFilterService();
        
        // Core components
        private DisplayBuffer buffer;
        private DisplayRenderer renderer;
        private RenderStateManager renderStateManager;
        
        private readonly DisplayModeManager modeManager;
        private readonly RenderCoordinator renderCoordinator;
        
        // Callback to trigger combat screen re-render when new messages are added
        // Used when external renderer (like combat screen) handles rendering
        private System.Action? externalRenderCallback = null;
        
        public CenterPanelDisplayManager(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            ICanvasContextManager contextManager,
            int maxLines = DISPLAY_BUFFER_MAX_LINES,
            GameStateManager? stateManager = null)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.contextManager = contextManager;
            this.stateManager = stateManager;
            this.layoutManager = new PersistentLayoutManager(canvas);
            
            // Initialize with standard mode
            this.buffer = new DisplayBuffer(maxLines);
            this.renderer = new DisplayRenderer(textWriter);
            this.renderStateManager = new RenderStateManager();
            
            this.modeManager = new DisplayModeManager(new StandardDisplayMode());
            this.renderCoordinator = new RenderCoordinator(
                canvas,
                renderer,
                layoutManager,
                renderStateManager,
                contextManager,
                buffer,
                modeManager.CurrentMode,
                stateManager);
        }
        
        /// <summary>
        /// Gets the display buffer
        /// </summary>
        public DisplayBuffer Buffer => buffer;
        
        /// <summary>
        /// Sets the game state manager (called after construction when state manager is available)
        /// </summary>
        public void SetStateManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager;
            renderCoordinator.SetStateManager(stateManager);
        }
        
        /// <summary>
        /// Sets the display mode (Standard, Combat, Menu, etc.)
        /// </summary>
        public void SetMode(DisplayMode mode)
        {
            modeManager.SetMode(mode, () => modeManager.Timing.CancelPending());
        }
        
        /// <summary>
        /// Sets an external render callback for cases where external renderer handles rendering
        /// When set, auto-rendering triggers the callback instead of PerformRender()
        /// Used for combat screen and other specialized rendering scenarios
        /// When set to an empty function () => { }, rendering is suppressed (for menu screens)
        /// </summary>
        public void SetExternalRenderCallback(System.Action? renderCallback)
        {
            externalRenderCallback = renderCallback;
        }
        
        /// <summary>
        /// Starts a batch transaction for adding multiple messages
        /// Messages are added to the buffer but render is only triggered when transaction completes
        /// </summary>
        /// <param name="autoRender">If true, automatically triggers render when transaction completes. If false, caller must call Render() explicitly.</param>
        public DisplayBatchTransaction StartBatch(bool autoRender = true)
        {
            return new DisplayBatchTransaction(this, autoRender);
        }
        
        /// <summary>
        /// Triggers a render (used by batch transactions and explicit render calls)
        /// </summary>
        internal void TriggerRender()
        {
            renderCoordinator.SetExternalRenderCallback(externalRenderCallback);
            renderCoordinator.TriggerRender(modeManager.Timing);
        }
        
        /// <summary>
        /// Adds a message to the display buffer and schedules a render
        /// </summary>
        public void AddMessage(string message, UIMessageType messageType = UIMessageType.System)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CenterPanelDisplayManager.cs:AddMessage", message = "Entry", data = new { messagePreview = message?.Substring(0, Math.Min(50, message?.Length ?? 0)), messageType = messageType.ToString(), bufferCount = buffer.Count }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
            // #endregion
            // Use MessageFilterService to determine if message should be displayed
            // This consolidates all filtering logic (menu states, character matching, race conditions)
            var currentCharacter = contextManager?.GetCurrentCharacter();
            var activeCharacter = stateManager?.GetActiveCharacter();
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CenterPanelDisplayManager.cs:AddMessage", message = "Character check", data = new { currentCharacter = currentCharacter?.Name, activeCharacter = activeCharacter?.Name, charactersMatch = currentCharacter == activeCharacter }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
            // #endregion
            bool shouldAddMessage = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                messageType,
                stateManager,
                contextManager,
                performRaceConditionCheck: true); // Enable race condition check for Dungeon state
            
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CenterPanelDisplayManager.cs:AddMessage", message = "Filter result", data = new { shouldAddMessage = shouldAddMessage }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
            // #endregion
            
            if (!shouldAddMessage)
            {
                // Message blocked by filter service - don't add to buffer
                return;
            }
            
            if (message != null)
            {
                buffer.Add(message);
            }
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CenterPanelDisplayManager.cs:AddMessage", message = "Message added to buffer", data = new { newBufferCount = buffer.Count, activeCharacter = activeCharacter?.Name }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
            // #endregion
            TriggerRender();
        }
        
        /// <summary>
        /// Adds structured ColoredText segments to the display buffer and schedules a render
        /// This eliminates round-trip conversions by storing structured data directly
        /// </summary>
        public void AddMessage(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            // Use MessageFilterService to determine if message should be displayed
            // This consolidates all filtering logic (menu states, character matching, race conditions)
            bool shouldAddMessage = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                messageType,
                stateManager,
                contextManager,
                performRaceConditionCheck: true); // Enable race condition check for Dungeon state
            
            if (!shouldAddMessage)
            {
                // Message blocked by filter service - don't add to buffer
                return;
            }
            
            buffer.Add(segments);
            TriggerRender();
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer and schedules a render
        /// </summary>
        public void AddMessages(IEnumerable<string> messages)
        {
            // Use MessageFilterService to determine if messages should be displayed
            bool shouldAddMessages = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                UIMessageType.System, // Default to System for batch operations
                stateManager,
                contextManager);
            
            if (!shouldAddMessages)
            {
                // Messages blocked by filter service - don't add to buffer
                return;
            }
            
            buffer.AddRange(messages);
            TriggerRender();
        }
        
        /// <summary>
        /// Adds multiple structured ColoredText segment lists to the display buffer and schedules a render
        /// This eliminates round-trip conversions by storing structured data directly
        /// </summary>
        public void AddMessages(IEnumerable<List<ColoredText>> segmentsList)
        {
            AddMessages(segmentsList, null);
        }

        /// <summary>
        /// Adds multiple structured ColoredText segment lists to the display buffer and schedules a render
        /// This version accepts a character parameter (for routing purposes, but not for filtering)
        /// Since messages are routed to per-character display managers, we accept all messages routed here
        /// Only filter by menu states, not by character (character filtering happens at routing level)
        /// </summary>
        /// <param name="segmentsList">The colored text segment lists to add</param>
        /// <param name="sourceCharacter">The character these messages belong to (for reference, not filtering)</param>
        public void AddMessages(IEnumerable<List<ColoredText>> segmentsList, Character? sourceCharacter)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CenterPanelDisplayManager.cs:AddMessages", message = "Entry", data = new { sourceCharacter = sourceCharacter?.Name, bufferCount = buffer.Count, segmentCount = segmentsList?.Count() ?? 0 }, sessionId = "debug-session", runId = "run1", hypothesisId = "H1" }) + "\n"); } catch { }
            // #endregion
            // Only check menu states - don't filter by character
            // Messages are already routed to the correct per-character display manager
            // Only the active character's display manager will be rendered
            var activeCharacter = stateManager?.GetActiveCharacter();
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CenterPanelDisplayManager.cs:AddMessages", message = "Character check", data = new { sourceCharacter = sourceCharacter?.Name, activeCharacter = activeCharacter?.Name, charactersMatch = sourceCharacter == activeCharacter }, sessionId = "debug-session", runId = "run1", hypothesisId = "H1" }) + "\n"); } catch { }
            // #endregion
            if (stateManager != null)
            {
                bool isMenuState = filterService.IsMenuState(stateManager);
                if (isMenuState)
                {
                    // Don't add messages if we're in a menu state
                    return;
                }
            }
            
            if (segmentsList != null)
            {
                buffer.AddRange(segmentsList);
            }
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CenterPanelDisplayManager.cs:AddMessages", message = "Messages added to buffer", data = new { newBufferCount = buffer.Count, activeCharacter = activeCharacter?.Name }, sessionId = "debug-session", runId = "run1", hypothesisId = "H1" }) + "\n"); } catch { }
            // #endregion
            TriggerRender();
        }
        
        public void AddMessageBatch(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            // Use MessageFilterService to determine if messages should be displayed
            bool shouldAddMessages = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                UIMessageType.System, // Default to System for batch operations
                stateManager,
                contextManager);
            
            if (!shouldAddMessages)
            {
                // Messages blocked by filter service - don't add to buffer
                return;
            }
            
            buffer.AddRange(messages);
            BatchOperationHelper.ScheduleRenderWithDelay(TriggerRender, delayAfterBatchMs);
        }
        
        public void AddMessageBatch(IEnumerable<List<ColoredText>> segmentsList, int delayAfterBatchMs = 0)
        {
            // Only check menu states - don't filter by character
            // Messages are already routed to the correct per-character display manager
            if (stateManager != null)
            {
                bool isMenuState = filterService.IsMenuState(stateManager);
                if (isMenuState)
                {
                    // Don't add messages if we're in a menu state
                    return;
                }
            }
            
            buffer.AddRange(segmentsList);
            BatchOperationHelper.ScheduleRenderWithDelay(TriggerRender, delayAfterBatchMs);
        }
        
        public async Task AddMessageBatchAsync(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            // Only check menu states - don't filter by character
            // Messages are already routed to the correct per-character display manager
            if (stateManager != null)
            {
                bool isMenuState = filterService.IsMenuState(stateManager);
                if (isMenuState)
                {
                    // Don't add messages if we're in a menu state
                    return;
                }
            }
            
            buffer.AddRange(messages);
            await BatchOperationHelper.ScheduleRenderWithDelayAsync(TriggerRender, delayAfterBatchMs);
        }
        
        public async Task AddMessageBatchAsync(IEnumerable<List<ColoredText>> segmentsList, int delayAfterBatchMs = 0)
        {
            // Only check menu states - don't filter by character
            // Messages are already routed to the correct per-character display manager
            if (stateManager != null)
            {
                bool isMenuState = filterService.IsMenuState(stateManager);
                if (isMenuState)
                {
                    // Don't add messages if we're in a menu state
                    return;
                }
            }
            
            buffer.AddRange(segmentsList);
            await BatchOperationHelper.ScheduleRenderWithDelayAsync(TriggerRender, delayAfterBatchMs);
        }
        
        /// <summary>
        /// Writes chunked text (all chunks added immediately, then rendered)
        /// </summary>
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            // Use MessageFilterService to determine if message should be displayed
            bool shouldAddMessage = filterService.ShouldDisplayMessage(
                null, // No source character - use context manager instead
                UIMessageType.System, // Default to System for chunked text
                stateManager,
                contextManager);
            
            if (!shouldAddMessage)
            {
                // Message blocked by filter service - don't add to buffer
                return;
            }
            
            config ??= UI.ChunkedTextReveal.DefaultConfig;
            
            if (!config.Enabled)
            {
                buffer.Add(message);
                TriggerRender();
                return;
            }
            
            // Split text into chunks
            var chunks = SplitIntoChunks(message, config.Strategy);
            
            if (chunks.Count == 0)
            {
                return;
            }
            
            // Add all chunks to buffer immediately
            foreach (var chunk in chunks)
            {
                buffer.Add(chunk);
                
                // Add blank line if configured
                if (config.AddBlankLineBetweenChunks && chunk != chunks[chunks.Count - 1])
                {
                    buffer.Add("");
                }
            }
            
            // Schedule single render for all chunks
            TriggerRender();
        }
        
        /// <summary>
        /// Clears the display buffer
        /// </summary>
        public void Clear()
        {
            buffer.Clear();
            renderStateManager.Reset();
            modeManager.Timing.ForceRender(() => renderCoordinator.PerformRender());
        }
        
        /// <summary>
        /// Clears the display buffer without triggering a render
        /// Used when switching to menu screens that handle their own rendering
        /// </summary>
        public void ClearWithoutRender()
        {
            buffer.Clear();
            renderStateManager.Reset();
            // Don't trigger a render - let the menu screen handle its own rendering
        }
        
        /// <summary>
        /// Scrolls the display up
        /// </summary>
        public void ScrollUp(int lines = 3)
        {
            // Calculate max scroll offset to ensure we don't scroll beyond valid range
            int maxOffset = renderCoordinator.CalculateMaxScrollOffset();
            buffer.ScrollUp(lines, maxOffset);
            modeManager.Timing.ForceRender(() => renderCoordinator.PerformRender());
        }
        
        /// <summary>
        /// Scrolls the display down
        /// </summary>
        public void ScrollDown(int lines = 3)
        {
            // Calculate max scroll offset based on current content
            int maxOffset = renderCoordinator.CalculateMaxScrollOffset();
            buffer.ScrollDown(lines, maxOffset);
            modeManager.Timing.ForceRender(() => renderCoordinator.PerformRender());
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode
        /// </summary>
        public void ResetScroll()
        {
            buffer.ResetScroll();
            modeManager.Timing.ScheduleRender(() => renderCoordinator.PerformRender());
        }
        
        /// <summary>
        /// Forces an immediate render (bypasses timing and NeedsRender check)
        /// Used for animation updates that need to render even when buffer hasn't changed
        /// </summary>
        public void ForceRender()
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CenterPanelDisplayManager.cs:ForceRender", message = "ForceRender called", data = new { }, sessionId = "debug-session", runId = "run1", hypothesisId = "H8" }) + "\n"); } catch { }
            // #endregion
            modeManager.Timing.ForceRender(() => renderCoordinator.PerformRender(force: true));
        }
        
        /// <summary>
        /// Forces a full layout render by resetting render state
        /// This ensures the layout is fully rendered even if state manager thinks nothing changed
        /// </summary>
        public void ForceFullLayoutRender()
        {
            renderStateManager.Reset();
            modeManager.Timing.ForceRender(() => renderCoordinator.PerformRender());
        }
        
        /// <summary>
        /// Cancels any pending renders
        /// Used when switching to menu screens that handle their own rendering
        /// </summary>
        public void CancelPendingRenders()
        {
            modeManager.Timing.CancelPending();
        }
        
        private List<string> SplitIntoChunks(string text, UI.ChunkedTextReveal.ChunkStrategy strategy) 
            => ChunkSplitterHelper.SplitIntoChunks(text, strategy);
    }
}

