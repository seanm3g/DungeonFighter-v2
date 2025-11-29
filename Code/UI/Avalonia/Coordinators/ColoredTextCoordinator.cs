using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Coordinators
{
    /// <summary>
    /// Coordinates ColoredText operations for the UI
    /// </summary>
    public class ColoredTextCoordinator
    {
        private readonly ICanvasTextManager textManager;
        private readonly MessageWritingCoordinator messageWritingCoordinator;

        public ColoredTextCoordinator(ICanvasTextManager textManager, MessageWritingCoordinator messageWritingCoordinator)
        {
            this.textManager = textManager;
            this.messageWritingCoordinator = messageWritingCoordinator;
        }

        /// <summary>
        /// Writes ColoredText directly - stores structured data to eliminate round-trip conversions
        /// </summary>
        public void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            // Store structured ColoredText directly - no conversion needed
            var segments = new List<ColoredText> { coloredText };
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.AddMessage(segments, messageType);
            }
            else
            {
                // Fallback: convert to string for non-CanvasTextManager implementations
                var markup = ColoredTextRenderer.RenderAsMarkup(segments);
                messageWritingCoordinator.WriteLine(markup, messageType);
            }
        }
        
        /// <summary>
        /// Writes ColoredText with newline - stores structured data to eliminate round-trip conversions
        /// </summary>
        public void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            WriteColoredText(coloredText, messageType);
        }
        
        /// <summary>
        /// Writes ColoredText segments directly - stores structured data to eliminate round-trip conversions
        /// This is the primary method for writing structured ColoredText
        /// </summary>
        public void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            if (segments == null || segments.Count == 0)
                return;
            
            // Store structured ColoredText directly - no conversion needed
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.AddMessage(segments, messageType);
            }
            else
            {
                // Fallback: convert to string for non-CanvasTextManager implementations
                var markup = ColoredTextRenderer.RenderAsMarkup(segments);
                messageWritingCoordinator.WriteLine(markup, messageType);
            }
        }
        
        /// <summary>
        /// Writes ColoredText segments with newline - converts to markup string to preserve colors
        /// </summary>
        public void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            WriteColoredSegments(segments, messageType);
        }
        
        /// <summary>
        /// Writes colored text using the builder pattern - converts to markup string to preserve colors
        /// </summary>
        public void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            if (builder == null)
                return;
            
            var segments = builder.Build();
            WriteColoredSegments(segments, messageType);
        }
        
        /// <summary>
        /// Writes colored text using the builder pattern with newline - converts to markup string to preserve colors
        /// </summary>
        public void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            if (builder == null)
                return;
            
            var segments = builder.Build();
            WriteLineColoredSegments(segments, messageType);
        }
    }
}

