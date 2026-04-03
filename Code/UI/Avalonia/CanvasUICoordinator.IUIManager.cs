using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia
{
    public partial class CanvasUICoordinator
    {
        #region IUIManager Implementation

        public void SetCloseAction(System.Action action)
        {
            windowManager.SetCloseAction(action);
        }

        public int CenterX => canvas.CenterX;

        public void Close()
        {
            windowManager.Close();
        }

        public void SetCharacter(Character? character)
        {
            contextCoordinator.SetCharacter(character);
        }

        public void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
            => messageWritingCoordinator.WriteLine(message, messageType);
        public void Write(string message) => messageWritingCoordinator.Write(message);
        public void WriteSystemLine(string message) => messageWritingCoordinator.WriteSystemLine(message);
        public void WriteMenuLine(string message) => messageWritingCoordinator.WriteMenuLine(message);
        public void WriteTitleLine(string message) => messageWritingCoordinator.WriteTitleLine(message);
        public void WriteDungeonLine(string message) => messageWritingCoordinator.WriteDungeonLine(message);
        public void WriteRoomLine(string message) => messageWritingCoordinator.WriteRoomLine(message);
        public void WriteEnemyLine(string message) => messageWritingCoordinator.WriteEnemyLine(message);
        public void WriteRoomClearedLine(string message) => messageWritingCoordinator.WriteRoomClearedLine(message);
        public void WriteEffectLine(string message) => messageWritingCoordinator.WriteEffectLine(message);
        public void WriteBlankLine() => messageWritingCoordinator.WriteBlankLine();
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
            => messageWritingCoordinator.WriteChunked(message, config);
        public void ResetForNewBattle() => messageWritingCoordinator.ResetForNewBattle();
        public void ResetMenuDelayCounter() => messageWritingCoordinator.ResetMenuDelayCounter();
        public int GetConsecutiveMenuLineCount() => messageWritingCoordinator.GetConsecutiveMenuLineCount();
        public int GetBaseMenuDelay() => messageWritingCoordinator.GetBaseMenuDelay();

        public void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
            => coloredTextCoordinator.WriteColoredText(coloredText, messageType);
        public void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
            => coloredTextCoordinator.WriteLineColoredText(coloredText, messageType);
        public void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
            => coloredTextCoordinator.WriteColoredSegments(segments, messageType);
        public void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
            => coloredTextCoordinator.WriteLineColoredSegments(segments, messageType);
        public void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
            => coloredTextCoordinator.WriteColoredTextBuilder(builder, messageType);
        public void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System)
            => coloredTextCoordinator.WriteLineColoredTextBuilder(builder, messageType);

        /// <summary>
        /// Writes colored segments at a specific position (for full-screen renderers like title).
        /// </summary>
        public void WriteLineColoredSegments(List<ColoredText> segments, int x, int y)
        {
            textManager.WriteLineColoredSegments(segments, x, y);
        }

        /// <summary>
        /// Writes multiple colored segment batches with optional delay (fire-and-forget).
        /// </summary>
        public void WriteColoredSegmentsBatch(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0, Character? character = null)
        {
            batchOperationCoordinator.WriteColoredSegmentsBatch(messageGroups, delayAfterBatchMs, character);
        }

        /// <summary>
        /// Writes multiple colored segment batches with optional delay (async).
        /// </summary>
        public System.Threading.Tasks.Task WriteColoredSegmentsBatchAsync(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0, Character? character = null)
        {
            return batchOperationCoordinator.WriteColoredSegmentsBatchAsync(messageGroups, delayAfterBatchMs, character);
        }

        #endregion
    }
}
