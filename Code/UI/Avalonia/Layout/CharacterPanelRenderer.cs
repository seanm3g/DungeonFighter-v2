using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.Avalonia.Layout
{

    /// <summary>
    /// Renders the character information panel (left side)
    /// </summary>
    public class CharacterPanelRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        
        public CharacterPanelRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
        }
        
        /// <summary>
        /// Renders the character information panel (left side)
        /// </summary>
        public void RenderCharacterPanel(Character character)
        {
            // Main border for character panel - starts at X=0 with no padding
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Blue);
            
            int y = LayoutConstants.LEFT_PANEL_Y + 1;
            int x = LayoutConstants.LEFT_PANEL_X + 2; // Reduced from +4 since border now starts at 0
            
            // Character name and level
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Hero), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            canvas.AddText(x, y, character.Name, EntityColorHelper.GetActorColor(character));
            y++;
            canvas.AddText(x, y, $"Lvl {character.Level}", AsciiArtAssets.Colors.Yellow);
            y++;
            
            // Display class points if character has any
            string classPointsText = GetClassPointsDisplay(character);
            if (!string.IsNullOrEmpty(classPointsText))
            {
                canvas.AddText(x, y, classPointsText, AsciiArtAssets.Colors.Gray);
                y++;
            }
            
            // Display class title with class-appropriate color
            string currentClass = character.GetCurrentClass();
            var classColor = EntityColorHelper.GetClassColorForDisplay(character);
            canvas.AddText(x, y, currentClass, classColor);
            y += 2;
            
            // Health bar
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Health), AsciiArtAssets.Colors.Gold);
            y += 2;
            canvas.AddText(x, y, "HP:", AsciiArtAssets.Colors.White);
            y++;
            
            // Clear the health bar and HP value area before redrawing to prevent text overlap
            int healthBarWidth = LayoutConstants.LEFT_PANEL_WIDTH - 8;
            int healthBarY = y;
            int hpValueY = y + 1;
            canvas.ClearProgressBarsInArea(x, healthBarY, healthBarWidth, 1);
            canvas.ClearTextInArea(x, hpValueY, healthBarWidth, 1);
            
            canvas.AddHealthBar(x, y, healthBarWidth, character.CurrentHealth, character.GetEffectiveMaxHealth(), entityId: $"player_{character.Name}");
            canvas.AddText(x, y + 1, $"{character.CurrentHealth}/{character.GetEffectiveMaxHealth()}", AsciiArtAssets.Colors.White);
            y += 3;
            
            // Stats section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Stats), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Calculate total damage
            int weaponDamage = (character.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
            int equipmentDamageBonus = character.GetEquipmentDamageBonus();
            int modificationDamageBonus = character.GetModificationDamageBonus();
            int totalDamage = character.GetEffectiveStrength() + weaponDamage + equipmentDamageBonus + modificationDamageBonus;
            
            // Add damage and armor stats first
            canvas.AddCharacterStat(x, y, "DMG", totalDamage, 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.Magenta);
            y++;
            canvas.AddCharacterStat(x, y, "ARM", character.GetTotalArmor(), 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.Magenta);
            y++;
            y++; // Blank line after armor
            
            // Determine primary stat based on class points
            string primaryStat = GetPrimaryStatForCharacter(character);
            
            // All stats are white by default, primary stat is purple
            canvas.AddCharacterStat(x, y, "STR", character.GetEffectiveStrength(), 0, 
                primaryStat == "Strength" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "AGI", character.GetEffectiveAgility(), 0, 
                primaryStat == "Agility" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "TECH", character.GetEffectiveTechnique(), 0, 
                primaryStat == "Technique" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "INT", character.GetEffectiveIntelligence(), 0, 
                primaryStat == "Intelligence" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y += 2;
            
            // Equipment section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Gear), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Render all equipment slots with consistent spacing
            RenderEquipmentSlot(x, ref y, "Weapon", character.Weapon, 1);
            RenderEquipmentSlot(x, ref y, "Head", character.Head, 1);
            RenderEquipmentSlot(x, ref y, "Body", character.Body, 1);
            RenderEquipmentSlot(x, ref y, "Feet", character.Feet, 1);
            
            // Combo sequence section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Combo), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var comboActions = character.GetComboActions();
            if (comboActions.Count > 0)
            {
                // Display combo sequence with arrow indicator
                // Limit display to prevent overflow (max 3 actions shown to account for descriptions)
                int maxActionsToShow = System.Math.Min(comboActions.Count, 3);
                for (int i = 0; i < maxActionsToShow; i++)
                {
                    var action = comboActions[i];
                    bool isNext = (character.ComboStep % comboActions.Count == i);
                    string indicator = isNext ? "→" : " ";
                    string actionName = action.Name;
                    
                    // Truncate long action names to fit in the panel (accounts for padding: panel width - left padding - right border - arrow space)
                    // Panel width is 32, left padding is 2, right border is 1, arrow "→ " is 2, so available width is 27
                    const int maxActionNameLength = 27; // Panel width (32) - left padding (2) - right border (1) - arrow (2) = 27
                    if (actionName.Length > maxActionNameLength)
                    {
                        actionName = actionName.Substring(0, maxActionNameLength - 3) + "...";
                    }
                    
                    // Use yellow for the next action, white for others
                    var color = isNext ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White;
                    canvas.AddText(x, y, $"{indicator} {actionName}", color);
                    y++;
                    
                    // Display action description below the action name with text wrapping
                    if (!string.IsNullOrEmpty(action.Description))
                    {
                        string description = action.Description;
                        // Wrap long descriptions to fit in the panel
                        // Available width is 27 (panel width 32 - left padding 2 - right border 1 - indent 2)
                        const int maxDescriptionWidth = 27;
                        var wrappedLines = textWriter.WrapText(description, maxDescriptionWidth);
                        
                        // Render each wrapped line with 2-space indent to align under the action name
                        foreach (var line in wrappedLines)
                        {
                            canvas.AddText(x + 2, y, line, AsciiArtAssets.Colors.Gray);
                            y++;
                        }
                    }
                }
                
                // If there are more actions, show a count
                if (comboActions.Count > maxActionsToShow)
                {
                    canvas.AddText(x, y, $"  ... +{comboActions.Count - maxActionsToShow} more", AsciiArtAssets.Colors.Gray);
                    y++;
                }
            }
            else
            {
                // No combo actions
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.Gray);
                y++;
            }
        }
        
        /// <summary>
        /// Helper method to render a single equipment slot with consistent formatting and text wrapping
        /// Uses colored text system to show item colors based on type and modifiers
        /// </summary>
        private void RenderEquipmentSlot(int x, ref int y, string slotName, Item? item, int spacingAfter = 1)
        {
            canvas.AddText(x, y, $"{slotName}:", AsciiArtAssets.Colors.Gray);
            y++;
            
            if (item != null)
            {
                // Get colored item name segments
                var itemNameSegments = ItemDisplayColoredText.FormatFullItemName(item);
                
                // Wrap text if it's too long (max width accounts for padding: panel width - left padding - right border)
                // Panel width is 32, left padding is 2, right border is 1, so available width is 29
                const int maxWidth = 29; // Panel width (32) - left padding (2) - right border (1) = 29
                var wrappedLines = textWriter.WrapColoredSegments(itemNameSegments, maxWidth);
                
                // Render each wrapped line with proper colors
                foreach (var lineSegments in wrappedLines)
                {
                    if (lineSegments.Count > 0)
                    {
                        textWriter.RenderSegments(lineSegments, x, y);
                    }
                    y++;
                }
            }
            else
            {
                // Empty slot - show "None" in gray
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.Gray);
                y++;
            }
            
            y += spacingAfter;
        }
        
        /// <summary>
        /// Renders an empty character panel when no character is loaded
        /// </summary>
        public void RenderEmptyCharacterPanel()
        {
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Gray);
            
            int y = LayoutConstants.LEFT_PANEL_Y + LayoutConstants.LEFT_PANEL_HEIGHT / 2;
            int x = LayoutConstants.LEFT_PANEL_X + 2; // Reduced from +6 since border now starts at 0
            
            canvas.AddText(x, y, "No Character", AsciiArtAssets.Colors.Gray);
            canvas.AddText(x, y + 1, "Loaded", AsciiArtAssets.Colors.Gray);
        }
        
        /// <summary>
        /// Gets a formatted string displaying all class points the character has
        /// Returns empty string if character has no class points
        /// </summary>
        private string GetClassPointsDisplay(Character character)
        {
            var classPoints = new List<string>();
            
            if (character.BarbarianPoints > 0)
                classPoints.Add($"Barb: {character.BarbarianPoints}");
            if (character.WarriorPoints > 0)
                classPoints.Add($"War: {character.WarriorPoints}");
            if (character.RoguePoints > 0)
                classPoints.Add($"Rog: {character.RoguePoints}");
            if (character.WizardPoints > 0)
                classPoints.Add($"Wiz: {character.WizardPoints}");
            
            return classPoints.Count > 0 ? string.Join(" | ", classPoints) : "";
        }
        
        /// <summary>
        /// Determines the primary stat for a character based on their class points
        /// </summary>
        private string GetPrimaryStatForCharacter(Character character)
        {
            // Get the highest class points to determine primary stat
            int barbarianPoints = character.BarbarianPoints;
            int warriorPoints = character.WarriorPoints;
            int roguePoints = character.RoguePoints;
            int wizardPoints = character.WizardPoints;
            
            // Find the class with the most points
            var classes = new List<(string stat, int points)>
            {
                ("Strength", barbarianPoints),  // Barbarian - Strength
                ("Agility", warriorPoints),    // Warrior - Agility
                ("Technique", roguePoints),      // Rogue - Technique
                ("Intelligence", wizardPoints)      // Wizard - Intelligence
            };
            
            classes.Sort((a, b) => b.points.CompareTo(a.points));
            
            // If no class points, no primary stat (all white)
            if (classes[0].points == 0)
                return "";
            
            return classes[0].stat;
        }
    }
}

