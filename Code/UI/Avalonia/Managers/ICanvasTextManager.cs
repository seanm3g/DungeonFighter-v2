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
        void RenderDisplayBufferFallback();
        void WriteLineColored(string message, int x, int y);
        int WriteLineColoredWrapped(string message, int x, int y, int maxWidth);
        void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null);
        void ClearDisplay();
        int BufferLineCount { get; }
    }
}
