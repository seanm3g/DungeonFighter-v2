using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Wrapper for the existing static UIManager to implement IUIManager interface
    /// </summary>
    public class ConsoleUIManager : IUIManager
    {
        public void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
        {
            UIManager.WriteLine(message, messageType);
        }

        public void Write(string message)
        {
            UIManager.Write(message);
        }

        public void WriteSystemLine(string message)
        {
            UIManager.WriteSystemLine(message);
        }

        public void WriteMenuLine(string message)
        {
            UIManager.WriteMenuLine(message);
        }

        public void WriteTitleLine(string message)
        {
            UIManager.WriteTitleLine(message);
        }

        public void WriteDungeonLine(string message)
        {
            UIManager.WriteDungeonLine(message);
        }

        public void WriteRoomLine(string message)
        {
            UIManager.WriteRoomLine(message);
        }

        public void WriteEnemyLine(string message)
        {
            UIManager.WriteEnemyLine(message);
        }

        public void WriteRoomClearedLine(string message)
        {
            UIManager.WriteRoomClearedLine(message);
        }

        public void WriteEffectLine(string message)
        {
            UIManager.WriteEffectLine(message);
        }

        public void WriteBlankLine()
        {
            UIManager.WriteBlankLine();
        }

        public void ResetForNewBattle()
        {
            UIManager.ResetForNewBattle();
        }

        public void ResetMenuDelayCounter()
        {
            UIManager.ResetMenuDelayCounter();
        }

        public int GetConsecutiveMenuLineCount()
        {
            return UIManager.GetConsecutiveMenuLineCount();
        }

        public int GetBaseMenuDelay()
        {
            return UIManager.GetBaseMenuDelay();
        }
        
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            UIManager.WriteChunked(message, config);
        }
        
        // ===== NEW COLORED TEXT SYSTEM METHODS =====
        
        public void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            UIManager.WriteColoredText(coloredText, messageType);
        }
        
        public void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            UIManager.WriteLineColoredText(coloredText, messageType);
        }
        
        public void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            UIManager.WriteColoredSegments(segments, messageType);
        }
        
        public void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            UIManager.WriteLineColoredSegments(segments, messageType);
        }
    }
}
