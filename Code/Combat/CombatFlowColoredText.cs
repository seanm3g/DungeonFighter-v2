using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using static RPGGame.Combat.Formatting.DamageFormatter;

namespace RPGGame
{
    /// <summary>
    /// Enhanced combat flow message formatting using the new ColoredText system
    /// Provides cleaner, more maintainable colored system messages
    /// 
    /// SPACING STANDARDIZATION:
    /// Uses ColoredTextBuilder which automatically handles spacing via CombatLogSpacingManager.
    /// See Documentation/05-Systems/COMBAT_LOG_SPACING_STANDARD.md for spacing guidelines.
    /// </summary>
    public static class CombatFlowColoredText
    {
        /// <summary>
        /// Formats health regeneration message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthRegenerationColored(
            string entityName, 
            int regenAmount, 
            int currentHealth, 
            int maxHealth)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(entityName, ColorPalette.Player);
            builder.Add("regenerates", ColorPalette.Healing);
            builder.AddSpace();
            builder.Add(regenAmount.ToString(), ColorPalette.Healing);
            builder.Add(" health (", Colors.White);
            builder.Add(currentHealth.ToString(), ColorPalette.Success);
            builder.Add("/", Colors.White);
            builder.Add(maxHealth.ToString(), Colors.Gray);
            builder.Add(")", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats combat system error message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatSystemErrorColored(string errorMessage)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("⚠️ ERROR: ", ColorPalette.Error);
            builder.Add(errorMessage, ColorPalette.Warning);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats combat start message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatCombatStartColored(
            string playerName, 
            string enemyName, 
            string locationName,
            string? environmentTheme = null)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("⚔️", ColorPalette.Warning);
            builder.Add("Combat", ColorPalette.Warning);
            builder.Add("begins:", ColorPalette.Warning);
            builder.Add(playerName, ColorPalette.Player);
            builder.Add("vs", Colors.White);
            builder.Add(enemyName, ColorPalette.Enemy);
            builder.Add("in", Colors.White);
            
            // Use environment color template if theme is provided, otherwise use default color
            if (!string.IsNullOrEmpty(environmentTheme))
            {
                string themeTemplate = environmentTheme.ToLower().Replace(" ", "");
                var environmentNameColored = ColorTemplateLibrary.GetTemplate(themeTemplate, locationName);
                builder.AddRange(environmentNameColored);
            }
            else
            {
                builder.Add(locationName, ColorPalette.Info);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats combat end message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatCombatEndColored(
            string playerName, 
            bool playerSurvived, 
            string enemyName)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("⚔️", ColorPalette.Info);
            builder.Add("Combat", ColorPalette.Info);
            builder.Add("ended:", ColorPalette.Info);
            builder.Add(playerName, ColorPalette.Player);
            
            if (playerSurvived)
            {
                builder.Add("survived", ColorPalette.Success);
            }
            else
            {
                builder.Add("died", ColorPalette.Error);
            }
            
            builder.Add("vs", Colors.White);
            builder.Add(enemyName, ColorPalette.Enemy);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats turn separator message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatTurnSeparatorColored(int turnNumber)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("─────────────", Colors.Gray);
            builder.Add($" Turn {turnNumber} ", ColorPalette.Info);
            builder.Add("─────────────", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats entity action header with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEntityActionHeaderColored(
            string entityName, 
            bool isPlayer, 
            bool isEnvironment = false,
            string? environmentTheme = null)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("▶ ", ColorPalette.Info);
            
            if (isEnvironment)
            {
                // Use environment color template if theme is provided, otherwise use default color
                if (!string.IsNullOrEmpty(environmentTheme))
                {
                    string themeTemplate = environmentTheme.ToLower().Replace(" ", "");
                    var environmentNameColored = ColorTemplateLibrary.GetTemplate(themeTemplate, entityName);
                    builder.AddRange(environmentNameColored);
                }
                else
                {
                    builder.Add(entityName, ColorPalette.Green);
                }
                builder.Add("'s turn", Colors.White);
            }
            else if (isPlayer)
            {
                builder.Add(entityName, ColorPalette.Player);
                builder.Add("'s turn", Colors.White);
            }
            else
            {
                builder.Add(entityName, ColorPalette.Enemy);
                builder.Add("'s turn", Colors.White);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats stun notification message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatStunNotificationColored(
            string entityName, 
            int turnsRemaining, 
            bool isPlayer)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("💫 ", ColorPalette.Warning);
            builder.Add(entityName, isPlayer ? ColorPalette.Player : ColorPalette.Enemy);
            builder.AddSpace();
            builder.Add("is", Colors.White);
            builder.AddSpace();
            builder.Add("stunned", ColorPalette.Warning);
            builder.Add($" ({turnsRemaining} turn{(turnsRemaining > 1 ? "s" : "")} remaining)", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats damage over time tick message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatDamageOverTimeColored(
            string entityName, 
            string effectName, 
            int damage, 
            bool isPlayer)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("🩸 ", ColorPalette.Error);
            builder.Add(entityName, isPlayer ? ColorPalette.Player : ColorPalette.Enemy);
            AddTakesDamageFrom(builder, damage, effectName, ColorPalette.Warning);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats battle summary statistics with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBattleSummaryColored(
            int totalPlayerDamage, 
            int totalEnemyDamage, 
            int playerComboCount, 
            int enemyComboCount)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("📊", ColorPalette.Info);
            builder.Add("Battle", ColorPalette.Info);
            builder.Add("Summary:", ColorPalette.Info);
            builder.Add("\n", Colors.White);
            builder.Add("Total", Colors.White);
            builder.Add("damage", Colors.White);
            builder.Add("dealt:", Colors.White);
            builder.Add(totalPlayerDamage.ToString(), ColorPalette.Player);
            builder.Add("vs", Colors.White);
            builder.Add(totalEnemyDamage.ToString(), ColorPalette.Enemy);
            builder.Add("received", Colors.White);
            builder.Add("\n", Colors.White);
            builder.Add("Combos", Colors.White);
            builder.Add("executed:", Colors.White);
            builder.Add(playerComboCount.ToString(), ColorPalette.Success);
            builder.Add("vs", Colors.White);
            builder.Add(enemyComboCount.ToString(), ColorPalette.Warning);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats action speed debug message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatActionSpeedDebugColored(
            string entityName, 
            double currentTime, 
            double nextReadyTime)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("⏱️ ", Colors.Gray);
            builder.Add(entityName, ColorPalette.Info);
            builder.Add($": current={currentTime:F2}s, next={nextReadyTime:F2}s", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats environmental action notification with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnvironmentalActionNotificationColored(
            string environmentName, 
            string actionName,
            string? environmentTheme = null)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("🌍 ", ColorPalette.Green);
            
            // Use environment color template if theme is provided, otherwise use default color
            if (!string.IsNullOrEmpty(environmentTheme))
            {
                string themeTemplate = environmentTheme.ToLower().Replace(" ", "");
                var environmentNameColored = ColorTemplateLibrary.GetTemplate(themeTemplate, environmentName);
                builder.AddRange(environmentNameColored);
            }
            else
            {
                builder.Add(environmentName, ColorPalette.Cyan);
            }
            
            AddUsesAction(builder, actionName, ColorPalette.Green);
            
            return builder.Build();
        }
    }
}

