using System;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Unified coordinator for all display update operations.
    /// Provides a single, consistent interface for clearing and refreshing the display.
    /// </summary>
    public class DisplayUpdateCoordinator
    {
        /// <summary>
        /// Types of clear operations available
        /// </summary>
        public enum ClearOperation
        {
            /// <summary>
            /// Clear display buffer only (triggers render)
            /// </summary>
            Buffer,
            
            /// <summary>
            /// Clear canvas only
            /// </summary>
            Canvas,
            
            /// <summary>
            /// Clear both buffer and canvas
            /// </summary>
            Both,
            
            /// <summary>
            /// Clear buffer without triggering render (for menu transitions)
            /// </summary>
            BufferWithoutRender
        }
        
        private readonly GameCanvasControl canvas;
        private readonly ICanvasTextManager textManager;
        private readonly CenterPanelDisplayManager? displayManager;
        
        public DisplayUpdateCoordinator(
            GameCanvasControl canvas,
            ICanvasTextManager textManager,
            CenterPanelDisplayManager? displayManager = null)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.textManager = textManager ?? throw new ArgumentNullException(nameof(textManager));
            this.displayManager = displayManager;
        }
        
        /// <summary>
        /// Clears the display based on the specified operation
        /// </summary>
        public void Clear(ClearOperation operation)
        {
            switch (operation)
            {
                case ClearOperation.Buffer:
                    if (displayManager != null)
                    {
                        displayManager.Clear();
                    }
                    else
                    {
                        textManager.ClearDisplayBuffer();
                    }
                    break;
                    
                case ClearOperation.Canvas:
                    canvas.Clear();
                    break;
                    
                case ClearOperation.Both:
                    canvas.Clear();
                    if (displayManager != null)
                    {
                        displayManager.Clear();
                    }
                    else
                    {
                        textManager.ClearDisplayBuffer();
                    }
                    break;
                    
                case ClearOperation.BufferWithoutRender:
                    if (displayManager != null)
                    {
                        displayManager.ClearWithoutRender();
                    }
                    else
                    {
                        textManager.ClearDisplayBufferWithoutRender();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Refreshes the canvas to display pending changes
        /// </summary>
        public void Refresh()
        {
            canvas.Refresh();
        }
        
        /// <summary>
        /// Clears the display and then refreshes the canvas
        /// </summary>
        public void ClearAndRefresh(ClearOperation operation)
        {
            Clear(operation);
            Refresh();
        }
    }
}

