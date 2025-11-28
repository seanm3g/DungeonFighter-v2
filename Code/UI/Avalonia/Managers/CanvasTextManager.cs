using System;
using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Facade for the unified center panel display system
    /// Delegates to CenterPanelDisplayManager for all operations
    /// Maintains backward compatibility with ICanvasTextManager interface
    /// </summary>
    public class CanvasTextManager : ICanvasTextManager
    {
        private readonly CenterPanelDisplayManager displayManager;
        private readonly ColoredTextWriter textWriter;
        
        public CanvasTextManager(GameCanvasControl canvas, ColoredTextWriter textWriter, ICanvasContextManager contextManager, int maxLines = 100)
        {
            this.textWriter = textWriter;
            this.displayManager = new CenterPanelDisplayManager(canvas, textWriter, contextManager, maxLines);
        }
        
        /// <summary>
        /// Gets the underlying display manager (for advanced usage)
        /// </summary>
        public CenterPanelDisplayManager DisplayManager => displayManager;
        
        /// <summary>
        /// Gets the current display buffer (for compatibility)
        /// Returns as strings for backwards compatibility
        /// </summary>
        public List<string> DisplayBuffer => new List<string>(displayManager.Buffer.MessagesAsStrings);
        
        /// <summary>
        /// Gets the number of lines in the buffer
        /// </summary>
        public int BufferLineCount => displayManager.Buffer.Count;
        
        /// <summary>
        /// Adds a message to the display buffer
        /// </summary>
        public void AddToDisplayBuffer(string message, UIMessageType messageType = UIMessageType.System)
        {
            displayManager.AddMessage(message, messageType);
        }
        
        /// <summary>
        /// Starts a batch transaction for adding multiple messages
        /// Messages are added to the buffer but render is only triggered when transaction completes
        /// </summary>
        /// <param name="autoRender">If true, automatically triggers render when transaction completes. If false, caller must call Render() explicitly.</param>
        public DisplayBatchTransaction StartBatch(bool autoRender = true)
        {
            return displayManager.StartBatch(autoRender);
        }
        
        /// <summary>
        /// Clears the display buffer
        /// </summary>
        public void ClearDisplayBuffer()
        {
            displayManager.Clear();
        }
        
        /// <summary>
        /// Renders the display buffer to the specified area (legacy method)
        /// </summary>
        public void RenderDisplayBuffer(int x, int y, int width, int height)
        {
            // This is handled by the display manager automatically
            // Force a render to ensure it's displayed
            displayManager.ForceRender();
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
            displayManager.WriteChunked(message, config);
        }
        
        /// <summary>
        /// Clears the display
        /// </summary>
        public void ClearDisplay()
        {
            displayManager.Clear();
        }
        
        /// <summary>
        /// Scrolls the display up
        /// </summary>
        public void ScrollUp(int lines = 3)
        {
            displayManager.ScrollUp(lines);
        }
        
        /// <summary>
        /// Scrolls the display down
        /// </summary>
        public void ScrollDown(int lines = 3)
        {
            displayManager.ScrollDown(lines);
        }
        
        /// <summary>
        /// Resets scrolling to auto-scroll mode
        /// </summary>
        public void ResetScroll()
        {
            displayManager.ResetScroll();
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer as a single batch
        /// Schedules a single render after all messages are added, with an optional delay
        /// </summary>
        public void AddMessageBatch(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            displayManager.AddMessageBatch(messages, delayAfterBatchMs);
        }
        
        /// <summary>
        /// Adds multiple messages to the display buffer as a single batch and waits for the delay
        /// This async version allows the combat loop to wait for each action's display to complete
        /// </summary>
        public async System.Threading.Tasks.Task AddMessageBatchAsync(IEnumerable<string> messages, int delayAfterBatchMs = 0)
        {
            await displayManager.AddMessageBatchAsync(messages, delayAfterBatchMs);
        }
    }
}
