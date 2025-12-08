using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;

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

            string enemyWeaponInfo = enemy.Weapon != null
                ? string.Format(AsciiArtAssets.UIText.WeaponSuffix, enemy.Weapon.Name)
                : "";
            string encounteredText = string.Format(AsciiArtAssets.UIText.EncounteredFormat, enemy.Name, enemyWeaponInfo);
            var encounteredBuilder = new ColoredTextBuilder();
            encounteredBuilder.Add(encounteredText, ColorPalette.Common);
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

