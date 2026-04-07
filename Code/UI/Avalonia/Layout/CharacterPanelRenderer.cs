using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.Actions.RollModification;

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
        private const string ToggleSectionThresholds = "toggle_section_thresholds";

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
            
            // --- HERO --- (left-aligned like STATS/GEAR; body order: name, HP bar, Lvl+class, XP)
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
                int nameY = y;
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
                int levelY = y;
                canvas.AddText(x, y, $"Lvl {character.Level} {currentClass}", AsciiArtAssets.Colors.Gold);
                y++;

                int xpRequired = character.Progression.GetXPRequiredForNextLevel();
                int xpY = y;
                canvas.AddText(x, y, $"XP {character.XP}/{xpRequired}", AsciiArtAssets.Colors.Cyan);
                y++;

                string classPointsText = GetClassPointsDisplay(character);
                int? classPointsY = null;
                if (!string.IsNullOrEmpty(classPointsText))
                {
                    classPointsY = y;
                    canvas.AddText(x, y, classPointsText, AsciiArtAssets.Colors.Gray);
                    y++;
                }

                if (interactionManager != null && stateManager != null)
                {
                    RegisterLeftPanelHoverRow(x, nameY, headerClickWidth, 1, "hero:name");
                    RegisterLeftPanelHoverRow(x, healthBarY, headerClickWidth, 2, "hero:hp");
                    RegisterLeftPanelHoverRow(x, levelY, headerClickWidth, 1, "hero:level");
                    RegisterLeftPanelHoverRow(x, xpY, headerClickWidth, 1, "hero:xp");
                    if (classPointsY.HasValue)
                        RegisterLeftPanelHoverRow(x, classPointsY.Value, headerClickWidth, 1, "hero:classpts");
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

                int damageRowY = y;
                canvas.AddCharacterStat(x, y, "Damage", totalDamage, 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.White);
                y++;
                int speedRowY = y;
                canvas.AddText(x, y, $"Speed:   {attackSpeed:F2}s", AsciiArtAssets.Colors.White);
                y++;
                int armorRowY = y;
                canvas.AddCharacterStat(x, y, "Armor", character.GetTotalArmor(), 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.White);
                y++;
                y++;

                string primaryStat = GetPrimaryStatForCharacter(character);

                int strRowY = y;
                canvas.AddCharacterStat(x, y, "STR", character.GetEffectiveStrength(), 0,
                    primaryStat == "Strength" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
                y++;
                int agiRowY = y;
                canvas.AddCharacterStat(x, y, "AGI", character.GetEffectiveAgility(), 0,
                    primaryStat == "Agility" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
                y++;
                int tecRowY = y;
                canvas.AddCharacterStat(x, y, "TECH", character.GetEffectiveTechnique(), 0,
                    primaryStat == "Technique" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
                y++;
                int intRowY = y;
                canvas.AddCharacterStat(x, y, "INT", character.GetEffectiveIntelligence(), 0,
                    primaryStat == "Intelligence" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
                y++;

                List<(int rowY, string idSuffix)>? expandedHoverTargets = interactionManager != null && stateManager != null
                    ? new List<(int rowY, string idSuffix)>()
                    : null;

                // Secondary stat lines (magic find, roll bonus, etc.) always shown when STATS is open; lphover:* rows registered below.
                RenderExpandedStats(character, x, ref y, expandedHoverTargets);

                y += 2;
                int statsAreaEndY = y;

                if (stateManager != null && interactionManager != null)
                {
                    stateManager.SetStatsAreaBounds(x, statsHeaderY, headerClickWidth, statsAreaEndY - statsHeaderY);
                    RegisterLeftPanelHoverRow(x, damageRowY, headerClickWidth, 1, "stat:damage");
                    RegisterLeftPanelHoverRow(x, speedRowY, headerClickWidth, 1, "stat:speed");
                    RegisterLeftPanelHoverRow(x, armorRowY, headerClickWidth, 1, "stat:armor");
                    RegisterLeftPanelHoverRow(x, strRowY, headerClickWidth, 1, "stat:str");
                    RegisterLeftPanelHoverRow(x, agiRowY, headerClickWidth, 1, "stat:agi");
                    RegisterLeftPanelHoverRow(x, tecRowY, headerClickWidth, 1, "stat:tec");
                    RegisterLeftPanelHoverRow(x, intRowY, headerClickWidth, 1, "stat:int");
                    if (expandedHoverTargets != null)
                    {
                        foreach (var (rowY, id) in expandedHoverTargets)
                            RegisterLeftPanelHoverRow(x, rowY, headerClickWidth, 1, id);
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
                RenderEquipmentSlot(x, ref y, headerClickWidth, "Weapon", character.Weapon, "gear:weapon", 1);
                RenderEquipmentSlot(x, ref y, headerClickWidth, "Head", character.Head, "gear:head", 1);
                RenderEquipmentSlot(x, ref y, headerClickWidth, "Body", character.Body, "gear:body", 1);
                RenderEquipmentSlot(x, ref y, headerClickWidth, "Feet", character.Feet, "gear:feet", 1);
            }

            // --- THRESHOLDS --- (dice roll thresholds; collapsible)
            int thresholdsHeaderY = y;
            canvas.AddText(x, y, FormatLeftPanelSectionHeader(UIConstants.Headers.Thresholds), AsciiArtAssets.Colors.Gold);
            y += 2;
            if (interactionManager != null && stateManager != null)
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = x,
                    Y = thresholdsHeaderY,
                    Width = headerClickWidth,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = ToggleSectionThresholds,
                    DisplayText = "Thresholds"
                });
            }

            bool thresholdsOpen = stateManager == null || !stateManager.ThresholdsCollapsed;
            if (thresholdsOpen)
            {
                var tm = RollModificationManager.GetThresholdManager();
                var config = GameConfiguration.Instance;
                int hit = tm.GetHitThreshold(character);
                int combo = tm.GetComboThreshold(character);
                int crit = tm.GetCriticalHitThreshold(character);
                int critMiss = tm.GetCriticalMissThreshold(character);
                int defaultHit = config.RollSystem.MissThreshold.Max > 0 ? config.RollSystem.MissThreshold.Max : 5;
                int defaultCombo = config.RollSystem.ComboThreshold.Min > 0 ? config.RollSystem.ComboThreshold.Min : 14;
                int defaultCrit = config.Combat.CriticalHitThreshold > 0 ? config.Combat.CriticalHitThreshold : 20;
                const int defaultCritMiss = 1;
                const int thresholdLabelWidth = 11;
                void RenderThresholdRow(int rowY, string labelName, int current, int def, string displayedValue)
                {
                    string labelPart = $"{labelName}:".PadRight(thresholdLabelWidth);
                    var valueColor = ThresholdDisplayFormatting.GetValueColor(current, def);
                    string modStr = ThresholdDisplayFormatting.FormatDeltaSuffix(current, def);
                    canvas.AddText(x, rowY, labelPart, AsciiArtAssets.Colors.Cyan);
                    canvas.AddText(x + thresholdLabelWidth, rowY, displayedValue, valueColor);
                    if (modStr.Length > 0)
                        canvas.AddText(x + thresholdLabelWidth + displayedValue.Length, rowY, modStr, valueColor);
                }

                int critY = y;
                RenderThresholdRow(y, "Crit", crit, defaultCrit, crit.ToString());
                y++;
                int comboY = y;
                RenderThresholdRow(y, "Combo", combo, defaultCombo, combo.ToString());
                y++;
                int hitY = y;
                RenderThresholdRow(y, "Hit", hit, defaultHit, (hit + 1).ToString());
                y++;
                int critMissY = y;
                RenderThresholdRow(y, "Crit Miss", critMiss, defaultCritMiss, critMiss.ToString());
                y++;

                if (interactionManager != null && stateManager != null)
                {
                    RegisterLeftPanelHoverRow(x, critY, headerClickWidth, 1, "thresh:crit");
                    RegisterLeftPanelHoverRow(x, comboY, headerClickWidth, 1, "thresh:combo");
                    RegisterLeftPanelHoverRow(x, hitY, headerClickWidth, 1, "thresh:hit");
                    RegisterLeftPanelHoverRow(x, critMissY, headerClickWidth, 1, "thresh:critmiss");
                }
            }
            if (thresholdsOpen)
                y += 1;

            // --- STATUS EFFECTS --- (hero; enemy effects are on the right panel)
            canvas.AddText(x, y, FormatLeftPanelSectionHeader(UIConstants.Headers.StatusEffects), AsciiArtAssets.Colors.Gold);
            y += 2;
            const int maxHeroEffectLines = 5;
            const int maxHeroLineLen = 29;
            var heroEffects = StatusEffectDisplayLines.Build(character, character);
            if (heroEffects.Count > 0)
            {
                for (int i = 0; i < Math.Min(heroEffects.Count, maxHeroEffectLines); i++)
                {
                    int effectRowY = y;
                    string line = heroEffects[i];
                    if (line.Length > maxHeroLineLen)
                        line = line.Substring(0, maxHeroLineLen - 3) + "...";
                    canvas.AddText(x, y, line, AsciiArtAssets.Colors.White);
                    if (interactionManager != null && stateManager != null)
                        RegisterLeftPanelHoverRow(x, effectRowY, headerClickWidth, 1, "status:" + i);
                    y++;
                }
                if (heroEffects.Count > maxHeroEffectLines)
                {
                    int overflowY = y;
                    canvas.AddText(x, y, $"+{heroEffects.Count - maxHeroEffectLines} more", AsciiArtAssets.Colors.Gray);
                    if (interactionManager != null && stateManager != null)
                        RegisterLeftPanelHoverRow(x, overflowY, headerClickWidth, 1, "status:overflow");
                    y++;
                }
            }
        }

        private void RegisterLeftPanelHoverRow(int x, int rowY, int width, int height, string idSuffix)
        {
            if (interactionManager == null || stateManager == null || height < 1 || width < 1)
                return;
            interactionManager.AddClickableElement(new ClickableElement
            {
                X = x,
                Y = rowY,
                Width = width,
                Height = height,
                Type = ElementType.Text,
                Value = LeftPanelHoverState.Prefix + idSuffix,
                DisplayText = "Left panel tooltip"
            });
        }
        
        /// <summary>
        /// Helper method to render a single equipment slot with consistent formatting and text wrapping
        /// Uses colored text system to show item colors based on type and modifiers
        /// </summary>
        private void RenderEquipmentSlot(int x, ref int y, int hoverWidth, string slotName, Item? item, string hoverGearId, int spacingAfter = 1)
        {
            int blockStartY = y;
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

            int contentEndY = y;
            y += spacingAfter;

            if (interactionManager != null && stateManager != null)
            {
                int h = contentEndY - blockStartY;
                if (h > 0)
                    RegisterLeftPanelHoverRow(x, blockStartY, hoverWidth, h, hoverGearId);
            }
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
        /// Renders secondary stats (magic find, roll bonus, etc.). When <paramref name="expandedHoverTargets"/> is non-null,
        /// appends (row Y, hover id) for each row; the caller registers <c>lphover:*</c> clickables for tooltips.
        /// </summary>
        private void RenderExpandedStats(Character character, int x, ref int y, List<(int rowY, string idSuffix)>? expandedHoverTargets)
        {
            // Magic Find (only if > 0)
            int magicFind = character.GetMagicFind();
            if (magicFind > 0)
            {
                int rowY = y;
                canvas.AddCharacterStat(x, y, "MAG FIND", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"+{magicFind}", AsciiArtAssets.Colors.Cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:magfind"));
            }
            
            // Roll Bonus (with breakdown)
            int intRollBonus = character.GetIntelligenceRollBonus();
            int eqRollBonus = character.GetEquipmentRollBonus();
            int modRollBonus = character.GetModificationRollBonus();
            int totalRollBonus = intRollBonus + eqRollBonus + modRollBonus;
            
            if (totalRollBonus > 0)
            {
                int rowY = y;
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
                expandedHoverTargets?.Add((rowY, "stat:roll"));
            }
            
            // Health Regen (only if > 0)
            int healthRegen = character.GetEquipmentHealthRegenBonus();
            if (healthRegen > 0)
            {
                int rowY = y;
                canvas.AddCharacterStat(x, y, "HP REGEN", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"+{healthRegen}/turn", AsciiArtAssets.Colors.Cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:hpregen"));
            }
            
            // Lifesteal (only if > 0)
            double lifesteal = character.GetModificationLifesteal();
            if (lifesteal > 0)
            {
                int rowY = y;
                canvas.AddCharacterStat(x, y, "LIFESTEAL", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"{lifesteal:P0}", AsciiArtAssets.Colors.Cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:lifesteal"));
            }
            
            // Bleed Chance (only if > 0)
            double bleedChance = character.GetModificationBleedChance();
            if (bleedChance > 0)
            {
                int rowY = y;
                canvas.AddCharacterStat(x, y, "BLEED", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"{bleedChance:P0}", AsciiArtAssets.Colors.Cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:bleed"));
            }
            
            // Burn Chance (only if > 0)
            double burnChance = character.GetModificationBurnChance();
            if (burnChance > 0)
            {
                int rowY = y;
                canvas.AddCharacterStat(x, y, "BURN", 0, 0, AsciiArtAssets.Colors.Cyan, AsciiArtAssets.Colors.Cyan);
                canvas.AddText(x + 9, y, $"{burnChance:P0}", AsciiArtAssets.Colors.Cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:burn"));
            }
        }
    }
}

