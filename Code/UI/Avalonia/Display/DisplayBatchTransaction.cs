using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Transaction-based batching API for adding messages to the display buffer
    /// Messages are added to the buffer but render is only triggered when the transaction completes
    /// This prevents duplicate rendering and allows explicit control over when rendering occurs
    /// </summary>
    public class DisplayBatchTransaction : IDisposable
    {
        private readonly CenterPanelDisplayManager? displayManager;
        private readonly List<List<ColoredText>> lines;
        private bool disposed = false;
        private bool autoRender;
        private readonly UIMessageType lineMessageType;
        
        internal DisplayBatchTransaction(CenterPanelDisplayManager? displayManager, bool autoRender, UIMessageType lineMessageType = UIMessageType.System)
        {
            this.displayManager = displayManager;
            this.autoRender = autoRender;
            this.lineMessageType = lineMessageType;
            this.lines = new List<List<ColoredText>>();
        }
        
        /// <summary>
        /// Adds a message to the batch (parsed for markup at insert time).
        /// </summary>
        public void Add(string message)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DisplayBatchTransaction));
            lines.Add(ColoredTextParser.Parse(message ?? ""));
        }
        
        /// <summary>
        /// Adds pre-built colored segments without markup round-trip (preserves exact RGB, e.g. creature names).
        /// </summary>
        public void Add(List<ColoredText> segments)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DisplayBatchTransaction));
            lines.Add(segments ?? new List<ColoredText>());
        }
        
        /// <summary>
        /// Adds multiple messages to the batch
        /// </summary>
        public void AddRange(IEnumerable<string> messagesToAdd)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DisplayBatchTransaction));
            foreach (var msg in messagesToAdd)
                Add(msg);
        }
        
        /// <summary>
        /// Completes the transaction and triggers render if autoRender is enabled
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            
            if (lines.Count > 0 && displayManager != null)
            {
                displayManager.AddMessages(lines, lineMessageType);
                
                if (autoRender)
                {
                    displayManager.TriggerRender();
                }
            }
            
            disposed = true;
        }
    }
}
