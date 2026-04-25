using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.ColorSystem.Applications
{
    /// <summary>
    /// Left HERO panel: while the HUD still shows <see cref="ClassPresentationConfig.DefaultNoPointsClassName"/> (e.g. Fighter),
    /// the hero name uses <see cref="ColorPalette.Gold"/>; after the class title advances, the name uses per-glyph cycles from
    /// solo path, weapon-path duo (same basis as hybrid titles), or blended multi-path.
    /// </summary>
    public static class HeroNamePanelColoredText
    {
        /// <summary>Builds one segment per character so <see cref="ColoredTextWriter.RenderSegments"/> paints a class-colored pattern.</summary>
        public static List<ColoredText> BuildLeftPanelHeroNameSegments(Character character)
        {
            if (character == null)
                return new List<ColoredText>();

            string name = character.Name ?? "";
            if (name.Length == 0)
                return new List<ColoredText>();

            var cfg = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            string displayClass = AttributeClassNameComposer.ComposeDisplayClass(character.Progression, cfg);
            if (string.Equals(displayClass, cfg.DefaultNoPointsClassName, StringComparison.Ordinal))
                return new List<ColoredText> { new ColoredText(name, ColorPalette.Gold.GetColor()) };

            Color[] palette = ResolvePalette(character.Progression);
            return PatternIntoSegments(name, palette);
        }

        private static List<ColoredText> PatternIntoSegments(string name, Color[] palette)
        {
            if (palette == null || palette.Length == 0)
                palette = new[] { Colors.White };

            var list = new List<ColoredText>(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                var ch = name[i].ToString();
                var color = palette[i % palette.Length];
                list.Add(new ColoredText(ch, color));
            }

            return list;
        }

        private static Color[] ResolvePalette(CharacterProgression progression)
        {
            var ranked = progression.GetClassPathsSortedByPoints();
            var active = ranked.Where(x => x.Points >= 1).ToList();
            if (active.Count == 0)
                return new[] { Colors.White };

            if (active.Count == 1)
                return SoloPalette(active[0].Path);

            OrderDuoPaths(ranked[0].Path, ranked[1].Path, out WeaponType a, out WeaponType b);
            return DuoPalette(a, b);
        }

        private static void OrderDuoPaths(WeaponType x, WeaponType y, out WeaponType first, out WeaponType second)
        {
            int ix = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, x);
            int iy = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, y);
            if (ix < 0) ix = 999;
            if (iy < 0) iy = 999;
            if (ix <= iy)
            {
                first = x;
                second = y;
            }
            else
            {
                first = y;
                second = x;
            }
        }

        private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

        private static Color[] SoloPalette(WeaponType path) => path switch
        {
            WeaponType.Mace => new[]
            {
                C(255, 107, 53), C(220, 50, 40), C(255, 180, 90), C(139, 0, 0)
            },
            WeaponType.Sword => new[]
            {
                C(176, 196, 222), C(255, 215, 0), C(220, 220, 230), C(70, 130, 180)
            },
            WeaponType.Dagger => new[]
            {
                C(147, 112, 219), C(0, 206, 209), C(72, 61, 99), C(175, 238, 238)
            },
            WeaponType.Wand => new[]
            {
                C(155, 89, 182), C(52, 152, 219), C(236, 240, 241), C(125, 60, 152)
            },
            _ => new[] { Colors.White }
        };

        private static Color[] DuoPalette(WeaponType first, WeaponType second) => (first, second) switch
        {
            (WeaponType.Mace, WeaponType.Sword) => new[]
            {
                C(255, 99, 71), C(192, 192, 200), C(255, 140, 0), C(105, 105, 170)
            },
            (WeaponType.Mace, WeaponType.Dagger) => new[]
            {
                C(255, 69, 0), C(138, 43, 226), C(255, 160, 122), C(75, 0, 130)
            },
            (WeaponType.Mace, WeaponType.Wand) => new[]
            {
                C(255, 80, 60), C(138, 43, 226), C(30, 144, 255), C(255, 200, 120)
            },
            (WeaponType.Sword, WeaponType.Dagger) => new[]
            {
                C(192, 192, 210), C(0, 191, 255), C(255, 223, 0), C(47, 79, 79)
            },
            (WeaponType.Sword, WeaponType.Wand) => new[]
            {
                C(230, 230, 250), C(123, 104, 238), C(255, 215, 0), C(72, 61, 139)
            },
            (WeaponType.Dagger, WeaponType.Wand) => new[]
            {
                C(186, 85, 211), C(46, 204, 113), C(52, 152, 219), C(102, 51, 153)
            },
            _ => SoloPalette(first)
        };
    }
}
