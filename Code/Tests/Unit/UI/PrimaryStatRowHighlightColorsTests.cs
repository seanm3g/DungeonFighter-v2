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
            TestForWeaponPath_SwordBlue(ref run, ref passed, ref failed);
            TestForWeaponPath_DaggerGreen(ref run, ref passed, ref failed);
            TestForWeaponPath_WandPurple(ref run, ref passed, ref failed);
            TestForWeaponPath_NullGray(ref run, ref passed, ref failed);

            TestBase.PrintSummary(nameof(PrimaryStatRowHighlightColorsTests), run, passed, failed);
        }

        private static void TestForWeaponPath_MaceRed(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_MaceRed));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Mace);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Red, c, "Mace path uses red (Barbarian)", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForWeaponPath_SwordBlue(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_SwordBlue));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Sword);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Blue, c, "Sword path uses blue (Warrior)", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForWeaponPath_DaggerGreen(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_DaggerGreen));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Dagger);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Green, c, "Dagger path uses green (Rogue)", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForWeaponPath_WandPurple(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_WandPurple));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Wand);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Purple, c, "Wand path uses purple (Wizard)", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForWeaponPath_NullGray(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestForWeaponPath_NullGray));
            Color c = PrimaryStatRowHighlightColors.ForWeaponPath(null);
            TestBase.AssertEqual(AsciiArtAssets.Colors.Gray, c, "Unknown path uses gray", ref run, ref passed, ref failed);
            TestBase.ClearCurrentTestName();
        }
    }
}
