using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display.Helpers;
using RPGGame.UI.Avalonia.Display.Mode;
using RPGGame.UI.Avalonia.Display.Render;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Effects;
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
        
        // Stats panel components
        private readonly StatsPanelStateManager statsPanelStateManager;
        private readonly StatsHeaderGlowAnimator glowAnimator;
        
        // Core components
        private DisplayBuffer buffer;
        private DisplayRenderer renderer;
        private RenderStateManager renderStateManager;
        
        private readonly DisplayModeManager modeManager;
        private readonly RenderCoordinator renderCoordinator;
        
        // Extracted helpers
        private readonly DisplayBufferOperations bufferOperations;
        private readonly DisplayScrollManager scrollManager;
        
        // Callback to trigger combat screen re-render when new messages are added
        // Used when external renderer (like combat screen) handles rendering
        private System.Action? externalRenderCallback = null;

        /// <summary>
        /// While &gt; 0, <see cref="TriggerRender"/> does not schedule work (avoids reactive double-paint
        /// while an imperative path such as <c>RenderRoomEntry</c> draws the buffer explicitly).
        /// Pending triggers are discarded when the scope ends — the imperative caller owns the final paint.
        /// </summary>
        private int imperativeRenderDepth;
        
        public CenterPanelDisplayManager(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            ICanvasContextManager contextManager,
            int maxLines = DISPLAY_BUFFER_MAX_LINES,
            GameStateManager? stateManager = null,
            ICanvasInteractionManager? interactionManager = null)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.contextManager = contextManager;
            this.stateManager = stateManager;
            
            // Create stats panel components
            this.statsPanelStateManager = new StatsPanelStateManager();
            this.glowAnimator = new StatsHeaderGlowAnimator();
            // STATS header uses static gold (see CharacterPanelRenderer); do not run glow timer or it shifts hue and refreshes the canvas ~24fps.
            
            this.layoutManager = new PersistentLayoutManager(canvas, interactionManager, statsPanelStateManager);
            
            // Initialize with standard mode
            this.buffer = new DisplayBuffer(maxLines);
            this.renderer = new DisplayRenderer(textWriter);
            this.renderStateManager = new RenderStateManager();
            var dungeonRendererForActionStrip = new DungeonRenderer(canvas, textWriter, new List<ClickableElement>());
            
            this.modeManager = new DisplayModeManager(new StandardDisplayMode());
            this.renderCoordinator = new RenderCoordinator(
                canvas,
                renderer,
                dungeonRendererForActionStrip,
                layoutManager,
                renderStateManager,
                contextManager,
                buffer,
                modeManager,
                statsPanelStateManager,
                stateManager);
            
            // Initialize extracted helpers
            this.bufferOperations = new DisplayBufferOperations(
                buffer,
                filterService,
                stateManager,
                contextManager,
                TriggerRender);
            this.scrollManager = new DisplayScrollManager(
                buffer,
                renderCoordinator,
                modeManager);
        }
        
        /// <summary>
        /// Gets the display buffer
        /// </summary>
        public DisplayBuffer Buffer => buffer;
        
        /// <summary>
        /// Gets the stats panel state manager
        /// </summary>
        public StatsPanelStateManager StatsPanelStateManager => statsPanelStateManager;
        
        /// <summary>
        /// Gets the glow animator
        /// </summary>
        public StatsHeaderGlowAnimator GlowAnimator => glowAnimator;
        
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
        /// True while the center panel uses <see cref="CombatDisplayMode"/> (active combat log / fight UI).
        /// Used to lock action-strip reorder; not cleared when enemy context is temporarily cleared during renders.
        /// </summary>
        public bool IsCombatDisplayMode => modeManager.CurrentMode is CombatDisplayMode;
        
        /// <summary>
        /// Sets an external render callback for cases where external renderer handles rendering
        /// When set, auto-rendering triggers the callback instead of PerformRender()
        /// Used for combat screen and other specialized rendering scenarios
        /// When set to an empty function () => { }, rendering is suppressed (for menu screens)
        /// </summary>
        public void SetExternalRenderCallback(System.Action? renderCallback)
        {
            externalRenderCallback = renderCallback;
            // Keep coordinator in sync so clearing the callback takes effect before the next TriggerRender
            // (otherwise a pending scheduled render can still invoke a stale combat delegate).
            renderCoordinator.SetExternalRenderCallback(renderCallback);
        }
        
        /// <summary>
        /// Starts a batch transaction for adding multiple messages
        /// Messages are added to the buffer but render is only triggered when transaction completes
        /// </summary>
        /// <param name="autoRender">If true, automatically triggers render when transaction completes. If false, caller must call Render() explicitly.</param>
        /// <param name="batchLineMessageType">Stored <see cref="UIMessageType"/> for every line in the batch (affects center-panel alignment and beat timing).</param>
        public DisplayBatchTransaction StartBatch(bool autoRender = true, UIMessageType batchLineMessageType = UIMessageType.System)
        {
            return new DisplayBatchTransaction(this, autoRender, batchLineMessageType);
        }
        
        /// <summary>
        /// Suppresses reactive <see cref="TriggerRender"/> scheduling while an imperative renderer
        /// (e.g. room entry) paints the buffer directly. Dispose to leave scope; coalesced triggers are dropped.
        /// </summary>
        public IDisposable BeginSuppressReactiveRenderDuringImperativeRender()
        {
            imperativeRenderDepth++;
            return new ImperativeRenderScope(this);
        }

        private sealed class ImperativeRenderScope : IDisposable
        {
            private readonly CenterPanelDisplayManager owner;
            private bool disposed;

            public ImperativeRenderScope(CenterPanelDisplayManager owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                owner.imperativeRenderDepth = Math.Max(0, owner.imperativeRenderDepth - 1);
            }
        }

        /// <summary>
        /// Triggers a render (used by batch transactions and explicit render calls)
        /// </summary>
        internal void TriggerRender()
        {
            if (imperativeRenderDepth > 0)
                return;

            renderCoordinator.SetExternalRenderCallback(externalRenderCallback);
            renderCoordinator.TriggerRender(modeManager.Timing);
        }
        
        /// <summary>
        /// Adds a message to the display buffer and schedules a render
        /// </summary>
        public void AddMessage(string message, UIMessageType messageType = UIMessageType.System)
        {
            if (bufferOperations.TryAddMessage(message, messageType))
            {
                TriggerRender();
            }
        }
        
        /// <summary>
        /// Adds structured ColoredText segments to the display buffer and schedules a render
        /// This eliminates round-trip conversions by storing structured data directly
        /// </summary>
        public void AddMessage(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            if (bufferOperations.TryAddMessage(segments, messageType))
            {
                TriggerRender();
            }
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer and schedules a render
        /// </summary>
        public void AddMessages(IEnumerable<string> messages)
        {
            if (bufferOperations.TryAddMessages(messages))
            {
                TriggerRender();
            }
        }
        
        /// <summary>
        /// Adds multiple structured ColoredText segment lists to the display buffer and schedules a render
        /// This eliminates round-trip conversions by storing structured data directly
        /// </summary>
        public void AddMessages(IEnumerable<List<ColoredText>> segmentsList)
        {
            AddMessages(segmentsList, null, UIMessageType.System);
        }

        /// <summary>
        /// Adds multiple structured lines using the same <see cref="UIMessageType"/> for each row (e.g. <see cref="UIMessageType.Title"/> for dungeon/room narrative blocks).
        /// </summary>
        public void AddMessages(IEnumerable<List<ColoredText>> segmentsList, UIMessageType lineMessageType)
        {
            AddMessages(segmentsList, null, lineMessageType);
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
            AddMessages(segmentsList, sourceCharacter, UIMessageType.System);
        }

        /// <summary>
        /// Adds multiple structured ColoredText segment lists with an explicit per-line message type.
        /// </summary>
        public void AddMessages(IEnumerable<List<ColoredText>> segmentsList, Character? sourceCharacter, UIMessageType lineMessageType)
        {
            // Only check menu states - don't filter by character
            // Messages are already routed to the correct per-character display manager
            // Only the active character's display manager will be rendered
            if (bufferOperations.TryAddMessages(segmentsList, lineMessageType))
            {
                TriggerRender();
            }
        }
        
        public void AddMessageBatch(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            if (bufferOperations.TryAddMessages(messages))
            {
                BatchOperationHelper.ScheduleRenderWithDelay(TriggerRender, delayAfterBatchMs);
            }
        }
        
        public void AddMessageBatch(IEnumerable<List<ColoredText>> segmentsList, int delayAfterBatchMs = 0)
        {
            if (bufferOperations.TryAddMessages(segmentsList, UIMessageType.System))
            {
                BatchOperationHelper.ScheduleRenderWithDelay(TriggerRender, delayAfterBatchMs);
            }
        }
        
        public async Task AddMessageBatchAsync(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            if (bufferOperations.TryAddMessages(messages))
            {
                await BatchOperationHelper.ScheduleRenderWithDelayAsync(TriggerRender, delayAfterBatchMs);
            }
        }
        
        public async Task AddMessageBatchAsync(IEnumerable<List<ColoredText>> segmentsList, int delayAfterBatchMs = 0)
        {
            if (bufferOperations.TryAddMessages(segmentsList, UIMessageType.System))
            {
                await BatchOperationHelper.ScheduleRenderWithDelayAsync(TriggerRender, delayAfterBatchMs);
            }
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
            bufferOperations.Clear();
            renderStateManager.Reset();
            contextManager.ClearCombatLogEnemyAlignmentSticky();
            modeManager.Timing.ForceRender(() => renderCoordinator.PerformRender());
        }
        
        /// <summary>
        /// Clears the display buffer without triggering a render
        /// Used when switching to menu screens that handle their own rendering
        /// </summary>
        public void ClearWithoutRender()
        {
            bufferOperations.Clear();
            renderStateManager.Reset();
            contextManager.ClearCombatLogEnemyAlignmentSticky();
            // Don't trigger a render - let the menu screen handle its own rendering
        }

        /// <summary>
        /// Replaces the center buffer with captured lab undo lines (bypasses message filters) and forces a render.
        /// Clears combat-log sticky alignment; caller should re-sync the lab enemy name after restore.
        /// </summary>
        public void ReplaceBufferFromLabSnapshot(
            IReadOnlyList<(List<ColoredText> Segments, UIMessageType MessageType)> lines,
            bool render = true)
        {
            bufferOperations.Clear();
            renderStateManager.Reset();
            contextManager.ClearCombatLogEnemyAlignmentSticky();
            if (lines != null)
            {
                foreach (var (segments, messageType) in lines)
                    buffer.Add(segments ?? new List<ColoredText>(), messageType);
            }

            if (render)
                ForceRender();
        }
        
        /// <summary>
        /// Scrolls the display up
        /// </summary>
        public void ScrollUp(int lines = 3)
        {
            scrollManager.ScrollUp(lines);
        }
        
        /// <summary>
        /// Scrolls the display down
        /// </summary>
        public void ScrollDown(int lines = 3)
        {
            scrollManager.ScrollDown(lines);
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode
        /// </summary>
        public void ResetScroll()
        {
            scrollManager.ResetScroll();
        }
        
        /// <summary>
        /// Forces an immediate render (bypasses timing and NeedsRender check)
        /// Used for animation updates that need to render even when buffer hasn't changed
        /// </summary>
        public void ForceRender()
        {
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

