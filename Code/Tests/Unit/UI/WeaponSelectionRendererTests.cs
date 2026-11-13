using System;
using System.Linq;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Renderers.Menu;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    public static class WeaponSelectionRendererTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestWeaponNameColor_UsesRarityCommonIsWhite(ref run, ref passed, ref failed);
            TestWeaponNameColor_UsesRarityForHigherTiers(ref run, ref passed, ref failed);
            TestCardHelpers_SpacedNameAndAlignedArt(ref run, ref passed, ref failed);
            TestDecorativeRule_UsesAsciiAndFitsZoomedContent(ref run, ref passed, ref failed);
            TestDecorativeRule_CentersUsingVisualZoomColumns(ref run, ref passed, ref failed);

            TestBase.PrintSummary(nameof(WeaponSelectionRendererTests), run, passed, failed);
        }

        private static void TestWeaponNameColor_UsesRarityCommonIsWhite(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestWeaponNameColor_UsesRarityCommonIsWhite));

            var w = new WeaponItem("Stick", tier: 1, baseDamage: 1, baseAttackSpeed: 1.0, weaponType: WeaponType.Wand)
            {
                Rarity = "Common"
            };

            Color c = WeaponSelectionRenderer.GetWeaponNameColor(w, isHovered: false);
            TestBase.AssertEqual(ColorPalette.Common.GetColor(), c, "Common weapon name uses the Common rarity color", ref run, ref passed, ref failed);

            Color cHover = WeaponSelectionRenderer.GetWeaponNameColor(w, isHovered: true);
            TestBase.AssertEqual(ColorPalette.Common.GetColor(), cHover, "Hover does not override common rarity color", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestWeaponNameColor_UsesRarityForHigherTiers(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestWeaponNameColor_UsesRarityForHigherTiers));

            var w = new WeaponItem("Blade", tier: 1, baseDamage: 1, baseAttackSpeed: 1.0, weaponType: WeaponType.Sword)
            {
                Rarity = "Rare"
            };

            Color c = WeaponSelectionRenderer.GetWeaponNameColor(w, isHovered: false);
            TestBase.AssertEqual(ColorPalette.Rare.GetColor(), c, "Rare weapon name uses the Rare rarity color", ref run, ref passed, ref failed);

            var w2 = new WeaponItem("Axe", tier: 1, baseDamage: 1, baseAttackSpeed: 1.0, weaponType: WeaponType.Mace)
            {
                Rarity = "Legendary"
            };

            Color c2 = WeaponSelectionRenderer.GetWeaponNameColor(w2, isHovered: true);
            // ItemThemeProvider uses orange for Legendary.
            TestBase.AssertEqual(ColorPalette.Orange.GetColor(), c2, "Legendary weapon name color matches rarity theme", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestCardHelpers_SpacedNameAndAlignedArt(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestCardHelpers_SpacedNameAndAlignedArt));

            TestBase.AssertEqual("W A R R I O R", WeaponSelectionRenderer.Spaced("Warrior"), "Class name is letter-spaced capitals", ref run, ref passed, ref failed);
            foreach (WeaponType type in new[] { WeaponType.Mace, WeaponType.Sword, WeaponType.Dagger, WeaponType.Wand })
            {
                string[] art = WeaponSelectionRenderer.WeaponArt(type);
                TestBase.AssertEqual(3, art.Length, $"{type} art is three rows", ref run, ref passed, ref failed);
                TestBase.AssertTrue(art.Max(l => l.Length) <= 26, $"{type} art fits a minimum-width card", ref run, ref passed, ref failed);
            }

            TestBase.ClearCurrentTestName();
        }

        private static void TestDecorativeRule_UsesAsciiAndFitsZoomedContent(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestDecorativeRule_UsesAsciiAndFitsZoomedContent));

            var preset = GameFonts.ActivePreset;
            double savedZoom = GameFonts.GetZoom(preset);
            try
            {
                GameFonts.SetZoom(preset, 1.0);
                string rule100 = MenuLayoutCalculator.BuildDecorativeRule(desiredLength: 40, contentWidth: 54);
                TestBase.AssertEqual(40, rule100.Length, "100% zoom keeps desired rule length when it fits", ref run, ref passed, ref failed);
                TestBase.AssertTrue(rule100.IndexOf('-') >= 0 && rule100.IndexOf('─') < 0,
                    "Decorative rule uses ASCII hyphen, not box-drawing", ref run, ref passed, ref failed);

                GameFonts.SetZoom(preset, 1.5);
                string rule150 = MenuLayoutCalculator.BuildDecorativeRule(desiredLength: 40, contentWidth: 54);
                int maxChars = CenterColumnTextZoom.ScaleWrapWidth(54);
                TestBase.AssertEqual(maxChars, rule150.Length,
                    "150% zoom clamps rule to ScaleWrapWidth so painted glyphs fit the box",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(
                    MenuLayoutCalculator.VisualCenterColumnColumns(rule150.Length) <= 54,
                    "Zoomed visual columns stay within content width",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                GameFonts.SetZoom(preset, savedZoom);
            }

            TestBase.ClearCurrentTestName();
        }

        private static void TestDecorativeRule_CentersUsingVisualZoomColumns(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestDecorativeRule_CentersUsingVisualZoomColumns));

            var preset = GameFonts.ActivePreset;
            double savedZoom = GameFonts.GetZoom(preset);
            try
            {
                const int contentX = 10;
                const int contentWidth = 54;
                const int ruleLen = 20;

                GameFonts.SetZoom(preset, 1.0);
                int x100 = MenuLayoutCalculator.CalculateCenteredZoomedTextX(contentX, contentWidth, ruleLen);
                TestBase.AssertEqual(
                    MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, ruleLen),
                    x100,
                    "100% zoom centering matches unscaled centering",
                    ref run, ref passed, ref failed);

                GameFonts.SetZoom(preset, 1.5);
                int x150 = MenuLayoutCalculator.CalculateCenteredZoomedTextX(contentX, contentWidth, ruleLen);
                int visual = MenuLayoutCalculator.VisualCenterColumnColumns(ruleLen);
                int expected = MenuLayoutCalculator.CalculateCenteredTextX(contentX, contentWidth, visual);
                TestBase.AssertEqual(expected, x150,
                    "150% zoom centering uses visual column width",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(x150 < x100,
                    "Zoomed centering shifts left so scaled glyphs do not overrun the right edge",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                GameFonts.SetZoom(preset, savedZoom);
            }

            TestBase.ClearCurrentTestName();
        }
    }
}

