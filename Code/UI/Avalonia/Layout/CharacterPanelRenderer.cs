using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.Avalonia.Layout
{

    /// <summary>
    /// Renders the character information panel (left side)
    /// </summary>
    public class CharacterPanelRenderer
    {
        private const string ToggleSectionHero = "toggle_section_hero";
        private const string ToggleSectionStats = "toggle_section_stats";
        private const string ToggleSectionGear = "toggle_section_gear";
        private const string ToggleSectionActions = "toggle_section_actions";
        private const string ToggleStatsExpansion = "toggle_stats_expansion";

        /// <summary>ASCII section line matching STATS: <c>====  LABEL  ====</c> (double spaces around label).</summary>
        private static string FormatLeftPanelSectionHeader(string label) => $"====  {label}  ====";

        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly StatsPanelStateManager? stateManager;
        private readonly ICanvasInteractionManager? interactionManager;
        
        public CharacterPanelRenderer(
            GameCanvasControl canvas, 
            ColoredTextWriter textWriter,
            StatsPanelStateManager? stateManager = null,
            ICanvasInteractionManager? interactionManager = null)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.stateManager = stateManager;
            this.interactionManager = interactionManager;
        }
        
        /// <summary>
        /// Renders the character information panel (left side)
        /// </summary>
        public void RenderCharacterPanel(Character character)
        {
            // Clear the left panel area before drawing so re-renders with clearCanvas: false (e.g. after level-up) do not leave duplicate content
            int leftX = LayoutConstants.LEFT_PANEL_X;
            int leftY = LayoutConstants.LEFT_PANEL_Y;
            int leftW = LayoutConstants.LEFT_PANEL_WIDTH;
            int leftH = LayoutConstants.LEFT_PANEL_HEIGHT + 1;
            canvas.ClearTextInArea(leftX, leftY, leftW, leftH);
            canvas.ClearProgressBarsInArea(leftX, leftY, leftW, leftH);
            canvas.ClearBoxesInArea(leftX, leftY, leftW, leftH);

            // Main border for character panel - starts at X=0 with no padding
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Blue);
            
            int y = LayoutConstants.LEFT_PANEL_Y + 1;
            int x = LayoutConstants.LEFT_PANEL_X + 2; // Reduced from +4 since border now starts at 0
            int headerClickWidth = LayoutConstants.LEFT_PANEL_WIDTH - 4;
            
            // --- HERO --- (left-aligned like STATS/GEAR/ACTIONS; body order: name, HP bar, Lvl+class, XP)
            int heroHeaderY = y;
            string heroHeaderText = FormatLeftPanelSectionHeader(UIConstants.Headers.Hero);
            canvas.AddText(x, y, heroHeaderText, AsciiArtAssets.Colors.Gold);
            y += 2;
            if (interactionManager != null && stateManager != null)
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = x,
                    Y = heroHeaderY,
                    Width = headerClickWidth,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = ToggleSectionHero,
                    DisplayText = "Hero"
                });
            }

            bool heroOpen = stateManager == null || !stateManager.HeroCollapsed;
            if (heroOpen)
            {
                canvas.AddText(x, y, character.Name, AsciiArtAssets.Colors.White);
                y++;

                int healthBarWidth = LayoutConstants.LEFT_PANEL_WIDTH - 4;
                int healthBarY = y;
                int hpValueY = y + 1;
                canvas.ClearProgressBarsInArea(x, healthBarY, healthBarWidth, 1);
                canvas.ClearTextInArea(x, hpValueY, healthBarWidth, 1);
                canvas.AddHealthBar(x, y, healthBarWidth, character.CurrentHealth, character.GetEffectiveMaxHealth(), entityId: $"player_{character.Name}");
                canvas.AddText(x, y + 1, $"{character.CurrentHealth}/{character.GetEffectiveMaxHealth()}", AsciiArtAssets.Colors.White);
                y = healthBarY + 3;

                string currentClass = character.GetCurrentClass();
                canvas.AddText(x, y, $"Lvl {character.Level} {currentClass}", AsciiArtAssets.Colors.Gold);
                y++;

                int xpRequired = character.Progression.GetXPRequiredForNextLevel();
                canvas.AddText(x, y, $"XP {character.XP}/{xpRequired}", AsciiArtAssets.Colors.Cyan);
                y++;

                string classPointsText = GetClassPointsDisplay(character);
                if (!string.IsNullOrEmpty(classPointsText))
                {
                    canvas.AddText(x, y, classPointsText, AsciiArtAssets.Colors.Gray);
                    y++;
                }

                y += 2;
            }

            // --- STATS --- (static gold like HERO/GEAR; no glow — glow animation shifted hue vs other headers)
            int statsHeaderY = y;
            string statsHeaderText = FormatLeftPanelSectionHeader(UIConstants.Headers.Stats);
            canvas.AddText(x, y, statsHeaderText, AsciiArtAssets.Colors.Gold);
            y += 2;
            if (interactionManager != null && stateManager != null)
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = x,
                    Y = statsHeaderY,
                    Width = headerClickWidth,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = ToggleSectionStats,
                    DisplayText = "Stats"
                });
            }

            bool statsOpen = stateManager == null || !stateManager.StatsCollapsed;
            if (statsOpen)
            {
                int weaponDamage = (character.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
                int equipmentDamageBonus = character.GetEquipmentDamageBonus();
                int modificationDamageBonus = character.GetModificationDamageBonus();
                int totalDamage = character.GetEffectiveStrength() + weaponDamage + equipmentDamageBonus + modificationDamageBonus;
                double attackSpeed = character.GetTotalAttackSpeed();

                canvas.AddCharacterStat(x, y, "Damage", totalDamage, 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddText(x, y, $"Speed:   {attackSpeed:F2}s", AsciiArtAssets.Colors.White);
                y++;
                canvas.AddCharacterStat(x, y, "Armor", character.GetTotalArmor(), 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.White);
                y++;
                y++;

                // Expansion toggle only for attribute + expanded block (not Damage/Speed/Armor) — avoids stray line toggle when clicking primary stats
                int statsExpansionClickStartY = y;

                string primaryStat = GetPrimaryStatForCharacter(character);

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
                y++;

                if (stateManager != null && stateManager.IsExpanded)
                {
                    // Do not advance y before RenderExpandedStats: when no secondary lines qualify,
                    // an extra y++ would leave a lone blank row between INT and GEAR when toggling.
                    RenderExpandedStats(character, x, ref y);
                }

                y += 2;
                int statsAreaEndY = y;

                if (stateManager != null && interactionManager != null)
                {
                    stateManager.SetStatsAreaBounds(x, statsHeaderY, headerClickWidth, statsAreaEndY - statsHeaderY);
                    int expansionClickHeight = statsAreaEndY - statsExpansionClickStartY;
                    if (expansionClickHeight > 0)
                    {
                        interactionManager.AddClickableElement(new ClickableElement
                        {
                            X = x,
                            Y = statsExpansionClickStartY,
                            Width = headerClickWidth,
                            Height = expansionClickHeight,
                            Type = ElementType.Text,
                            Value = ToggleStatsExpansion,
                            DisplayText = "Stats detail"
                        });
                    }
                }
            }
            else if (stateManager != null)
            {
                stateManager.ResetStatsAreaBounds();
            }

            // --- GEAR ---
            int gearHeaderY = y;
            canvas.AddText(x, y, FormatLeftPanelSectionHeader(UIConstants.Headers.Gear), AsciiArtAssets.Colors.Gold);
            y += 2;
            if (interactionManager != null && stateManager != null)
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = x,
                    Y = gearHeaderY,
                    Width = headerClickWidth,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = ToggleSectionGear,
                    DisplayText = "Gear"
                });
            }

            if (stateManager == null || !stateManager.GearCollapsed)
            {
                RenderEquipmentSlot(x, ref y, "Weapon", character.Weapon, 1);
                RenderEquipmentSlot(x, ref y, "Head", character.Head, 1);
                RenderEquipmentSlot(x, ref y, "Body", character.Body, 1);
                RenderEquipmentSlot(x, ref y, "Feet", character.Feet, 1);
            }

            // --- ACTIONS ---
            int actionsHeaderY = y;
            canvas.AddText(x, y, FormatLeftPanelSectionHeader(UIConstants.Headers.Combo), AsciiArtAssets.Colors.Gold);
            y += 2;
            if (interactionManager != null && stateManager != null)
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = x,
                    Y = actionsHeaderY,
                    Width = headerClickWidth,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = ToggleSectionActions,
                    DisplayText = "Actions"
                });
            }

            if (stateManager == null || !stateManager.ActionsCollapsed)
            {
                var comboActions = character.GetComboActions();
                if (comboActions.Count > 0)
                {
                    const int maxActionNameLength = 27;
                    const int maxDescriptionWidth = 27;
                    int panelBottom = LayoutConstants.LEFT_PANEL_Y + LayoutConstants.LEFT_PANEL_HEIGHT - 2;

                    for (int i = 0; i < comboActions.Count; i++)
                    {
                        var action = comboActions[i];
                        bool isNext = (character.ComboStep % comboActions.Count == i);
                        string indicator = isNext ? "→" : " ";
                        string actionName = action.Name ?? "";
                        if (actionName.Length > maxActionNameLength)
                            actionName = actionName.Substring(0, maxActionNameLength - 3) + "...";

                        var color = isNext ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White;
                        canvas.AddText(x, y, $"{indicator} {actionName}", color);
                        y++;

                        if (isNext && !string.IsNullOrEmpty(action.Description))
                        {
                            var wrappedLines = textWriter.WrapText(action.Description, maxDescriptionWidth);
                            foreach (var line in wrappedLines)
                            {
                                if (y > panelBottom)
                                    break;
                                canvas.AddText(x + 2, y, line, AsciiArtAssets.Colors.Gray);
                                y++;
                            }
                        }
                    }
                }
                else
                {
                    canvas.AddText(x, y, "None", AsciiArtAssets.Colors.Gray);
                    y++;
                }
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
            int leftX = LayoutConstants.LEFT_PANEL_X;
            int leftY = LayoutConstants.LEFT_PANEL_Y;
            int leftW = LayoutConstants.LEFT_PANEL_WIDTH;
            int leftH = LayoutConstants.LEFT_PANEL_HEIGHT + 1;
            canvas.ClearTextInArea(leftX, leftY, leftW, leftH);
            canvas.ClearBoxesInArea(leftX, leftY, leftW, leftH);

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
        
        /// <summary>
        /// Renders expanded stats that are normally hidden
        /// </summary>
        private void RenderExpandedStats(Character character, int x, ref int y)
        {
            // Magic Find (only if > 0)
            int magicFind = character.GetMagicFind();
            if (magicFind > 0)
            {
                canvas.AddCharacterStat(x, y, "MAG FIND", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"+{magicFind}", AsciiArtAssets.Colors.Cyan);
                y++;
            }
            
            // Roll Bonus (with breakdown)
            int intRollBonus = character.GetIntelligenceRollBonus();
            int eqRollBonus = character.GetEquipmentRollBonus();
            int modRollBonus = character.GetModificationRollBonus();
            int totalRollBonus = intRollBonus + eqRollBonus + modRollBonus;
            
            if (totalRollBonus > 0)
            {
                canvas.AddCharacterStat(x, y, "ROLL", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                string rollBreakdown = $"+{totalRollBonus}";
                if (intRollBonus > 0 || eqRollBonus > 0 || modRollBonus > 0)
                {
                    rollBreakdown += $" (INT:+{intRollBonus}";
                    if (eqRollBonus > 0) rollBreakdown += $" EQ:+{eqRollBonus}";
                    if (modRollBonus > 0) rollBreakdown += $" MOD:+{modRollBonus}";
                    rollBreakdown += ")";
                }
                canvas.AddText(x + 9, y, rollBreakdown, AsciiArtAssets.Colors.Cyan);
                y++;
            }
            
            // Health Regen (only if > 0)
            int healthRegen = character.GetEquipmentHealthRegenBonus();
            if (healthRegen > 0)
            {
                canvas.AddCharacterStat(x, y, "HP REGEN", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"+{healthRegen}/turn", AsciiArtAssets.Colors.Cyan);
                y++;
            }
            
            // Lifesteal (only if > 0)
            double lifesteal = character.GetModificationLifesteal();
            if (lifesteal > 0)
            {
                canvas.AddCharacterStat(x, y, "LIFESTEAL", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"{lifesteal:P0}", AsciiArtAssets.Colors.Cyan);
                y++;
            }
            
            // Bleed Chance (only if > 0)
            double bleedChance = character.GetModificationBleedChance();
            if (bleedChance > 0)
            {
                canvas.AddCharacterStat(x, y, "BLEED", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"{bleedChance:P0}", AsciiArtAssets.Colors.Cyan);
                y++;
            }
            
            // Burn Chance (only if > 0)
            double burnChance = character.GetModificationBurnChance();
            if (burnChance > 0)
            {
                canvas.AddCharacterStat(x, y, "BURN", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"{burnChance:P0}", AsciiArtAssets.Colors.Cyan);
                y++;
            }
        }
    }
}

