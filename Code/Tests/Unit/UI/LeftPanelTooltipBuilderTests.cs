using System;
using System.Linq;
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

            var ampLines = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "stat:amp", 50, 12);
            TestBase.AssertTrue(ampLines.Any(l => l.Contains("per combo step", StringComparison.OrdinalIgnoreCase)),
                "AMP tooltip describes per-step base multiplier",
                ref run, ref passed, ref failed);

            var gearEmpty = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "gear:weapon", 40, 8);
            TestBase.AssertTrue(gearEmpty.Any(l => l.Contains("No item", StringComparison.OrdinalIgnoreCase)),
                "empty weapon slot",
                ref run, ref passed, ref failed);

            var w = new WeaponItem("Rusty", tier: 1, baseDamage: 5, baseAttackSpeed: 1.0, WeaponType.Sword)
            {
                GearAction = "JAB"
            };
            c.Weapon = w;
            var gearW = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "gear:weapon", 50, 30);
            TestBase.AssertTrue(gearW.Any(l => l.Contains("Rusty", StringComparison.Ordinal)),
                "weapon name in tooltip",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(gearW.Any(l => l.Contains("Weapon damage", StringComparison.Ordinal)),
                "weapon stats line",
                ref run, ref passed, ref failed);

            var bag = new WeaponItem("Spare", tier: 1, baseDamage: 3, baseAttackSpeed: 1.0, WeaponType.Sword);
            c.Inventory.Add(bag);
            var invTip = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "inv:0", 50, 30);
            TestBase.AssertTrue(invTip.Any(l => l.Contains("Spare", StringComparison.Ordinal)),
                "bag item tooltip",
                ref run, ref passed, ref failed);

            var heroHp = LeftPanelTooltipBuilder.BuildLines(c, LeftPanelHoverState.Prefix + "hero:hp", 40, 8);
            TestBase.AssertTrue(heroHp.Any(l => l.Contains("HP", StringComparison.Ordinal)),
                "hero hp tooltip",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("LeftPanelTooltipBuilder Tests", run, passed, failed);
        }
    }
}
