using System;
using System.Linq;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 0.4: one text pipeline for rules; template braces never reach the player.</summary>
    public static class RuleTextTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== RuleText Tests ===\n");
            _run = _passed = _failed = 0;

            Plain_Resolves_Tokens();
            Lines_Describe_Rules_One_Per_Line();
            Every_Skill_Node_Reads_Without_Braces();
            Rolled_Item_Cards_Read_Without_Braces();

            TestBase.PrintSummary("RuleText Tests", _run, _passed, _failed);
        }

        private static void Plain_Resolves_Tokens()
        {
            TestBase.AssertEqual("+8% damage", RuleText.Plain("+{8}% damage"), "{8} at scale 1", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("+0.5% damage", RuleText.Plain("+{8}% damage", 0.0625), "{8} at power 0.0625", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("+10–20% bleed", RuleText.Plain("+{10-20}% bleed"), "{lo-hi} reads as a range", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("deal n damage", RuleText.Plain("deal {n} damage"), "An unrolled {n} loses its braces", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("one line", RuleText.Plain("one\n   line"), "Line breaks collapse to one line", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(RuleText.HasTemplateBraces(RuleText.Plain("{a} {b-c} }{")), "No braces survive", ref _run, ref _passed, ref _failed);
        }

        private static void Lines_Describe_Rules_One_Per_Line()
        {
            var lines = RuleText.Lines("ONHIT -> MINT RAGE 2; WHILE -> DAMAGE 10%", "Test");
            TestBase.AssertEqual(2, lines.Count, "Two rules make two lines", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(lines.All(l => !l.Contains('\n') && !RuleText.HasTemplateBraces(l)), "Each rule is one plain line", ref _run, ref _passed, ref _failed);

            var rolled = RuleText.Lines("WHILE -> DAMAGE {n}%", "Test", rolledValue: 12);
            TestBase.AssertTrue(rolled.Count == 1 && rolled[0].Contains("12"), $"{{n}} takes the rolled value ({string.Join(" | ", rolled)})", ref _run, ref _passed, ref _failed);
        }

        private static void Every_Skill_Node_Reads_Without_Braces()
        {
            var trees = SkillTreeService.Trees;
            var nodes = trees?.Trees.SelectMany(t => t.Nodes).ToList();
            TestBase.AssertTrue(nodes != null && nodes.Count > 0, "Skill trees load", ref _run, ref _passed, ref _failed);
            if (nodes == null)
                return;
            var leaks = nodes.Where(n => RuleText.HasTemplateBraces(n.DisplayEffect) || RuleText.HasTemplateBraces(RuleText.SkillNode(n)))
                .Select(n => n.Id).ToList();
            TestBase.AssertTrue(leaks.Count == 0, $"No skill node shows template braces ({string.Join(", ", leaks.Take(5))})", ref _run, ref _passed, ref _failed);

            var barb = nodes.FirstOrDefault(n => (n.Effect ?? "").Contains("+{8}%"));
            if (barb != null)
                TestBase.AssertTrue(RuleText.SkillNode(barb).Contains('%') && !RuleText.SkillNode(barb).Contains("{8}"),
                    $"The Phase 1 leak (+{{8}}%) now reads as a number: {RuleText.SkillNode(barb)}", ref _run, ref _passed, ref _failed);
        }

        private static void Rolled_Item_Cards_Read_Without_Braces()
        {
            LootGenerator.Initialize();
            var hero = TestDataBuilders.Character().WithName("Reader").WithLevel(10).Build();
            int checkedItems = 0;
            string? leak = null;
            for (int i = 0; i < 150 && leak == null; i++)
            {
                int level = 1 + (i % 30);
                var item = LootGenerator.GenerateLoot(level, level, hero);
                if (item == null)
                    continue;
                checkedItems++;
                var lines = ItemTooltipFormatter.BuildItemTooltipLines(hero, item, "bag", maxLines: 60, includeExtendedDetails: true);
                foreach (var line in lines)
                {
                    string text = string.Concat(line.Select(t => t.Text));
                    if (RuleText.HasTemplateBraces(text))
                    {
                        leak = $"{item.Name}: {text.Trim()}";
                        break;
                    }
                }
            }
            TestBase.AssertTrue(checkedItems > 50, $"Rolled enough items ({checkedItems})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(leak == null, $"No rolled item card shows template braces ({leak})", ref _run, ref _passed, ref _failed);
        }
    }
}
