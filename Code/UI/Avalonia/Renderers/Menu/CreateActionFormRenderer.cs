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
    /// Renders the create action form screen
    /// </summary>
    public class CreateActionFormRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        
        public CreateActionFormRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
        }
        
        /// <summary>
        /// Renders the create action form content
        /// </summary>
        public int RenderCreateActionFormContent(int x, int y, int width, int height, ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null, bool isEditMode = false)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Simple centered menu layout
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            
            // Title
            string title = isEditMode ? "=== EDIT ACTION ===" : "=== CREATE NEW ACTION ===";
            canvas.AddText(menuStartX, menuStartY, title, AsciiArtAssets.Colors.Gold);
            menuStartY += 2;
            
            // Progress indicator
            string progress = $"Step {currentStep + 1} of {formSteps.Length}";
            canvas.AddText(menuStartX, menuStartY, progress, AsciiArtAssets.Colors.Cyan);
            menuStartY += 2;
            
            // Current field
            string currentField = formSteps[currentStep];
            canvas.AddText(menuStartX, menuStartY, $"Enter {currentField}:", AsciiArtAssets.Colors.White);
            menuStartY += 1;
            
            // Show current input or value
            if (!string.IsNullOrEmpty(currentInput))
            {
                canvas.AddText(menuStartX, menuStartY, $"> {currentInput}_", AsciiArtAssets.Colors.Yellow);
                menuStartY += 2;
            }
            else
            {
                string currentValue = GetCurrentValue(actionData, currentStep);
                if (!string.IsNullOrEmpty(currentValue))
                {
                    canvas.AddText(menuStartX, menuStartY, $"Current: {currentValue}", AsciiArtAssets.Colors.Gray);
                    menuStartY += 1;
                }
                canvas.AddText(menuStartX, menuStartY, "> _", AsciiArtAssets.Colors.Yellow);
                menuStartY += 2;
            }
            
            // Show preview of action so far (scrollable if needed)
            menuStartY += 1;
            canvas.AddText(menuStartX, menuStartY, "Action Preview:", AsciiArtAssets.Colors.Yellow);
            menuStartY += 1;
            
            // Basic Properties
            canvas.AddText(menuStartX, menuStartY, $"  Name: {actionData.Name}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  MultiHitCount: {actionData.MultiHitCount}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Damage Multiplier: {actionData.DamageMultiplier}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Speed: {actionData.Length}", AsciiArtAssets.Colors.White);
            menuStartY++;
            
            // Status Effects (only show if any are true)
            bool hasStatusEffects = actionData.CausesBleed || actionData.CausesWeaken || actionData.CausesSlow || 
                                   actionData.CausesPoison || actionData.CausesBurn;
            if (hasStatusEffects)
            {
                menuStartY++;
                canvas.AddText(menuStartX, menuStartY, "  Status Effects:", AsciiArtAssets.Colors.Cyan);
                menuStartY++;
                if (actionData.CausesBleed) { canvas.AddText(menuStartX, menuStartY, "    - Bleed", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.CausesWeaken) { canvas.AddText(menuStartX, menuStartY, "    - Weaken", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.CausesSlow) { canvas.AddText(menuStartX, menuStartY, "    - Slow", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.CausesPoison) { canvas.AddText(menuStartX, menuStartY, "    - Poison", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.CausesBurn) { canvas.AddText(menuStartX, menuStartY, "    - Burn", AsciiArtAssets.Colors.White); menuStartY++; }
            }
            
            // Combo Properties (only show if combo action)
            if (actionData.IsComboAction)
            {
                menuStartY++;
                canvas.AddText(menuStartX, menuStartY, "  Combo: Order=" + actionData.ComboOrder + 
                    (actionData.ComboBonusDuration > 0 ? $", Duration={actionData.ComboBonusDuration}" : ""), 
                    AsciiArtAssets.Colors.Cyan);
                menuStartY++;
            }
            
            // Advanced Mechanics (only show if any are set)
            bool hasStatBonuses = (actionData.StatBonuses != null && actionData.StatBonuses.Count > 0) ||
                (actionData.StatBonus != 0 && !string.IsNullOrEmpty(actionData.StatBonusType));
            bool hasRollBonusAdjustments = actionData.CriticalMissThresholdAdjustment != 0 || actionData.HitThresholdAdjustment != 0 ||
                actionData.ComboThresholdAdjustment != 0 || actionData.CriticalHitThresholdAdjustment != 0;
            bool hasThresholds = (actionData.Thresholds != null && actionData.Thresholds.Count > 0) || actionData.HealthThreshold > 0;
            bool hasAccumulations = actionData.Accumulations != null && actionData.Accumulations.Count > 0;
            bool hasAdvanced = hasRollBonusAdjustments || actionData.RollBonus != 0 || hasStatBonuses || actionData.MultiHitCount > 1 ||
                              actionData.SkipNextTurn || actionData.RepeatLastAction || hasThresholds || hasAccumulations;
            if (hasAdvanced)
            {
                menuStartY++;
                canvas.AddText(menuStartX, menuStartY, "  Advanced:", AsciiArtAssets.Colors.Cyan);
                menuStartY++;
                if (hasRollBonusAdjustments)
                {
                    if (actionData.CriticalMissThresholdAdjustment != 0) { canvas.AddText(menuStartX, menuStartY, $"    - Roll bonus Crit Miss: {actionData.CriticalMissThresholdAdjustment:+0;-0;0}", AsciiArtAssets.Colors.White); menuStartY++; }
                    if (actionData.HitThresholdAdjustment != 0) { canvas.AddText(menuStartX, menuStartY, $"    - Roll bonus Hit: {actionData.HitThresholdAdjustment:+0;-0;0}", AsciiArtAssets.Colors.White); menuStartY++; }
                    if (actionData.ComboThresholdAdjustment != 0) { canvas.AddText(menuStartX, menuStartY, $"    - Roll bonus Combo: {actionData.ComboThresholdAdjustment:+0;-0;0}", AsciiArtAssets.Colors.White); menuStartY++; }
                    if (actionData.CriticalHitThresholdAdjustment != 0) { canvas.AddText(menuStartX, menuStartY, $"    - Roll bonus Crit: {actionData.CriticalHitThresholdAdjustment:+0;-0;0}", AsciiArtAssets.Colors.White); menuStartY++; }
                }
                if (actionData.RollBonus != 0) { canvas.AddText(menuStartX, menuStartY, $"    - Accuracy: {actionData.RollBonus:+0;-0;0}", AsciiArtAssets.Colors.White); menuStartY++; }
                var statEntries = GetStatBonusEntries(actionData);
                if (statEntries.Count > 0)
                {
                    string durationText = string.IsNullOrWhiteSpace(actionData.Cadence) ? "1 turn" : actionData.Cadence;
                    foreach (var entry in statEntries)
                    {
                        if (entry.Value == 0 && string.IsNullOrEmpty(entry.Type)) continue;
                        canvas.AddText(menuStartX, menuStartY, $"    - Stat Bonus: +{entry.Value} {entry.Type} ({durationText})", AsciiArtAssets.Colors.White);
                        menuStartY++;
                    }
                }
                if (actionData.MultiHitCount > 1) { canvas.AddText(menuStartX, menuStartY, $"    - Multi-Hit: {actionData.MultiHitCount} hits", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.SkipNextTurn) { canvas.AddText(menuStartX, menuStartY, "    - Skips Next Turn", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.RepeatLastAction) { canvas.AddText(menuStartX, menuStartY, "    - Repeats Last Action", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.Thresholds != null && actionData.Thresholds.Count > 0)
                {
                    var thresholdParts = actionData.Thresholds.Select(t =>
                    {
                        string q = string.IsNullOrEmpty(t.Qualifier) ? "" : t.Qualifier + " ";
                        string op = string.IsNullOrEmpty(t.Operator) ? "" : " " + t.Operator + " ";
                        string val = string.Equals(t.ValueKind, "%", StringComparison.OrdinalIgnoreCase) ? $"{t.Value:F2}%" : $"{t.Value:F2}";
                        return $"{q}{t.Type}{op}{val}";
                    });
                    canvas.AddText(menuStartX, menuStartY, $"    - Thresholds: {string.Join(", ", thresholdParts)}", AsciiArtAssets.Colors.White);
                    menuStartY++;
                }
                else if (actionData.HealthThreshold > 0)
                    { canvas.AddText(menuStartX, menuStartY, $"    - Health Threshold: {actionData.HealthThreshold:P0}", AsciiArtAssets.Colors.White); menuStartY++; }
                if (hasAccumulations && actionData.Accumulations != null)
                {
                    var accParts = actionData.Accumulations
                        .Where(a => !string.IsNullOrEmpty(a.Type))
                        .Select(a =>
                        {
                            var param = string.IsNullOrEmpty(a.ModifiesParam) ? "Damage" : a.ModifiesParam;
                            var kind = string.IsNullOrEmpty(a.ValueKind) ? "#" : a.ValueKind;
                            var val = kind == "%" ? $"{a.Value}%" : a.Value.ToString("F0");
                            var sign = a.Value >= 0 ? "+" : "";
                            return $"{sign}{val} {param} per {a.Type}";
                        })
                        .ToList();
                    if (accParts.Count > 0)
                    {
                        canvas.AddText(menuStartX, menuStartY, $"    - Accumulations: {string.Join("; ", accParts)}", AsciiArtAssets.Colors.White);
                        menuStartY++;
                    }
                }
            }
            
            // Tags (only show if tags exist)
            if (actionData.Tags != null && actionData.Tags.Count > 0)
            {
                menuStartY++;
                canvas.AddText(menuStartX, menuStartY, $"  Tags: {string.Join(", ", actionData.Tags)}", AsciiArtAssets.Colors.Cyan);
                menuStartY++;
            }
            
            // Description (always show)
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Description: {(string.IsNullOrEmpty(actionData.Description) ? "(not set)" : actionData.Description)}", AsciiArtAssets.Colors.Gray);
            
            menuStartY += 3;
            
            // Instructions
            canvas.AddText(menuStartX, menuStartY, "Type your input and press Enter", AsciiArtAssets.Colors.Gray);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, "Type 'back' to go to previous step", AsciiArtAssets.Colors.Gray);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, "Type 'cancel' or '0' to cancel", AsciiArtAssets.Colors.Gray);
            return currentLineCount;
        }

        private string GetCurrentValue(ActionData actionData, int step)
        {
            return step switch
            {
                0 => actionData.Name,
                1 => actionData.Description,
                2 => actionData.DamageMultiplier.ToString(),
                3 => actionData.Length.ToString(),
                4 => actionData.CausesBleed.ToString(),
                5 => actionData.CausesWeaken.ToString(),
                6 => actionData.CausesSlow.ToString(),
                7 => actionData.CausesPoison.ToString(),
                8 => actionData.CausesBurn.ToString(),
                9 => "false", // CausesStun - not in ActionData
                10 => actionData.IsComboAction.ToString(),
                11 => actionData.ComboOrder.ToString(),
                12 => actionData.ComboBonusDuration.ToString(),
                13 => actionData.SkipNextTurn.ToString(),
                14 => actionData.CriticalMissThresholdAdjustment.ToString(),
                15 => actionData.HitThresholdAdjustment.ToString(),
                16 => actionData.ComboThresholdAdjustment.ToString(),
                17 => actionData.CriticalHitThresholdAdjustment.ToString(),
                18 => actionData.StatBonus.ToString(),
                19 => actionData.StatBonusType,
                20 => actionData.MultiHitCount.ToString(),
                21 => actionData.RepeatLastAction.ToString(),
                22 => (actionData.Thresholds != null && actionData.Thresholds.Count > 0)
                    ? string.Join(", ", actionData.Thresholds.Select(t =>
                    {
                        string q = string.IsNullOrEmpty(t.Qualifier) ? "" : t.Qualifier + " ";
                        string op = string.IsNullOrEmpty(t.Operator) ? "" : " " + t.Operator + " ";
                        string val = string.Equals(t.ValueKind, "%", StringComparison.OrdinalIgnoreCase) ? $"{t.Value:F2}%" : $"{t.Value:F2}";
                        return $"{q}{t.Type}{op}{val}";
                    }))
                    : actionData.HealthThreshold.ToString("F2"),
                23 => actionData.Tags != null ? string.Join(", ", actionData.Tags) : "",
                _ => ""
            };
        }

        private static List<StatBonusEntry> GetStatBonusEntries(ActionData actionData)
        {
            if (actionData == null) return new List<StatBonusEntry>();
            if (actionData.StatBonuses != null && actionData.StatBonuses.Count > 0)
                return actionData.StatBonuses;
            if (actionData.StatBonus != 0 || !string.IsNullOrEmpty(actionData.StatBonusType))
                return new List<StatBonusEntry> { new StatBonusEntry { Value = actionData.StatBonus, Type = actionData.StatBonusType ?? "" } };
            return new List<StatBonusEntry>();
        }
    }
}

