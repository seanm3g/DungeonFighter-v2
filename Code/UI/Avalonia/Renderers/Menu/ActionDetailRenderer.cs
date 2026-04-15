using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the action detail view screen
    /// </summary>
    public class ActionDetailRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public ActionDetailRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the action detail content
        /// </summary>
        public int RenderActionDetailContent(int x, int y, int width, int height, ActionData action)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple top-left menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Title
            string title = $"=== {action.Name} ===";
            canvas.AddText(menuStartX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 2;
            
            // Basic Information
            canvas.AddText(menuStartX, menuStartY, "Basic Information:", AsciiArtAssets.Colors.Cyan);
            menuStartY += 1;
            
            canvas.AddText(menuStartX, menuStartY, $"  Target Type: {action.TargetType}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Description: {action.Description}", AsciiArtAssets.Colors.White);
            menuStartY += 2;
            
            // Combat Stats
            canvas.AddText(menuStartX, menuStartY, "Combat Stats:", AsciiArtAssets.Colors.Cyan);
            menuStartY += 1;
            
            canvas.AddText(menuStartX, menuStartY, $"  Damage Multiplier: {action.DamageMultiplier}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Cooldown: {action.Cooldown}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Speed: {action.Length}", AsciiArtAssets.Colors.White);
            menuStartY++;
            bool hasSheetMods = !string.IsNullOrWhiteSpace(action.SpeedMod) || !string.IsNullOrWhiteSpace(action.DamageMod)
                || !string.IsNullOrWhiteSpace(action.MultiHitMod) || !string.IsNullOrWhiteSpace(action.AmpMod);
            if (hasSheetMods)
            {
                canvas.AddText(menuStartX, menuStartY, "Modifiers (next action/ability):", AsciiArtAssets.Colors.Cyan);
                menuStartY++;
                if (!string.IsNullOrWhiteSpace(action.SpeedMod)) { canvas.AddText(menuStartX, menuStartY, $"  SpeedMod: {action.SpeedMod}%", AsciiArtAssets.Colors.White); menuStartY++; }
                if (!string.IsNullOrWhiteSpace(action.DamageMod)) { canvas.AddText(menuStartX, menuStartY, $"  DamageMod: {action.DamageMod}%", AsciiArtAssets.Colors.White); menuStartY++; }
                if (!string.IsNullOrWhiteSpace(action.MultiHitMod)) { canvas.AddText(menuStartX, menuStartY, $"  MultiHitMod: {action.MultiHitMod}", AsciiArtAssets.Colors.White); menuStartY++; }
                if (!string.IsNullOrWhiteSpace(action.AmpMod)) { canvas.AddText(menuStartX, menuStartY, $"  AmpMod: {action.AmpMod}%", AsciiArtAssets.Colors.White); menuStartY++; }
            }
            menuStartY += 1;
            
            // Status Effects
            bool hasStatusEffects = action.CausesBleed || action.CausesWeaken || action.CausesSlow || 
                                   action.CausesPoison || action.CausesBurn;
            if (hasStatusEffects)
            {
                canvas.AddText(menuStartX, menuStartY, "Status Effects:", AsciiArtAssets.Colors.Cyan);
                menuStartY += 1;
                
                if (action.CausesBleed)
                {
                    canvas.AddText(menuStartX, menuStartY, "  - Causes Bleed", AsciiArtAssets.Colors.Red);
                    menuStartY++;
                }
                if (action.CausesWeaken)
                {
                    canvas.AddText(menuStartX, menuStartY, "  - Causes Weaken", AsciiArtAssets.Colors.Yellow);
                    menuStartY++;
                }
                if (action.CausesSlow)
                {
                    canvas.AddText(menuStartX, menuStartY, "  - Causes Slow", AsciiArtAssets.Colors.Blue);
                    menuStartY++;
                }
                if (action.CausesPoison)
                {
                    canvas.AddText(menuStartX, menuStartY, "  - Causes Poison", AsciiArtAssets.Colors.Green);
                    menuStartY++;
                }
                if (action.CausesBurn)
                {
                    canvas.AddText(menuStartX, menuStartY, "  - Causes Burn", AsciiArtAssets.Colors.Red);
                    menuStartY++;
                }
                menuStartY += 1;
            }
            
            // Combo Information
            if (action.IsComboAction || action.ComboOrder > 0)
            {
                canvas.AddText(menuStartX, menuStartY, "Combo Information:", AsciiArtAssets.Colors.Cyan);
                menuStartY += 1;
                if (action.ComboOrder > 0)
                {
                    canvas.AddText(menuStartX, menuStartY, $"  Combo Order: {action.ComboOrder}", AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                if (action.ComboBonusAmount > 0)
                {
                    canvas.AddText(menuStartX, menuStartY, $"  Combo Bonus Amount: {action.ComboBonusAmount}", AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                if (action.ComboBonusDuration > 0)
                {
                    canvas.AddText(menuStartX, menuStartY, $"  Combo Bonus Duration: {action.ComboBonusDuration}", AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                menuStartY += 1;
            }
            
            // Roll Bonuses (Crit Miss, Hit, Combo, Crit — threshold adjustments)
            bool hasRollBonusAdjustments = action.CriticalMissThresholdAdjustment != 0 || action.HitThresholdAdjustment != 0 ||
                action.ComboThresholdAdjustment != 0 || action.CriticalHitThresholdAdjustment != 0;
            if (hasRollBonusAdjustments)
            {
                canvas.AddText(menuStartX, menuStartY, "Roll Bonuses:", AsciiArtAssets.Colors.Cyan);
                menuStartY += 1;
                if (action.CriticalMissThresholdAdjustment != 0)
                    canvas.AddText(menuStartX, menuStartY, $"  Crit Miss: {action.CriticalMissThresholdAdjustment:+0;-0;0}", AsciiArtAssets.Colors.White);
                menuStartY += action.CriticalMissThresholdAdjustment != 0 ? 1 : 0;
                if (action.HitThresholdAdjustment != 0)
                    canvas.AddText(menuStartX, menuStartY, $"  Hit: {action.HitThresholdAdjustment:+0;-0;0}", AsciiArtAssets.Colors.White);
                menuStartY += action.HitThresholdAdjustment != 0 ? 1 : 0;
                if (action.ComboThresholdAdjustment != 0)
                    canvas.AddText(menuStartX, menuStartY, $"  Combo: {action.ComboThresholdAdjustment:+0;-0;0}", AsciiArtAssets.Colors.White);
                menuStartY += action.ComboThresholdAdjustment != 0 ? 1 : 0;
                if (action.CriticalHitThresholdAdjustment != 0)
                    canvas.AddText(menuStartX, menuStartY, $"  Crit: {action.CriticalHitThresholdAdjustment:+0;-0;0}", AsciiArtAssets.Colors.White);
                menuStartY += action.CriticalHitThresholdAdjustment != 0 ? 1 : 0;
                menuStartY += 1;
            }

            // Stat Bonuses (list or legacy single)
            var statBonusEntries = GetStatBonusEntries(action);
            if (statBonusEntries.Count > 0)
            {
                canvas.AddText(menuStartX, menuStartY, "Stat Bonuses:", AsciiArtAssets.Colors.Cyan);
                menuStartY += 1;
                string durationText = string.IsNullOrWhiteSpace(action.Cadence) ? "1 turn" : action.Cadence;
                foreach (var entry in statBonusEntries)
                {
                    if (entry.Value == 0 && string.IsNullOrEmpty(entry.Type)) continue;
                    canvas.AddText(menuStartX, menuStartY, $"  +{entry.Value} {entry.Type} ({durationText})", AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                menuStartY += 1;
            }

            action.NormalizeChainPositionBonuses();
            if (!string.IsNullOrWhiteSpace(action.ModifyBasedOnChainPosition) && action.ChainPositionBonuses != null && action.ChainPositionBonuses.Count > 0)
            {
                canvas.AddText(menuStartX, menuStartY, "Chain position bonuses (MOD on):", AsciiArtAssets.Colors.Cyan);
                menuStartY += 1;
                foreach (var c in action.ChainPositionBonuses)
                {
                    if (string.IsNullOrWhiteSpace(c.ModifiesParam)) continue;
                    var basis = string.IsNullOrWhiteSpace(c.PositionBasis) ? "ComboSlotIndex0" : c.PositionBasis;
                    var kind = string.IsNullOrWhiteSpace(c.ValueKind) ? "#" : c.ValueKind;
                    string paramLabel = ChainPositionBonusApplier.GetDisplayNameForModifiesParam(c.ModifiesParam);
                    canvas.AddText(menuStartX, menuStartY,
                        $"  {paramLabel} value={c.Value} kind={kind} basis={basis}",
                        AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                menuStartY += 1;
            }
            
            // Special Properties
            List<string> specialProperties = new List<string>();
            if (action.RollBonus != 0)
                specialProperties.Add($"Accuracy: {action.RollBonus}");
            if (action.MultiHitCount > 1)
                specialProperties.Add($"Multi-Hit Count: {action.MultiHitCount}");
            if (action.SkipNextTurn)
                specialProperties.Add("Skips Next Turn");
            if (action.RepeatLastAction)
                specialProperties.Add("Repeats Last Action");
            if (action.Thresholds != null && action.Thresholds.Count > 0)
                specialProperties.Add($"Thresholds: {string.Join(", ", action.Thresholds.Select(t => string.Equals(t.ValueKind, "%", StringComparison.OrdinalIgnoreCase) ? $"{t.Type} {t.Value:F0}%" : $"{t.Type} {t.Value:F0}"))}");
            else if (action.HealthThreshold > 0)
                specialProperties.Add($"Health Threshold: {action.HealthThreshold:P0}");
            
            if (specialProperties.Any())
            {
                canvas.AddText(menuStartX, menuStartY, "Special Properties:", AsciiArtAssets.Colors.Cyan);
                menuStartY += 1;
                
                foreach (var prop in specialProperties)
                {
                    canvas.AddText(menuStartX, menuStartY, $"  - {prop}", AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                menuStartY += 1;
            }
            
            // Tags
            if (action.Tags != null && action.Tags.Any())
            {
                canvas.AddText(menuStartX, menuStartY, "Tags:", AsciiArtAssets.Colors.Cyan);
                menuStartY += 1;
                
                string tagsString = string.Join(", ", action.Tags);
                canvas.AddText(menuStartX, menuStartY, $"  {tagsString}", AsciiArtAssets.Colors.White);
                menuStartY += 2;
            }
            
            menuStartY += 1;
            
            // Edit hint
            canvas.AddText(menuStartX, menuStartY, "Press 'E' to edit this action", AsciiArtAssets.Colors.Yellow);
            menuStartY += 2;
            
            // Navigation
            string backText = MenuOptionFormatter.Format(0, "Back to Action List");
            var backOption = new ClickableElement
            {
                X = menuStartX,
                Y = menuStartY,
                Width = backText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = backText
            };
            clickableElements.Add(backOption);
            canvas.AddText(menuStartX, menuStartY, backText, AsciiArtAssets.Colors.White);
            return currentLineCount;
        }

        private static List<StatBonusEntry> GetStatBonusEntries(ActionData action)
        {
            if (action == null) return new List<StatBonusEntry>();
            if (action.StatBonuses != null && action.StatBonuses.Count > 0)
                return action.StatBonuses;
            if (action.StatBonus != 0 || !string.IsNullOrEmpty(action.StatBonusType))
                return new List<StatBonusEntry> { new StatBonusEntry { Value = action.StatBonus, Type = action.StatBonusType ?? "" } };
            return new List<StatBonusEntry>();
        }
    }
}

