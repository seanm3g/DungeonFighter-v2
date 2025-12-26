using Avalonia.Media;
using RPGGame.UI;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders menu options with colored text and hover effects.
    /// Extracted from MainMenuRenderer to improve Single Responsibility Principle compliance.
    /// </summary>
    public class MenuOptionRenderer
    {
        private readonly GameCanvasControl canvas;

        public MenuOptionRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }

        /// <summary>
        /// Renders a menu option with colored text
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="number">Menu option number</param>
        /// <param name="text">Menu option text</param>
        /// <param name="color">Base color for the option</param>
        /// <param name="isHovered">Whether the option is currently hovered</param>
        public void RenderColoredMenuOption(int x, int y, int number, string text, Color color, bool isHovered)
        {
            Color numberColor = isHovered ? Colors.Yellow : color;
            Color textColor = isHovered ? Colors.Yellow : color;
            
            string numberText = $"[{number}]";
            canvas.AddText(x, y, numberText, numberColor);
            canvas.AddText(x + numberText.Length + 1, y, text, textColor);
        }

        /// <summary>
        /// Renders the Load Game option with special coloring for character name and level
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="number">Menu option number</param>
        /// <param name="characterName">Character name to display</param>
        /// <param name="characterLevel">Character level to display</param>
        /// <param name="nameColor">Color for character name</param>
        /// <param name="levelColor">Color for character level</param>
        /// <param name="isHovered">Whether the option is currently hovered</param>
        public void RenderColoredLoadGameOption(int x, int y, int number, string characterName, int characterLevel, Color nameColor, Color levelColor, bool isHovered)
        {
            Color baseColor = isHovered ? Colors.Yellow : Colors.White;
            Color charNameColor = isHovered ? Colors.Yellow : nameColor;
            Color charLevelColor = isHovered ? Colors.Yellow : levelColor;
            
            string numberText = $"[{number}]";
            string prefix = "Load Game - *";
            string namePart = characterName;
            string middlePart = " - lvl ";
            string levelPart = characterLevel.ToString();
            string suffix = "*";
            
            int currentX = x;
            
            // Render [2]
            canvas.AddText(currentX, y, numberText, isHovered ? Colors.Yellow : baseColor);
            currentX += numberText.Length + 1;
            
            // Render "Load Game - *"
            canvas.AddText(currentX, y, prefix, baseColor);
            currentX += prefix.Length;
            
            // Render character name in gold
            canvas.AddText(currentX, y, namePart, charNameColor);
            currentX += namePart.Length;
            
            // Render " - lvl "
            canvas.AddText(currentX, y, middlePart, baseColor);
            currentX += middlePart.Length;
            
            // Render level in orange/gold
            canvas.AddText(currentX, y, levelPart, charLevelColor);
            currentX += levelPart.Length;
            
            // Render closing "*"
            canvas.AddText(currentX, y, suffix, baseColor);
        }
    }
}

