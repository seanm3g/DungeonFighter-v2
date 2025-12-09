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
            ScrollDebugLogger.Log($"ActionDetailRenderer: RenderActionDetailContent called for action '{action.Name}'");
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
            
            canvas.AddText(menuStartX, menuStartY, $"  Type: {action.Type}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Target Type: {action.TargetType}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Description: {action.Description}", AsciiArtAssets.Colors.White);
            menuStartY += 2;
            
            // Combat Stats
            canvas.AddText(menuStartX, menuStartY, "Combat Stats:", AsciiArtAssets.Colors.Cyan);
            menuStartY += 1;
            
            canvas.AddText(menuStartX, menuStartY, $"  Base Value: {action.BaseValue}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Damage Multiplier: {action.DamageMultiplier}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Range: {action.Range}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Cooldown: {action.Cooldown}", AsciiArtAssets.Colors.White);
            menuStartY++;
            canvas.AddText(menuStartX, menuStartY, $"  Length: {action.Length}", AsciiArtAssets.Colors.White);
            menuStartY += 2;
            
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
                
                canvas.AddText(menuStartX, menuStartY, $"  Is Combo Action: {action.IsComboAction}", AsciiArtAssets.Colors.White);
                menuStartY++;
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
            
            // Stat Bonuses
            if (action.StatBonus > 0 && !string.IsNullOrEmpty(action.StatBonusType))
            {
                canvas.AddText(menuStartX, menuStartY, "Stat Bonuses:", AsciiArtAssets.Colors.Cyan);
                menuStartY += 1;
                
                canvas.AddText(menuStartX, menuStartY, $"  Stat Bonus Type: {action.StatBonusType}", AsciiArtAssets.Colors.White);
                menuStartY++;
                canvas.AddText(menuStartX, menuStartY, $"  Stat Bonus: {action.StatBonus}", AsciiArtAssets.Colors.White);
                menuStartY++;
                canvas.AddText(menuStartX, menuStartY, $"  Stat Bonus Duration: {action.StatBonusDuration}", AsciiArtAssets.Colors.White);
                menuStartY += 2;
            }
            
            // Special Properties
            List<string> specialProperties = new List<string>();
            if (action.RollBonus != 0)
                specialProperties.Add($"Roll Bonus: {action.RollBonus}");
            if (action.MultiHitCount > 1)
                specialProperties.Add($"Multi-Hit Count: {action.MultiHitCount}");
            if (action.SelfDamagePercent > 0)
                specialProperties.Add($"Self Damage: {action.SelfDamagePercent}%");
            if (action.SkipNextTurn)
                specialProperties.Add("Skips Next Turn");
            if (action.RepeatLastAction)
                specialProperties.Add("Repeats Last Action");
            if (action.EnemyRollPenalty != 0)
                specialProperties.Add($"Enemy Roll Penalty: {action.EnemyRollPenalty}");
            
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
            
            ScrollDebugLogger.Log($"ActionDetailRenderer: Finished rendering action '{action.Name}'");
            
            return currentLineCount;
        }
    }
}

