using System;
using System.Linq;
using RPGGame.LootReveal;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 2.4 (D4.5): summary, then a staged reveal per item, then Continue (the item stays in the bag).</summary>
    public static class LootRevealTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== LootReveal Tests ===\n");
            _run = _passed = _failed = 0;
            try
            {
                var hero = new Character("Revealer", 3);
                new GameInitializer().InitializeNewGame(hero, new System.Collections.Generic.List<Dungeon>(), 1);
                LootGenerator.Initialize();
                var common = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Plain Cap").Build();
                common.Rarity = "Common";
                var rare = new HeadItem("Fine Crown", tier: 2, armor: 9) { Rarity = "Rare", Material = "Iron" };
                hero.Inventory.Add(common);
                hero.Inventory.Add(rare);

                var session = LootRevealSession.Start(hero, new Item[] { rare, common })!;
                TestBase.AssertTrue(session != null && LootRevealSession.Active == session, "A reveal starts when loot dropped", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(ReferenceEquals(session!.Entries[0].Item, common), "Commons reveal first, the best last", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(session.Entries[0].Fast && session.AwaitingChoice, "A Common shows all its beats at once (fast batch)", ref _run, ref _passed, ref _failed);

                var rareEntry = session.Entries[1];
                TestBase.AssertTrue(rareEntry.Beats[0] == "..." && rareEntry.Beats.Any(b => b.Contains("RARE")), "A Rare gets a beat of anticipation, then its drop", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(rareEntry.Beats.Any(b => b.StartsWith("vs ")), "It compares against what you wear", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(rareEntry.Beats.Any(b => b.Contains("Defense")), $"One line per attribute ({string.Join(" | ", rareEntry.Beats)})", ref _run, ref _passed, ref _failed);

                // Q5: beats are animated on screen (headless and in tests the buffer doesn't type, so beats land at once).
                TestBase.AssertTrue(LootRevealSession.AnimatedBeats, "Reveal beats animate by default (Q5)", ref _run, ref _passed, ref _failed);
                // Q4: Salvage is off. S does nothing, the prompt has no Salvage, a Salvage choice keeps the item.
                TestBase.AssertFalse(LootRevealSession.SalvageEnabled, "Salvage is switched off by default (Q4)", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!LootRevealSession.ChoicePrompt.Contains("Salvage") && session.Entries.All(x => x.Suggested != LootChoice.Salvage),
                    "No Salvage in the prompt or as a suggestion", ref _run, ref _passed, ref _failed);
                TestBase.AssertFalse(session.HandleInput("s"), "S is ignored while Salvage is off", ref _run, ref _passed, ref _failed);
                string first = session.Choose(LootChoice.Salvage);
                TestBase.AssertTrue(hero.Inventory.Contains(common) && first.StartsWith("Kept"), "A Salvage choice keeps the item while Salvage is off", ref _run, ref _passed, ref _failed);
                TestBase.AssertFalse(session.AwaitingChoice, "The Rare stages its beats", ref _run, ref _passed, ref _failed);
                int steps = 0;
                while (!session.AwaitingChoice && steps++ < 20)
                    session.HandleInput("1");
                TestBase.AssertTrue(session.AwaitingChoice, $"Continue walks the beats to the verdict ({steps} presses)", ref _run, ref _passed, ref _failed);
                var oldHead = hero.Head;
                // Playtest 2026-11-21: no Equip / Keep on the reveal; E and K are ignored and Continue keeps the item.
                TestBase.AssertFalse(session.HandleInput("e") || session.HandleInput("k"), "E / K are ignored on the verdict", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(session.CurrentLines().All(l => !l.Contains("[E]") && !l.Contains("[K]")), "No Equip / Keep prompt line", ref _run, ref _passed, ref _failed);
                session.HandleInput("1");
                TestBase.AssertTrue(ReferenceEquals(hero.Head, oldHead) && hero.Inventory.Contains(rare),
                    "Continue leaves the item in the bag and the worn piece on", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(session.IsDone && LootRevealSession.Active == null, "The reveal ends after the last item", ref _run, ref _passed, ref _failed);

                var lines = session.TakeNewLines();
                TestBase.AssertTrue(lines.Any(l => l.StartsWith("Kept")) && lines.All(l => !l.Contains("[E]")), "The screen prints what happened", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(LootRevealSession.Start(hero, Array.Empty<Item>()) == null, "No loot, no reveal", ref _run, ref _passed, ref _failed);

                // The switch back on restores the old discard.
                LootRevealSession.SalvageEnabled = true;
                try
                {
                    var scrap = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("Scrap Cap").Build();
                    scrap.Rarity = "Common";
                    hero.Inventory.Add(scrap);
                    var s2 = LootRevealSession.Start(hero, new Item[] { scrap })!;
                    string salvaged = s2.Choose(LootChoice.Salvage);
                    TestBase.AssertTrue(!hero.Inventory.Contains(scrap) && salvaged.StartsWith("Salvaged"), "Switch on: Salvage removes the item", ref _run, ref _passed, ref _failed);
                }
                finally { LootRevealSession.SalvageEnabled = false; }

                // Mock 3: a piece that drops the Leather set from 3 to 2 loses Lucky: a sidegrade, keep for now.
                var three = new Character("Lucky Three", 3);
                new GameInitializer().InitializeNewGame(three, new System.Collections.Generic.List<Dungeon>(), 1);
                three.UnequipItem("head");
                var boots = new FeetItem("Bronze Slippers", tier: 2, armor: 6) { Rarity = "Common", Material = "Bronze" };
                var entry = LootRevealBuilder.Build(three, boots);
                TestBase.AssertTrue(entry.Verdict == LootVerdict.Sidegrade && entry.Suggested == LootChoice.Keep && entry.VerdictLine.Contains("Lucky"),
                    $"Breaking the Leather set's Lucky is a sidegrade: {entry.VerdictLine}", ref _run, ref _passed, ref _failed);

                // A worse-stat Uncommon that brings an action for an empty strip slot is a sidegrade, not "worse".
                var two = new Character("Two Slots", 3);
                new GameInitializer().InitializeNewGame(two, new System.Collections.Generic.List<Dungeon>(), 1);
                two.ActionLabActionSlotBonus = 1;
                var action = ActionLoader.GetAllActions().FirstOrDefault(a => a.IsComboAction && !two.GetComboActions().Any(c => c.Name == a.Name) && GearActionNames.Resolve(new HeadItem("x") { GearAction = a.Name }).Contains(a.Name));
                if (action != null && ComboSequenceMaxHelper.GetEffectiveMax(two) > two.GetComboActions().Count)
                {
                    var weakHelm = new HeadItem("Thin Hood", tier: 1, armor: 0) { Rarity = "Uncommon", GearAction = action.Name };
                    var e2 = LootRevealBuilder.Build(two, weakHelm);
                    TestBase.AssertTrue(e2.Verdict != LootVerdict.Worse && e2.VerdictLine.Contains("empty strip slot"),
                        $"An action for an empty slot counts: {e2.VerdictLine}", ref _run, ref _passed, ref _failed);
                }
            }
            finally
            {
                LootRevealSession.Clear();
            }
            TestBase.PrintSummary("LootReveal Tests", _run, _passed, _failed);
        }
    }
}
