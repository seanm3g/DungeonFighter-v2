using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data.ItemGeneration;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Item roller (Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md): item level, rarity line counts, tier gates
    /// (item level, rarity: commons own the highest tiers), re-routing, roll-ups, implicits, defense / damage scaling,
    /// request constraints (trade-up, new-game armor) and determinism.
    /// </summary>
    public static class ItemRollerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        private static LootDataCache _cache = null!;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemRoller Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            _cache = LootDataCache.Load();

            TestSameSeedSameItem();
            TestItemLevelIsLootLevel();
            TestLineCountsFollowRarity();
            TestTierGatesItemLevelAndRarity();
            TestCommonsOwnTheHighestTiers();
            TestCommonsRollCommonOnlyTiers();
            TestNoDuplicateFamilies();
            TestReRoutingFillsEmptyPools();
            TestRollUpsCanLiftTiersPastItemLevel();
            TestForcedRarityHolds();
            TestTradeUpConstraints();
            TestNewGameBonusIsArmor();
            TestDefenseIsRolledOnce();
            TestDamageScalesWithItemLevel();
            TestImplicitSurvivesTriggerCleanUp();
            TestNameComposition();
            TestWeaponsLeanToClassMaterials();
            TestActionsComeFromGearPool();

            TestBase.PrintSummary("ItemRoller Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static ItemRoller Roller() => new ItemRoller(_cache);

        private static IEnumerable<(Item Item, ItemRollSpec Spec)> Roll(ItemRollRequest template, int count, int seed0 = 1000)
        {
            var roller = Roller();
            for (int i = 0; i < count; i++)
            {
                var req = new ItemRollRequest
                {
                    HeroLevel = template.HeroLevel,
                    DungeonLevel = template.DungeonLevel,
                    ItemLevel = template.ItemLevel,
                    Weapon = template.Weapon,
                    Slot = template.Slot,
                    Tier = template.Tier,
                    Rarity = template.Rarity,
                    AllowRollUps = template.AllowRollUps,
                    Seed = seed0 + i
                };
                var item = roller.Roll(req, null, out var spec);
                if (item != null)
                    yield return (item, spec);
            }
        }

        private static ItemModTier? TierOf(ItemRollLine line)
        {
            if (line.Pool == ModPools.Action)
                return null;
            var family = _cache.ItemMods.Find(line.Family);
            return family != null && line.Tier >= 1 && line.Tier <= family.Tiers.Count ? family.Tiers[line.Tier - 1] : null;
        }

        private static void TestSameSeedSameItem()
        {
            var a = Roller().Roll(new ItemRollRequest { HeroLevel = 20, DungeonLevel = 22, Seed = 77 }, null, out var sa);
            var b = Roller().Roll(new ItemRollRequest { HeroLevel = 20, DungeonLevel = 22, Seed = 77 }, null, out var sb);
            TestBase.AssertNotNull(a, "an item rolls", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(a?.Name, b?.Name, "same seed, same name", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(string.Join("|", sa.Lines), string.Join("|", sb.Lines), "same seed, same lines", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemLevelIsLootLevel()
        {
            var item = Roller().Roll(new ItemRollRequest { HeroLevel = 48, DungeonLevel = 49, AllowRollUps = false, Seed = 1 }, null, out var spec);
            TestBase.AssertEqual(50, spec.LootLevel, "loot level = dungeon − (hero − dungeon)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(50, item?.Level ?? 0, "item level = loot level (no ±1 roll)", ref _testsRun, ref _testsPassed, ref _testsFailed);

            Roller().Roll(new ItemRollRequest { HeroLevel = 40, DungeonLevel = 5, AllowRollUps = false, Seed = 1 }, null, out var low);
            TestBase.AssertEqual(1, low.ItemLevel, "item level clamps at 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLineCountsFollowRarity()
        {
            foreach (var rarity in _cache.RarityData)
            {
                int minLines = ModPools.All.Sum(p => rarity.PoolRange(p).Min);
                bool ok = true;
                string detail = "";
                foreach (var (item, spec) in Roll(new ItemRollRequest { ItemLevel = 60, Rarity = rarity.Name, AllowRollUps = false }, 150))
                {
                    int planned = spec.PlannedLines.Values.Sum();
                    int cap = Math.Max(rarity.MaxLines, minLines) + Math.Max(0, item.MinGeneratedActionBonuses);
                    if (planned < minLines || planned > cap || spec.Lines.Count > planned)
                    {
                        ok = false;
                        detail = $"{item.Name}: planned {planned}, rolled {spec.Lines.Count}, min {minLines}, cap {cap}";
                        break;
                    }
                }
                TestBase.AssertTrue(ok, $"{rarity.Name}: min ≤ lines ≤ max {detail}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestTierGatesItemLevelAndRarity()
        {
            bool levelOk = true, rarityOk = true;
            foreach (string rarity in new[] { "Common", "Rare", "Legendary" })
            {
                foreach (var (item, spec) in Roll(new ItemRollRequest { ItemLevel = 18, Rarity = rarity, AllowRollUps = false }, 200))
                {
                    foreach (var line in spec.Lines)
                    {
                        var tier = TierOf(line);
                        if (tier == null)
                            continue;
                        if (tier.LevelFor(spec.Slot) > 18)
                            levelOk = false;
                        if (!tier.AllowsRarity(RarityOrder.IndexOf(rarity)))
                            rarityOk = false;
                    }
                }
            }
            TestBase.AssertTrue(levelOk, "no line above the item level (roll-ups off)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(rarityOk, "no line above its tier's rarity requirement", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Data rule from the 11-14 design: at every item level a common can reach the highest value of any stat.</summary>
        private static void TestCommonsOwnTheHighestTiers()
        {
            var failures = new List<string>();
            foreach (var family in _cache.ItemMods.InPool(ModPools.Stat))
            {
                foreach (int level in new[] { 1, 25, 60, 100 })
                {
                    double Best(int rarityIndex) => family.Tiers
                        .Where(t => t.Level <= level && t.AllowsRarity(rarityIndex))
                        .Select(t => t.Max).DefaultIfEmpty(0).Max();
                    double common = Best(0);
                    for (int r = 1; r < RarityOrder.Names.Length; r++)
                        if (Best(r) > common)
                            failures.Add($"{family.Family} L{level}: {RarityOrder.Names[r]} {Best(r)} > Common {common}");
                }
            }
            TestBase.AssertTrue(failures.Count == 0, "commons have the highest possible stat values " + string.Join("; ", failures.Take(3)),
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCommonsRollCommonOnlyTiers()
        {
            int commonOnly = 0;
            foreach (var (_, spec) in Roll(new ItemRollRequest { ItemLevel = 60, Rarity = "Common", AllowRollUps = false }, 1500))
                commonOnly += spec.Lines.Count(l => TierOf(l)?.MaxRarity == "Common");
            TestBase.AssertTrue(commonOnly > 0, $"commons roll common-only tiers ({commonOnly} lines in 1500 items)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNoDuplicateFamilies()
        {
            bool ok = true;
            foreach (var (_, spec) in Roll(new ItemRollRequest { ItemLevel = 80, Rarity = "Mythic" }, 300))
            {
                var fams = spec.Lines.Where(l => l.Pool != ModPools.Action).Select(l => l.Family).ToList();
                if (fams.Count != fams.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                    ok = false;
                var actions = spec.Lines.Where(l => l.Pool == ModPools.Action).Select(l => l.Family).ToList();
                if (actions.Count != actions.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                    ok = false;
            }
            TestBase.AssertTrue(ok, "no family or action twice on one item", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReRoutingFillsEmptyPools()
        {
            // Item level 1: no mechanic family is eligible yet, so planned mechanic lines move to another pool.
            int planned = 0, rerouted = 0;
            foreach (var (_, spec) in Roll(new ItemRollRequest { ItemLevel = 1, Rarity = "Epic", AllowRollUps = false }, 200))
            {
                planned += spec.PlannedLines.GetValueOrDefault(ModPools.Mechanic);
                rerouted += spec.Lines.Count(l => l.From == ModPools.Mechanic);
            }
            bool mechanicAtL1 = _cache.ItemMods.InPool(ModPools.Mechanic).Any(f => f.Tiers.Any(t => t.Level <= 1));
            TestBase.AssertTrue(mechanicAtL1 || (planned > 0 && rerouted == planned),
                $"mechanic lines at L1 re-route (planned {planned}, re-routed {rerouted})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollUpsCanLiftTiersPastItemLevel()
        {
            int lifted = 0, ilvlUps = 0;
            foreach (var (item, spec) in Roll(new ItemRollRequest { HeroLevel = 10, DungeonLevel = 10, Rarity = "Rare" }, 1500))
            {
                lifted += spec.Lines.Count(l => l.RolledUp && TierOf(l) is { } t && t.LevelFor(spec.Slot) > spec.ItemLevel);
                ilvlUps += spec.ItemLevel > spec.LootLevel ? 1 : 0;
            }
            TestBase.AssertTrue(lifted > 0, $"tier roll-ups can pass the item level ({lifted} lines)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ilvlUps > 0, $"item level rolls up sometimes ({ilvlUps}/1500)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestForcedRarityHolds()
        {
            bool ok = Roll(new ItemRollRequest { ItemLevel = 30, Rarity = "Rare" }, 300).All(x => x.Item.Rarity == "Rare");
            TestBase.AssertTrue(ok, "a forced rarity never rolls up", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTradeUpConstraints()
        {
            var item = LootGenerator.GenerateTradeUp(3, 27, weapon: false, "Epic", null);
            TestBase.AssertNotNull(item, "trade-up rolls", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Epic", item?.Rarity, "trade-up rarity is exact", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(27, item?.Level ?? 0, "trade-up item level is exact", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, item?.Tier ?? 0, "trade-up tier is exact", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(item != null && item is not WeaponItem, "trade-up respects armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNewGameBonusIsArmor()
        {
            var hero = TestDataBuilders.CreateTestCharacter();
            bool ok = true;
            for (int i = 0; i < 30; i++)
            {
                var item = LootGenerator.GenerateNewGameBonusLoot(hero);
                if (item == null || item is WeaponItem || item.IsStarterItem)
                    ok = false;
            }
            TestBase.AssertTrue(ok, "new-game bonus is armor and never a starter base", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDefenseIsRolledOnce()
        {
            bool ok = true;
            foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 40, Slot = "chest", AllowRollUps = false }, 50))
            {
                if (item is not ChestItem chest || !chest.DefenseRolled || chest.GetBaseArmor() != chest.Armor)
                    ok = false;
            }
            TestBase.AssertTrue(ok, "armor stores rolled defense and is not rescaled when read", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDamageScalesWithItemLevel()
        {
            double Avg(int ilvl) => Roll(new ItemRollRequest { ItemLevel = ilvl, Weapon = true, Tier = 2, AllowRollUps = false }, 200)
                .Select(x => ((WeaponItem)x.Item).BaseDamage).DefaultIfEmpty(0).Average();
            double low = Avg(1), high = Avg(80);
            TestBase.AssertTrue(high > low, $"weapon damage grows with item level ({low:0.0} → {high:0.0})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestImplicitSurvivesTriggerCleanUp()
        {
            var withImplicit = Roll(new ItemRollRequest { ItemLevel = 20, AllowRollUps = false }, 50)
                .Select(x => x.Item).FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.Implicit));
            TestBase.AssertNotNull(withImplicit, "bases carry implicits", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (withImplicit == null)
                return;
            bool Has(Item i) => (i.TriggerBundles ?? new()).Concat(i.EquipEffects ?? new()).Any(b => MaterialTriggerMerge.IsImplicit(i, b));
            TestBase.AssertTrue(Has(withImplicit), "the implicit's trigger is on the item", ref _testsRun, ref _testsPassed, ref _testsFailed);
            MaterialTriggerMerge.ClearCatalogTriggerStamp(withImplicit);
            MaterialTriggerMerge.RepairMissingMaterialTrigger(withImplicit);
            TestBase.AssertTrue(Has(withImplicit), "the implicit survives material trigger clean-ups (save load)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNameComposition()
        {
            bool ok = true;
            string sample = "";
            foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 50, Rarity = "Rare" }, 60))
            {
                sample = item.Name;
                if (!item.Name.StartsWith(item.Material, StringComparison.Ordinal) || !item.Name.Contains(item.BaseName, StringComparison.Ordinal))
                    ok = false;
                // Rebuilding the name must not stack keywords.
                if (ItemGenerator.GenerateItemNameWithBonuses(item) != item.Name)
                    ok = false;
            }
            TestBase.AssertTrue(ok, $"name = material + keyword + base + keyword, stable (e.g. {sample})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeaponsLeanToClassMaterials()
        {
            int total = 0, own = 0;
            foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 30, Weapon = true }, 600))
            {
                total++;
                if (ItemMaterialRules.TryGetWeaponClassLadder(item.WeaponType, out var c, out var u, out var r)
                    && new[] { c, u, r }.Contains(item.Material, StringComparer.OrdinalIgnoreCase))
                    own++;
            }
            double share = total == 0 ? 0 : own / (double)total;
            TestBase.AssertTrue(share > 0.85, $"weapons mostly take their class's materials ({share:P0})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionsComeFromGearPool()
        {
            var pool = new HashSet<string>(_cache.ActionBonuses.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
            bool ok = Roll(new ItemRollRequest { ItemLevel = 40, Rarity = "Legendary" }, 200)
                .SelectMany(x => x.Item.ActionBonuses)
                .All(a => pool.Contains(a.Name) && RPGGame.Data.GearActionPools.CanRollOnGear(ActionLoader.GetActionData(a.Name)));
            TestBase.AssertTrue(ok, "gear actions come from the hero gear pool (no enemy, environment or starter actions)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
