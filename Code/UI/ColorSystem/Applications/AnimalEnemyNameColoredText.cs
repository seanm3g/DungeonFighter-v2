using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.ColorSystem.Applications
{
    /// <summary>
    /// Builds per-glyph colored segments for creature-themed enemy names so the text reads as fur, hide,
    /// ice, etc. (same idea as <see cref="HeroNamePanelColoredText"/> class palettes).
    /// Matches whole words only (e.g. <c>bat</c> does not match inside <c>combat</c>).
    /// </summary>
    public static class AnimalEnemyNameColoredText
    {
        private enum CreatureKind
        {
            Yeti,
            Wolf,
            Bear,
            Spider,
            Bat,
            Boar,
            Treant,
            Salamander,
        }

        private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

        /// <summary>Returns per-character segments, or null when no creature keyword applies.</summary>
        public static List<ColoredText>? TryBuildSegments(string enemyName)
        {
            if (string.IsNullOrWhiteSpace(enemyName))
                return null;

            string trimmed = enemyName.Trim();
            var kind = ResolveCreatureKind(trimmed);
            if (kind == null)
                return null;

            Color[] palette = Palette(kind.Value);
            return PatternIntoSegments(trimmed, palette);
        }

        private static CreatureKind? ResolveCreatureKind(string text)
        {
            // Order: more specific / distinctive tokens first when relevant.
            if (ContainsWholeWord(text, "yeti")) return CreatureKind.Yeti;
            if (ContainsWholeWord(text, "wolf")) return CreatureKind.Wolf;
            if (ContainsWholeWord(text, "bear")) return CreatureKind.Bear;
            if (ContainsWholeWord(text, "spider")) return CreatureKind.Spider;
            if (ContainsWholeWord(text, "bat")) return CreatureKind.Bat;
            if (ContainsWholeWord(text, "boar")) return CreatureKind.Boar;
            if (ContainsWholeWord(text, "treant")) return CreatureKind.Treant;
            if (ContainsWholeWord(text, "salamander")) return CreatureKind.Salamander;
            return null;
        }

        private static bool ContainsWholeWord(string haystack, string word)
        {
            return Regex.IsMatch(haystack, @"\b" + Regex.Escape(word) + @"\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static List<ColoredText> PatternIntoSegments(string name, Color[] palette)
        {
            var list = new List<ColoredText>(name.Length);
            int n = palette.Length;
            for (int i = 0; i < name.Length; i++)
            {
                string ch = name[i].ToString();
                list.Add(new ColoredText(ch, palette[i % n]));
            }

            return list;
        }

        private static Color[] Palette(CreatureKind kind) => kind switch
        {
            CreatureKind.Yeti => new[]
            {
                C(240, 248, 255), C(176, 224, 230), C(135, 206, 250), C(230, 230, 250),
                C(200, 230, 255), C(255, 250, 250), C(173, 216, 230), C(224, 255, 255),
                C(185, 215, 235), C(245, 252, 255),
            },
            CreatureKind.Wolf => new[]
            {
                C(169, 169, 169), C(192, 192, 192), C(220, 220, 230), C(105, 105, 105),
                C(211, 211, 211), C(128, 128, 128), C(176, 196, 222), C(90, 90, 95),
                C(150, 155, 165), C(200, 200, 208),
            },
            CreatureKind.Bear => new[]
            {
                C(101, 67, 33), C(139, 90, 43), C(160, 82, 45), C(210, 180, 140),
                C(92, 64, 51), C(181, 136, 99), C(62, 39, 35), C(222, 184, 135),
                C(120, 77, 48), C(194, 148, 108),
            },
            CreatureKind.Spider => new[]
            {
                C(75, 0, 130), C(48, 25, 52), C(102, 51, 153), C(25, 25, 112),
                C(72, 61, 139), C(128, 0, 128), C(47, 79, 79), C(63, 13, 18),
                C(85, 40, 95), C(55, 35, 70),
            },
            CreatureKind.Bat => new[]
            {
                C(20, 20, 30), C(40, 40, 55), C(15, 15, 25), C(55, 48, 85),
                C(30, 30, 40), C(70, 60, 90), C(25, 25, 35), C(45, 38, 58),
                C(35, 32, 48), C(50, 45, 68),
            },
            CreatureKind.Boar => new[]
            {
                C(139, 90, 43), C(160, 82, 45), C(205, 133, 63), C(122, 63, 42),
                C(184, 115, 51), C(92, 58, 46), C(210, 170, 130), C(178, 93, 63),
                C(150, 85, 55), C(195, 145, 110),
            },
            CreatureKind.Treant => new[]
            {
                C(34, 85, 51), C(85, 107, 47), C(107, 142, 35), C(74, 93, 59),
                C(139, 115, 85), C(46, 125, 50), C(102, 119, 68), C(56, 76, 42),
                C(125, 135, 90), C(78, 101, 56),
            },
            CreatureKind.Salamander => new[]
            {
                C(255, 94, 77), C(255, 140, 0), C(255, 180, 90), C(204, 51, 0),
                C(255, 117, 56), C(233, 150, 122), C(178, 34, 34), C(255, 215, 0),
                C(220, 90, 40), C(255, 160, 122),
            },
            _ => new[] { Colors.White }
        };
    }
}
