using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem.Themes;

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
            
            // Calculate content box dimensions (vertical packing follows center-column zoom stride)
            int stride = CenterColumnTextZoom.LineStride;
            int boxPadding = 4;
            int boxWidth = Math.Min(60, width - boxPadding * 2);
            // Title (3) + spacing (2) + weapons (5 each) + separator (1) + instructions (2)
            int titleLines = 3;
            int titleSpacingLines = 2;
            int weaponLines = weapons != null ? weapons.Count * 5 : 0;
            int separatorLines = 1;
            int instructionsLines = 2;
            int contentLines = titleLines + titleSpacingLines + weaponLines + separatorLines + instructionsLines;
            int boxHeight = contentLines * stride + 4; // 2-row padding top + bottom so rules clear the stroke
            
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
            int titleX = MenuLayoutCalculator.CalculateCenteredZoomedTextX(contentX, contentWidth, title.Length);
            
            // Decorative rules: ASCII + zoom-clamped so they cannot paint past the gold box
            string topLine = MenuLayoutCalculator.BuildDecorativeRule(title.Length + 4, Math.Max(1, contentWidth - 2));
            int topLineX = MenuLayoutCalculator.CalculateCenteredZoomedTextX(contentX, contentWidth, topLine.Length);
            canvas.AddText(topLineX, currentY, topLine, AsciiArtAssets.Colors.Gold);
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
            
            // Title in gold
            canvas.AddText(titleX, currentY, title, AsciiArtAssets.Colors.Gold);
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
            
            // Decorative line below title
            canvas.AddText(topLineX, currentY, topLine, AsciiArtAssets.Colors.Gold);
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
            
            // Add spacing after title (as calculated in box height)
            currentY += titleSpacingLines * stride;

            if (weapons == null || weapons.Count == 0)
            {
                canvas.AddText(contentX, currentY, "No weapons available", AsciiArtAssets.Colors.Red);
                return currentY - y + 2;
            }

            var previews = new List<WeaponItem>(weapons.Count);
            for (int pi = 0; pi < weapons.Count; pi++)
                previews.Add(GameInitializer.CreateStarterWeaponForMenuIndex(pi + 1));

            var classPresentation = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();

            var statRows = new List<(string damage, string speed, string actionSlots)>(weapons.Count);
            int maxDamageLen = 0;
            int maxSpeedLen = 0;
            int maxActionLen = 0;
            bool anyActionSlotColumn = false;
            for (int pi = 0; pi < weapons.Count; pi++)
            {
                WeaponItem previewForStats = previews[pi];
                string damageText = $"Damage: {previewForStats.GetTotalDamage()}";
                // No hero yet: show the swing at base agility (8s × weapon), in seconds.
                double baseSwing = GameConfiguration.Instance.Combat.BaseAttackTime * previewForStats.GetTotalAttackSpeed();
                string speedText = $"Attack time: {baseSwing:F1}s";
                int actionSlots = ClassPresentationConfig.GetEquippedWeaponComboSlotBonus(previewForStats.WeaponType);
                string actionSlotText = actionSlots > 0 ? $"Action slots: +{actionSlots}" : "";
                if (actionSlots > 0)
                    anyActionSlotColumn = true;
                statRows.Add((damageText, speedText, actionSlotText));
                maxDamageLen = Math.Max(maxDamageLen, damageText.Length);
                maxSpeedLen = Math.Max(maxSpeedLen, speedText.Length);
                if (actionSlotText.Length > 0)
                    maxActionLen = Math.Max(maxActionLen, actionSlotText.Length);
            }

            const string statSeparator = "  │  ";
            int maxStatsLineLen = 0;
            for (int pi = 0; pi < weapons.Count; pi++)
            {
                var (damageText, speedText, actionSlotText) = statRows[pi];
                string line = damageText.PadRight(maxDamageLen) + statSeparator + speedText.PadRight(maxSpeedLen);
                if (anyActionSlotColumn)
                {
                    string actionCell = actionSlotText.Length > 0
                        ? actionSlotText
                        : new string(' ', maxActionLen);
                    line += statSeparator + actionCell;
                }

                maxStatsLineLen = Math.Max(maxStatsLineLen, line.Length);
            }

            int sharedStatsX = MenuLayoutCalculator.CalculateCenteredZoomedTextX(contentX, contentWidth, maxStatsLineLen);

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
                int optionX = MenuLayoutCalculator.CalculateCenteredZoomedTextX(contentX, contentWidth, maxLength);
                
                // Weapon icon/indicator
                string weaponIcon = "◆"; // Decorative bullet
                canvas.AddText(optionX - 2, currentY, weaponIcon, accent);
                
                // Add clickable element (expanded to include icon)
                var option = new ClickableElement
                {
                    X = optionX - 2,
                    Y = currentY,
                    Width = maxLength + 2,
                    Height = 4 * stride, // Name + class + stats rows (zoomed stride)
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
                canvas.AddText(nameX, currentY, weaponName ?? "", GetWeaponNameColor(preview, option.IsHovered));
                currentY = MenuLayoutCalculator.NextMenuLineY(currentY);

                string className = classPresentation.GetDisplayName(preview.WeaponType);
                const string classLabel = "Class: ";
                int classBlockWidth = classLabel.Length + (className ?? "").Length;
                int classLineX = MenuLayoutCalculator.CalculateCenteredZoomedTextX(contentX, contentWidth, classBlockWidth);
                Color classLabelColor = option.IsHovered ? AsciiArtAssets.Colors.Gold : AsciiArtAssets.Colors.Silver;
                Color classNameColor = option.IsHovered ? AsciiArtAssets.Colors.White : accent;
                canvas.AddText(classLineX, currentY, classLabel, classLabelColor);
                canvas.AddText(classLineX + classLabel.Length, currentY, className ?? "", classNameColor, accent, 0.35, 2);
                currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
                
                var (damageText, speedText, actionSlotText) = statRows[i];
                RenderAlignedStatLine(
                    sharedStatsX,
                    currentY,
                    damageText,
                    speedText,
                    actionSlotText,
                    maxDamageLen,
                    maxSpeedLen,
                    statSeparator,
                    anyActionSlotColumn);
                currentY += 2 * stride;
            }
            
            
            // Decorative separator before instructions (match title rule width/position)
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
            canvas.AddText(topLineX, currentY, topLine, AsciiArtAssets.Colors.DarkGray);
            currentY += 2 * stride;
            
            // Instructions at bottom with subtle styling
            string bottomInstructions = UIConstants.Messages.PressNumberOrClick;
            int bottomX = MenuLayoutCalculator.CalculateCenteredZoomedTextX(contentX, contentWidth, bottomInstructions.Length);
            canvas.AddText(bottomX, currentY, bottomInstructions, AsciiArtAssets.Colors.Cyan);
            
            return currentY - y + 2;
        }

        private void RenderAlignedStatLine(
            int statsX,
            int y,
            string damageText,
            string speedText,
            string actionSlotText,
            int maxDamageLen,
            int maxSpeedLen,
            string statSeparator,
            bool anyActionSlotColumn)
        {
            const string separatorChar = "│";
            int damageStart = statsX;
            int firstSeparatorPos = GetFirstStatSeparatorX(statsX, maxDamageLen);
            int speedStart = firstSeparatorPos + separatorChar.Length + 2;

            canvas.AddText(damageStart, y, damageText, AsciiArtAssets.Colors.Green);
            canvas.AddText(firstSeparatorPos, y, separatorChar, AsciiArtAssets.Colors.Gray);
            canvas.AddText(speedStart, y, speedText, AsciiArtAssets.Colors.Blue);

            if (!anyActionSlotColumn || string.IsNullOrEmpty(actionSlotText))
                return;

            int secondSeparatorPos = statsX + maxDamageLen + statSeparator.Length + maxSpeedLen + 2;
            int actionSlotStart = secondSeparatorPos + separatorChar.Length + 2;
            canvas.AddText(secondSeparatorPos, y, separatorChar, AsciiArtAssets.Colors.Gray);
            canvas.AddText(actionSlotStart, y, actionSlotText, AsciiArtAssets.Colors.Cyan);
        }

        internal static int GetFirstStatSeparatorX(int statsX, int maxDamageLen) => statsX + maxDamageLen + 2;

        internal static Color GetWeaponNameColor(WeaponItem preview, bool isHovered)
        {
            // Weapon names should read as "gear text": use rarity coloring (Common = white),
            // and keep hover from overriding that rarity signal.
            string rarity = preview?.Rarity?.Trim() ?? "Common";
            return ItemThemeProvider.GetRarityColor(rarity);
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

