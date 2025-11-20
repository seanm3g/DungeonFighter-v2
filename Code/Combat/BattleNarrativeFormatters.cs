using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Specialized formatters for different types of battle narratives.
    /// Each formatter handles a specific narrative pattern, reducing code duplication.
    /// </summary>

    // ============================================================================
    // Single Entity Formatters
    // ============================================================================

    public static class FirstBloodFormatter
    {
        public static List<ColoredText> Format(string narrativeText)
        {
            return new NarrativeTextBuilder()
                .AddEmoji("⚔️ ", ColorPalette.Warning)
                .AddText(narrativeText, ColorPalette.Critical)
                .Build();
        }
    }

    public static class CriticalHitFormatter
    {
        public static List<ColoredText> Format(string actorName, string narrativeText)
        {
            string text = narrativeText.Replace("{name}", actorName);
            
            return new NarrativeTextBuilder()
                .AddEmoji("💥 ", ColorPalette.Critical)
                .AddTextWithHighlight(text, actorName, ColorPalette.Warning, ColorPalette.Critical, ColorPalette.Warning)
                .Build();
        }
    }

    public static class CriticalMissFormatter
    {
        public static List<ColoredText> Format(string actorName, string narrativeText)
        {
            string text = narrativeText.Replace("{name}", actorName);
            
            return new NarrativeTextBuilder()
                .AddEmoji("❌ ", ColorPalette.Miss)
                .AddTextWithHighlight(text, actorName, ColorPalette.Gray, ColorPalette.Miss, ColorPalette.Gray)
                .Build();
        }
    }

    public static class EnvironmentalActionFormatter
    {
        public static List<ColoredText> Format(string effectDescription, string narrativeText)
        {
            string text = narrativeText.Replace("{effect}", effectDescription);
            
            return new NarrativeTextBuilder()
                .AddEmoji("🌍 ", ColorPalette.Green)
                .AddTextWithHighlight(text, effectDescription, ColorPalette.Cyan, ColorPalette.Green, ColorPalette.Cyan)
                .Build();
        }
    }

    public static class HealthRecoveryFormatter
    {
        public static List<ColoredText> Format(string targetName, string narrativeText)
        {
            string text = narrativeText.Replace("{name}", targetName);
            
            return new NarrativeTextBuilder()
                .AddEmoji("💚 ", ColorPalette.Healing)
                .AddTextWithHighlight(text, targetName, ColorPalette.Success, ColorPalette.Healing, ColorPalette.Success)
                .Build();
        }
    }

    public static class HealthLeadChangeFormatter
    {
        public static List<ColoredText> Format(string leaderName, string narrativeText, bool isPlayer)
        {
            string text = narrativeText.Replace("{name}", leaderName);
            ColorPalette entityColor = isPlayer ? ColorPalette.Player : ColorPalette.Enemy;
            
            return new NarrativeTextBuilder()
                .AddEmoji("⚡ ", ColorPalette.Warning)
                .AddTextWithHighlight(text, leaderName, ColorPalette.Yellow, entityColor, ColorPalette.Yellow)
                .Build();
        }
    }

    public static class Below50PercentFormatter
    {
        public static List<ColoredText> Format(string entityName, string narrativeText, bool isPlayer)
        {
            string text = narrativeText.Replace("{name}", entityName);
            ColorPalette entityColor = isPlayer ? ColorPalette.Player : ColorPalette.Enemy;
            
            return new NarrativeTextBuilder()
                .AddEmoji("⚠️ ", ColorPalette.Warning)
                .AddTextWithHighlight(text, entityName, ColorPalette.Orange, entityColor, ColorPalette.Orange)
                .Build();
        }
    }

    public static class Below10PercentFormatter
    {
        public static List<ColoredText> Format(string entityName, string narrativeText, bool isPlayer)
        {
            string text = narrativeText.Replace("{name}", entityName);
            ColorPalette entityColor = isPlayer ? ColorPalette.Player : ColorPalette.Enemy;
            
            return new NarrativeTextBuilder()
                .AddEmoji("💀 ", ColorPalette.Critical)
                .AddTextWithHighlight(text, entityName, ColorPalette.Error, entityColor, ColorPalette.Error)
                .Build();
        }
    }

    public static class GenericNarrativeFormatter
    {
        public static List<ColoredText> Format(string narrativeText, ColorPalette primaryColor = ColorPalette.Info)
        {
            return new NarrativeTextBuilder()
                .AddEmoji("📖 ", primaryColor)
                .AddText(narrativeText, primaryColor)
                .Build();
        }
    }

    // ============================================================================
    // Dual Entity Formatters
    // ============================================================================

    public static class IntenseBattleFormatter
    {
        public static List<ColoredText> Format(string playerName, string enemyName, string narrativeText)
        {
            string text = narrativeText.Replace("{player}", playerName).Replace("{enemy}", enemyName);
            
            return new NarrativeTextBuilder()
                .AddEmoji("🔥 ", ColorPalette.Critical)
                .AddTextWithDualHighlight(text, playerName, enemyName, ColorPalette.Warning, ColorPalette.Player, ColorPalette.Enemy)
                .Build();
        }
    }

    public static class PlayerDefeatedFormatter
    {
        public static List<ColoredText> Format(string enemyName, string narrativeText)
        {
            string text = narrativeText.Replace("{enemy}", enemyName);
            
            return new NarrativeTextBuilder()
                .AddEmoji("☠️ ", ColorPalette.Critical)
                .AddTextWithHighlight(text, enemyName, ColorPalette.Error, ColorPalette.Enemy, ColorPalette.Error)
                .Build();
        }
    }

    public static class EnemyDefeatedFormatter
    {
        public static List<ColoredText> Format(string enemyName, string playerName, string narrativeText)
        {
            string text = narrativeText.Replace("{name}", enemyName).Replace("{player}", playerName);
            
            return new NarrativeTextBuilder()
                .AddEmoji("✨ ", ColorPalette.Success)
                .AddTextWithDualHighlight(text, enemyName, playerName, ColorPalette.Success, ColorPalette.Enemy, ColorPalette.Player)
                .Build();
        }
    }

    // ============================================================================
    // Quote/Taunt Formatters
    // ============================================================================

    public static class PlayerTauntFormatter
    {
        public static List<ColoredText> Format(string playerName, string enemyName, string tauntText)
        {
            string text = tauntText.Replace("{name}", playerName).Replace("{enemy}", enemyName);
            var builder = new NarrativeTextBuilder().AddEmoji("💬 ", ColorPalette.Info);
            
            if (text.StartsWith("\""))
            {
                int quoteEnd = text.IndexOf("\"", 1);
                if (quoteEnd > 0)
                {
                    builder.AddText(text.Substring(0, quoteEnd + 1), ColorPalette.Cyan);
                    string remaining = text.Substring(quoteEnd + 1);
                    
                    // Highlight names in remaining text
                    int playerIndex = remaining.IndexOf(playerName);
                    int enemyIndex = remaining.IndexOf(enemyName);
                    
                    if (playerIndex >= 0 && enemyIndex >= 0)
                    {
                        builder.AddTextWithDualHighlight(remaining, playerName, enemyName, ColorPalette.Info, ColorPalette.Player, ColorPalette.Enemy);
                    }
                    else if (playerIndex >= 0)
                    {
                        builder.AddTextWithHighlight(remaining, playerName, ColorPalette.Info, ColorPalette.Player, ColorPalette.Info);
                    }
                    else
                    {
                        builder.AddText(remaining, ColorPalette.Info);
                    }
                }
                else
                {
                    builder.AddText(text, ColorPalette.Cyan);
                }
            }
            else
            {
                builder.AddTextWithDualHighlight(text, playerName, enemyName, ColorPalette.Info, ColorPalette.Player, ColorPalette.Enemy);
            }
            
            return builder.Build();
        }
    }

    public static class EnemyTauntFormatter
    {
        public static List<ColoredText> Format(string enemyName, string playerName, string tauntText)
        {
            string text = tauntText.Replace("{name}", enemyName).Replace("{player}", playerName);
            var builder = new NarrativeTextBuilder().AddEmoji("💬 ", ColorPalette.Warning);
            
            if (text.StartsWith("\""))
            {
                int quoteEnd = text.IndexOf("\"", 1);
                if (quoteEnd > 0)
                {
                    builder.AddText(text.Substring(0, quoteEnd + 1), ColorPalette.Orange);
                    string remaining = text.Substring(quoteEnd + 1);
                    
                    // Highlight names in remaining text
                    int enemyIndex = remaining.IndexOf(enemyName);
                    int playerIndex = remaining.IndexOf(playerName);
                    
                    if (enemyIndex >= 0 && playerIndex >= 0)
                    {
                        builder.AddTextWithDualHighlight(remaining, enemyName, playerName, ColorPalette.Warning, ColorPalette.Enemy, ColorPalette.Player);
                    }
                    else if (enemyIndex >= 0)
                    {
                        builder.AddTextWithHighlight(remaining, enemyName, ColorPalette.Warning, ColorPalette.Enemy, ColorPalette.Warning);
                    }
                    else
                    {
                        builder.AddText(remaining, ColorPalette.Warning);
                    }
                }
                else
                {
                    builder.AddText(text, ColorPalette.Orange);
                }
            }
            else
            {
                builder.AddTextWithDualHighlight(text, enemyName, playerName, ColorPalette.Warning, ColorPalette.Enemy, ColorPalette.Player);
            }
            
            return builder.Build();
        }
    }

    // ============================================================================
    // Combo Formatter
    // ============================================================================

    public static class ComboFormatter
    {
        public static List<ColoredText> Format(string actorName, string targetName, bool isPlayerCombo)
        {
            var builder = new NarrativeTextBuilder().AddEmoji("🎯 ", ColorPalette.Critical);
            
            if (isPlayerCombo)
            {
                builder
                    .AddText(actorName, ColorPalette.Player)
                    .AddText(" unleashes a ", ColorPalette.Success)
                    .AddText("devastating combo sequence", ColorPalette.Critical)
                    .AddText("! Each strike flows into the next with deadly precision!", ColorPalette.Success);
            }
            else
            {
                builder
                    .AddText(actorName, ColorPalette.Enemy)
                    .AddText(" demonstrates ", ColorPalette.Warning)
                    .AddText("masterful technique", ColorPalette.Critical)
                    .AddText(" with a brutal combo that leaves ", ColorPalette.Warning)
                    .AddText(targetName, ColorPalette.Player)
                    .AddText(" reeling!", ColorPalette.Warning);
            }
            
            return builder.Build();
        }
    }
}

