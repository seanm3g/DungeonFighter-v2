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
    /// See Documentation/COMBAT_LOG_SPACING_STANDARD.md for spacing guidelines.
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
            builder.Add(" health", Colors.White);
            
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
        /// Lead-in line when a swing rolls the room's trigger face, e.g. "Kara rolled a 13 - the Flooded Vault answers!".
        /// </summary>
        /// <summary>Formats the notice before a free combo action (the last combo roll succeeded).</summary>
        public static List<ColoredText> FormatFreeComboActionColored(string actorName)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(actorName, ColorPalette.Cyan);
            builder.Add("keeps the combo going - free action!", Colors.White);
            return builder.Build();
        }

        public static List<ColoredText> FormatRoomRollTriggerColored(string rollerName, string roomName, int triggerRoll)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(rollerName, ColorPalette.Cyan);
            builder.Add("rolled a", Colors.White);
            builder.AddSpace();
            builder.Add(triggerRoll.ToString(), ColorPalette.Gold);
            builder.Add(" - the", Colors.White);
            builder.AddSpace();
            builder.Add(roomName, ColorPalette.Green);
            builder.Add(" answers!", Colors.White);
            return builder.Build();
        }

    }
}

