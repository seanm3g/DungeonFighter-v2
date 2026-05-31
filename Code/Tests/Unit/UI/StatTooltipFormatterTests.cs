using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    public static class StatTooltipFormatterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== StatTooltipFormatter Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestBase.AssertTrue(StatTooltipFormatter.TryBuild(null, "stat:str", 12) == null,
                "null character -> null",
                ref run, ref passed, ref failed);

            var c = new Character("T", 1);
            var str = StatTooltipFormatter.TryBuild(c, "stat:str", 20)!;
            string strFlat = string.Join("\n", str.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(strFlat.Contains("Strength (STR)", StringComparison.Ordinal),
                "STR title",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(strFlat.Contains("Effective:", StringComparison.Ordinal) && strFlat.Contains("6", StringComparison.Ordinal),
                "STR effective highlight",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(strFlat.Contains("Character", StringComparison.Ordinal) && strFlat.Contains("Base", StringComparison.Ordinal),
                "STR character section",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(strFlat.Contains("Sum:", StringComparison.Ordinal),
                "STR sum line",
                ref run, ref passed, ref failed);

            var dmg = StatTooltipFormatter.TryBuild(c, "stat:damage", 24)!;
            string dmgFlat = string.Join("\n", dmg.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(dmgFlat.Contains("Attack total", StringComparison.Ordinal),
                "damage attack total label",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(dmgFlat.Contains("Components", StringComparison.Ordinal),
                "damage components section",
                ref run, ref passed, ref failed);

            var armor = StatTooltipFormatter.TryBuild(c, "stat:armor", 20)!;
            string armorFlat = string.Join("\n", armor.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(armorFlat.Contains("Equipped pieces", StringComparison.Ordinal) && armorFlat.Contains("Head", StringComparison.Ordinal),
                "armor per-slot breakdown",
                ref run, ref passed, ref failed);

            c.Stats.Agility = 10;
            c.Stats.TempAgilityBonus = 2;
            var helm = new HeadItem("Agile Helm", tier: 1, armor: 1) { BaseAgility = 4 };
            helm.StatBonuses.Add(new StatBonus { Name = "of Alacrity", StatType = "AGI", Value = 10 });
            c.EquipItem(helm, "head");

            var agi = StatTooltipFormatter.TryBuild(c, "stat:agi", 24)!;
            string agiFlat = string.Join("\n", agi.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(agiFlat.Contains("After character mods", StringComparison.Ordinal) && agiFlat.Contains("12", StringComparison.Ordinal),
                "AGI after character mods",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(agiFlat.Contains("Gear", StringComparison.Ordinal) && agiFlat.Contains("+5", StringComparison.Ordinal),
                "AGI gear section when bonuses present",
                ref run, ref passed, ref failed);

            var speed = StatTooltipFormatter.TryBuild(c, "stat:speed", 30)!;
            string speedFlat = string.Join("\n", speed.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(speedFlat.Contains("Attack time", StringComparison.Ordinal) && speedFlat.Contains("Final", StringComparison.Ordinal),
                "speed title and final highlight",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(speedFlat.Contains("AGI (feeds speed)", StringComparison.Ordinal) && speedFlat.Contains("Calculation", StringComparison.Ordinal),
                "speed AGI and calculation sections",
                ref run, ref passed, ref failed);

            var amp = StatTooltipFormatter.TryBuild(c, "stat:amp", 24)!;
            string ampFlat = string.Join("\n", amp.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(ampFlat.Contains("Base per combo step", StringComparison.Ordinal) && ampFlat.Contains("INT (feeds AMP)", StringComparison.Ordinal),
                "amp highlight and TECH section",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(ampFlat.Contains("Scaling:", StringComparison.Ordinal)
                    && ampFlat.Contains("Base", StringComparison.Ordinal)
                    && ampFlat.Contains("Combo 4", StringComparison.Ordinal),
                "amp scaling example through combo 4",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(ampFlat.Contains("Slot 1", StringComparison.Ordinal) && ampFlat.Contains('×'),
                "amp strip slot rows",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("StatTooltipFormatter Tests", run, passed, failed);
        }
    }
}
