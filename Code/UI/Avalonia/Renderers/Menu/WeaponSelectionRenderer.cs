using System;
using System.Collections.Generic;
using Avalonia.Media;
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
        /// Renders the weapon selection content with centered layout and fancy styling
        /// </summary>
        public int RenderWeaponSelectionContent(int x, int y, int width, int height, List<StartingWeapon> weapons)
        {
            // Calculate content box dimensions
            int boxPadding = 4;
            int boxWidth = Math.Min(60, width - boxPadding * 2);
            int contentHeight = 3 + (weapons.Count * 4) + 3; // Title + weapons + instructions
            int boxHeight = contentHeight + 2; // Add padding
            
            // Center the box
            int boxX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, boxWidth);
            int boxY = y + (height / 2) - (boxHeight / 2);
            
            // Draw decorative border box
            canvas.AddBox(boxX, boxY, boxWidth, boxHeight, AsciiArtAssets.Colors.Gold);
            
            // Calculate content area inside box
            int contentX = boxX + 2;
            int contentY = boxY + 1;
            int contentWidth = boxWidth - 4;
            int currentY = contentY;
            
            // Title with decorative styling
            string title = "Choose your starting weapon";
            int titleX = MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, title.Length);
            
            // Decorative line above title
            string topLine = new string(AsciiArtAssets.UIElements.BorderHorizontal[0], Math.Min(title.Length + 4, contentWidth - 2));
            int topLineX = MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, topLine.Length);
            canvas.AddText(topLineX, currentY, topLine, AsciiArtAssets.Colors.Gold);
            currentY++;
            
            // Title in gold
            canvas.AddText(titleX, currentY, title, AsciiArtAssets.Colors.Gold);
            currentY++;
            
            // Decorative line below title
            canvas.AddText(topLineX, currentY, topLine, AsciiArtAssets.Colors.Gold);
            currentY += 2;
            
            // Find max weapon display text length for centering
            int maxLength = 0;
            foreach (var weapon in weapons)
            {
                string displayText = MenuOptionFormatter.Format(weapons.IndexOf(weapon) + 1, weapon.name);
                if (displayText.Length > maxLength)
                    maxLength = displayText.Length;
            }
            
            // Weapon color scheme - different colors for visual variety
            var weaponColors = new[]
            {
                AsciiArtAssets.Colors.Cyan,    // Mace
                AsciiArtAssets.Colors.Yellow,  // Sword
                AsciiArtAssets.Colors.Magenta, // Dagger
                AsciiArtAssets.Colors.Purple   // Wand
            };
            
            // Render weapon options with fancy styling
            for (int i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];
                int weaponNum = i + 1;
                string displayText = MenuOptionFormatter.Format(weaponNum, weapon.name);
                
                // Center each weapon option
                int optionX = MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, maxLength);
                
                // Weapon icon/indicator
                string weaponIcon = "◆"; // Decorative bullet
                canvas.AddText(optionX - 2, currentY, weaponIcon, weaponColors[i % weaponColors.Length]);
                
                // Add clickable element (expanded to include icon)
                var option = new ClickableElement
                {
                    X = optionX - 2,
                    Y = currentY,
                    Width = maxLength + 2,
                    Height = 3, // Include stats line
                    Type = ElementType.MenuOption,
                    Value = weaponNum.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                interactionManager.AddClickableElement(option);
                
                // Display weapon option with color
                Color weaponColor = option.IsHovered ? AsciiArtAssets.Colors.White : weaponColors[i % weaponColors.Length];
                canvas.AddMenuOption(optionX, currentY, weaponNum, weapon.name, weaponColor, option.IsHovered);
                currentY++;
                
                // Weapon stats with better formatting
                string damageText = $"Damage: {weapon.damage:F1}";
                string speedText = $"Speed: {weapon.attackSpeed:F2}s";
                string separatorChar = "│";
                string stats = $"  {damageText}  {separatorChar}  {speedText}";
                int statsX = MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, stats.Length);
                
                // Color-code stats - render each part separately
                int damageStart = statsX + 2; // Account for leading spaces
                int separatorPos = statsX + stats.IndexOf(separatorChar);
                int speedStart = separatorPos + separatorChar.Length + 2; // After separator and spaces
                
                canvas.AddText(damageStart, currentY, damageText, AsciiArtAssets.Colors.Green);
                canvas.AddText(separatorPos, currentY, separatorChar, AsciiArtAssets.Colors.Gray);
                canvas.AddText(speedStart, currentY, speedText, AsciiArtAssets.Colors.Blue);
                currentY += 2;
            }
            
            // Decorative separator before instructions
            currentY++;
            string separator = new string(AsciiArtAssets.UIElements.BorderHorizontal[0], Math.Min(40, contentWidth - 2));
            int separatorX = MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, separator.Length);
            canvas.AddText(separatorX, currentY, separator, AsciiArtAssets.Colors.DarkGray);
            currentY += 2;
            
            // Instructions at bottom with subtle styling
            string bottomInstructions = UIConstants.Messages.PressNumberOrClick;
            int bottomX = MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, bottomInstructions.Length);
            canvas.AddText(bottomX, currentY, bottomInstructions, AsciiArtAssets.Colors.Cyan);
            
            return currentY - y + 2;
        }
    }
}

