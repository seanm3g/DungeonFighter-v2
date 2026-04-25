using System;
using System.Collections.Generic;
using System.Linq;
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
            // Calculate height to include title (3 lines) + spacing (2) + all weapons (4 lines each) + separator (1) + instructions (2)
            // Each weapon takes: 1 line for name/number + 1 line for stats + 2 lines spacing = 4 lines total
            int titleHeight = 3; // top line + title + bottom line
            int titleSpacing = 2; // spacing after title
            int weaponsHeight = weapons != null ? weapons.Count * 4 : 0; // 4 lines per weapon
            int separatorHeight = 1; // separator line
            int instructionsHeight = 2; // instructions + spacing
            int contentHeight = titleHeight + titleSpacing + weaponsHeight + separatorHeight + instructionsHeight;
            int boxHeight = contentHeight + 4; // Add padding: 2 lines top + 2 lines bottom to avoid border overlap
            
            // Center the box, but ensure it doesn't go above the viewport
            int boxX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, boxWidth);
            int boxY = y + (height / 2) - (boxHeight / 2);
            
            // Ensure minimum top margin to prevent text from being clipped or covered
            // The center panel border is at Y=0, and content area starts at Y=1
            // We need significant clearance to ensure the box border doesn't overlap with center panel border
            // and to ensure text inside the box has proper clearance from both borders
            int minTopMargin = 4; // Increased from 2 to provide more clearance
            if (boxY < y + minTopMargin)
            {
                boxY = y + minTopMargin;
            }
            
            // Ensure box doesn't extend below the available height
            if (boxY + boxHeight > y + height)
            {
                boxY = (y + height) - boxHeight;
                // If box is still too tall, ensure it starts at minimum margin
                if (boxY < y + minTopMargin)
                {
                    boxY = y + minTopMargin;
                }
            }
            
            // Calculate content area inside box (calculate before drawing box)
            // Start content 2 lines inside box to ensure text doesn't get covered by border
            int contentX = boxX + 2;
            int contentY = boxY + 2; // 2 lines padding from top border
            int contentWidth = boxWidth - 4;
            int currentY = contentY;
            
            
            // Draw decorative border box AFTER calculating positions to ensure proper spacing
            canvas.AddBox(boxX, boxY, boxWidth, boxHeight, AsciiArtAssets.Colors.Gold);
            
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
            currentY++;
            
            // Add spacing after title (as calculated in box height)
            currentY += titleSpacing;

            if (weapons == null || weapons.Count == 0)
            {
                canvas.AddText(contentX, currentY, "No weapons available", AsciiArtAssets.Colors.Red);
                return currentY - y + 2;
            }

            var previews = new List<WeaponItem>(weapons.Count);
            for (int pi = 0; pi < weapons.Count; pi++)
                previews.Add(GameInitializer.CreateStarterWeaponForMenuIndex(pi + 1));

            // Find max weapon display text length for centering (actual item names from the starter pipeline)
            int maxLength = 0;
            for (int i = 0; i < weapons.Count; i++)
            {
                string weaponName = string.IsNullOrWhiteSpace(previews[i].Name) ? $"Weapon {i + 1}" : previews[i].Name;
                string displayText = MenuOptionFormatter.Format(i + 1, weaponName);
                if (displayText.Length > maxLength)
                    maxLength = displayText.Length;
            }
            
            // Render all weapons
            for (int i = 0; i < weapons.Count; i++)
            {
                int weaponNum = i + 1; // First weapon (index 0) should be numbered [1]
                WeaponItem preview = previews[i];
                Color accent = WeaponMenuAccentColor(preview.WeaponType);
                
                // Ensure weapon name is not null or empty - use fallback if needed
                string weaponName = string.IsNullOrWhiteSpace(preview.Name) ? $"Weapon {weaponNum}" : preview.Name;
                
                string displayText = MenuOptionFormatter.Format(weaponNum, weaponName);
                
                // Center each weapon option
                int optionX = MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, maxLength);
                
                // Weapon icon/indicator
                string weaponIcon = "◆"; // Decorative bullet
                canvas.AddText(optionX - 2, currentY, weaponIcon, accent);
                
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
                Color weaponColor = option.IsHovered ? AsciiArtAssets.Colors.White : accent;
                
                // Render menu option - render number first, then name
                string numberText = $"[{weaponNum}]";
                canvas.AddText(optionX, currentY, numberText, weaponColor);
                int nameX = optionX + numberText.Length + 1;
                canvas.AddText(nameX, currentY, weaponName ?? "", option.IsHovered ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                currentY++;
                
                // Weapon stats from the same pipeline as InitializeNewGame (starter-tagged menu rows + tuning)
                string damageText = $"Damage: {preview.GetTotalDamage()}";
                string speedText = $"Speed: {preview.GetTotalAttackSpeed():F2}×";
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

        private static Color WeaponMenuAccentColor(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => AsciiArtAssets.Colors.Cyan,
            WeaponType.Sword => AsciiArtAssets.Colors.Yellow,
            WeaponType.Dagger => AsciiArtAssets.Colors.Magenta,
            WeaponType.Wand => AsciiArtAssets.Colors.Purple,
            _ => AsciiArtAssets.Colors.Gray
        };
    }
}

