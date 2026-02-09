namespace RPGGame.UI.Avalonia.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame;
    using RPGGame.Actions.RollModification;
    using RPGGame.Combat;
    using RPGGame.UI;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Renders the right panel (location/enemy info or inventory actions)
    /// </summary>
    public class RightPanelRenderer
    {
        private readonly GameCanvasControl canvas;
        
        public RightPanelRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        /// <summary>
        /// Renders the dungeon and enemy information panel (right side)
        /// Shows combo sequence and action pool when on inventory page or combo management, otherwise shows location/enemy info
        /// </summary>
        public void RenderRightPanel(Enemy? enemy, string? dungeonName, string? roomName, string title, Character? character)
        {
            // Clear the right panel content area before rendering to prevent text overlap
            int contentX = LayoutConstants.RIGHT_PANEL_X + 2;
            int contentY = LayoutConstants.RIGHT_PANEL_Y + 1;
            int contentWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 4;  // Account for left and right borders
            int contentHeight = LayoutConstants.RIGHT_PANEL_HEIGHT - 2; // Account for top and bottom borders
            
            canvas.ClearTextInArea(contentX, contentY, contentWidth, contentHeight);
            canvas.ClearProgressBarsInArea(contentX, contentY, contentWidth, contentHeight);
            
            // Main border for right panel - extends to right edge with no padding
            canvas.AddBorder(LayoutConstants.RIGHT_PANEL_X, LayoutConstants.RIGHT_PANEL_Y, LayoutConstants.RIGHT_PANEL_WIDTH, LayoutConstants.RIGHT_PANEL_HEIGHT, AsciiArtAssets.Colors.Purple);
            
            int y = LayoutConstants.RIGHT_PANEL_Y + 1;
            int x = LayoutConstants.RIGHT_PANEL_X + 2;
            
            // Check if we're on the inventory page or combo management page
            if ((title == "INVENTORY" || title == "COMBO MANAGEMENT") && character != null)
            {
                // Render combo sequence and action pool for inventory page and combo management
                RenderInventoryRightPanel(x, y, character);
            }
            else
            {
                // Render location and enemy info for other pages
                RenderLocationEnemyPanel(x, y, enemy, dungeonName, roomName, character);
            }
        }
        
        /// <summary>
        /// Renders combo sequence and action pool for inventory page
        /// </summary>
        private void RenderInventoryRightPanel(int x, int y, Character character)
        {
            // Combo Sequence section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("COMBO SEQUENCE"), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var comboActions = character.GetComboActions();
            if (comboActions.Count > 0)
            {
                int currentStepInSequence = (character.ComboStep % comboActions.Count) + 1;
                canvas.AddText(x, y, $"Step: {currentStepInSequence}/{comboActions.Count}", AsciiArtAssets.Colors.White);
                y += 2;
                
                // Show combo sequence (limit to fit in panel)
                int maxDisplay = Math.Min(comboActions.Count, 8); // Limit to 8 to fit in panel
                for (int i = 0; i < maxDisplay; i++)
                {
                    var action = comboActions[i];
                    string currentStep = (character.ComboStep % comboActions.Count == i) ? " ←" : "";
                    string actionName = action.Name;
                    if (actionName.Length > 20)
                        actionName = actionName.Substring(0, 17) + "...";
                    canvas.AddText(x, y, $"{i + 1}. {actionName}{currentStep}", AsciiArtAssets.Colors.White);
                    y++;
                }
                
                if (comboActions.Count > maxDisplay)
                {
                    canvas.AddText(x, y, $"... +{comboActions.Count - maxDisplay} more", AsciiArtAssets.Colors.Gray);
                    y++;
                }
            }
            else
            {
                canvas.AddText(x, y, "(No combo set)", AsciiArtAssets.Colors.DarkGray);
                y += 2;
            }
            
            y += 1; // Spacing
            
            // Action Pool section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("ACTION POOL"), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var actionPool = character.GetActionPool();
            if (actionPool.Count > 0)
            {
                canvas.AddText(x, y, $"Total: {actionPool.Count}", AsciiArtAssets.Colors.White);
                y += 2;
                
                // Group actions by name and show unique actions
                var uniqueActions = actionPool.GroupBy(a => a.Name)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderBy(a => a.Name)
                    .Take(6) // Limit to 6 to fit in panel
                    .ToList();
                
                foreach (var actionGroup in uniqueActions)
                {
                    string actionName = actionGroup.Name;
                    if (actionName.Length > 20)
                        actionName = actionName.Substring(0, 17) + "...";
                    string countText = actionGroup.Count > 1 ? $" x{actionGroup.Count}" : "";
                    canvas.AddText(x, y, $"{actionName}{countText}", AsciiArtAssets.Colors.Cyan);
                    y++;
                }
                
                if (actionPool.Count > uniqueActions.Sum(a => a.Count))
                {
                    int remaining = actionPool.Count - uniqueActions.Sum(a => a.Count);
                    canvas.AddText(x, y, $"... +{remaining} more", AsciiArtAssets.Colors.Gray);
                }
            }
            else
            {
                canvas.AddText(x, y, "(No actions)", AsciiArtAssets.Colors.DarkGray);
            }
        }
        
        /// <summary>
        /// Renders location and enemy information panel
        /// </summary>
        private void RenderLocationEnemyPanel(int x, int y, Enemy? enemy, string? dungeonName, string? roomName, Character? character)
        {
            // Location section - always shown
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Location), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Dungeon - always shown
            canvas.AddText(x, y, "Dungeon:", AsciiArtAssets.Colors.Gray);
            y++;
            if (!string.IsNullOrEmpty(dungeonName))
            {
                string displayDungeon = dungeonName;
                if (displayDungeon.Length > 20)
                    displayDungeon = displayDungeon.Substring(0, 17) + "...";
                canvas.AddText(x, y, displayDungeon, AsciiArtAssets.Colors.Cyan);
            }
            else
            {
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.DarkGray);
            }
            y += 2;
            
            // Room - always shown
            canvas.AddText(x, y, "Room:", AsciiArtAssets.Colors.Gray);
            y++;
            if (!string.IsNullOrEmpty(roomName))
            {
                string displayRoom = roomName;
                if (displayRoom.Length > 20)
                    displayRoom = displayRoom.Substring(0, 17) + "...";
                canvas.AddText(x, y, displayRoom, AsciiArtAssets.Colors.Yellow);
            }
            else
            {
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.DarkGray);
            }
            y += 2;
            
            // Enemy section - always shown
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Enemy), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            if (enemy != null)
            {
                string enemyName = enemy.Name;
                if (enemyName.Length > 20)
                    enemyName = enemyName.Substring(0, 17) + "...";
                
                canvas.AddText(x, y, enemyName, EntityColorHelper.GetEnemyColor(enemy));
                y += 2;
                
                // Health bar
                // Clear the health bar and HP value area before redrawing to prevent text overlap
                int healthBarWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 8;
                int healthBarY = y;
                int hpValueY = y + 1;
                canvas.ClearProgressBarsInArea(x, healthBarY, healthBarWidth, 1);
                canvas.ClearTextInArea(x, hpValueY, healthBarWidth, 1);
                
                canvas.AddHealthBar(x, y, healthBarWidth, enemy.CurrentHealth, enemy.MaxHealth, 
                    entityId: $"enemy_{enemy.Name}");
                canvas.AddText(x, y + 1, $"{enemy.CurrentHealth}/{enemy.MaxHealth}", AsciiArtAssets.Colors.White);
                y += 3;
                
                // Calculate enemy damage (Strength + weapon damage)
                int weaponDamage = (enemy.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
                int totalDamage = enemy.GetEffectiveStrength() + weaponDamage;
                
                // Damage and Armor on same line
                canvas.AddText(x, y, $"DMG:  {totalDamage}", AsciiArtAssets.Colors.White);
                y++;
                canvas.AddText(x, y, $"ARM:  {enemy.Armor}", AsciiArtAssets.Colors.White);
                y += 2;
                
                // Attributes
                canvas.AddCharacterStat(x, y, "STR", enemy.Strength, 0, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddCharacterStat(x, y, "AGI", enemy.Agility, 0, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddCharacterStat(x, y, "TECH", enemy.Technique, 0, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddCharacterStat(x, y, "INT", enemy.Intelligence, 0, AsciiArtAssets.Colors.White);
            }
            else
            {
                // Show empty enemy state
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.DarkGray);
                y += 2;
                canvas.AddText(x, y, "No active", AsciiArtAssets.Colors.DarkGray);
                y++;
                canvas.AddText(x, y, "combat", AsciiArtAssets.Colors.DarkGray);
            }
            y += 2;

            // Dice thresholds section (player only when character is present)
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Thresholds), AsciiArtAssets.Colors.Gold);
            y += 2;
            if (character != null)
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
                // Display modifier as "bonus": positive when requirement is lower (default - current). E.g. +5 to HIT = 5 lower requirement to hit. Not used for accuracy.
                string mod(int current, int def) => current != def ? (def - current) > 0 ? $" (+{def - current})" : $" ({def - current})" : "";
                const int thresholdLabelWidth = 11; // "Crit Miss:" + 1 space so numbers align
                canvas.AddText(x, y, $"{"Crit:".PadRight(thresholdLabelWidth)}{crit}{mod(crit, defaultCrit)}", AsciiArtAssets.Colors.Cyan);
                y++;
                canvas.AddText(x, y, $"{"Combo:".PadRight(thresholdLabelWidth)}{combo}{mod(combo, defaultCombo)}", AsciiArtAssets.Colors.Cyan);
                y++;
                canvas.AddText(x, y, $"{"Hit:".PadRight(thresholdLabelWidth)}{hit + 1}{mod(hit, defaultHit)}", AsciiArtAssets.Colors.Cyan);
                y++;
                canvas.AddText(x, y, $"{"Crit Miss:".PadRight(thresholdLabelWidth)}{critMiss}{mod(critMiss, defaultCritMiss)}", AsciiArtAssets.Colors.Cyan);
                y++;
            }
            else
            {
                canvas.AddText(x, y, "(No character)", AsciiArtAssets.Colors.DarkGray);
                y++;
            }
            y += 2;

            // Status effects section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.StatusEffects), AsciiArtAssets.Colors.Gold);
            y += 2;
            const int maxEffectLines = 5;
            const int maxLineLen = 26;
            if (character != null)
            {
                var playerEffects = GetActiveStatusEffectLines(character, character);
                if (playerEffects.Count == 0)
                {
                    canvas.AddText(x, y, "(None)", AsciiArtAssets.Colors.DarkGray);
                    y++;
                }
                else
                {
                    for (int i = 0; i < Math.Min(playerEffects.Count, maxEffectLines); i++)
                    {
                        string line = playerEffects[i];
                        if (line.Length > maxLineLen)
                            line = line.Substring(0, maxLineLen - 3) + "...";
                        canvas.AddText(x, y, line, AsciiArtAssets.Colors.White);
                        y++;
                    }
                    if (playerEffects.Count > maxEffectLines)
                    {
                        canvas.AddText(x, y, $"+{playerEffects.Count - maxEffectLines} more", AsciiArtAssets.Colors.Gray);
                        y++;
                    }
                }
            }
            if (enemy != null)
            {
                canvas.AddText(x, y, "Enemy:", AsciiArtAssets.Colors.Gray);
                y++;
                const int maxEnemyEffectLines = 3;
                var enemyEffects = GetActiveStatusEffectLines(enemy, null);
                if (enemyEffects.Count == 0)
                {
                    canvas.AddText(x, y, "  (None)", AsciiArtAssets.Colors.DarkGray);
                    y++;
                }
                else
                {
                    int toShow = Math.Min(enemyEffects.Count, maxEnemyEffectLines);
                    for (int i = 0; i < toShow; i++)
                    {
                        string line = "  " + enemyEffects[i];
                        if (line.Length > maxLineLen)
                            line = line.Substring(0, maxLineLen - 3) + "...";
                        canvas.AddText(x, y, line, AsciiArtAssets.Colors.White);
                        y++;
                    }
                    if (enemyEffects.Count > toShow)
                    {
                        canvas.AddText(x, y, $"  +{enemyEffects.Count - toShow} more", AsciiArtAssets.Colors.Gray);
                        y++;
                    }
                }
            }
            if (character == null && enemy == null)
            {
                canvas.AddText(x, y, "(None)", AsciiArtAssets.Colors.DarkGray);
            }
        }

        /// <summary>
        /// Builds a list of active status effect display strings for an actor (and Character.Effects when actor is a Character).
        /// </summary>
        private static List<string> GetActiveStatusEffectLines(Actor actor, Character? asCharacter)
        {
            var lines = new List<string>();
            if (actor.IsWeakened && actor.WeakenTurns > 0)
                lines.Add($"Weakened ({actor.WeakenTurns} turn{(actor.WeakenTurns != 1 ? "s" : "")})");
            if (actor.IsStunned && actor.StunTurnsRemaining > 0)
                lines.Add($"Stunned ({actor.StunTurnsRemaining} turn{(actor.StunTurnsRemaining != 1 ? "s" : "")})");
            if (actor.RollPenalty != 0 && actor.RollPenaltyTurns > 0)
                lines.Add($"Roll -{actor.RollPenalty} ({actor.RollPenaltyTurns} turn{(actor.RollPenaltyTurns != 1 ? "s" : "")})");
            if (actor.PoisonStacks > 0)
                lines.Add(actor.IsBleeding ? $"Bleed x{actor.PoisonStacks}" : $"Poison x{actor.PoisonStacks}");
            if (actor.BurnStacks > 0)
                lines.Add($"Burn x{actor.BurnStacks}");
            if (actor.HasCriticalMissPenalty && actor.CriticalMissPenaltyTurns > 0)
                lines.Add($"Crit miss ({actor.CriticalMissPenaltyTurns} turn{(actor.CriticalMissPenaltyTurns != 1 ? "s" : "")})");
            if (actor.VulnerabilityStacks > 0 && actor.VulnerabilityTurns > 0)
                lines.Add($"Vuln x{actor.VulnerabilityStacks} ({actor.VulnerabilityTurns}t)");
            if (actor.HardenStacks > 0 && actor.HardenTurns > 0)
                lines.Add($"Harden x{actor.HardenStacks} ({actor.HardenTurns}t)");
            if (actor.FortifyStacks > 0 && actor.FortifyTurns > 0)
                lines.Add($"Fortify x{actor.FortifyStacks} ({actor.FortifyTurns}t)");
            if (actor.FocusStacks > 0 && actor.FocusTurns > 0)
                lines.Add($"Focus x{actor.FocusStacks} ({actor.FocusTurns}t)");
            if (actor.ExposeStacks > 0 && actor.ExposeTurns > 0)
                lines.Add($"Expose x{actor.ExposeStacks} ({actor.ExposeTurns}t)");
            if (actor.HPRegenStacks > 0 && actor.HPRegenTurns > 0)
                lines.Add($"HP Regen x{actor.HPRegenStacks} ({actor.HPRegenTurns}t)");
            if (actor.ArmorBreakStacks > 0 && actor.ArmorBreakTurns > 0)
                lines.Add($"Armor brk x{actor.ArmorBreakStacks} ({actor.ArmorBreakTurns}t)");
            if (actor.HasPierce && actor.PierceTurns > 0)
                lines.Add($"Pierce ({actor.PierceTurns} turn{(actor.PierceTurns != 1 ? "s" : "")})");
            if (actor.ReflectStacks > 0 && actor.ReflectTurns > 0)
                lines.Add($"Reflect x{actor.ReflectStacks} ({actor.ReflectTurns}t)");
            if (actor.IsSilenced && actor.SilenceTurns > 0)
                lines.Add($"Silence ({actor.SilenceTurns} turn{(actor.SilenceTurns != 1 ? "s" : "")})");
            if (actor.HasAbsorb && actor.AbsorbTurns > 0)
                lines.Add($"Absorb ({actor.AbsorbTurns} turn{(actor.AbsorbTurns != 1 ? "s" : "")})");
            if (actor.TemporaryHP > 0 && actor.TemporaryHPTurns > 0)
                lines.Add($"Temp HP ({actor.TemporaryHPTurns} turn{(actor.TemporaryHPTurns != 1 ? "s" : "")})");
            if (actor.IsConfused && actor.ConfusionTurns > 0)
                lines.Add($"Confused ({actor.ConfusionTurns} turn{(actor.ConfusionTurns != 1 ? "s" : "")})");
            if (actor.IsMarked && actor.MarkTurns > 0)
                lines.Add($"Marked ({actor.MarkTurns} turn{(actor.MarkTurns != 1 ? "s" : "")})");
            if (asCharacter != null)
            {
                int rollBonus = asCharacter.Effects.GetTempRollBonus();
                if (rollBonus != 0 && asCharacter.Effects.TempRollBonusTurns > 0)
                {
                    int t = asCharacter.Effects.TempRollBonusTurns;
                    lines.Add($"Accuracy +{rollBonus} ({t} turn{(t != 1 ? "s" : "")})");
                }
                if (asCharacter.Effects.SlowTurns > 0)
                    lines.Add($"Slow ({asCharacter.Effects.SlowTurns} turn{(asCharacter.Effects.SlowTurns != 1 ? "s" : "")})");
                if (asCharacter.Effects.HasShield)
                    lines.Add("Shield");
                if (asCharacter.Effects.SkipNextTurn)
                    lines.Add("Skip turn");
                if (asCharacter.Effects.GuaranteeNextSuccess)
                    lines.Add("Guarantee hit");
                if (asCharacter.Effects.ExtraAttacks > 0)
                    lines.Add($"Extra atk x{asCharacter.Effects.ExtraAttacks}");
                if (asCharacter.Effects.ComboModeActive)
                    lines.Add("Combo mode");
                if (asCharacter.Effects.RerollCharges > 0)
                    lines.Add($"Reroll x{asCharacter.Effects.RerollCharges}");
            }
            return lines;
        }
    }
}

