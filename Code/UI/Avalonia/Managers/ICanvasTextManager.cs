using RPGGame.UI;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Interface for managing canvas text operations
    /// </summary>
    public interface ICanvasTextManager
    {
        void AddToDisplayBuffer(string message, UIMessageType messageType = UIMessageType.System);
        void ClearDisplayBuffer();
        void RenderDisplayBuffer(int x, int y, int width, int height);
        void WriteLineColored(string message, int x, int y);
        int WriteLineColoredWrapped(string message, int x, int y, int maxWidth);
        void WriteLineColoredSegments(System.Collections.Generic.List<RPGGame.UI.ColorSystem.ColoredText> segments, int x, int y);
        void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null);
        void ClearDisplay();
        int BufferLineCount { get; }
        void ScrollUp(int lines = 3);
        void ScrollDown(int lines = 3);
        void ResetScroll();
        List<string> DisplayBuffer { get; }
        
        void AddMessageBatch(System.Collections.Generic.IEnumerable<string> messages, int delayAfterBatchMs = 0);
    }
}
