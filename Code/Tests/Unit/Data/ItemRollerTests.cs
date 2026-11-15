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
            TestPoolWeightsPickLines();
            TestRollUpsCanLiftTiersPastItemLevel();
            TestForcedRarityHolds();
            TestTradeUpConstraints();
            TestNewGameBonusIsArmor();
            TestDefenseIsRolledOnce();
            TestDamageScalesWithItemLevel();
            TestImplicitSurvivesTriggerCleanUp();
            TestImplicitScalesWithItemLevel();
            TestCombatImplicitShowsInTooltip();
            TestNameComposition();
            TestWoodAndClothDrop();
            TestMaterialPoolCrossWiresParse();
            TestSuffixesNeverRepeatPrefixes();
            TestHybridsRollBothParts();
            TestAnimalPackDoubles();
            TestSuffixKindsFollowRarity();
            TestWeaponsLeanToClassMaterials();
            TestActionsComeFromGearPool();
            TestRarerActionsWeighLess();
            TestOffClassArmorMaterialPool();

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
                bool ok = true;
                string detail = "";
                foreach (var (item, spec) in Roll(new ItemRollRequest { ItemLevel = 60, Rarity = rarity.Name, AllowRollUps = false }, 150))
                {
                    int lines = spec.Lines.Count;
                    int cap = Math.Max(rarity.MaxLines, rarity.MinLines) + Math.Max(0, item.MinGeneratedActionBonuses);
                    bool hasAction = spec.Lines.Any(l => l.Pool == ModPools.Action);
                    if (lines < rarity.MinLines || lines > cap || !hasAction)
                    {
                        ok = false;
                        detail = $"{item.Name}: {lines} lines, min {rarity.MinLines}, cap {cap}, action {hasAction}";
                        break;
                    }
                }
                TestBase.AssertTrue(ok, $"{rarity.Name}: min ≤ lines ≤ max, always an action {detail}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestPoolWeightsPickLines()
        {
            // Item level 1: pools with nothing eligible are skipped, so the item still gets its lines.
            bool mechanicAtL1 = _cache.ItemMods.InPool(ModPools.Mechanic).Any(f => f.Tiers.Any(t => t.Level <= 1));
            int short1 = 0, mechanic1 = 0;
            foreach (var (_, spec) in Roll(new ItemRollRequest { ItemLevel = 1, Rarity = "Epic", AllowRollUps = false }, 200))
            {
                if (spec.Lines.Count < 5)
                    short1++;
                mechanic1 += spec.Lines.Count(l => l.Pool == ModPools.Mechanic);
            }
            TestBase.AssertTrue(short1 == 0 && (mechanicAtL1 || mechanic1 == 0),
                $"empty pools are skipped at L1 (short {short1}, mechanic {mechanic1})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // The weights are the dial: only material weight → every weighted line is a material line while one is left.
            var cfg = GameConfiguration.Instance.LootSystem.ItemGeneration;
            var saved = cfg.PoolWeights;
            try
            {
                cfg.PoolWeights = new Dictionary<string, double> { ["material"] = 1, ["stat"] = 0, ["mechanic"] = 0, ["action"] = 0 };
                int material = 0, other = 0;
                foreach (var (_, spec) in Roll(new ItemRollRequest { ItemLevel = 60, Rarity = "Rare", AllowRollUps = false }, 200))
                {
                    material += spec.Lines.Count(l => l.Pool == ModPools.Material);
                    // Suffixes roll by kind, not by pool weight; count only the weighted (prefix) lines.
                    other += spec.Lines.Count(l => (l.Pool == ModPools.Stat || l.Pool == ModPools.Mechanic)
                        && string.IsNullOrWhiteSpace(_cache.ItemMods.Find(l.Family)?.SuffixKind));
                }
                TestBase.AssertTrue(material > 0 && other == 0, $"pool weights decide each line's pool (material {material}, stat+mechanic {other})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                cfg.PoolWeights = saved;
            }
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

        private static void TestCombatImplicitShowsInTooltip()
        {
            var item = Roll(new ItemRollRequest { ItemLevel = 20, AllowRollUps = false }, 200)
                .Select(x => x.Item)
                .FirstOrDefault(i => (i.TriggerBundles ?? new()).Any(b => MaterialTriggerMerge.IsImplicit(i, b)));
            TestBase.AssertNotNull(item, "some bases carry a combat implicit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (item == null)
                return;
            var implicitBundle = item.TriggerBundles.First(b => MaterialTriggerMerge.IsImplicit(item, b));
            string expected = ItemTriggerBundleDisplay.FormatSummary(implicitBundle);
            var tooltip = ItemTooltipFormatter.BuildItemTooltipLines(null, item, "Weapon", 60, includeExtendedDetails: true)
                .Select(line => string.Concat(line.Select(seg => seg.Text)))
                .ToList();
            TestBase.AssertTrue(tooltip.Any(l => l.Contains("Triggers")) && tooltip.Any(l => l.Contains(expected)),
                $"the combat implicit shows in the tooltip ({expected})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestImplicitScalesWithItemLevel()
        {
            var cfg = new ItemGenerationConfig();
            RPGGame.Data.ActionTriggerBundle Amp() => new RPGGame.Data.ActionTriggerBundle
            {
                When = "ONCRITICAL", Count = "1", Scope = "ACTION", Mechanics = "hero_next_action_amp", Value = 10,
                Description = "On crit, your next action gains +10% amp."
            };
            var low = Amp();
            var high = Amp();
            ItemImplicitScaling.Apply(low, 1, cfg);
            ItemImplicitScaling.Apply(high, 60, cfg);
            TestBase.AssertEqual(10.0, low.Value ?? 0, "implicit at item level 1 keeps its catalog value", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue((high.Value ?? 0) > 10, $"percent implicit grows with item level (ilvl 60: {high.Value})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(high.Description!.Contains($"+{high.Value:0}%"), $"description shows the scaled value ({high.Description})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var face = new RPGGame.Data.ActionTriggerBundle { When = "ONCONNECT", Count = "1", Mechanics = "crit_face_min:19", Value = 19 };
            var perStr = new RPGGame.Data.ActionTriggerBundle { When = "WHILE_EQUIPPED", Count = "1", Mechanics = "hero_action_damage", Value = 1, ScaleFrom = "STR" };
            ItemImplicitScaling.Apply(face, 90, cfg);
            ItemImplicitScaling.Apply(perStr, 90, cfg);
            TestBase.AssertEqual(19.0, face.Value ?? 0, "discrete implicits (crit face) do not scale", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, perStr.Value ?? 0, "implicits that already scale with the hero (scaleFrom) do not scale", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNameComposition()
        {
            bool ok = true;
            string sample = "";
            foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 50, Rarity = "Rare" }, 60))
            {
                sample = item.Name;
                int material = item.Name.IndexOf(item.Material, StringComparison.Ordinal);
                int baseAt = item.Name.IndexOf(item.BaseName, StringComparison.Ordinal);
                if (material < 0 || baseAt < material)
                    ok = false;
                // Rebuilding the name must not stack keywords.
                if (ItemGenerator.GenerateItemNameWithBonuses(item) != item.Name)
                    ok = false;
            }
            TestBase.AssertTrue(ok, $"name = prefix + material + base + suffix, stable (e.g. {sample})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // A common-only ("Uncanny") line puts one "Uncanny" at the front; the keyword itself drops it.
            var lucky = new HeadItem("Cap", 1, 5) { BaseName = "Cap", Material = "Iron" };
            lucky.StatBonuses.Add(new StatBonus { Name = "of the Uncanny Ox", StatType = "STR", ModLine = new ItemModLineInfo { Pool = "stat", Affix = "suffix", Tier = 2, Keyword = "of the Uncanny Ox" } });
            lucky.Modifications.Add(new Modification { Name = "Sharp", ModLine = new ItemModLineInfo { Pool = "stat", Affix = "prefix", Tier = 1, Keyword = "Sharp" } });
            TestBase.AssertEqual("Uncanny Sharp Iron Cap of the Ox", ItemGenerator.ComposeRolledItemName(lucky), "Uncanny moves to the front of the name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWoodAndClothDrop()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 10, Weapon = false }, 800))
                seen.Add(item.Material ?? "");
            TestBase.AssertTrue(seen.Contains("Wood") && seen.Contains("Cloth"), "Wood and Cloth drop on armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSuffixesNeverRepeatPrefixes()
        {
            // Suffix stat types and prefix effects that land in the same combat number (AFFIX_PASS 4b).
            var suffixOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ARMOR", "HEALTH BONUS", "MAGIC FIND" };
            var prefixOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Damage", "HIT", "CRIT", "COMBO", "AttackSpeed" };
            var bad = new List<string>();
            IEnumerable<ItemModFamily> Leaves(ItemModFamily f) =>
                string.Equals(f.Kind, "hybrid", StringComparison.OrdinalIgnoreCase)
                    ? (f.Parts ?? new List<string>()).Select(p => _cache.ItemMods.Find(p)).Where(x => x != null).Select(x => x!)
                    : new[] { f };
            foreach (var family in _cache.ItemMods.InPool(ModPools.Stat))
                foreach (var leaf in Leaves(family))
                {
                    if (family.Affix == "suffix" && prefixOnly.Contains(leaf.StatType ?? ""))
                        bad.Add($"{family.Family}:{leaf.StatType}");
                    var template = (_cache.Modifications ?? new List<Modification>())
                        .FirstOrDefault(m => string.Equals(m.Name?.Trim(), leaf.Template, StringComparison.OrdinalIgnoreCase));
                    if (family.Affix == "prefix" && template != null && suffixOnly.Contains(template.Effect ?? ""))
                        bad.Add($"{family.Family}:{template.Effect}");
                    if (family.Affix == "suffix" && leaf.Affix != "suffix" || family.Affix == "prefix" && leaf.Affix != "prefix")
                        bad.Add($"{family.Family} mixes slots ({leaf.Family})");
                }
            TestBase.AssertTrue(bad.Count == 0, $"suffixes never repeat a prefix's combat number ({string.Join(", ", bad)})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHybridsRollBothParts()
        {
            int hybrids = 0;
            bool ok = true;
            string detail = "";
            foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 50, Rarity = "Legendary", AllowRollUps = false }, 400))
            {
                var lines = item.StatBonuses.Where(s => s.ModLine != null).Select(s => (s.ModLine!, s.StatType ?? "", s.Value))
                    .Concat(item.Modifications.Where(m => m.ModLine != null).Select(m => (m.ModLine!, m.Effect ?? "", m.RolledValue)));
                foreach (var group in lines.GroupBy(l => l.Item1.Family))
                {
                    var family = _cache.ItemMods.Find(group.Key);
                    if (family == null || !string.Equals(family.Kind, "hybrid", StringComparison.OrdinalIgnoreCase))
                        continue;
                    hybrids++;
                    if (group.Count() != family.Parts!.Count || group.Select(g => g.Item2).Distinct().Count() != family.Parts.Count)
                    {
                        ok = false;
                        detail = $"{item.Name}: {group.Key} has {group.Count()} lines";
                    }
                }
            }
            TestBase.AssertTrue(hybrids > 0 && ok, $"hybrids roll one line per part, each a different stat ({hybrids} hybrids) {detail}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSuffixKindsFollowRarity()
        {
            var cfg = GameConfiguration.Instance.LootSystem.ItemGeneration;
            bool ok = true;
            string detail = "";
            int threeSuffixes = 0;
            foreach (var rarity in _cache.RarityData)
            {
                var must = cfg.SuffixKindsFor(rarity.Name);
                foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 60, Rarity = rarity.Name, AllowRollUps = false }, 120))
                {
                    var kinds = item.StatBonuses.Select(s => s.ModLine).Concat(item.Modifications.Select(m => m.ModLine))
                        .Where(l => l != null && !string.IsNullOrWhiteSpace(l.SuffixKind))
                        .GroupBy(l => l!.Family).Select(g => g.First()!.SuffixKind!).ToList();
                    if (kinds.Count != kinds.Distinct().Count() || must.Any(k => !kinds.Contains(k)))
                    {
                        ok = false;
                        detail = $"{rarity.Name} {item.Name}: [{string.Join(",", kinds)}]";
                    }
                    if (kinds.Count == 3)
                        threeSuffixes++;
                }
            }
            TestBase.AssertTrue(ok, $"each rarity gets its suffix kinds, at most one per kind {detail}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(threeSuffixes > 0, $"the rarest items carry all three suffixes ({threeSuffixes})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int poeticCommons = Roll(new ItemRollRequest { ItemLevel = 60, Rarity = "Common", AllowRollUps = false }, 1500)
                .Count(x => x.Item.Modifications.Concat<object>(x.Item.StatBonuses).Any(o =>
                    (o is Modification m ? m.ModLine : ((StatBonus)o).ModLine)?.SuffixKind == SuffixKinds.Rare));
            double share = poeticCommons / 1500.0;
            TestBase.AssertTrue(cfg.PoeticSuffixChance > 0 ? share > 0 && share < 3 * cfg.PoeticSuffixChance + 0.01 : share == 0,
                $"commons get a poetic phrase only at the configured chance ({share:P1}, chance {cfg.PoeticSuffixChance:P0})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // A Common item is exactly one action plus one descriptor suffix (beyond any action minimum its base sets).
            bool plain = Roll(new ItemRollRequest { ItemLevel = 60, Rarity = "Common", AllowRollUps = false }, 400).All(x =>
            {
                var mods = x.Item.Modifications.Where(m => m.ModLine != null).Select(m => m.ModLine!)
                    .Concat(x.Item.StatBonuses.Where(s => s.ModLine != null).Select(s => s.ModLine!)).ToList();
                return mods.Count == 1 && mods[0].SuffixKind == SuffixKinds.Common
                    && x.Item.ActionBonuses.Count == Math.Max(1, x.Item.MinGeneratedActionBonuses);
            });
            TestBase.AssertTrue(plain, "a Common item is one descriptor suffix plus its action", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var named = new HeadItem("Cap", 1, 5) { BaseName = "Cap", Material = "Iron" };
            named.Modifications.Add(new Modification { Name = "Bathed in Blood", ModLine = new ItemModLineInfo { Pool = "mechanic", Affix = "suffix", Tier = 1, Keyword = "Bathed in Blood", Family = "Vampiric", SuffixKind = "rare" } });
            named.StatBonuses.Add(new StatBonus { Name = "of the Dire Bear", StatType = "STR", ModLine = new ItemModLineInfo { Pool = "stat", Affix = "suffix", Tier = 7, Keyword = "of the Dire Bear", Family = "Strength", SuffixKind = "uncommon" } });
            named.StatBonuses.Add(new StatBonus { Name = "of Wit", StatType = "INT", ModLine = new ItemModLineInfo { Pool = "stat", Affix = "suffix", Tier = 1, Keyword = "of Wit", Family = "Wit", SuffixKind = "common" } });
            TestBase.AssertEqual("Iron Cap of Wit of the Dire Bear Bathed in Blood", ItemGenerator.ComposeRolledItemName(named),
                "suffixes print descriptor, animal, poetic phrase", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAnimalPackDoubles()
        {
            StatBonus Bear() => new StatBonus { Name = "of the Bear", StatType = "STR", Value = 10, ModLine = new ItemModLineInfo { Pool = "stat", Affix = "suffix", Family = "Strength", Tier = 3, Keyword = "of the Bear", SuffixKind = "uncommon" } };
            var head = new HeadItem("Cap", 1, 5);
            var feet = new FeetItem("Boots", 1, 5);
            head.StatBonuses.Add(Bear());
            var one = AnimalPacks.Count(new Item?[] { head, feet });
            TestBase.AssertEqual(1.0, AnimalPacks.Multiplier(head.StatBonuses[0], one), "one Bear piece: no pack", ref _testsRun, ref _testsPassed, ref _testsFailed);
            feet.StatBonuses.Add(Bear());
            var two = AnimalPacks.Count(new Item?[] { head, feet });
            TestBase.AssertEqual(2.0, AnimalPacks.Multiplier(head.StatBonuses[0], two), "two Bear pieces: Bear lines x2", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var prefix = new StatBonus { StatType = "STR", Value = 10, ModLine = new ItemModLineInfo { Pool = "stat", Affix = "prefix", Family = "Strength" } };
            TestBase.AssertEqual(1.0, AnimalPacks.Multiplier(prefix, two), "only suffix (animal) lines pack", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMaterialPoolCrossWiresParse()
        {
            var bad = new List<string>();
            int wires = 0;
            foreach (var family in _cache.ItemMods.InPool(ModPools.Material))
            {
                var template = (_cache.Modifications ?? new List<Modification>())
                    .FirstOrDefault(m => string.Equals(m.Name?.Trim(), family.Template, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(template?.CrossWire))
                    continue;
                wires++;
                var rule = CrossWireRule.Parse(template!.CrossWire, family.Family);
                if (rule == null || (rule.Verb == "MINT" && RPGGame.Data.MaterialBuildsLoader.FindByKeyword(rule.Store) == null))
                    bad.Add(family.Family);
            }
            TestBase.AssertTrue(wires >= 18 && bad.Count == 0,
                $"material-pool cross-wires parse and mint real stores ({wires} wires; bad: {string.Join(", ", bad)})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestRarerActionsWeighLess()
        {
            int common = LootDataCache.RarityToWeight("Common"), unmarked = LootDataCache.RarityToWeight(null);
            int rare = LootDataCache.RarityToWeight("RARE"), legendary = LootDataCache.RarityToWeight("Legendary");
            TestBase.AssertTrue(common == unmarked && common > rare && rare > legendary && legendary > 0,
                $"rarer actions weigh less on gear (common {common}, rare {rare}, legendary {legendary})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOffClassArmorMaterialPool()
        {
            var mods = _cache.Modifications.Where(m => m.GetPrefixCategory() == ModificationPrefixCategory.Material).ToList();
            var rng = new Random(5);
            var uncommon = Enumerable.Range(0, 3000).Select(_ => ItemMaterialRules.PickNonWeaponMaterial(mods, "Uncommon", rng)).ToList();
            var common = Enumerable.Range(0, 3000).Select(_ => ItemMaterialRules.PickNonWeaponMaterial(mods, "Common", rng)).ToList();
            double unknownShare = uncommon.Count(m => m == "Unknown") / 3000.0;
            TestBase.AssertTrue(unknownShare < 0.2 && unknownShare > 0,
                $"off-class Uncommon armor is no longer always Unknown ({unknownShare:P0})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(uncommon.Any(ItemMaterialRules.IsSetMaterial), "off-class Uncommon armor can take a set material", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!common.Any(m => m is "Unknown" or "Strange" or "Celestial") && !uncommon.Any(m => m is "Strange" or "Celestial"),
                "classless materials stay at or below the item's rarity", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
