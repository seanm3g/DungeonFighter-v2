using System;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers.Helpers;

namespace RPGGame.Tests.Unit.UI
{
    public static class ItemRendererHelperWeaponNameColorTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestCommonWeaponName_IsWhite(ref run, ref passed, ref failed);

            TestBase.PrintSummary(nameof(ItemRendererHelperWeaponNameColorTests), run, passed, failed);
        }

        private static void TestCommonWeaponName_IsWhite(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestCommonWeaponName_IsWhite));

            var weapon = new WeaponItem("Stick", tier: 1, baseDamage: 1, baseAttackSpeed: 1.0, weaponType: WeaponType.Wand)
            {
                Rarity = "Common"
            };

            var segs = ItemRendererHelper.BuildItemNameSegments(itemIndex: 0, item: weapon, character: null);
            var nameSeg = segs.FindLast(s => s != null && s.Text != null && s.Text.Contains("Stick"));
            TestBase.AssertNotNull(nameSeg, "Weapon name segment exists", ref run, ref passed, ref failed);
            if (nameSeg != null)
                TestBase.AssertEqual(Colors.White, nameSeg.Color, "Common weapon name segment color is white", ref run, ref passed, ref failed);

            TestBase.ClearCurrentTestName();
        }
    }
}

