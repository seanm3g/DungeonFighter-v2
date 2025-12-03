using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the weapon selection screen
    /// </summary>
    public class WeaponSelectionRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasInteractionManager interactionManager;
        private readonly ICanvasTextManager textManager;
        
        public WeaponSelectionRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasInteractionManager interactionManager, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.interactionManager = interactionManager;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the weapon selection content with centered layout
        /// </summary>
        public int RenderWeaponSelectionContent(int x, int y, int width, int height, List<StartingWeapon> weapons)
        {
            // Center the content vertically
            int centerY = y + (height / 2) - (weapons.Count * 3) / 2 - 2;
            
            // Instructions
            string instructions = "Choose your starting weapon:";
            int instructionX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, instructions.Length);
            canvas.AddText(instructionX, centerY, instructions, AsciiArtAssets.Colors.White);
            centerY += 3;
            
            // Find max weapon display text length for centering
            int maxLength = 0;
            foreach (var weapon in weapons)
            {
                string displayText = MenuOptionFormatter.Format(weapons.IndexOf(weapon) + 1, weapon.name);
                if (displayText.Length > maxLength)
                    maxLength = displayText.Length;
            }
            
            // Render weapon options centered
            foreach (var weapon in weapons)
            {
                int weaponNum = weapons.IndexOf(weapon) + 1;
                string displayText = MenuOptionFormatter.Format(weaponNum, weapon.name);
                
                // Center each weapon option
                int optionX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, maxLength);
                
                // Add clickable element
                var option = new ClickableElement
                {
                    X = optionX,
                    Y = centerY,
                    Width = displayText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = weaponNum.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                interactionManager.AddClickableElement(option);
                
                // Display weapon option using canvas.AddMenuOption (same as main menu)
                canvas.AddMenuOption(optionX, centerY, weaponNum, weapon.name, AsciiArtAssets.Colors.White, option.IsHovered);
                centerY++;
                
                // Weapon stats (indented slightly)
                string stats = $"    Damage: {weapon.damage:F1}, Attack Speed: {weapon.attackSpeed:F2}s";
                int statsX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, stats.Length);
                canvas.AddText(statsX, centerY, stats, AsciiArtAssets.Colors.Gray);
                centerY += 2;
            }
            
            // Instructions at bottom
            centerY += 2;
            string bottomInstructions = UIConstants.Messages.PressNumberOrClick;
            int bottomX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, bottomInstructions.Length);
            canvas.AddText(bottomX, centerY, bottomInstructions, AsciiArtAssets.Colors.White);
            
            return centerY - y;
        }
    }
}

