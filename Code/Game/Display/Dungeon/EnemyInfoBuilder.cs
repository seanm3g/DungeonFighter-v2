using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
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
        /// "A {enemy} with {weapon} appears." as structured segments (creature RGB preserved — no markup round-trip).
        /// </summary>
        public static List<ColoredText> BuildEncounterAppearanceSegments(Enemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            var encounteredBuilder = new ColoredTextBuilder();
            encounteredBuilder.Add("A ", Colors.White);
            EntityColorHelper.AppendEnemyNameColored(encounteredBuilder, enemy);
            if (enemy.Weapon != null)
            {
                encounteredBuilder.Add(" with ", Colors.White);
                var weaponRarity = enemy.Weapon.Rarity ?? "Common";
                var weaponColor = ItemThemeProvider.GetRarityColor(weaponRarity);
                encounteredBuilder.Add(enemy.Weapon.Name, weaponColor);
            }
            encounteredBuilder.Add(" appears.", Colors.White);
            return encounteredBuilder.Build();
        }

        /// <summary>
        /// Builds enemy info lines
        /// </summary>
        public static List<string> BuildEnemyInfo(Enemy enemy)
        {
            var info = new List<string>();

            string renderedText = ColoredTextRenderer.RenderAsMarkup(BuildEncounterAppearanceSegments(enemy));
            info.Add(renderedText.TrimStart());

            return info;
        }
    }
}

