using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.UI
{
    public static class LeftPanelTooltipBuilderTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== LeftPanelTooltipBuilder Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var empty = LeftPanelTooltipBuilder.BuildLines(null, LeftPanelHoverState.Prefix + "stat:damage", 40, 8);
            TestBase.AssertTrue(empty.Count == 0, "null character -> empty", ref run, ref passed, ref failed);

            var c = new Character("T", 1);
            var badKey = LeftPanelTooltipBuilder.BuildLines(c, "not-a-key", 40, 8);
            TestBase.AssertTrue(badKey.Count == 0, "missing prefix -> empty", ref run, ref passed, ref failed);

            var dmg = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "stat:damage", 50, 20);
            TestBase.AssertTrue(dmg.Count > 0, "damage tooltip has lines", ref run, ref passed, ref failed);
            TestBase.AssertTrue(dmg.Any(l => l.Contains("Attack total", StringComparison.Ordinal)),
                "damage mentions attack total",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(dmg.Any(l => l.Contains("Components", StringComparison.Ordinal)),
                "damage mentions components section",
                ref run, ref passed, ref failed);

            var strLines = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "stat:str", 50, 12);
            TestBase.AssertTrue(strLines.Any(l => l.Contains("Effective", StringComparison.Ordinal)),
                "STR tooltip shows effective",
                ref run, ref passed, ref failed);

            c.Stats.Agility = 10;
            c.Stats.TempAgilityBonus = 2;
            var agileHelm = new HeadItem("Agile Helm", tier: 1, armor: 1)
            {
                BaseAgility = 4
            };
            agileHelm.StatBonuses.Add(new StatBonus { Name = "of Alacrity", StatType = "AGI", Value = 10 });
            c.EquipItem(agileHelm, "head");

            var agiLines = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "stat:agi", 80, 20);
            TestBase.AssertTrue(agiLines.Any(l => l.Contains("Base", StringComparison.Ordinal) && l.Contains("10", StringComparison.Ordinal)),
                "AGI tooltip shows base value",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(agiLines.Any(l => l.Contains("After character mods", StringComparison.Ordinal) && l.Contains("12", StringComparison.Ordinal)),
                "AGI tooltip shows value after character mods",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(agiLines.Any(l => l.Contains("Gear", StringComparison.Ordinal) && l.Contains("+5", StringComparison.Ordinal)),
                "AGI tooltip shows gear section when bonuses present",
                ref run, ref passed, ref failed);

            var speedLines = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "stat:speed", 90, 30);
            TestBase.AssertTrue(speedLines.Any(l => l.Contains("Attack time", StringComparison.Ordinal) && l.Contains("Final", StringComparison.Ordinal)),
                "Speed tooltip title and final",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(speedLines.Any(l => l.Contains("AGI (feeds speed)", StringComparison.Ordinal) && l.Contains("Calculation", StringComparison.Ordinal)),
                "Speed tooltip structured sections",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(speedLines.Any(l => l.Contains("Base", StringComparison.Ordinal) && l.Contains("10", StringComparison.Ordinal)),
                "Speed tooltip shows AGI base in attribute section",
                ref run, ref passed, ref failed);

            var ampLines = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "stat:amp", 50, 24);
            TestBase.AssertTrue(ampLines.Any(l => l.Contains("Base per combo step", StringComparison.Ordinal)),
                "AMP tooltip describes per-step base multiplier",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(ampLines.Any(l => l.Contains("Scaling:", StringComparison.Ordinal) && l.Contains("Combo 4", StringComparison.Ordinal)),
                "AMP tooltip scaling example through combo 4",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(ampLines.Any(l => l.Contains("INT (feeds AMP)", StringComparison.Ordinal)),
                "AMP tooltip TECH section",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(ampLines.Any(l => l.Contains("Slot 1", StringComparison.Ordinal) && l.Contains('×')),
                "AMP tooltip lists per-strip-slot multiplier (tier 1)",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(ampLines.Any(l => l.Contains("Slot 2", StringComparison.Ordinal) && l.Contains('×')),
                "AMP tooltip lists second strip slot",
                ref run, ref passed, ref failed);

            var gearEmpty = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "gear:weapon", 40, 8);
            TestBase.AssertTrue(gearEmpty.Any(l => l.Contains("No item", StringComparison.OrdinalIgnoreCase)),
                "empty weapon slot",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(gearEmpty.Count >= 3 && gearEmpty[1] == "",
                "left-panel hover tooltip inserts a blank line between title and detail",
                ref run, ref passed, ref failed);

            var w = new WeaponItem("Rusty", tier: 1, baseDamage: 5, baseAttackSpeed: 1.0, WeaponType.Sword)
            {
                GearAction = "JAB"
            };
            w.AttributeRequirements = new AttributeRequirements(new System.Collections.Generic.Dictionary<string, int> { ["strength"] = 99 });
            c.Weapon = w;
            var gearW = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "gear:weapon", 50, 30);
            TestBase.AssertTrue(gearW.Any(l => l.Contains("Rusty", StringComparison.Ordinal)),
                "weapon name in tooltip",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(gearW.Any(l => l.Contains("Requires:", StringComparison.Ordinal)),
                "weapon tooltip shows attribute requirements summary",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(gearW.Any(l => l.Contains("Weapon damage", StringComparison.Ordinal)),
                "weapon stats line",
                ref run, ref passed, ref failed);

            var bag = new WeaponItem("Spare", tier: 1, baseDamage: 3, baseAttackSpeed: 1.0, WeaponType.Sword)
            {
                GearAction = "JAB"
            };
            c.Inventory.Add(bag);
            var invTip = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "inv:0", 50, 30);
            TestBase.AssertTrue(invTip.Any(l => l.Contains("Spare", StringComparison.Ordinal)),
                "bag item tooltip",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(invTip.Any(l => l.Contains("Grants:", StringComparison.Ordinal) || l.StartsWith("Actions:", StringComparison.Ordinal)),
                "bag item tooltip lists resolved gear actions",
                ref run, ref passed, ref failed);
            int inventoryNameIndex = invTip.FindIndex(l => l.Contains("Spare", StringComparison.Ordinal));
            TestBase.AssertTrue(inventoryNameIndex >= 0 && inventoryNameIndex + 1 < invTip.Count && invTip[inventoryNameIndex + 1] == "",
                "inventory hover tooltip separates item name from rarity/details",
                ref run, ref passed, ref failed);

            var boots = new FeetItem("Striders", tier: 1, armor: 2) { ExtraActionSlots = 2 };
            c.EquipItem(boots, "feet");
            var feetTip = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "gear:feet", 50, 30);
            TestBase.AssertTrue(feetTip.Any(l => l.Contains("Striders", StringComparison.Ordinal)),
                "feet item name in tooltip",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(feetTip.Any(l => l.Contains("combo strip", StringComparison.OrdinalIgnoreCase) && l.Contains('2')),
                "feet tooltip mentions extra combo strip slots from catalog",
                ref run, ref passed, ref failed);

            var heroHp = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "hero:hp", 40, 8);
            TestBase.AssertTrue(heroHp.Any(l => l.Contains("HP", StringComparison.Ordinal)),
                "hero hp tooltip",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("LeftPanelTooltipBuilder Tests", run, passed, failed);
        }
    }
}
