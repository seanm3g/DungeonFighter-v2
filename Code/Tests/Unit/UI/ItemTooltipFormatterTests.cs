using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    public static class ItemTooltipFormatterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemTooltipFormatter Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var hero = new Character("T", 1);
            var wrap = new FeetItem("Reinforced Thick Wrap", tier: 1, armor: 3)
            {
                Rarity = "Common",
                Level = 1
            };
            wrap.Modifications.Add(new Modification
            {
                Name = "Reinforced",
                Effect = "ARMOR",
                RolledValue = 1
            });
            wrap.AttributeRequirements = new AttributeRequirements(
                new System.Collections.Generic.Dictionary<string, int> { ["strength"] = 99 });

            var lines = ItemTooltipFormatter.BuildItemTooltipLines(hero, wrap, "Inventory", 30);
            string flat = string.Join("\n", lines.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(lines.Count >= 5, "item tooltip has multiple sections", ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Reinforced Thick Wrap", StringComparison.Ordinal),
                "tooltip includes item name",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Affixes", StringComparison.Ordinal),
                "tooltip has affixes section",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Stats", StringComparison.Ordinal),
                "tooltip has stats section",
                ref run, ref passed, ref failed);

            var sandals = new FeetItem("Sandals", 1, 0)
            {
                BaseTechnique = 3,
                BaseIntelligence = 5,
                Rarity = "Common"
            };
            var sandalLines = ItemTooltipFormatter.BuildItemTooltipLines(hero, sandals, "Inventory", 30);
            string sandalFlat = string.Join("\n", sandalLines.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(sandalFlat.Contains("Technique", StringComparison.Ordinal) && sandalFlat.Contains("+3", StringComparison.Ordinal),
                "tooltip lists catalog Technique",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(sandalFlat.Contains("Intelligence", StringComparison.Ordinal) && sandalFlat.Contains("+5", StringComparison.Ordinal),
                "tooltip lists catalog Intelligence",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("+1 armor on this piece", StringComparison.Ordinal),
                "ARMOR affix explains effect",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Strength", StringComparison.Ordinal) && flat.Contains("not met", StringComparison.OrdinalIgnoreCase),
                "unmet requirements flagged",
                ref run, ref passed, ref failed);

            string armorDesc = ItemDisplayFormatter.GetModificationEffectDescription(wrap.Modifications[0]);
            TestBase.AssertTrue(armorDesc.Contains("armor", StringComparison.OrdinalIgnoreCase),
                "GetModificationEffectDescription handles ARMOR",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("ItemTooltipFormatter Tests", run, passed, failed);
        }
    }
}
