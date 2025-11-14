using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Interface for managing mouse interactions and clickable elements
    /// </summary>
    public interface ICanvasInteractionManager
    {
        List<ClickableElement> ClickableElements { get; }
        (int x, int y) HoverPosition { get; }
        
        ClickableElement? GetElementAt(int x, int y);
        bool SetHoverPosition(int x, int y);
        void ClearClickableElements();
        void AddClickableElement(ClickableElement element);
        ClickableElement CreateMenuOption(int x, int y, int width, string value, string displayText);
        ClickableElement CreateButton(int x, int y, int width, string value, string displayText);
    }
}
