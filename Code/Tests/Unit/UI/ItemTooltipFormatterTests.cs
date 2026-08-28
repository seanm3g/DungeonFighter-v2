using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
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
            wrap.Tags = new System.Collections.Generic.List<string> { "leather", "starter" };

            var lines = ItemTooltipFormatter.BuildItemTooltipLines(hero, wrap, "Inventory", 30);
            string flat = string.Join("\n", lines.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(lines.Count >= 3, "item tooltip has primary sections", ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Wrap", StringComparison.Ordinal),
                "tooltip includes item name",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Common", StringComparison.Ordinal),
                "tooltip includes rarity",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Tags:", StringComparison.Ordinal)
                    && flat.Contains("leather", StringComparison.Ordinal)
                    && flat.Contains("starter", StringComparison.Ordinal),
                "compact tooltip shows item tags",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Stats", StringComparison.Ordinal),
                "tooltip has stats section",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!flat.Contains("Affixes", StringComparison.Ordinal),
                "compact tooltip omits Affixes until Alt",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Hold Alt for more", StringComparison.Ordinal),
                "compact tooltip hints Alt for more",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!flat.Contains("Tier 1", StringComparison.Ordinal),
                "compact tooltip omits tier/level on rarity line",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(flat.Contains("Strength", StringComparison.Ordinal) && flat.Contains("not met", StringComparison.OrdinalIgnoreCase),
                "compact tooltip shows unmet requirements",
                ref run, ref passed, ref failed);

            var extended = ItemTooltipFormatter.BuildItemTooltipLines(hero, wrap, "Inventory", 30, includeExtendedDetails: true);
            string extFlat = string.Join("\n", extended.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(extFlat.Contains("Affixes", StringComparison.Ordinal),
                "Alt tooltip has affixes section",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(extFlat.Contains("Tier 1", StringComparison.Ordinal),
                "Alt tooltip includes tier on rarity line",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(extFlat.Contains("+1 armor on this piece", StringComparison.Ordinal),
                "ARMOR affix explains effect",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!extFlat.Contains("Hold Alt for more", StringComparison.Ordinal),
                "Alt tooltip omits Hold Alt hint",
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

            var triggered = new LegsItem("Tassets", 1, 17)
            {
                Rarity = "Common",
                Level = 1,
                Material = "Iron",
                TriggerBundles = new System.Collections.Generic.List<RPGGame.Data.ActionTriggerBundle>
                {
                    ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(0))
                }
            };
            var trigLines = ItemTooltipFormatter.BuildItemTooltipLines(hero, triggered, "Legs", 30);
            string trigFlat = string.Join("\n", trigLines.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(trigFlat.Contains("Material", StringComparison.Ordinal),
                "tooltip has Material set section",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(trigFlat.Contains("Iron", StringComparison.OrdinalIgnoreCase),
                "tooltip shows Iron set",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!trigFlat.Contains("Triggers", StringComparison.Ordinal),
                "tooltip no longer lists item Triggers",
                ref run, ref passed, ref failed);
            string summary = ItemTriggerBundleDisplay.FormatSummary(triggered.TriggerBundles[0]);
            TestBase.AssertTrue(summary.Contains("Wound Momentum", StringComparison.Ordinal)
                    || summary.Contains("On connect", StringComparison.OrdinalIgnoreCase),
                "FormatSummary readable",
                ref run, ref passed, ref failed);

            string armorDesc = ItemDisplayFormatter.GetModificationEffectDescription(wrap.Modifications[0]);
            TestBase.AssertTrue(armorDesc.Contains("armor", StringComparison.OrdinalIgnoreCase),
                "GetModificationEffectDescription handles ARMOR",
                ref run, ref passed, ref failed);

            // Animal suffix: Triggers section must show Boar identity + body text (not SalvageCharm /
            // empty-after-dash from wrong WHEN×SCOPE×mech match + hover wrap truncation).
            TriggersLoader.ClearCache();
            var boarLegs = new LegsItem("breeches", 1, 0)
            {
                Rarity = "Common",
                Level = 1,
                BaseAgility = 3,
                BaseTechnique = 5,
                ExtraActionSlots = 1
            };
            StatBonusTriggerMerge.ApplySuffixToItem(boarLegs, new StatBonus
            {
                Name = "of the Boar",
                TriggerName = "BoarSuffix",
                Tags = new System.Collections.Generic.List<string> { "beast" },
                Description = "On connect, gain miss salvage for the fight."
            });
            var boarBundle = boarLegs.TriggerBundles.First(b =>
                string.Equals(b.IdentityName, "BoarSuffix", StringComparison.OrdinalIgnoreCase));
            string boarSummary = ItemTriggerBundleDisplay.FormatSummary(boarBundle);
            TestBase.AssertTrue(boarSummary.Contains("Boar", StringComparison.OrdinalIgnoreCase),
                "BoarSuffix summary uses Boar identity name",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!boarSummary.Contains("Salvage Charm", StringComparison.OrdinalIgnoreCase),
                "BoarSuffix summary is not collapsed onto SalvageCharm",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(boarSummary.Contains("miss salvage", StringComparison.OrdinalIgnoreCase)
                    || boarSummary.Contains("On connect", StringComparison.OrdinalIgnoreCase),
                "BoarSuffix summary includes trigger body text after em dash",
                ref run, ref passed, ref failed);
            int dash = boarSummary.IndexOf(" — ", StringComparison.Ordinal);
            TestBase.AssertTrue(dash > 0 && dash + 3 < boarSummary.Length
                    && !string.IsNullOrWhiteSpace(boarSummary.Substring(dash + 3)),
                "BoarSuffix summary has non-empty text after em dash",
                ref run, ref passed, ref failed);

            var boarLines = ItemTooltipFormatter.BuildItemTooltipLines(hero, boarLegs, "Legs", 40);
            string boarFlat = string.Join("\n", boarLines.Select(ColoredTextRenderer.RenderAsPlainText));
            TestBase.AssertTrue(boarFlat.Contains("Triggers", StringComparison.Ordinal),
                "Boar tooltip has Triggers section",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(boarFlat.Contains("miss salvage", StringComparison.OrdinalIgnoreCase),
                "Boar tooltip Triggers section includes salvage text",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!boarFlat.Contains("of the Boar — Trigger", StringComparison.OrdinalIgnoreCase),
                "Boar trigger is not duplicated under Stats",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("ItemTooltipFormatter Tests", run, passed, failed);
        }
    }
}
