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
            // Main border for character panel
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X + 2, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH - 2, LayoutConstants.LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Blue);
            
            int y = LayoutConstants.LEFT_PANEL_Y + 1;
            int x = LayoutConstants.LEFT_PANEL_X + 4;
            
            // Character name and level
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Hero), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            canvas.AddText(x, y, character.Name, ColorPalette.Player.GetColor());
            y++;
            canvas.AddText(x, y, $"Lvl {character.Level}", AsciiArtAssets.Colors.Yellow);
            y++;
            canvas.AddText(x, y, character.GetCurrentClass(), AsciiArtAssets.Colors.Cyan);
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
            
            canvas.AddHealthBar(x, y, healthBarWidth, character.CurrentHealth, character.GetEffectiveMaxHealth());
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
            
            // Determine primary stat based on class points
            string primaryStat = GetPrimaryStatForCharacter(character);
            
            // All stats are white by default, primary stat is purple
            canvas.AddCharacterStat(x, y, "STR", character.GetEffectiveStrength(), 0, 
                primaryStat == "STR" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "AGI", character.GetEffectiveAgility(), 0, 
                primaryStat == "AGI" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "TEC", character.GetEffectiveTechnique(), 0, 
                primaryStat == "TEC" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "INT", character.GetEffectiveIntelligence(), 0, 
                primaryStat == "INT" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y += 2;
            
            // Equipment section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Gear), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Render all equipment slots with increased spacing for text wrapping
            RenderEquipmentSlot(x, ref y, "Weapon", character.Weapon, 2);
            RenderEquipmentSlot(x, ref y, "Head", character.Head, 2);
            RenderEquipmentSlot(x, ref y, "Body", character.Body, 2);
            RenderEquipmentSlot(x, ref y, "Feet", character.Feet, 1);
            
            // Combo sequence section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Combo), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var comboActions = character.GetComboActions();
            if (comboActions.Count > 0)
            {
                // Display combo sequence with arrow indicator
                // Limit display to prevent overflow (max 5 actions shown)
                int maxActionsToShow = System.Math.Min(comboActions.Count, 5);
                for (int i = 0; i < maxActionsToShow; i++)
                {
                    var action = comboActions[i];
                    bool isNext = (character.ComboStep % comboActions.Count == i);
                    string indicator = isNext ? "→" : " ";
                    string actionName = action.Name;
                    
                    // Truncate long action names to fit in the panel
                    const int maxActionNameLength = 18;
                    if (actionName.Length > maxActionNameLength)
                    {
                        actionName = actionName.Substring(0, maxActionNameLength - 3) + "...";
                    }
                    
                    // Use yellow for the next action, white for others
                    var color = isNext ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White;
                    canvas.AddText(x, y, $"{indicator} {actionName}", color);
                    y++;
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
                
                // Wrap text if it's too long (max width of 17 characters)
                const int maxWidth = 17;
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
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X + 2, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH - 2, LayoutConstants.LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Gray);
            
            int y = LayoutConstants.LEFT_PANEL_Y + LayoutConstants.LEFT_PANEL_HEIGHT / 2;
            int x = LayoutConstants.LEFT_PANEL_X + 6;
            
            canvas.AddText(x, y, "No Character", AsciiArtAssets.Colors.Gray);
            canvas.AddText(x, y + 1, "Loaded", AsciiArtAssets.Colors.Gray);
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
                ("STR", barbarianPoints),  // Barbarian - Strength
                ("AGI", warriorPoints),    // Warrior - Agility
                ("TEC", roguePoints),      // Rogue - Technique
                ("INT", wizardPoints)      // Wizard - Intelligence
            };
            
            classes.Sort((a, b) => b.points.CompareTo(a.points));
            
            // If no class points, no primary stat (all white)
            if (classes[0].points == 0)
                return "";
            
            return classes[0].stat;
        }
    }
}

