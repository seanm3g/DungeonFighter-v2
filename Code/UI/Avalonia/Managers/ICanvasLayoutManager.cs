using RPGGame;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Interface for managing canvas layout operations
    /// </summary>
    public interface ICanvasLayoutManager
    {
        // Layout constants
        int LEFT_MARGIN { get; }
        int RIGHT_MARGIN { get; }
        int TOP_MARGIN { get; }
        int BOTTOM_MARGIN { get; }
        int CONTENT_WIDTH { get; }
        int CONTENT_HEIGHT { get; }
        int SCREEN_WIDTH { get; }
        int SCREEN_CENTER { get; }
        
        // Layout calculation methods
        int CalculateCenterX(int displayLength, int screenCenter);
        int CalculateCenterX(int displayLength);
        int CalculateCenterY(int startY, int height, int contentLines);
        int CalculateContentWidth(int totalWidth, int leftMargin = 2, int rightMargin = 2);
        int CalculateContentHeight(int totalHeight, int topMargin = 2, int bottomMargin = 2);
        (int x, int y) CalculateMenuOptionPosition(int menuStartX, int menuStartY, int optionIndex, int maxOptionLength);
        (int x, int y) CalculateButtonPosition(int buttonAreaX, int buttonAreaY, int buttonIndex, int buttonsPerRow, int buttonWidth, int buttonSpacing = 2);
        (int x, int y, int width) CalculateTextArea(int contentX, int contentY, int contentWidth, int padding = 2);
        bool IsPositionValid(int x, int y, int width, int height, int screenWidth = 210, int screenHeight = 52);
        (int x, int y) ClampPosition(int x, int y, int width, int height, int screenWidth = 210, int screenHeight = 52);
    }
}
