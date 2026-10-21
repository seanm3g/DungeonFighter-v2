using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

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
        /// Renders the Load Character(s) option with the saved fighter count highlighted
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="number">Menu option number</param>
        /// <param name="fighterCount">Number of saved fighters to display</param>
        /// <param name="countColor">Color for the fighter count</param>
        /// <param name="isHovered">Whether the option is currently hovered</param>
        public void RenderColoredLoadGameOption(int x, int y, int number, int fighterCount, Color countColor, bool isHovered)
        {
            Color baseColor = isHovered ? Colors.Yellow : Colors.White;
            Color fighterCountColor = isHovered ? Colors.Yellow : countColor;
            
            string numberText = $"[{number}]";
            string prefix = UIConstants.MenuOptions.LoadGame + UIConstants.Formats.LoadCharactersSeparator;
            string countPart = fighterCount.ToString();
            string suffix = " " + UIConstants.Formats.FighterCountLabel(fighterCount);
            
            int currentX = x;
            
            // Render [2]
            canvas.AddText(currentX, y, numberText, baseColor);
            currentX += numberText.Length + 1;
            
            // Render "Load Character(s) | "
            canvas.AddText(currentX, y, prefix, baseColor);
            currentX += prefix.Length;
            
            // Render fighter count in orange
            canvas.AddText(currentX, y, countPart, fighterCountColor);
            currentX += countPart.Length;
            
            // Render " Fighters"
            canvas.AddText(currentX, y, suffix, baseColor);
        }

        /// <summary>
        /// Renders a menu option with colored character name
        /// Character name is rendered in its class-specific color, rest of text in base color
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="number">Menu option number</param>
        /// <param name="character">Character to get name and color from</param>
        /// <param name="suffixText">Text to display after character name (e.g., " - Level X - Class")</param>
        /// <param name="baseColor">Base color for the option and suffix text</param>
        /// <param name="isHovered">Whether the option is currently hovered</param>
        public void RenderColoredMenuOptionWithCharacter(int x, int y, int number, Character character, string suffixText, Color baseColor, bool isHovered)
        {
            Color numberColor = isHovered ? Colors.Yellow : baseColor;
            Color textColor = isHovered ? Colors.Yellow : baseColor;
            Color characterNameColor = isHovered ? Colors.Yellow : EntityColorHelper.GetActorColor(character);
            
            string numberText = $"[{number}]";
            string characterName = character.Name;
            
            int currentX = x;
            
            // Render [number]
            canvas.AddText(currentX, y, numberText, numberColor);
            currentX += numberText.Length + 1;
            
            // Render character name in its class color
            canvas.AddText(currentX, y, characterName, characterNameColor);
            currentX += characterName.Length;
            
            // Render suffix text in base color
            if (!string.IsNullOrEmpty(suffixText))
            {
                canvas.AddText(currentX, y, suffixText, textColor);
            }
        }
    }
}

