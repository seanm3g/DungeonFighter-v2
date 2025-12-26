using System;
using System.Collections.Generic;

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
        private readonly List<string> messages;
        private bool disposed = false;
        private bool autoRender;
        
        internal DisplayBatchTransaction(CenterPanelDisplayManager? displayManager, bool autoRender)
        {
            this.displayManager = displayManager;
            this.autoRender = autoRender;
            this.messages = new List<string>();
        }
        
        /// <summary>
        /// Adds a message to the batch
        /// </summary>
        public void Add(string message)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DisplayBatchTransaction));
            messages.Add(message);
        }
        
        /// <summary>
        /// Adds multiple messages to the batch
        /// </summary>
        public void AddRange(IEnumerable<string> messagesToAdd)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DisplayBatchTransaction));
            messages.AddRange(messagesToAdd);
        }
        
        /// <summary>
        /// Completes the transaction and triggers render if autoRender is enabled
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            
            if (messages.Count > 0 && displayManager != null)
            {
                // Use AddMessages which includes character validation checks
                // This prevents background combat from adding messages to the display buffer
                displayManager.AddMessages(messages);
                
                // Note: AddMessages already triggers render, but we check autoRender here
                // in case the behavior changes in the future
                if (autoRender)
                {
                    displayManager.TriggerRender();
                }
            }
            
            disposed = true;
        }
    }
}

