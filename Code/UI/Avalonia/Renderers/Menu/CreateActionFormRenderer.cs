using System;
using System.Collections.Generic;
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
            canvas.AddText(menuStartX, menuStartY, $"  Type: {actionData.Type}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Target: {actionData.TargetType}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Base Value: {actionData.BaseValue}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Range: {actionData.Range}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Damage Multiplier: {actionData.DamageMultiplier}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Length: {actionData.Length}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Cooldown: {actionData.Cooldown}", AsciiArtAssets.Colors.White);
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
                    (actionData.ComboBonusAmount > 0 ? $", Bonus=+{actionData.ComboBonusAmount} for {actionData.ComboBonusDuration} turns" : ""), 
                    AsciiArtAssets.Colors.Cyan);
                menuStartY++;
            }
            
            // Advanced Mechanics (only show if any are set)
            bool hasAdvanced = actionData.RollBonus != 0 || actionData.StatBonus != 0 || actionData.MultiHitCount > 1 ||
                              actionData.SelfDamagePercent > 0 || actionData.SkipNextTurn || actionData.RepeatLastAction ||
                              actionData.EnemyRollPenalty != 0 || actionData.HealthThreshold > 0 || 
                              actionData.ConditionalDamageMultiplier != 1.0;
            if (hasAdvanced)
            {
                menuStartY++;
                canvas.AddText(menuStartX, menuStartY, "  Advanced:", AsciiArtAssets.Colors.Cyan);
                menuStartY++;
                if (actionData.RollBonus != 0) { canvas.AddText(menuStartX, menuStartY, $"    - Roll Bonus: {actionData.RollBonus:+0;-0;0}", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.StatBonus != 0 && !string.IsNullOrEmpty(actionData.StatBonusType)) 
                { 
                    canvas.AddText(menuStartX, menuStartY, $"    - Stat Bonus: +{actionData.StatBonus} {actionData.StatBonusType} ({actionData.StatBonusDuration} turns)", AsciiArtAssets.Colors.White); 
                    menuStartY++; 
                }
                if (actionData.MultiHitCount > 1) { canvas.AddText(menuStartX, menuStartY, $"    - Multi-Hit: {actionData.MultiHitCount} hits", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.SelfDamagePercent > 0) { canvas.AddText(menuStartX, menuStartY, $"    - Self Damage: {actionData.SelfDamagePercent}%", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.SkipNextTurn) { canvas.AddText(menuStartX, menuStartY, "    - Skips Next Turn", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.RepeatLastAction) { canvas.AddText(menuStartX, menuStartY, "    - Repeats Last Action", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.EnemyRollPenalty != 0) { canvas.AddText(menuStartX, menuStartY, $"    - Enemy Roll Penalty: {actionData.EnemyRollPenalty}", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.HealthThreshold > 0) { canvas.AddText(menuStartX, menuStartY, $"    - Health Threshold: {actionData.HealthThreshold:P0}", AsciiArtAssets.Colors.White); menuStartY++; }
                if (actionData.ConditionalDamageMultiplier != 1.0) { canvas.AddText(menuStartX, menuStartY, $"    - Conditional Damage: {actionData.ConditionalDamageMultiplier}x", AsciiArtAssets.Colors.White); menuStartY++; }
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
                1 => actionData.Type,
                2 => actionData.TargetType,
                3 => actionData.BaseValue.ToString(),
                4 => actionData.Range.ToString(),
                5 => actionData.Description,
                6 => actionData.DamageMultiplier.ToString(),
                7 => actionData.Length.ToString(),
                8 => actionData.Cooldown.ToString(),
                9 => actionData.CausesBleed.ToString(),
                10 => actionData.CausesWeaken.ToString(),
                11 => actionData.CausesSlow.ToString(),
                12 => actionData.CausesPoison.ToString(),
                13 => actionData.CausesBurn.ToString(),
                14 => "false", // CausesStun - not in ActionData
                15 => actionData.IsComboAction.ToString(),
                16 => actionData.ComboOrder.ToString(),
                17 => actionData.ComboBonusAmount.ToString(),
                18 => actionData.ComboBonusDuration.ToString(),
                19 => actionData.RollBonus.ToString(),
                20 => actionData.StatBonus.ToString(),
                21 => actionData.StatBonusType,
                22 => actionData.StatBonusDuration.ToString(),
                23 => actionData.MultiHitCount.ToString(),
                24 => actionData.SelfDamagePercent.ToString(),
                25 => actionData.SkipNextTurn.ToString(),
                26 => actionData.RepeatLastAction.ToString(),
                27 => actionData.EnemyRollPenalty.ToString(),
                28 => actionData.HealthThreshold.ToString("F2"),
                29 => actionData.ConditionalDamageMultiplier.ToString("F2"),
                30 => actionData.Tags != null ? string.Join(", ", actionData.Tags) : "",
                _ => ""
            };
        }
    }
}

