using System;
using Avalonia.Media;
using RPGGame.Tests;
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
            TestBase.AssertEqual(Colors.White, c, "Common weapon name color is white", ref run, ref passed, ref failed);

            Color cHover = WeaponSelectionRenderer.GetWeaponNameColor(w, isHovered: true);
            TestBase.AssertEqual(Colors.White, cHover, "Hover does not override common rarity color", ref run, ref passed, ref failed);

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
            TestBase.AssertEqual(Colors.Blue, c, "Rare weapon name color is blue", ref run, ref passed, ref failed);

            var w2 = new WeaponItem("Axe", tier: 1, baseDamage: 1, baseAttackSpeed: 1.0, weaponType: WeaponType.Mace)
            {
                Rarity = "Legendary"
            };

            Color c2 = WeaponSelectionRenderer.GetWeaponNameColor(w2, isHovered: true);
            // ItemThemeProvider uses orange for Legendary.
            TestBase.AssertEqual(ColorPalette.Orange.GetColor(), c2, "Legendary weapon name color matches rarity theme", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }
    }
}

