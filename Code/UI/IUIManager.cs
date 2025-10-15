using System;

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
    }
}
