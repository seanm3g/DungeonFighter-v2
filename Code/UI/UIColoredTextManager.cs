using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI
{
    /// <summary>
    /// Manages all colored text operations and rendering
    /// Handles colored text segments, builders, and formatted output
    /// </summary>
    public class UIColoredTextManager
    {
        private readonly UIOutputManager _outputManager;
        private readonly UIDelayManager _delayManager;

        public UIColoredTextManager(UIOutputManager outputManager, UIDelayManager delayManager)
        {
            _outputManager = outputManager;
            _delayManager = delayManager;
        }

        /// <summary>
        /// Writes colored text using the new ColoredText system
        /// Routes to custom UI manager if available, otherwise uses console
        /// </summary>
        public void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            var customUIManager = _outputManager.GetCustomUIManager();
            if (customUIManager != null)
            {
                // Route to custom UI manager (e.g., CanvasUICoordinator)
                customUIManager.WriteColoredText(coloredText, messageType);
            }
            else
            {
                // For console output, use the new system
                var segments = new List<ColoredText> { coloredText };
                ColoredConsoleWriter.WriteSegments(segments);
            }

            _delayManager.ApplyDelay(messageType);
        }

        /// <summary>
        /// Writes a list of colored text segments
        /// Routes to custom UI manager if available, otherwise uses console
        /// </summary>
        public void WriteColoredText(List<ColoredText> coloredTexts, UIMessageType messageType = UIMessageType.System)
        {
            var customUIManager = _outputManager.GetCustomUIManager();
            if (customUIManager != null)
            {
                // Route to custom UI manager (e.g., CanvasUICoordinator)
                customUIManager.WriteColoredSegments(coloredTexts, messageType);
            }
            else
            {
                // For console output, use the new system
                ColoredConsoleWriter.WriteSegments(coloredTexts);
            }

            _delayManager.ApplyDelay(messageType);
        }

        /// <summary>
        /// Writes colored text using the new ColoredText system with newline
        /// Routes to custom UI manager if available, otherwise uses console
        /// </summary>
        public void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            var customUIManager = _outputManager.GetCustomUIManager();
            if (customUIManager != null)
            {
                // Route to custom UI manager (e.g., CanvasUICoordinator)
                customUIManager.WriteLineColoredText(coloredText, messageType);
            }
            else
            {
                // For console output, use the new system
                var segments = new List<ColoredText> { coloredText };
                ColoredConsoleWriter.WriteSegments(segments);
                Console.WriteLine();
            }

            _delayManager.ApplyDelay(messageType);
        }

        /// <summary>
        /// Writes colored text segments using the new ColoredText system
        /// Routes to custom UI manager if available, otherwise uses console
        /// </summary>
        public void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            var customUIManager = _outputManager.GetCustomUIManager();
            if (customUIManager != null)
            {
                // Route to custom UI manager (e.g., CanvasUICoordinator)
                customUIManager.WriteColoredSegments(segments, messageType);
            }
            else
            {
                // For console output, use the new system
                ColoredConsoleWriter.WriteSegments(segments);
            }

            _delayManager.ApplyDelay(messageType);
        }

        /// <summary>
        /// Writes colored text segments using the new ColoredText system with newline
        /// Routes to custom UI manager if available, otherwise uses console
        /// </summary>
        public void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            var customUIManager = _outputManager.GetCustomUIManager();
            if (customUIManager != null)
            {
                // Route to custom UI manager (e.g., CanvasUICoordinator)
                customUIManager.WriteLineColoredSegments(segments, messageType);
            }
            else
            {
                // For console output, use the new system
                ColoredConsoleWriter.WriteSegments(segments);
                Console.WriteLine();
            }

            _delayManager.ApplyDelay(messageType);
        }

        /// <summary>
        /// Writes colored text using the builder pattern
        /// </summary>
        public void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            var segments = builder.Build();
            WriteColoredSegments(segments, messageType);
        }

        /// <summary>
        /// Writes colored text using the builder pattern with newline
        /// </summary>
        public void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
        {
            var segments = builder.Build();
            WriteLineColoredSegments(segments, messageType);
        }
    }
}

