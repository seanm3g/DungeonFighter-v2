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
            TestBase.AssertTrue(dmg.Any(l => l.Contains("Total:", StringComparison.Ordinal)),
                "damage mentions Total",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(dmg.Any(l => l.Contains("STR", StringComparison.OrdinalIgnoreCase) || l.Contains("effective", StringComparison.OrdinalIgnoreCase)),
                "damage mentions breakdown",
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
            TestBase.AssertTrue(agiLines.Any(l => l.Contains("Base value: 10", StringComparison.Ordinal)),
                "AGI tooltip shows base value",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(agiLines.Any(l => l.Contains("Attribute-modified value: 12", StringComparison.Ordinal)),
                "AGI tooltip shows attribute-modified value before gear",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(agiLines.Any(l => l.Contains("Gear attribute add:", StringComparison.Ordinal) && l.Contains("flat/catalog/material +4", StringComparison.Ordinal) && l.Contains("suffix +1", StringComparison.Ordinal)),
                "AGI tooltip splits gear attribute add into flat and suffix pieces",
                ref run, ref passed, ref failed);

            var speedLines = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "stat:speed", 90, 30);
            TestBase.AssertTrue(speedLines.Any(l => l.Contains("AGI input: base 10", StringComparison.Ordinal) && l.Contains("gear attributes +5", StringComparison.Ordinal)),
                "Speed tooltip shows AGI base and gear attribute input before equation",
                ref run, ref passed, ref failed);

            var ampLines = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "stat:amp", 50, 24);
            TestBase.AssertTrue(ampLines.Any(l => l.Contains("per combo step", StringComparison.OrdinalIgnoreCase) || l.Contains("Base per combo step", StringComparison.OrdinalIgnoreCase)),
                "AMP tooltip describes per-step base multiplier",
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
            TestBase.AssertTrue(invTip.Any(l => l.StartsWith("Actions:", StringComparison.Ordinal)),
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
