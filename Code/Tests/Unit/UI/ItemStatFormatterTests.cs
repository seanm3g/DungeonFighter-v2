using System;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="ItemStatFormatter.FormatStatLine"/> weapon speed and damage comparison coloring.
    /// </summary>
    public static class ItemStatFormatterTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemStatFormatter Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSpeedFasterThanBaselineGreen();
            TestSpeedSlowerThanBaselineRed();
            TestSpeedEqualBaselineWhite();
            TestSpeedNoBaselineWhite();
            TestSpeedNonWeaponDisplayedStaysWhite();

            TestDamageHigherThanBaselineGreen();
            TestDamageLowerThanBaselineRed();
            TestDamageEqualBaselineWhite();
            TestDamageNoBaselineUsesDamagePalette();

            TestCommonRarityWeaponDamageComparisonStillColored();
            TestCommonRarityWeaponSpeedComparisonStillColored();

            TestArmorHigherThanBaselineGreen();
            TestArmorLowerThanBaselineRed();
            TestArmorEqualBaselineWhite();
            TestArmorNoBaselineUsesSuccessPalette();

            TestFeetStatsIncludeZeroActionSlots();
            TestFeetStatsIncludePositiveActionSlots();
            TestNonFeetStatsOmitZeroActionSlots();
            TestWandWeaponShowsClassActionSlotBonus();
            TestActionSlotHigherThanBaselineGreen();

            TestBase.PrintSummary("ItemStatFormatter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static Color ExpectedPaletteColor(ColorPalette palette) =>
            new ColoredText("x", palette.GetColor()).Color;

        private static ColoredText? FindSpeedValueSegment(System.Collections.Generic.List<ColoredText> segments)
        {
            return segments.FirstOrDefault(s => s.Text.Contains('×'));
        }

        private static ColoredText? FindDamageValueSegment(System.Collections.Generic.List<ColoredText> segments)
        {
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Text.Contains("Damage:", StringComparison.Ordinal))
                    return segments[i + 1];
            }
            return null;
        }

        private static ColoredText? FindArmorValueSegment(System.Collections.Generic.List<ColoredText> segments)
        {
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Text.Contains("Armor:", StringComparison.Ordinal))
                    return segments[i + 1];
            }
            return null;
        }

        private static ColoredText? FindActionSlotValueSegment(System.Collections.Generic.List<ColoredText> segments)
        {
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Text.Contains("Action slots:", StringComparison.Ordinal))
                    return segments[i + 1];
            }
            return null;
        }

        private static void TestSpeedFasterThanBaselineGreen()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedFasterThanBaselineGreen ---");
            var faster = new WeaponItem("Fast", 1, 10, 0.5, WeaponType.Sword) { Rarity = "Rare" };
            var slower = new WeaponItem("Slow", 1, 10, 1.0, WeaponType.Sword) { Rarity = "Rare" };
            string stat = $"Speed: {faster.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, faster, slower);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Success);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected success color for faster weapon"); }
        }

        private static void TestSpeedSlowerThanBaselineRed()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedSlowerThanBaselineRed ---");
            var faster = new WeaponItem("Fast", 1, 10, 0.5, WeaponType.Sword) { Rarity = "Rare" };
            var slower = new WeaponItem("Slow", 1, 10, 1.0, WeaponType.Sword) { Rarity = "Rare" };
            string stat = $"Speed: {slower.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, slower, faster);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Error);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected error color for slower weapon"); }
        }

        private static void TestSpeedEqualBaselineWhite()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedEqualBaselineWhite ---");
            var a = new WeaponItem("A", 1, 10, 0.75, WeaponType.Sword) { Rarity = "Rare" };
            var b = new WeaponItem("B", 1, 8, 0.75, WeaponType.Dagger) { Rarity = "Rare" };
            string stat = $"Speed: {a.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, a, b);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == new ColoredText("x", Colors.White).Color;
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected white when speeds match"); }
        }

        private static void TestSpeedNoBaselineWhite()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedNoBaselineWhite ---");
            var w = new WeaponItem("W", 1, 10, 0.6, WeaponType.Sword) { Rarity = "Rare" };
            string stat = $"Speed: {w.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, w, weaponSpeedBaseline: null);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == new ColoredText("x", Colors.White).Color;
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected white with no baseline"); }
        }

        private static void TestSpeedNonWeaponDisplayedStaysWhite()
        {
            _testsRun++;
            Console.WriteLine("--- TestSpeedNonWeaponDisplayedStaysWhite ---");
            var baseline = new WeaponItem("B", 1, 10, 1.0, WeaponType.Sword) { Rarity = "Rare" };
            var head = new HeadItem("H", 1, armor: 2) { Rarity = "Rare" };
            string stat = "Speed: 9.99×";
            var segments = ItemStatFormatter.FormatStatLine(stat, head, baseline);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == new ColoredText("x", Colors.White).Color;
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: non-weapon line should not apply weapon compare"); }
        }

        private static void TestDamageHigherThanBaselineGreen()
        {
            _testsRun++;
            Console.WriteLine("--- TestDamageHigherThanBaselineGreen ---");
            var high = new WeaponItem("High", 1, 15, 1.0, WeaponType.Wand) { Rarity = "Rare" };
            var low = new WeaponItem("Low", 1, 5, 1.0, WeaponType.Sword) { Rarity = "Rare" };
            string stat = $"Damage: {high.GetTotalDamage()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, high, low);
            var valueSeg = FindDamageValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Success);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected success color when new damage is higher"); }
        }

        private static void TestDamageLowerThanBaselineRed()
        {
            _testsRun++;
            Console.WriteLine("--- TestDamageLowerThanBaselineRed ---");
            var high = new WeaponItem("High", 1, 15, 1.0, WeaponType.Sword) { Rarity = "Rare" };
            var low = new WeaponItem("Low", 1, 5, 1.0, WeaponType.Wand) { Rarity = "Rare" };
            string stat = $"Damage: {low.GetTotalDamage()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, low, high);
            var valueSeg = FindDamageValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Error);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected error color when new damage is lower"); }
        }

        private static void TestDamageEqualBaselineWhite()
        {
            _testsRun++;
            Console.WriteLine("--- TestDamageEqualBaselineWhite ---");
            var a = new WeaponItem("A", 1, 10, 0.5, WeaponType.Sword) { Rarity = "Rare" };
            var b = new WeaponItem("B", 1, 10, 1.0, WeaponType.Dagger) { Rarity = "Rare" };
            string stat = $"Damage: {a.GetTotalDamage()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, a, b);
            var valueSeg = FindDamageValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == new ColoredText("x", Colors.White).Color;
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected white when damage matches"); }
        }

        private static void TestDamageNoBaselineUsesDamagePalette()
        {
            _testsRun++;
            Console.WriteLine("--- TestDamageNoBaselineUsesDamagePalette ---");
            var w = new WeaponItem("W", 1, 12, 0.6, WeaponType.Sword) { Rarity = "Rare" };
            string stat = $"Damage: {w.GetTotalDamage()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, w, weaponSpeedBaseline: null);
            var valueSeg = FindDamageValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Damage);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected default damage palette with no baseline"); }
        }

        /// <summary>
        /// Regression: Common items must not skip FormatStatLine styling (inventory is mostly Common).
        /// </summary>
        private static void TestCommonRarityWeaponDamageComparisonStillColored()
        {
            _testsRun++;
            Console.WriteLine("--- TestCommonRarityWeaponDamageComparisonStillColored ---");
            var high = new WeaponItem("High", 1, 15, 1.0, WeaponType.Wand) { Rarity = "Common" };
            var low = new WeaponItem("Low", 1, 5, 1.0, WeaponType.Sword) { Rarity = "Common" };
            string stat = $"Damage: {high.GetTotalDamage()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, high, low);
            var valueSeg = FindDamageValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Success);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: Common rarity must still get compare colors for damage"); }
        }

        private static void TestCommonRarityWeaponSpeedComparisonStillColored()
        {
            _testsRun++;
            Console.WriteLine("--- TestCommonRarityWeaponSpeedComparisonStillColored ---");
            var faster = new WeaponItem("Fast", 1, 10, 0.5, WeaponType.Sword) { Rarity = "Common" };
            var slower = new WeaponItem("Slow", 1, 10, 1.0, WeaponType.Sword) { Rarity = "Common" };
            string stat = $"Speed: {faster.GetTotalAttackSpeed():F2}×";
            var segments = ItemStatFormatter.FormatStatLine(stat, faster, slower);
            var valueSeg = FindSpeedValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Success);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: Common rarity must still get compare colors for speed"); }
        }

        private static void TestArmorHigherThanBaselineGreen()
        {
            _testsRun++;
            Console.WriteLine("--- TestArmorHigherThanBaselineGreen ---");
            var high = new ChestItem("Plate", tier: 1, armor: 10) { Rarity = "Rare" };
            var low = new ChestItem("Coat", tier: 1, armor: 4) { Rarity = "Common" };
            string stat = $"Armor: +{high.GetTotalArmor()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, high, weaponSpeedBaseline: null, armorComparisonBaseline: low);
            var valueSeg = FindArmorValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Success);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected success color when armor is higher than baseline"); }
        }

        private static void TestArmorLowerThanBaselineRed()
        {
            _testsRun++;
            Console.WriteLine("--- TestArmorLowerThanBaselineRed ---");
            var high = new ChestItem("Plate", tier: 1, armor: 10) { Rarity = "Rare" };
            var low = new ChestItem("Coat", tier: 1, armor: 4) { Rarity = "Common" };
            string stat = $"Armor: +{low.GetTotalArmor()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, low, weaponSpeedBaseline: null, armorComparisonBaseline: high);
            var valueSeg = FindArmorValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Error);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected error color when armor is lower than baseline"); }
        }

        private static void TestArmorEqualBaselineWhite()
        {
            _testsRun++;
            Console.WriteLine("--- TestArmorEqualBaselineWhite ---");
            var a = new ChestItem("A", tier: 1, armor: 8) { Rarity = "Rare" };
            var b = new ChestItem("B", tier: 1, armor: 8) { Rarity = "Rare" };
            string stat = $"Armor: +{a.GetTotalArmor()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, a, weaponSpeedBaseline: null, armorComparisonBaseline: b);
            var valueSeg = FindArmorValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == new ColoredText("x", Colors.White).Color;
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected white when armor matches baseline"); }
        }

        private static void TestArmorNoBaselineUsesSuccessPalette()
        {
            _testsRun++;
            Console.WriteLine("--- TestArmorNoBaselineUsesSuccessPalette ---");
            var chest = new ChestItem("Solo", tier: 1, armor: 6) { Rarity = "Common" };
            string stat = $"Armor: +{chest.GetTotalArmor()}";
            var segments = ItemStatFormatter.FormatStatLine(stat, chest, weaponSpeedBaseline: null, armorComparisonBaseline: null);
            var valueSeg = FindArmorValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Success);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected default success palette with no armor baseline"); }
        }

        private static void TestFeetStatsIncludeZeroActionSlots()
        {
            _testsRun++;
            Console.WriteLine("--- TestFeetStatsIncludeZeroActionSlots ---");
            var hero = new Character("Stats", 1);
            var feet = new FeetItem("Shoes", tier: 1, armor: 0) { ExtraActionSlots = 0 };
            var stats = ItemStatFormatter.GetItemStats(feet, hero);
            var ok = stats.Contains("Action slots: +0");
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: feet stats should show +0 action slots"); }
        }

        private static void TestFeetStatsIncludePositiveActionSlots()
        {
            _testsRun++;
            Console.WriteLine("--- TestFeetStatsIncludePositiveActionSlots ---");
            var hero = new Character("Stats", 1);
            var feet = new FeetItem("Striders", tier: 1, armor: 1) { ExtraActionSlots = 2 };
            var stats = ItemStatFormatter.GetItemStats(feet, hero);
            var ok = stats.Contains("Action slots: +2");
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: feet stats should show positive action slots"); }
        }

        private static void TestNonFeetStatsOmitZeroActionSlots()
        {
            _testsRun++;
            Console.WriteLine("--- TestNonFeetStatsOmitZeroActionSlots ---");
            var hero = new Character("Stats", 1);
            var chest = new ChestItem("Vest", tier: 1, armor: 1) { ExtraActionSlots = 0 };
            var stats = ItemStatFormatter.GetItemStats(chest, hero);
            var ok = !stats.Any(s => s.StartsWith("Action slots:", StringComparison.Ordinal));
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: zero-slot non-feet gear should not add an action-slot row"); }
        }

        private static void TestWandWeaponShowsClassActionSlotBonus()
        {
            _testsRun++;
            Console.WriteLine("\n--- TestWandWeaponShowsClassActionSlotBonus ---");
            var hero = new Character("Stats", 1);
            var wand = new WeaponItem("Stick", tier: 1, baseDamage: 5, baseAttackSpeed: 0.5, WeaponType.Wand);
            var stats = ItemStatFormatter.GetItemStats(wand, hero);
            var ok = stats.Contains("Action slots: +1");
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: Wand weapons should show +1 class action slot"); }
        }

        private static void TestActionSlotHigherThanBaselineGreen()
        {
            _testsRun++;
            Console.WriteLine("--- TestActionSlotHigherThanBaselineGreen ---");
            var high = new FeetItem("Striders", tier: 1, armor: 1) { ExtraActionSlots = 2 };
            var low = new FeetItem("Shoes", tier: 1, armor: 1) { ExtraActionSlots = 0 };
            var segments = ItemStatFormatter.FormatStatLine("Action slots: +2", high, weaponSpeedBaseline: null, armorComparisonBaseline: low);
            var valueSeg = FindActionSlotValueSegment(segments);
            var ok = valueSeg != null && valueSeg.Color == ExpectedPaletteColor(ColorPalette.Success);
            if (ok) _testsPassed++; else { _testsFailed++; Console.WriteLine("  FAIL: expected success color when action slots are higher than baseline"); }
        }
    }
}
