using System;
using System.Linq;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 2.2: the item card's reading order: comparison first, one line per attribute, next set step, Alt for details.</summary>
    public static class ItemCardTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemCard Tests ===\n");
            _run = _passed = _failed = 0;
            bool old = ItemCard.Enabled;
            try
            {
                ItemCard.Enabled = true;
                var hero = new Character("Card Reader", 3);
                new GameInitializer().InitializeNewGame(hero, new System.Collections.Generic.List<Dungeon>(), 1);
                var helm = new HeadItem("Iron Cap", tier: 2, armor: 9) { Rarity = "Uncommon", Material = "Iron", ImplicitRule = "WHILE -> DAMAGE 5%" };
                hero.Inventory.Add(helm);

                var compact = Text(ItemTooltipFormatter.BuildItemTooltipLines(hero, helm, "bag", 40, includeExtendedDetails: false));
                int vs = compact.FindIndex(l => l.Contains("vs Leather Helmet:"));
                int mod = compact.FindIndex(l => l.Contains("◆"));
                TestBase.AssertTrue(compact[0].Contains("Iron Cap"), "Line 1 is the name", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(compact[1].Contains("Uncommon head · Iron · T2"), $"Line 2 is rarity · slot · material · tier: {compact[1]}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(vs == 2, $"The comparison comes first after the header: {string.Join(" | ", compact)}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(compact[vs].Contains("Defense"), "One line per attribute (Defense summed)", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(mod > vs, "The modifier follows the comparison", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(compact.Last().Contains("Alt: details"), "Details sit behind the Alt toggle", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(compact.Count <= 9, $"About five lines above the fold ({compact.Count})", ref _run, ref _passed, ref _failed);

                var full = Text(ItemTooltipFormatter.BuildItemTooltipLines(hero, helm, "bag", 80, includeExtendedDetails: true));
                TestBase.AssertTrue(full.Any(l => l.Contains("Details")) && full.Count > compact.Count, "Alt shows the details under the card", ref _run, ref _passed, ref _failed);

                var worn = Text(ItemTooltipFormatter.BuildItemTooltipLines(hero, hero.Body!, "body", 40, includeExtendedDetails: false));
                TestBase.AssertTrue(worn.Any(l => l.Contains("Gives:")), $"A worn piece says what it gives: {string.Join(" | ", worn)}", ref _run, ref _passed, ref _failed);

                var common = new HeadItem("Plain Cap", tier: 1, armor: 2) { Rarity = "Common", Material = "Stone" };
                string? set = ItemCard.SetLine(hero, common, "head");
                TestBase.AssertTrue(set == null, "No set line before material sets arrive, when this piece wouldn't start one", ref _run, ref _passed, ref _failed);
                var leather = new HeadItem("Leather Cap", tier: 1, armor: 2) { Rarity = "Common", Material = "Leather" };
                TestBase.AssertTrue(ItemCard.SetLine(hero, leather, "head")?.StartsWith("Leather ") == true, "A set the hero is wearing shows its next step", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                ItemCard.Enabled = old;
            }
            TestBase.PrintSummary("ItemCard Tests", _run, _passed, _failed);
        }

        private static System.Collections.Generic.List<string> Text(System.Collections.Generic.List<System.Collections.Generic.List<RPGGame.UI.ColorSystem.ColoredText>> lines) =>
            lines.Select(l => string.Concat(l.Select(s => s.Text))).ToList();
    }
}
