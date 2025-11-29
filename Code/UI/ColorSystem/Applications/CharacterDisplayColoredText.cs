using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Formats character information using the new ColoredText system
    /// Provides clean, maintainable character display formatting
    /// </summary>
    public static class CharacterDisplayColoredText
    {
        /// <summary>
        /// Formats character name with class and level
        /// </summary>
        public static List<ColoredText> FormatCharacterHeader(string name, string className, int level)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(name, ColorPalette.Player);
            builder.Add(" - ", Colors.Gray);
            builder.Add($"Level {level}", ColorPalette.Info);
            builder.Add(" ", Colors.White);
            builder.Add(className, ColorPalette.Warning);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats health display
        /// </summary>
        public static List<ColoredText> FormatHealth(int current, int max)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("HP: ", ColorPalette.Info);
            
            float percentage = (float)current / max;
            var healthColor = percentage > 0.5f ? ColorPalette.Success :
                             percentage > 0.25f ? ColorPalette.Warning :
                             ColorPalette.Error;
            
            builder.Add($"{current}/{max}", healthColor);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats health with bar
        /// </summary>
        public static List<ColoredText> FormatHealthBar(int current, int max, int barWidth = 20)
        {
            return MenuDisplayColoredText.FormatStatusBar("HP", current, max, barWidth);
        }
        
        /// <summary>
        /// Formats experience progress
        /// </summary>
        public static List<ColoredText> FormatExperience(int current, int required)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("XP: ", ColorPalette.Info);
            builder.Add(current.ToString(), ColorPalette.Success);
            builder.Add(" / ", Colors.Gray);
            builder.Add(required.ToString(), Colors.White);
            
            float percentage = (float)current / required * 100;
            builder.Add($" ({percentage:F0}%)", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a single stat
        /// </summary>
        public static List<ColoredText> FormatStat(string statName, int value, int? bonus = null)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add($"{statName}: ", ColorPalette.Info);
            builder.Add(value.ToString(), Colors.White);
            
            if (bonus.HasValue && bonus.Value != 0)
            {
                var sign = bonus.Value > 0 ? "+" : "";
                var bonusColor = bonus.Value > 0 ? ColorPalette.Success : ColorPalette.Error;
                builder.Add($" ({sign}{bonus.Value})", bonusColor);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats all character stats
        /// </summary>
        public static List<List<ColoredText>> FormatStats(Character character)
        {
            var lines = new List<List<ColoredText>>();
            
            // Use base stats without bonuses for now (can be enhanced later)
            lines.Add(FormatStat("STR", character.Strength));
            lines.Add(FormatStat("AGI", character.Agility));
            lines.Add(FormatStat("TEC", character.Technique));
            lines.Add(FormatStat("INT", character.Intelligence));
            
            return lines;
        }
        
        /// <summary>
        /// Formats combat stats
        /// </summary>
        public static List<List<ColoredText>> FormatCombatStats(Character character)
        {
            var lines = new List<List<ColoredText>>();
            
            // Strength (Attack)
            var attackLine = new ColoredTextBuilder();
            attackLine.Add("Strength: ", ColorPalette.Info);
            attackLine.Add(character.Stats.Strength.ToString(), ColorPalette.Damage);
            lines.Add(attackLine.Build());
            
            // Armor (Defense)
            var defenseLine = new ColoredTextBuilder();
            defenseLine.Add("Armor: ", ColorPalette.Info);
            defenseLine.Add(character.GetTotalArmor().ToString(), ColorPalette.Success);
            lines.Add(defenseLine.Build());
            
            // Agility (Speed)
            var speedLine = new ColoredTextBuilder();
            speedLine.Add("Agility: ", ColorPalette.Info);
            speedLine.Add(character.Stats.Agility.ToString(), ColorPalette.Warning);
            lines.Add(speedLine.Build());
            
            return lines;
        }
        
        /// <summary>
        /// Formats equipped items summary
        /// </summary>
        public static List<List<ColoredText>> FormatEquipmentSummary(Character character)
        {
            var lines = new List<List<ColoredText>>();
            
            // Weapon
            lines.Add(ItemDisplayColoredText.FormatEquippedItem("Weapon", character.Weapon));
            
            // Armor slots
            lines.Add(ItemDisplayColoredText.FormatEquippedItem("Head", character.Head));
            lines.Add(ItemDisplayColoredText.FormatEquippedItem("Body", character.Body));
            lines.Add(ItemDisplayColoredText.FormatEquippedItem("Feet", character.Feet));
            
            return lines;
        }
        
        /// <summary>
        /// Formats character status effects
        /// </summary>
        public static List<List<ColoredText>> FormatStatusEffects(Character character)
        {
            var lines = new List<List<ColoredText>>();
            
            // Check for status effects (this would need to access character's status effect system)
            // For now, showing examples of how it would work
            
            var headerLine = new ColoredTextBuilder();
            headerLine.Add("Status Effects:", ColorPalette.Info);
            lines.Add(headerLine.Build());
            
            // Placeholder for status effects - actual implementation would query character.Effects
            // This is an example of how status effects would be displayed
            // Uncomment and adapt when status effects are accessible
            
            /*
            if (character.Effects.IsPoisoned)
            {
                var poisonLine = new ColoredTextBuilder();
                poisonLine.Add("  • ", Colors.Gray);
                poisonLine.Add("Poisoned", ColorPalette.Error);
                poisonLine.Add(" (3 turns)", Colors.Gray);
                lines.Add(poisonLine.Build());
            }
            */
            
            return lines;
        }
        
        /// <summary>
        /// Formats complete character sheet
        /// </summary>
        public static List<List<ColoredText>> FormatCharacterSheet(Character character)
        {
            var lines = new List<List<ColoredText>>();
            
            // Header
            lines.Add(MenuDisplayColoredText.FormatMenuTitle("Character Sheet", centered: true));
            lines.Add(new List<ColoredText>()); // Blank line
            
            // Character info
            lines.Add(FormatCharacterHeader(character.Name, character.GetCurrentClass(), character.Level));
            lines.Add(FormatHealthBar(character.CurrentHealth, character.GetEffectiveMaxHealth()));
            lines.Add(FormatExperience(character.XP, 100)); // Simplified for now
            lines.Add(new List<ColoredText>()); // Blank line
            
            // Stats section
            lines.Add(MenuDisplayColoredText.FormatSectionHeader("Attributes"));
            lines.AddRange(FormatStats(character));
            lines.Add(new List<ColoredText>()); // Blank line
            
            // Combat stats section
            lines.Add(MenuDisplayColoredText.FormatSectionHeader("Combat Stats"));
            lines.AddRange(FormatCombatStats(character));
            lines.Add(new List<ColoredText>()); // Blank line
            
            // Equipment section
            lines.Add(MenuDisplayColoredText.FormatSectionHeader("Equipment"));
            lines.AddRange(FormatEquipmentSummary(character));
            
            return lines;
        }
        
        /// <summary>
        /// Formats level up message
        /// </summary>
        public static List<ColoredText> FormatLevelUp(string characterName, int newLevel)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("★", ColorPalette.Gold);
            builder.Add(characterName, ColorPalette.Player);
            builder.Add("reached", Colors.White);
            builder.Add("level", Colors.White);
            builder.Add(newLevel.ToString(), ColorPalette.Success);
            builder.Add("!", Colors.White);
            builder.Add("★", ColorPalette.Gold);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats stat increase notification
        /// </summary>
        public static List<ColoredText> FormatStatIncrease(string statName, int oldValue, int newValue)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add($"{statName}: ", ColorPalette.Info);
            builder.Add(oldValue.ToString(), Colors.Gray);
            builder.Add(" → ", ColorPalette.Success);
            builder.Add(newValue.ToString(), ColorPalette.Success);
            builder.Add($" (+{newValue - oldValue})", ColorPalette.Success);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats death message
        /// </summary>
        public static List<ColoredText> FormatDeath(string characterName)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("☠ ", ColorPalette.Error);
            builder.Add(characterName, ColorPalette.Player);
            builder.Add(" has fallen in battle... ", Colors.White);
            builder.Add("☠", ColorPalette.Error);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats quick character summary for combat
        /// </summary>
        public static List<ColoredText> FormatCombatSummary(Character character)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(character.Name, ColorPalette.Player);
            builder.Add(" [", Colors.Gray);
            builder.Add($"Lvl {character.Level}", ColorPalette.Info);
            builder.Add("] HP: ", Colors.Gray);
            
            float healthPercentage = (float)character.CurrentHealth / character.GetEffectiveMaxHealth();
            var healthColor = healthPercentage > 0.5f ? ColorPalette.Success :
                             healthPercentage > 0.25f ? ColorPalette.Warning :
                             ColorPalette.Error;
            
            builder.Add($"{character.CurrentHealth}/{character.GetEffectiveMaxHealth()}", healthColor);
            
            return builder.Build();
        }
    }
}
