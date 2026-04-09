using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.ColorSystem.Applications
{
    /// <summary>
    /// Colored, multi-line level-up text for the center log and dungeon completion screen.
    /// Banner line includes "LEVEL UP" in gold so <see cref="DisplayRenderer"/> can apply shimmer.
    /// </summary>
    public static class LevelUpDisplayColoredText
    {
        public static List<List<ColoredText>> BuildDisplayLines(LevelUpInfo info)
        {
            var lines = new List<List<ColoredText>>();
            if (!info.IsValid)
                return lines;

            lines.Add(BuildBannerLine());
            lines.Add(BuildReachedLevelLine(info.NewLevel));

            if (info.HasWeapon && !string.IsNullOrEmpty(info.ClassName))
            {
                lines.Add(BuildClassPointLine(info.ClassName));
                if (!string.IsNullOrEmpty(info.StatIncreaseMessage))
                    lines.Add(BuildStatsLine(info.StatIncreaseMessage));
                if (!string.IsNullOrEmpty(info.CurrentClass))
                    lines.Add(BuildCurrentClassLine(info.CurrentClass));
                if (!string.IsNullOrEmpty(info.FullNameWithQualifier))
                    lines.Add(BuildNameLine(info.FullNameWithQualifier));
                if (!string.IsNullOrEmpty(info.ClassPointsInfo))
                    lines.Add(BuildClassPointsLine(info.ClassPointsInfo));
                if (!string.IsNullOrEmpty(info.ClassUpgradeInfo))
                    lines.Add(BuildNextUpgradesLine(info.ClassUpgradeInfo));
            }
            else
            {
                lines.Add(BuildNoWeaponLine());
            }

            return lines;
        }

        private static List<ColoredText> BuildBannerLine()
        {
            var b = ColoredTextBuilder.Start();
            b.Add("    ", Colors.White);
            b.Add("★ ", ColorPalette.Gold);
            b.Add("★ ", ColorPalette.Yellow);
            b.Add("LEVEL UP!", ColorPalette.Gold);
            b.Add(" ★", ColorPalette.Yellow);
            b.Add(" ★", ColorPalette.Gold);
            return b.Build();
        }

        private static List<ColoredText> BuildReachedLevelLine(int newLevel)
        {
            var b = ColoredTextBuilder.Start();
            b.Add("You reached level ", Colors.White);
            b.Add(newLevel.ToString(), ColorPalette.Success);
            b.Add("!", ColorPalette.Success);
            return b.Build();
        }

        private static List<ColoredText> BuildClassPointLine(string className)
        {
            var b = ColoredTextBuilder.Start();
            b.Add("Gained ", Colors.White);
            b.Add("+1 ", ColorPalette.Success);
            b.Add(className, ColorPalette.Info);
            b.Add(" class point!", Colors.White);
            return b.Build();
        }

        private static List<ColoredText> BuildStatsLine(string statIncreaseMessage)
        {
            var b = ColoredTextBuilder.Start();
            b.Add("Stats increased: ", ColorPalette.Cyan);
            b.Add(statIncreaseMessage, Colors.White);
            return b.Build();
        }

        private static List<ColoredText> BuildCurrentClassLine(string currentClass)
        {
            var b = ColoredTextBuilder.Start();
            b.Add("Current class: ", ColorPalette.Gray);
            b.Add(currentClass, ColorPalette.Player);
            return b.Build();
        }

        private static List<ColoredText> BuildNameLine(string fullNameWithQualifier)
        {
            var b = ColoredTextBuilder.Start();
            b.Add("You are now known as: ", Colors.White);
            b.Add(fullNameWithQualifier, ColorPalette.Gold);
            return b.Build();
        }

        private static List<ColoredText> BuildClassPointsLine(string classPointsInfo)
        {
            var b = ColoredTextBuilder.Start();
            b.Add("Class Points: ", ColorPalette.Info);
            b.Add(classPointsInfo, Colors.White);
            return b.Build();
        }

        private static List<ColoredText> BuildNextUpgradesLine(string classUpgradeInfo)
        {
            var b = ColoredTextBuilder.Start();
            b.Add("Next Upgrades: ", ColorPalette.Warning);
            b.Add(classUpgradeInfo, Colors.White);
            return b.Build();
        }

        private static List<ColoredText> BuildNoWeaponLine()
        {
            var b = ColoredTextBuilder.Start();
            b.Add("No weapon equipped - equal stat increases (+2 all stats)", ColorPalette.Warning);
            return b.Build();
        }
    }
}
