using System;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    public static class PrimaryStatRowHighlightColorsTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestForWeaponPath_MaceRed(ref run, ref passed, ref failed);
            TestForWeaponPath_SwordGreen(ref run, ref passed, ref failed);
            TestForWeaponPath_DaggerYellow(ref run, ref passed, ref failed);
            TestForWeaponPath_WandCyan(ref run, ref passed, ref failed);
            TestForWeaponPath_NullPurple(ref run, ref passed, ref failed);

            TestBase.PrintSummary(nameof(PrimaryStatRowHighlightColorsTests), run, passed, failed);
        }

        private static void TestForWeaponPath_MaceRed(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_MaceRed));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Mace);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Red, c, "Mace path uses red (Barbarian)", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForWeaponPath_SwordGreen(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_SwordGreen));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Sword);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Green, c, "Sword path uses green (Warrior)", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForWeaponPath_DaggerYellow(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_DaggerYellow));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Dagger);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Yellow, c, "Dagger path uses yellow (Rogue)", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForWeaponPath_WandCyan(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_WandCyan));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Wand);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Cyan, c, "Wand path uses cyan (Wizard)", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForWeaponPath_NullPurple(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_NullPurple));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(null);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Purple, c, "Unknown path keeps legacy purple", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }
    }
}
