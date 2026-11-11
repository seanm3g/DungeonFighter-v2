using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>The store registry (<see cref="Stores"/>): one read / add / spend interface over mints, statuses and resources.</summary>
    public static class StoresTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Stores Tests ===\n");
            _run = _passed = _failed = 0;

            TestEveryMaterialKeywordIsAStore();
            TestMintReadAddSpend();
            TestEnemiesHaveNoMints();
            TestExposeStore();
            TestDotStoresSpendButDoNotAdd();
            TestStackResource();
            TestUnknownStore();

            TestBase.PrintSummary("Stores Tests", _run, _passed, _failed);
        }

        private static void TestEveryMaterialKeywordIsAStore()
        {
            TestBase.SetCurrentTestName(nameof(TestEveryMaterialKeywordIsAStore));
            foreach (var build in MaterialBuildsLoader.GetAll().Where(b => !string.IsNullOrWhiteSpace(b.Keyword)))
            {
                bool ok = Stores.TryGet(build.Keyword, out var def) && def.Kind == StoreKind.Mint && def.CanSpend;
                TestBase.AssertTrue(ok, $"{build.Keyword} ({build.Material}) is a mint store", ref _run, ref _passed, ref _failed);
            }
            TestBase.AssertTrue(Stores.TryGet("rage", out _), "store ids are case-insensitive", ref _run, ref _passed, ref _failed);
        }

        private static void TestMintReadAddSpend()
        {
            TestBase.SetCurrentTestName(nameof(TestMintReadAddSpend));
            var hero = new Character("StoreHero", 5);
            TestBase.AssertEqual(0, Stores.Get(hero, "RAGE"), "empty bank", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, Stores.Add(hero, "RAGE", 4), "add returns amount", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, hero.Effects.GetMaterialKeyword("RAGE"), "add writes the material bank", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(3, Stores.Spend(hero, "RAGE", 3), "spend returns spent", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, Stores.SpendAll(hero, "RAGE"), "spend all takes the rest", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, Stores.Spend(hero, "RAGE", 5), "spend on empty is 0", ref _run, ref _passed, ref _failed);
        }

        private static void TestEnemiesHaveNoMints()
        {
            TestBase.SetCurrentTestName(nameof(TestEnemiesHaveNoMints));
            var enemy = TestDataBuilders.Enemy().WithName("StoreFoe").Build();
            TestBase.AssertEqual(0, Stores.Add(enemy, "RAGE", 3), "enemies cannot hold mints", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, Stores.Get(enemy, "RAGE"), "enemy mint read is 0", ref _run, ref _passed, ref _failed);
        }

        private static void TestExposeStore()
        {
            TestBase.SetCurrentTestName(nameof(TestExposeStore));
            var foe = new Character("ExposeStoreFoe", 5);
            Stores.Add(foe, "expose", 2);
            TestBase.AssertEqual(2, Stores.Get(foe, "expose"), "expose stacks via store", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(foe.ExposeTurns > 0, "expose gets turns", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, Stores.Spend(foe, "expose", 1), "spend one stack", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, Stores.SpendAll(foe, "expose"), "spend the rest", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(ExposeStatus.IsExposed(foe), "expose ends when spent out", ref _run, ref _passed, ref _failed);
        }

        private static void TestDotStoresSpendButDoNotAdd()
        {
            TestBase.SetCurrentTestName(nameof(TestDotStoresSpendButDoNotAdd));
            var foe = new Character("DotStoreFoe", 5);
            foe.BleedIntensity = 3;
            foe.PendingBleedFromHits = 2;
            TestBase.AssertEqual(5, Stores.Get(foe, "bleed"), "bleed counts live + pending", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, Stores.Spend(foe, "bleed", 4), "bleed spends live first, then pending", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, foe.BleedIntensity, "live bleed spent", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, foe.PendingBleedFromHits, "one pending left", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, Stores.Add(foe, "bleed", 2), "DoTs are applied by their handlers, not the registry", ref _run, ref _passed, ref _failed);

            foe.PoisonPercentOfMaxHealth = 1.0;
            TestBase.AssertEqual(4, Stores.Get(foe, "poison"), "poison unit is 0.25% max HP", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, Stores.SpendAll(foe, "poison"), "poison spends", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(foe.PoisonPercentOfMaxHealth == 0, "poison cleared", ref _run, ref _passed, ref _failed);
        }

        private static void TestStackResource()
        {
            TestBase.SetCurrentTestName(nameof(TestStackResource));
            var hero = new Character("ResourceHero", 5);
            Stores.Add(hero, "harden", 2);
            TestBase.AssertEqual(2, Stores.Get(hero, "harden"), "harden via store", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(Stores.DefaultStatusTurns, hero.HardenTurns, "harden turns set", ref _run, ref _passed, ref _failed);
            Stores.SpendAll(hero, "harden");
            TestBase.AssertNull(hero.HardenStacks, "harden cleared when spent out", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, hero.HardenTurns, "harden turns cleared", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, Stores.Add(hero, "stun", 1), "read-only stores refuse adds", ref _run, ref _passed, ref _failed);
        }

        private static void TestUnknownStore()
        {
            TestBase.SetCurrentTestName(nameof(TestUnknownStore));
            var hero = new Character("UnknownStoreHero", 1);
            TestBase.AssertEqual(0, Stores.Get(hero, "NOT_A_STORE"), "unknown store reads 0", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, Stores.Add(hero, "NOT_A_STORE", 1), "unknown store add is 0", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(Stores.TryGet("vulnerability", out _), "Vulnerability is not a separate store (folded into Expose)", ref _run, ref _passed, ref _failed);
        }
    }
}
