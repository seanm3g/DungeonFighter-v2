using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame.Display.Dungeon
{
    /// <summary>
    /// Builds enemy information display
    /// </summary>
    public static class EnemyInfoBuilder
    {
        /// <summary>
        /// Builds enemy info lines
        /// </summary>
        public static List<string> BuildEnemyInfo(Enemy enemy)
        {
            var info = new List<string>();

            // Build the "A {enemy} with {weapon} appears." message with proper colors
            var encounteredBuilder = new ColoredTextBuilder();
            
            // "A "
            encounteredBuilder.Add("A ", Colors.White);
            
            // Enemy name in enemy-specific color
            encounteredBuilder.Add(enemy.Name, EntityColorHelper.GetEnemyColor(enemy));
            
            // Weapon info if present
            if (enemy.Weapon != null)
            {
                // " with "
                encounteredBuilder.Add(" with ", Colors.White);
                
                // Weapon name in rarity color
                var weaponRarity = enemy.Weapon.Rarity ?? "Common";
                var weaponColor = ItemThemeProvider.GetRarityColor(weaponRarity);
                encounteredBuilder.Add(enemy.Weapon.Name, weaponColor);
            }
            
            // " appears."
            encounteredBuilder.Add(" appears.", Colors.White);
            
            string renderedText = ColoredTextRenderer.RenderAsMarkup(encounteredBuilder.Build());
            // Trim any leading spaces that might be added during rendering
            info.Add(renderedText.TrimStart());

            string statsText = AsciiArtAssets.UIText.FormatEnemyStats(enemy.CurrentHealth, enemy.MaxHealth, enemy.Armor);
            var statsBuilder = new ColoredTextBuilder();
            statsBuilder.Add(statsText, Colors.White);
            info.Add(ColoredTextRenderer.RenderAsMarkup(statsBuilder.Build()));

            string attackText = AsciiArtAssets.UIText.FormatEnemyAttack(enemy.Strength, enemy.Agility, enemy.Technique, enemy.Intelligence);
            var attackBuilder = new ColoredTextBuilder();
            attackBuilder.Add(attackText, ColorPalette.Rare);
            info.Add(ColoredTextRenderer.RenderAsMarkup(attackBuilder.Build()));
            info.Add("");

            return info;
        }
    }
}

