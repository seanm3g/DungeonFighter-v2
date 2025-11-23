using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Interface for UI managers to provide a common interface for different UI implementations
    /// </summary>
    public interface IUIManager
    {
        void WriteLine(string message, UIMessageType messageType = UIMessageType.System);
        void Write(string message);
        void WriteSystemLine(string message);
        void WriteMenuLine(string message);
        void WriteTitleLine(string message);
        void WriteDungeonLine(string message);
        void WriteRoomLine(string message);
        void WriteEnemyLine(string message);
        void WriteRoomClearedLine(string message);
        void WriteEffectLine(string message);
        void WriteBlankLine();
        void ResetForNewBattle();
        void ResetMenuDelayCounter();
        int GetConsecutiveMenuLineCount();
        int GetBaseMenuDelay();
        
        /// <summary>
        /// Writes text with chunked reveal (progressive text display)
        /// </summary>
        void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null);
        
        // ===== NEW COLORED TEXT SYSTEM METHODS =====
        
        /// <summary>
        /// Writes colored text using the new ColoredText system
        /// </summary>
        void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System);
        
        /// <summary>
        /// Writes colored text using the new ColoredText system with newline
        /// </summary>
        void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System);
        
        /// <summary>
        /// Writes colored text segments using the new ColoredText system
        /// </summary>
        void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System);
        
        /// <summary>
        /// Writes colored text segments using the new ColoredText system with newline
        /// </summary>
        void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System);
        
        /// <summary>
        /// Writes colored text using the builder pattern
        /// </summary>
        void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System);
        
        /// <summary>
        /// Writes colored text using the builder pattern with newline
        /// </summary>
        void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System);
    }
}
