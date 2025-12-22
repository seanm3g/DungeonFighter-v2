using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.Game.TestRunners.Helpers
{
    /// <summary>
    /// Helper class for color system tests
    /// </summary>
    public static class ColorSystemTestHelpers
    {
        public static (int passed, int failed) TestPaletteGroup(ColorPalette[] palettes, CanvasUICoordinator uiCoordinator)
        {
            int passed = 0, failed = 0;
            foreach (var palette in palettes)
            {
                if (palette.GetColor().A > 0) passed++;
                else { failed++; uiCoordinator.WriteLine($"  ✗ {palette} returned invalid color"); }
            }
            return (passed, failed);
        }

        public static (int passed, int failed) TestPatternGroup(string[] patterns, bool checkColor, CanvasUICoordinator uiCoordinator)
        {
            int passed = 0, failed = 0;
            foreach (var pattern in patterns)
            {
                if (!ColorPatterns.HasPattern(pattern)) { failed++; uiCoordinator.WriteLine($"  ✗ Pattern '{pattern}' not found"); continue; }
                if (checkColor)
                {
                    var color = ColorPatterns.GetColorForPattern(pattern);
                    if (color.A > 0) passed++;
                    else { failed++; uiCoordinator.WriteLine($"  ✗ Pattern '{pattern}' returned invalid color"); }
                }
                else
                {
                    var palette = ColorPatterns.GetPaletteForPattern(pattern);
                    if (palette != ColorPalette.White || pattern == "common") passed++;
                    else { failed++; uiCoordinator.WriteLine($"  ✗ Pattern '{pattern}' returned default palette"); }
                }
            }
            return (passed, failed);
        }

        public static (int passed, int failed) TestColorConsistency(CanvasUICoordinator uiCoordinator)
        {
            var damageColor1 = ColorPatterns.GetColorForPattern("damage");
            var damageColor2 = ColorPatterns.GetColorForPattern("damage");
            var healingColor = ColorPatterns.GetColorForPattern("healing");
            int passed = 0, failed = 0;
            
            if (damageColor1 == damageColor2) { uiCoordinator.WriteLine($"  ✓ Damage color is consistent"); passed++; }
            else { uiCoordinator.WriteLine($"  ✗ Damage color inconsistent"); failed++; }
            
            if (healingColor != damageColor1) { uiCoordinator.WriteLine($"  ✓ Healing color differs from damage"); passed++; }
            else { uiCoordinator.WriteLine($"  ✗ Healing color same as damage"); failed++; }
            
            return (passed, failed);
        }

        public static (int passed, int failed) TestMissingColorDetection(CanvasUICoordinator uiCoordinator)
        {
            var testPatterns = new[] { "damage", "healing", "critical", "success", "error" };
            int passed = 0, failed = 0;
            foreach (var pattern in testPatterns)
            {
                var color = ColorPatterns.GetColorForPattern(pattern);
                if (color.A == 255) passed++;
                else { failed++; uiCoordinator.WriteLine($"  ✗ Pattern '{pattern}' has invalid color (A={color.A})"); }
            }
            return (passed, failed);
        }

        public static (int passed, int failed) TestSingleColor(string name, ColorPalette palette, CanvasUICoordinator uiCoordinator)
        {
            var color = palette.GetColor();
            if (color.A > 0) { uiCoordinator.WriteLine($"  ✓ {name} color: RGB({color.R}, {color.G}, {color.B})"); return (1, 0); }
            uiCoordinator.WriteLine($"  ✗ {name} color invalid");
            return (0, 1);
        }

        public static (int passed, int failed) TestColorDifferentiation(CanvasUICoordinator uiCoordinator)
        {
            var damageColor = ColorPalette.Damage.GetColor();
            var healingColor = ColorPalette.Healing.GetColor();
            var criticalColor = ColorPalette.Critical.GetColor();
            if (damageColor != healingColor && damageColor != criticalColor && healingColor != criticalColor)
            { uiCoordinator.WriteLine($"  ✓ All colors are distinct"); return (1, 0); }
            uiCoordinator.WriteLine($"  ✗ Some colors are identical");
            return (0, 1);
        }

        public static (int passed, int failed) TestRarityColorGroup((string name, ColorPalette palette)[] rarities, CanvasUICoordinator uiCoordinator)
        {
            int passed = 0, failed = 0;
            foreach (var (name, palette) in rarities)
            {
                var color = palette.GetColor();
                if (color.A > 0) { uiCoordinator.WriteLine($"  ✓ {name}: RGB({color.R}, {color.G}, {color.B})"); passed++; }
                else { uiCoordinator.WriteLine($"  ✗ {name} color invalid"); failed++; }
            }
            return (passed, failed);
        }

        public static (int passed, int failed) TestRarityProgression((string name, ColorPalette palette)[] rarities, CanvasUICoordinator uiCoordinator)
        {
            var colors = rarities.Select(r => r.palette.GetColor()).ToList();
            var distinctColors = colors.Distinct().Count();
            if (distinctColors == colors.Count) { uiCoordinator.WriteLine($"  ✓ All rarity colors are distinct"); return (1, 0); }
            uiCoordinator.WriteLine($"  ✗ Some rarity colors are identical ({distinctColors}/{colors.Count} unique)");
            return (0, 1);
        }

        public static (int passed, int failed) TestStatusEffectPatterns(CanvasUICoordinator uiCoordinator)
        {
            var statusPatterns = new[] { "poison", "fire", "ice", "lightning", "stun", "buff", "debuff" };
            int passed = 0, failed = 0;
            foreach (var pattern in statusPatterns)
            {
                if (!ColorPatterns.HasPattern(pattern)) { passed++; continue; }
                var color = ColorPatterns.GetColorForPattern(pattern);
                if (color.A > 0) passed++;
                else { failed++; uiCoordinator.WriteLine($"  ✗ Pattern '{pattern}' returned invalid color"); }
            }
            return (passed, failed);
        }

        public static (int passed, int failed) TestElementColors(CanvasUICoordinator uiCoordinator)
        {
            var elements = new[] { ("fire", ColorPalette.Red), ("ice", ColorPalette.Cyan), ("poison", ColorPalette.Green) };
            int passed = 0, failed = 0;
            foreach (var (element, expectedPalette) in elements)
            {
                if (ColorPatterns.GetPaletteForPattern(element) == expectedPalette) passed++;
                else { failed++; uiCoordinator.WriteLine($"  ✗ Element '{element}' color mismatch"); }
            }
            return (passed, failed);
        }

        public static ColorPalette[] GetBasicPalettes() => new[]
        {
            ColorPalette.White, ColorPalette.Black, ColorPalette.Red, ColorPalette.Green,
            ColorPalette.Blue, ColorPalette.Yellow, ColorPalette.Cyan, ColorPalette.Magenta,
        };

        public static ColorPalette[] GetGamePalettes() => new[]
        {
            ColorPalette.Damage, ColorPalette.Healing, ColorPalette.Critical,
            ColorPalette.Success, ColorPalette.Warning, ColorPalette.Error,
        };

        public static ColorPalette[] GetRarityPalettes() => new[]
        {
            ColorPalette.Common, ColorPalette.Uncommon, ColorPalette.Rare,
            ColorPalette.Epic, ColorPalette.Legendary,
        };

        public static string[] GetCombatPatterns() => new[] { "damage", "healing", "critical", "miss", "block", "dodge" };
        public static string[] GetRarityPatterns() => new[] { "common", "uncommon", "rare", "epic", "legendary" };
        public static string[] GetElementPatterns() => new[] { "fire", "ice", "lightning", "poison", "dark", "light" };
    }
}

