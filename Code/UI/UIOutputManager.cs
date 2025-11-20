using System;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI
{
    /// <summary>
    /// Manages console and custom UI output operations
    /// Handles delegation between console output and custom UI managers
    /// </summary>
    public class UIOutputManager
    {
        private readonly IUIManager? _customUIManager;
        private readonly UIConfiguration _uiConfig;

        public UIOutputManager(IUIManager? customUIManager, UIConfiguration uiConfig)
        {
            _customUIManager = customUIManager;
            _uiConfig = uiConfig;
        }

        /// <summary>
        /// Writes a line to console or custom UI with color markup support
        /// </summary>
        public void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
        {
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteLine(message, messageType);
                return;
            }

            // Console output with color support
            if (UIManager.EnableColorMarkup && ColorParser.HasColorMarkup(message))
            {
                ColoredConsoleWriter.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Writes text without newline with color markup support
        /// </summary>
        public void Write(string message)
        {
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.Write(message);
                return;
            }

            // Console output with color support
            if (UIManager.EnableColorMarkup && ColorParser.HasColorMarkup(message))
            {
                ColoredConsoleWriter.Write(message);
            }
            else
            {
                Console.Write(message);
            }
        }

        /// <summary>
        /// Writes a blank line without any delay
        /// </summary>
        public void WriteBlankLine()
        {
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteBlankLine();
                return;
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Writes text with chunked reveal (progressive text display)
        /// </summary>
        public void WriteChunked(string message, ChunkedTextReveal.RevealConfig? config = null)
        {
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteChunked(message, config);
                return;
            }

            // Use ChunkedTextReveal for console output
            ChunkedTextReveal.RevealText(message, config);
        }

        /// <summary>
        /// Writes a menu line with potential custom UI handling
        /// </summary>
        public void WriteMenuLine(string message)
        {
            // Use custom UI manager if one is set
            if (_customUIManager != null)
            {
                _customUIManager.WriteMenuLine(message);
                return;
            }

            // Console output with color support
            if (UIManager.EnableColorMarkup && ColorParser.HasColorMarkup(message))
            {
                ColoredConsoleWriter.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Resets state for new battle (delegates to custom UI manager if set)
        /// </summary>
        public void ResetForNewBattle()
        {
            if (_customUIManager != null)
            {
                _customUIManager.ResetForNewBattle();
            }
        }

        /// <summary>
        /// Gets the custom UI manager (if set)
        /// </summary>
        public IUIManager? GetCustomUIManager() => _customUIManager;
    }
}

