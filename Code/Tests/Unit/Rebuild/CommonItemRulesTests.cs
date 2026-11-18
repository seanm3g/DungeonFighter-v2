using System;
using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 2.1 (D0.7, D5.6): Commons are stats and a material; the save migration strips the rest.</summary>
    public static class CommonItemRulesTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CommonItemRules Tests ===\n");
            _run = _passed = _failed = 0;

            // Save migration: a Common loses its modifier and action; an Uncommon keeps both; a starter keeps its action.
            var common = Loaded(new HeadItem("Old Cap", 1, 3) { Rarity = "Common", ImplicitRule = "WHILE -> DAMAGE 5%", GearAction = "BRACE",
                ActionBonuses = new List<ActionBonus> { new() { Name = "BRACE" } }, Material = "Iron" });
            TestBase.AssertTrue(string.IsNullOrEmpty(common.ImplicitRule), "A loaded Common loses its modifier", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(common.ActionBonuses.Count == 0 && string.IsNullOrEmpty(common.GearAction), "A loaded Common loses its action", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Iron", common.Material, "A loaded Common keeps its material", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(3, ((HeadItem)common).Armor, "A loaded Common keeps its stats", ref _run, ref _passed, ref _failed);

            var uncommon = Loaded(new HeadItem("Good Cap", 1, 3) { Rarity = "Uncommon", ImplicitRule = "WHILE -> DAMAGE 5%",
                ActionBonuses = new List<ActionBonus> { new() { Name = "BRACE" } } });
            TestBase.AssertFalse(string.IsNullOrEmpty(uncommon.ImplicitRule), "An Uncommon keeps its modifier", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, uncommon.ActionBonuses.Count, "An Uncommon keeps its action", ref _run, ref _passed, ref _failed);

            var starter = Loaded(new HeadItem("Leather Helmet", 1, 1) { Rarity = "Common", Tags = new List<string> { "starter" }, ImplicitRule = "WHILE -> DAMAGE 5%",
                ActionBonuses = new List<ActionBonus> { new() { Name = "BRACE" } } });
            TestBase.AssertTrue(string.IsNullOrEmpty(starter.ImplicitRule), "A starter Common loses its modifier too (D0.7)", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, starter.ActionBonuses.Count, "A starter keeps its action (D5.6)", ref _run, ref _passed, ref _failed);

            // A new hero's starter gear carries no modifier.
            var hero = new Character("Plain Starter", 1);
            new GameInitializer().InitializeNewGame(hero, new List<Dungeon>(), 1);
            foreach (var item in new Item?[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet })
                if (item != null)
                    TestBase.AssertTrue(string.IsNullOrEmpty(item.ImplicitRule), $"Starter {item.Name} has no modifier", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(hero.GetComboActions().Count >= 1, "The starter weapon still gives the strip its action", ref _run, ref _passed, ref _failed);

            TestBase.PrintSummary("CommonItemRules Tests", _run, _passed, _failed);
        }

        private static Item Loaded(Item item) => ItemTypeConverter.ConvertItemToProperType(item)!;
    }
}
