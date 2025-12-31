using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;
using static RPGGame.Utils.GameConstants;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Facade for the unified center panel display system
    /// Delegates to CenterPanelDisplayManager for all operations
    /// Maintains backward compatibility with ICanvasTextManager interface
    /// Now supports per-character display managers for complete isolation in multi-character scenarios
    /// </summary>
    public class CanvasTextManager : ICanvasTextManager
    {
        // Per-character display managers for complete isolation
        private readonly Dictionary<string, CenterPanelDisplayManager> characterDisplayManagers = new();
        private CenterPanelDisplayManager? currentDisplayManager;
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly ICanvasContextManager contextManager;
        private readonly int maxLines;
        private GameStateManager? stateManager;
        
        public CanvasTextManager(GameCanvasControl canvas, ColoredTextWriter textWriter, ICanvasContextManager contextManager, int maxLines = DISPLAY_BUFFER_MAX_LINES, GameStateManager? stateManager = null)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.contextManager = contextManager;
            this.maxLines = maxLines;
            this.stateManager = stateManager;
            
            // Create default display manager (for when no character is active)
            this.currentDisplayManager = new CenterPanelDisplayManager(canvas, textWriter, contextManager, maxLines, stateManager);
        }
        
        /// <summary>
        /// Sets the game state manager (called after construction when state manager is available)
        /// </summary>
        public void SetStateManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager;
            
            // Update state manager in current display manager
            if (currentDisplayManager != null)
            {
                currentDisplayManager.SetStateManager(stateManager);
            }
            
            // Update state manager in all character display managers
            foreach (var displayManager in characterDisplayManagers.Values)
            {
                displayManager.SetStateManager(stateManager);
            }
        }
        
        /// <summary>
        /// Switches to the display manager for the specified character
        /// Creates a new display manager if one doesn't exist for this character
        /// </summary>
        public void SwitchToCharacterDisplayManager(Character? character)
        {
            if (character == null)
            {
                // No character - use default display manager
                currentDisplayManager = GetOrCreateDefaultDisplayManager();
                return;
            }
            
            // Get character ID from state manager
            string? characterId = stateManager?.GetCharacterId(character);
            if (string.IsNullOrEmpty(characterId))
            {
                // No character ID - use default display manager
                currentDisplayManager = GetOrCreateDefaultDisplayManager();
                return;
            }
            
            // Get or create display manager for this character
            currentDisplayManager = GetOrCreateCharacterDisplayManager(characterId);
        }
        
        /// <summary>
        /// Gets or creates the display manager for a specific character ID
        /// This allows messages to be routed to the correct character's display manager
        /// even when that character is not currently active
        /// </summary>
        private CenterPanelDisplayManager GetOrCreateCharacterDisplayManager(string characterId)
        {
            if (!characterDisplayManagers.TryGetValue(characterId, out var displayManager))
            {
                // Create new display manager for this character
                displayManager = new CenterPanelDisplayManager(canvas, textWriter, contextManager, maxLines, stateManager);
                characterDisplayManagers[characterId] = displayManager;
            }
            
            return displayManager;
        }
        
        /// <summary>
        /// Gets the display manager for a specific character
        /// Returns the active character's display manager if character is null or not found
        /// This allows messages to be routed to the correct character's buffer
        /// </summary>
        public CenterPanelDisplayManager GetDisplayManagerForCharacter(Character? character)
        {
            if (character == null)
            {
                return DisplayManager; // Return active character's display manager
            }
            
            // Get character ID from state manager
            string? characterId = stateManager?.GetCharacterId(character);
            if (string.IsNullOrEmpty(characterId))
            {
                return DisplayManager; // Return active character's display manager
            }
            
            // Get or create display manager for this character
            return GetOrCreateCharacterDisplayManager(characterId);
        }
        
        /// <summary>
        /// Gets or creates the default display manager (for when no character is active)
        /// </summary>
        private CenterPanelDisplayManager GetOrCreateDefaultDisplayManager()
        {
            const string DEFAULT_KEY = "__default__";
            
            if (!characterDisplayManagers.TryGetValue(DEFAULT_KEY, out var displayManager))
            {
                displayManager = new CenterPanelDisplayManager(canvas, textWriter, contextManager, maxLines, stateManager);
                characterDisplayManagers[DEFAULT_KEY] = displayManager;
            }
            
            return displayManager;
        }
        
        /// <summary>
        /// Gets the game state manager (for advanced usage)
        /// </summary>
        public GameStateManager? StateManager => stateManager;
        
        /// <summary>
        /// Gets the underlying display manager (for advanced usage)
        /// Returns the active character's display manager, or default if no character is active
        /// </summary>
        public CenterPanelDisplayManager DisplayManager
        {
            get
            {
                // Ensure we have a current display manager
                if (currentDisplayManager == null)
                {
                    // Try to get display manager for active character
                    var activeCharacter = stateManager?.GetActiveCharacter();
                    if (activeCharacter != null)
                    {
                        SwitchToCharacterDisplayManager(activeCharacter);
                    }
                    else
                    {
                        currentDisplayManager = GetOrCreateDefaultDisplayManager();
                    }
                }
                
                // Fallback to default if somehow still null (should never happen, but satisfies compiler)
                return currentDisplayManager ?? GetOrCreateDefaultDisplayManager();
            }
        }
        
        /// <summary>
        /// Gets the current display buffer (for compatibility)
        /// Returns as strings for backwards compatibility
        /// </summary>
        public List<string> DisplayBuffer => new List<string>(DisplayManager.Buffer.MessagesAsStrings);
        
        /// <summary>
        /// Gets the number of lines in the buffer
        /// </summary>
        public int BufferLineCount => DisplayManager.Buffer.Count;
        
        /// <summary>
        /// Adds a message to the display buffer
        /// </summary>
        public void AddToDisplayBuffer(string message, UIMessageType messageType = UIMessageType.System)
        {
            DisplayManager.AddMessage(message, messageType);
        }
        
        /// <summary>
        /// Starts a batch transaction for adding multiple messages
        /// Messages are added to the buffer but render is only triggered when transaction completes
        /// </summary>
        /// <param name="autoRender">If true, automatically triggers render when transaction completes. If false, caller must call Render() explicitly.</param>
        public DisplayBatchTransaction StartBatch(bool autoRender = true)
        {
            return DisplayManager.StartBatch(autoRender);
        }
        
        /// <summary>
        /// Clears the display buffer
        /// </summary>
        public void ClearDisplayBuffer()
        {
            DisplayManager.Clear();
        }
        
        /// <summary>
        /// Clears the display buffer without triggering a render
        /// Used when switching to menu screens that handle their own rendering
        /// </summary>
        public void ClearDisplayBufferWithoutRender()
        {
            DisplayManager.ClearWithoutRender();
        }
        
        /// <summary>
        /// Renders the display buffer to the specified area (legacy method)
        /// </summary>
        public void RenderDisplayBuffer(int x, int y, int width, int height)
        {
            // This is handled by the display manager automatically
            // Force a render to ensure it's displayed
            DisplayManager.ForceRender();
        }
        
        /// <summary>
        /// Writes colored text to canvas
        /// </summary>
        public void WriteLineColored(string message, int x, int y)
        {
            textWriter.WriteLineColored(message, x, y);
        }
        
        /// <summary>
        /// Writes colored text with wrapping
        /// </summary>
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            return textWriter.WriteLineColoredWrapped(message, x, y, maxWidth);
        }
        
        /// <summary>
        /// Writes colored text segments
        /// </summary>
        public void WriteLineColoredSegments(List<ColoredText> segments, int x, int y)
        {
            textWriter.RenderSegments(segments, x, y);
        }
        
        /// <summary>
        /// Writes chunked text with reveal animation
        /// </summary>
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            DisplayManager.WriteChunked(message, config);
        }
        
        /// <summary>
        /// Clears the display
        /// </summary>
        public void ClearDisplay()
        {
            DisplayManager.Clear();
        }
        
        /// <summary>
        /// Scrolls the display up
        /// </summary>
        public void ScrollUp(int lines = 3)
        {
            DisplayManager.ScrollUp(lines);
        }
        
        /// <summary>
        /// Scrolls the display down
        /// </summary>
        public void ScrollDown(int lines = 3)
        {
            DisplayManager.ScrollDown(lines);
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode
        /// </summary>
        public void ResetScroll()
        {
            DisplayManager.ResetScroll();
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer as a single batch
        /// Schedules a single render after all messages are added, with an optional delay
        /// </summary>
        public void AddMessageBatch(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            DisplayManager.AddMessageBatch(messages, delayAfterBatchMs);
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer as a single batch and waits for the delay
        /// This async version allows the combat loop to wait for each action's display to complete
        /// </summary>
        public async System.Threading.Tasks.Task AddMessageBatchAsync(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            await DisplayManager.AddMessageBatchAsync(messages, delayAfterBatchMs);
        }
    }
}
