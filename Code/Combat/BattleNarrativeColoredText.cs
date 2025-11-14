using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Enhanced battle narrative formatting using the new ColoredText system
    /// Provides cleaner, more maintainable colored narrative messages
    /// </summary>
    public static class BattleNarrativeColoredText
    {
        /// <summary>
        /// Formats first blood narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatFirstBloodColored(string narrativeText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("⚔️ ", ColorPalette.Warning);
            builder.Add(narrativeText, ColorPalette.Critical);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats critical hit narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatCriticalHitColored(string actorName, string narrativeText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("💥 ", ColorPalette.Critical);
            
            // Split narrative text to highlight the actor's name
            string text = narrativeText.Replace("{name}", actorName);
            
            // If the actor name is in the text, color it differently
            if (text.Contains(actorName))
            {
                int startIndex = text.IndexOf(actorName);
                builder.Add(text.Substring(0, startIndex), ColorPalette.Warning);
                builder.Add(actorName, ColorPalette.Critical);
                builder.Add(text.Substring(startIndex + actorName.Length), ColorPalette.Warning);
            }
            else
            {
                builder.Add(text, ColorPalette.Warning);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats critical miss narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatCriticalMissColored(string actorName, string narrativeText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("❌ ", ColorPalette.Miss);
            
            // Split narrative text to highlight the actor's name
            string text = narrativeText.Replace("{name}", actorName);
            
            // If the actor name is in the text, color it differently
            if (text.Contains(actorName))
            {
                int startIndex = text.IndexOf(actorName);
                builder.Add(text.Substring(0, startIndex), ColorPalette.Gray);
                builder.Add(actorName, ColorPalette.Miss);
                builder.Add(text.Substring(startIndex + actorName.Length), ColorPalette.Gray);
            }
            else
            {
                builder.Add(text, ColorPalette.Gray);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats environmental action narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnvironmentalActionColored(string effectDescription, string narrativeText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("🌍 ", ColorPalette.Green);
            
            string text = narrativeText.Replace("{effect}", effectDescription);
            
            // Highlight the effect description
            if (text.Contains(effectDescription))
            {
                int startIndex = text.IndexOf(effectDescription);
                builder.Add(text.Substring(0, startIndex), ColorPalette.Cyan);
                builder.Add(effectDescription, ColorPalette.Green);
                builder.Add(text.Substring(startIndex + effectDescription.Length), ColorPalette.Cyan);
            }
            else
            {
                builder.Add(text, ColorPalette.Cyan);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats health recovery narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthRecoveryColored(string targetName, string narrativeText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("💚 ", ColorPalette.Healing);
            
            string text = narrativeText.Replace("{name}", targetName);
            
            // Highlight the target's name
            if (text.Contains(targetName))
            {
                int startIndex = text.IndexOf(targetName);
                builder.Add(text.Substring(0, startIndex), ColorPalette.Success);
                builder.Add(targetName, ColorPalette.Healing);
                builder.Add(text.Substring(startIndex + targetName.Length), ColorPalette.Success);
            }
            else
            {
                builder.Add(text, ColorPalette.Success);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats health lead change narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthLeadChangeColored(string leaderName, string narrativeText, bool isPlayer)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("⚡ ", ColorPalette.Warning);
            
            string text = narrativeText.Replace("{name}", leaderName);
            
            // Use different colors for player vs enemy
            ColorPalette entityColor = isPlayer ? ColorPalette.Player : ColorPalette.Enemy;
            
            // Highlight the leader's name
            if (text.Contains(leaderName))
            {
                int startIndex = text.IndexOf(leaderName);
                builder.Add(text.Substring(0, startIndex), ColorPalette.Yellow);
                builder.Add(leaderName, entityColor);
                builder.Add(text.Substring(startIndex + leaderName.Length), ColorPalette.Yellow);
            }
            else
            {
                builder.Add(text, ColorPalette.Yellow);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats below 50% health narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBelow50PercentColored(string entityName, string narrativeText, bool isPlayer)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("⚠️ ", ColorPalette.Warning);
            
            string text = narrativeText.Replace("{name}", entityName);
            
            // Use different colors for player vs enemy
            ColorPalette entityColor = isPlayer ? ColorPalette.Player : ColorPalette.Enemy;
            
            // Highlight the entity's name
            if (text.Contains(entityName))
            {
                int startIndex = text.IndexOf(entityName);
                builder.Add(text.Substring(0, startIndex), ColorPalette.Orange);
                builder.Add(entityName, entityColor);
                builder.Add(text.Substring(startIndex + entityName.Length), ColorPalette.Orange);
            }
            else
            {
                builder.Add(text, ColorPalette.Orange);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats below 10% health narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBelow10PercentColored(string entityName, string narrativeText, bool isPlayer)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("💀 ", ColorPalette.Critical);
            
            string text = narrativeText.Replace("{name}", entityName);
            
            // Use different colors for player vs enemy
            ColorPalette entityColor = isPlayer ? ColorPalette.Player : ColorPalette.Enemy;
            
            // Highlight the entity's name
            if (text.Contains(entityName))
            {
                int startIndex = text.IndexOf(entityName);
                builder.Add(text.Substring(0, startIndex), ColorPalette.Error);
                builder.Add(entityName, entityColor);
                builder.Add(text.Substring(startIndex + entityName.Length), ColorPalette.Error);
            }
            else
            {
                builder.Add(text, ColorPalette.Error);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats intense battle narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatIntenseBattleColored(string playerName, string enemyName, string narrativeText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("🔥 ", ColorPalette.Critical);
            
            string text = narrativeText.Replace("{player}", playerName).Replace("{enemy}", enemyName);
            
            // Highlight both player and enemy names
            int playerIndex = text.IndexOf(playerName);
            int enemyIndex = text.IndexOf(enemyName);
            
            if (playerIndex >= 0 && enemyIndex >= 0)
            {
                if (playerIndex < enemyIndex)
                {
                    builder.Add(text.Substring(0, playerIndex), ColorPalette.Warning);
                    builder.Add(playerName, ColorPalette.Player);
                    builder.Add(text.Substring(playerIndex + playerName.Length, enemyIndex - playerIndex - playerName.Length), ColorPalette.Warning);
                    builder.Add(enemyName, ColorPalette.Enemy);
                    builder.Add(text.Substring(enemyIndex + enemyName.Length), ColorPalette.Warning);
                }
                else
                {
                    builder.Add(text.Substring(0, enemyIndex), ColorPalette.Warning);
                    builder.Add(enemyName, ColorPalette.Enemy);
                    builder.Add(text.Substring(enemyIndex + enemyName.Length, playerIndex - enemyIndex - enemyName.Length), ColorPalette.Warning);
                    builder.Add(playerName, ColorPalette.Player);
                    builder.Add(text.Substring(playerIndex + playerName.Length), ColorPalette.Warning);
                }
            }
            else
            {
                builder.Add(text, ColorPalette.Warning);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats good combo narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatGoodComboColored(string actorName, string targetName, bool isPlayerCombo)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("🎯 ", ColorPalette.Critical);
            
            if (isPlayerCombo)
            {
                builder.Add(actorName, ColorPalette.Player);
                builder.Add(" unleashes a ", ColorPalette.Success);
                builder.Add("devastating combo sequence", ColorPalette.Critical);
                builder.Add("! Each strike flows into the next with deadly precision!", ColorPalette.Success);
            }
            else
            {
                builder.Add(actorName, ColorPalette.Enemy);
                builder.Add(" demonstrates ", ColorPalette.Warning);
                builder.Add("masterful technique", ColorPalette.Critical);
                builder.Add(" with a brutal combo that leaves ", ColorPalette.Warning);
                builder.Add(targetName, ColorPalette.Player);
                builder.Add(" reeling!", ColorPalette.Warning);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats player defeated narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatPlayerDefeatedColored(string enemyName, string narrativeText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("☠️ ", ColorPalette.Critical);
            
            string text = narrativeText.Replace("{enemy}", enemyName);
            
            // Highlight the enemy's name
            if (text.Contains(enemyName))
            {
                int startIndex = text.IndexOf(enemyName);
                builder.Add(text.Substring(0, startIndex), ColorPalette.Error);
                builder.Add(enemyName, ColorPalette.Enemy);
                builder.Add(text.Substring(startIndex + enemyName.Length), ColorPalette.Error);
            }
            else
            {
                builder.Add(text, ColorPalette.Error);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats enemy defeated narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnemyDefeatedColored(string enemyName, string playerName, string narrativeText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("✨ ", ColorPalette.Success);
            
            string text = narrativeText.Replace("{name}", enemyName).Replace("{player}", playerName);
            
            // Highlight both enemy and player names
            int enemyIndex = text.IndexOf(enemyName);
            int playerIndex = text.IndexOf(playerName);
            
            if (enemyIndex >= 0 && playerIndex >= 0)
            {
                if (enemyIndex < playerIndex)
                {
                    builder.Add(text.Substring(0, enemyIndex), ColorPalette.Success);
                    builder.Add(enemyName, ColorPalette.Enemy);
                    builder.Add(text.Substring(enemyIndex + enemyName.Length, playerIndex - enemyIndex - enemyName.Length), ColorPalette.Success);
                    builder.Add(playerName, ColorPalette.Player);
                    builder.Add(text.Substring(playerIndex + playerName.Length), ColorPalette.Success);
                }
                else
                {
                    builder.Add(text.Substring(0, playerIndex), ColorPalette.Success);
                    builder.Add(playerName, ColorPalette.Player);
                    builder.Add(text.Substring(playerIndex + playerName.Length, enemyIndex - playerIndex - playerName.Length), ColorPalette.Success);
                    builder.Add(enemyName, ColorPalette.Enemy);
                    builder.Add(text.Substring(enemyIndex + enemyName.Length), ColorPalette.Success);
                }
            }
            else if (enemyIndex >= 0)
            {
                builder.Add(text.Substring(0, enemyIndex), ColorPalette.Success);
                builder.Add(enemyName, ColorPalette.Enemy);
                builder.Add(text.Substring(enemyIndex + enemyName.Length), ColorPalette.Success);
            }
            else
            {
                builder.Add(text, ColorPalette.Success);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats player taunt narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatPlayerTauntColored(string playerName, string enemyName, string tauntText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("💬 ", ColorPalette.Info);
            
            // Replace placeholders
            string text = tauntText.Replace("{name}", playerName).Replace("{enemy}", enemyName);
            
            // Check if it's a quote (starts with ")
            if (text.StartsWith("\""))
            {
                int quoteEnd = text.IndexOf("\"", 1);
                if (quoteEnd > 0)
                {
                    // Quote part
                    builder.Add(text.Substring(0, quoteEnd + 1), ColorPalette.Cyan);
                    
                    // Remaining text
                    string remaining = text.Substring(quoteEnd + 1);
                    
                    // Highlight player name in remaining text
                    if (remaining.Contains(playerName))
                    {
                        int nameIndex = remaining.IndexOf(playerName);
                        builder.Add(remaining.Substring(0, nameIndex), ColorPalette.Info);
                        builder.Add(playerName, ColorPalette.Player);
                        builder.Add(remaining.Substring(nameIndex + playerName.Length), ColorPalette.Info);
                    }
                    else
                    {
                        builder.Add(remaining, ColorPalette.Info);
                    }
                }
                else
                {
                    builder.Add(text, ColorPalette.Cyan);
                }
            }
            else
            {
                // No quote, highlight names
                int playerIndex = text.IndexOf(playerName);
                int enemyIndex = text.IndexOf(enemyName);
                
                if (playerIndex >= 0 && enemyIndex >= 0)
                {
                    if (playerIndex < enemyIndex)
                    {
                        builder.Add(text.Substring(0, playerIndex), ColorPalette.Info);
                        builder.Add(playerName, ColorPalette.Player);
                        builder.Add(text.Substring(playerIndex + playerName.Length, enemyIndex - playerIndex - playerName.Length), ColorPalette.Info);
                        builder.Add(enemyName, ColorPalette.Enemy);
                        builder.Add(text.Substring(enemyIndex + enemyName.Length), ColorPalette.Info);
                    }
                    else
                    {
                        builder.Add(text.Substring(0, enemyIndex), ColorPalette.Info);
                        builder.Add(enemyName, ColorPalette.Enemy);
                        builder.Add(text.Substring(enemyIndex + enemyName.Length, playerIndex - enemyIndex - enemyName.Length), ColorPalette.Info);
                        builder.Add(playerName, ColorPalette.Player);
                        builder.Add(text.Substring(playerIndex + playerName.Length), ColorPalette.Info);
                    }
                }
                else if (playerIndex >= 0)
                {
                    builder.Add(text.Substring(0, playerIndex), ColorPalette.Info);
                    builder.Add(playerName, ColorPalette.Player);
                    builder.Add(text.Substring(playerIndex + playerName.Length), ColorPalette.Info);
                }
                else
                {
                    builder.Add(text, ColorPalette.Info);
                }
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats enemy taunt narrative with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatEnemyTauntColored(string enemyName, string playerName, string tauntText)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("💬 ", ColorPalette.Warning);
            
            // Replace placeholders
            string text = tauntText.Replace("{name}", enemyName).Replace("{player}", playerName);
            
            // Check if it's a quote (starts with ")
            if (text.StartsWith("\""))
            {
                int quoteEnd = text.IndexOf("\"", 1);
                if (quoteEnd > 0)
                {
                    // Quote part
                    builder.Add(text.Substring(0, quoteEnd + 1), ColorPalette.Orange);
                    
                    // Remaining text
                    string remaining = text.Substring(quoteEnd + 1);
                    
                    // Highlight enemy name in remaining text
                    if (remaining.Contains(enemyName))
                    {
                        int nameIndex = remaining.IndexOf(enemyName);
                        builder.Add(remaining.Substring(0, nameIndex), ColorPalette.Warning);
                        builder.Add(enemyName, ColorPalette.Enemy);
                        builder.Add(remaining.Substring(nameIndex + enemyName.Length), ColorPalette.Warning);
                    }
                    else
                    {
                        builder.Add(remaining, ColorPalette.Warning);
                    }
                }
                else
                {
                    builder.Add(text, ColorPalette.Orange);
                }
            }
            else
            {
                // No quote, highlight names
                int playerIndex = text.IndexOf(playerName);
                int enemyIndex = text.IndexOf(enemyName);
                
                if (playerIndex >= 0 && enemyIndex >= 0)
                {
                    if (enemyIndex < playerIndex)
                    {
                        builder.Add(text.Substring(0, enemyIndex), ColorPalette.Warning);
                        builder.Add(enemyName, ColorPalette.Enemy);
                        builder.Add(text.Substring(enemyIndex + enemyName.Length, playerIndex - enemyIndex - enemyName.Length), ColorPalette.Warning);
                        builder.Add(playerName, ColorPalette.Player);
                        builder.Add(text.Substring(playerIndex + playerName.Length), ColorPalette.Warning);
                    }
                    else
                    {
                        builder.Add(text.Substring(0, playerIndex), ColorPalette.Warning);
                        builder.Add(playerName, ColorPalette.Player);
                        builder.Add(text.Substring(playerIndex + playerName.Length, enemyIndex - playerIndex - playerName.Length), ColorPalette.Warning);
                        builder.Add(enemyName, ColorPalette.Enemy);
                        builder.Add(text.Substring(enemyIndex + enemyName.Length), ColorPalette.Warning);
                    }
                }
                else if (enemyIndex >= 0)
                {
                    builder.Add(text.Substring(0, enemyIndex), ColorPalette.Warning);
                    builder.Add(enemyName, ColorPalette.Enemy);
                    builder.Add(text.Substring(enemyIndex + enemyName.Length), ColorPalette.Warning);
                }
                else
                {
                    builder.Add(text, ColorPalette.Warning);
                }
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a generic narrative message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatGenericNarrativeColored(string narrativeText, ColorPalette primaryColor = ColorPalette.Info)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("📖 ", primaryColor);
            builder.Add(narrativeText, primaryColor);
            
            return builder.Build();
        }
    }
}


