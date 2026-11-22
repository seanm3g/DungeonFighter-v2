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
            TestWeaponDamageIsFixedToTheBase();
            TestImplicitScalesWithItemLevel();
            TestCombatImplicitShowsInTooltip();
            TestModifierKeywordNotation();
            TestModifierRulesChangeOtherActions();
            TestCatalogModifiersGrowWithTier();
            TestRarityRollUpKeepsDropRarity();
            TestCommonsHaveOneOrTwoLines();
            TestCommonsRollNoMechanicLines();
            TestLoadedCommonLosesMechanicPrefix();
            TestEveryTierHasItsOwnName();
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
            TestEveryItemHasAffinityAndMaterial();
            TestSlotDomainsGateLines();
            TestOffSlotIsRareSingleAndLower();
            TestFirstMechanicLineServesAffinity();
            TestClassAffinityTiltsLines();
            TestEveryHybridHasLines();
            TestEveryRuleLineParses();
            TestModifiersReachOutOnlyFromEpic();

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
                    // Rebuild 2.1 (D5.6): actions start at Uncommon; a Common has none.
                    bool common = string.Equals(rarity.Name, "Common", StringComparison.OrdinalIgnoreCase);
                    if (lines < rarity.MinLines || lines > cap || hasAction == common)
                    {
                        ok = false;
                        detail = $"{item.Name}: {lines} lines, min {rarity.MinLines}, cap {cap}, action {hasAction}";
                        break;
                    }
                }
                TestBase.AssertTrue(ok, $"{rarity.Name}: min ≤ lines ≤ max, an action from Uncommon up (none on a Common) {detail}", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        /// <summary>User 2026-11-15: damage comes from the base's own range (the tier carries the level), and higher tiers swing faster.</summary>
        private static void TestWeaponDamageIsFixedToTheBase()
        {
            var cfg = GameConfiguration.Instance.LootSystem.ItemGeneration;
            bool inRange = true, speedOk = true;
            string detail = "";
            foreach (int ilvl in new[] { 1, 80 })
            {
                foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = ilvl, Weapon = true, AllowRollUps = false }, 200))
                {
                    var w = (WeaponItem)item;
                    // A few catalog rows share a name; the item came from one of them.
                    var rows = _cache.WeaponData.Where(r => r.Name == w.BaseName && r.Tier == w.Tier).ToList();
                    if (!rows.Any(r => w.BaseDamage >= r.DamageMin && w.BaseDamage <= r.DamageMax))
                    {
                        inRange = false;
                        detail = $"{w.BaseName} ilvl {ilvl}: {w.BaseDamage} outside [{rows[0].DamageMin}-{rows[0].DamageMax}]";
                    }
                    if (!rows.Any(r => Math.Abs(w.BaseAttackSpeed - Math.Round(r.AttackSpeed * (1.0 - cfg.WeaponSpeedPerTier * (r.Tier - 1)), 3)) < 1e-6))
                        speedOk = false;
                }
            }
            TestBase.AssertTrue(inRange, $"weapon damage stays in its base's range at any item level {detail}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(speedOk, "weapon swing time is the base's speed cut per tier", ref _testsRun, ref _testsPassed, ref _testsFailed);

            double Avg(int tier) => _cache.WeaponData.Where(r => r.Tier == tier).Average(r => (r.DamageMin + r.DamageMax) / 2.0);
            bool climbs = Enumerable.Range(2, 4).All(t => Avg(t) > Avg(t - 1));
            TestBase.AssertTrue(climbs, $"each weapon tier hits harder than the last ({string.Join(" < ", Enumerable.Range(1, 5).Select(t => Avg(t).ToString("0")))})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>A modifier with its numbers blanked, so a rolled rule compares to its catalog template.</summary>
        private static string Shape(string? rule) =>
            System.Text.RegularExpressions.Regex.Replace(rule ?? "", @"\{[^}]*\}|-?[0-9]+(\.[0-9]+)?", "#");

        /// <summary>Modifier card notation (2026-11-17): trigger → effect, tags in brackets, actions / stores in caps.</summary>
        private static void TestModifierKeywordNotation()
        {
            string Text(string spec) =>
                string.Concat(ModifierTooltipFormatter.StyleRule(CrossWireRule.Parse(spec, "test")!).Select(s => s.Text));

            string tagas = Text("WHILE IF ACTION CRIT_HEAL -> TAGAS status");
            TestBase.AssertTrue(tagas.Contains("CRIT HEAL  →  ") && tagas.Contains("[status]") && !tagas.Contains("Knack"),
                $"TAGAS reads as ACTION → counts as [tag] ({tagas})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string classTag = Text("WHILE IF CLASS barbarian -> TAGAS ragecaller");
            TestBase.AssertTrue(classTag.Contains("[barbarian]") && classTag.Contains("[ragecaller]"),
                $"class and granted tags both use [tag] ({classTag})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            string mint = Text("ONCONNECT EVERY 2 IF TAG warbrute -> MINT CRISIS 1");
            TestBase.AssertTrue(mint.Contains("  →  ") && mint.Contains("[warbrute]") && mint.Contains("CRISIS"),
                $"triggered rules split trigger → effect ({mint})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var segs = ModifierTooltipFormatter.StyleRule(CrossWireRule.Parse("ONCASH:CRISIS -> APPLY expose 1", "test")!);
            TestBase.AssertTrue(segs.Any(s => s.Text == "expose" && s.Color == ModifierTooltipFormatter.StatusColor)
                    && segs.Any(s => s.Text == "CRISIS" && s.Color == ModifierTooltipFormatter.StoreColor),
                "statuses and stores get their own colors", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var reader = CrossWireRule.Parse("ONCONNECT EVERY 2 IF TAG status -> APPLY brittle 1", "ring")!;
            TestBase.AssertTrue(ModifierTooltipFormatter.ReadsTag(reader, "status") && !ModifierTooltipFormatter.ReadsTag(reader, "focus"),
                "a TAG condition reads that tag only", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCombatImplicitShowsInTooltip()
        {
            bool oldCard = RPGGame.UI.ItemCard.Enabled;
            RPGGame.UI.ItemCard.Enabled = false; // checks the pre-2.2 layout
            try { TestCombatImplicitShowsInTooltipLegacy(); }
            finally { RPGGame.UI.ItemCard.Enabled = oldCard; }
        }

        private static void TestCombatImplicitShowsInTooltipLegacy()
        {
            var item = Roll(new ItemRollRequest { ItemLevel = 20, AllowRollUps = false }, 200)
                .Select(x => x.Item)
                .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.ImplicitRule));
            TestBase.AssertNotNull(item, "some items carry a base modifier", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (item == null)
                return;
            var firstRule = ModifierTooltipFormatter.Rules(item).FirstOrDefault();
            string? expected = firstRule == null ? null : string.Concat(ModifierTooltipFormatter.StyleRule(firstRule).Select(s => s.Text));
            var tooltip = ItemTooltipFormatter.BuildItemTooltipLines(null, item, "Weapon", 60, includeExtendedDetails: false)
                .Select(line => string.Concat(line.Select(seg => seg.Text)))
                .ToList();
            int header = tooltip.FindIndex(l => l.Trim() == "Modifier");
            TestBase.AssertTrue(expected != null && header >= 0 && tooltip.Any(l => l.Contains(expected)),
                $"the base modifier shows without Alt ({expected})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            int requires = tooltip.FindIndex(l => l.StartsWith("Requires", StringComparison.Ordinal));
            int stats = tooltip.FindIndex(l => l.Trim() == "Stats");
            TestBase.AssertTrue(header >= 0 && (requires < 0 || header < requires) && (stats < 0 || header < stats),
                "the modifier sits above requirements and stats", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!tooltip.Any(l => l.Trim() == "Triggers"), "no Triggers section from the base or material",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!tooltip.Any(l => l.Trim() == "Material"),
                "material set info waits for Alt", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Every catalog base has a modifier; tier 1 is one rule, tier 5 has two rules (user 2026-11-15 / 11-17).</summary>
        private static void TestCatalogModifiersGrowWithTier()
        {
            var rows = _cache.WeaponData.Select(w => (w.Name, w.Tier, w.Modifier))
                .Concat(_cache.ArmorData.Select(a => (a.Name, a.Tier, a.Modifier))).ToList();
            var rng = new Random(5);
            var bad = rows.Where(r => BaseModifierRoller.FromCatalog(r.Modifier, rng) == null).ToList();
            TestBase.AssertTrue(bad.Count == 0, $"every base has a modifier that parses ({bad.Count} bad, e.g. {bad.FirstOrDefault().Name})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(rows.Where(r => r.Tier == 1).All(r => !(r.Modifier ?? "").Contains(';')),
                "tier-1 modifiers are a single rule", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(rows.Where(r => r.Tier >= 5).All(r => (r.Modifier ?? "").Contains(';')),
                "tier-5 modifiers carry a second rule", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>A rolled-up item shows the higher rarity and its line count, but its lines roll at the drop rarity.</summary>
        private static void TestRarityRollUpKeepsDropRarity()
        {
            var cfg = GameConfiguration.Instance.LootSystem.ItemGeneration;
            TestBase.AssertTrue(Math.Abs(cfg.RarityRollUp.Chance - 0.02) < 1e-9 && cfg.RarityRollUp.Decay >= 1.0,
                $"rarity roll-up is 2% per step with no decay ({cfg.RarityRollUp.Chance:P0}, x{cfg.RarityRollUp.Decay})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            int up = 0, total = 4000;
            bool ok = true;
            string detail = "";
            foreach (var (item, spec) in Roll(new ItemRollRequest { ItemLevel = 60 }, total))
            {
                if (string.IsNullOrWhiteSpace(spec.DropRarity))
                    continue;
                up++;
                int drop = RarityOrder.IndexOf(spec.DropRarity), shown = RarityOrder.IndexOf(item.Rarity);
                bool linesAtDrop = spec.Lines.All(l => TierOf(l) is not { } t || t.AllowsRarity(drop));
                if (shown <= drop || item.DropRarity != spec.DropRarity || !linesAtDrop)
                {
                    ok = false;
                    detail = $"{item.Name}: {spec.DropRarity} -> {item.Rarity}, {spec.Lines.Count} lines";
                }
            }
            TestBase.AssertTrue(up > 0 && up < total * 0.06, $"about 2% of drops roll up a rarity ({up}/{total})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ok, $"rolled-up items keep their drop rarity for line rolls {detail}", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>User 2026-11-15: a Common has 1 line (its action) or 2 (plus one descriptor suffix).</summary>
        private static void TestCommonsHaveOneOrTwoLines()
        {
            var counts = Roll(new ItemRollRequest { ItemLevel = 30, Rarity = "Common", AllowRollUps = false }, 400)
                .Select(x => x.Spec.Lines.Count - Math.Max(0, x.Item.MinGeneratedActionBonuses - 1)).ToList();
            TestBase.AssertTrue(counts.All(c => c is 1 or 2) && counts.Contains(1) && counts.Contains(2),
                $"Commons roll 1 or 2 lines, both happen ({counts.Count(c => c == 1)} x1, {counts.Count(c => c == 2)} x2)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Playtest 2026-11-21 ("dour Bone Club of Might"): a Common's second line came from the material pool, whose
        /// families are all mechanics (cross-wires, listeners, TUNE…). Commons roll stat lines only (D0.7), at most two.
        /// </summary>
        private static void TestCommonsRollNoMechanicLines()
        {
            if (!CommonItemRules.Enabled)
            {
                TestBase.AssertTrue(true, "commonsArePlain off (skipped)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }
            string? bad = null;
            foreach (var (item, spec) in Roll(new ItemRollRequest { ItemLevel = 60, Rarity = "Common", AllowRollUps = false }, 600))
            {
                int rolled = spec.Lines.Select(l => (l.Family, l.Keyword)).Distinct().Count();
                if (rolled > 2 || spec.Lines.Any(l => l.Pool != ModPools.Stat) || item.Modifications.Any(CommonItemRules.IsMechanic)
                    || item.ActionBonuses.Count > 0 || !string.IsNullOrWhiteSpace(item.ImplicitRule))
                {
                    bad = $"{item.Name}: {string.Join(", ", spec.Lines.Select(l => l.Pool + ":" + l.Keyword))}";
                    break;
                }
            }
            TestBase.AssertTrue(bad == null, $"Commons: at most 2 stat lines, no mechanic, action or modifier {bad}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>A saved Common that rolled a cross-wire prefix loses it on load (and its name drops the word).</summary>
        private static void TestLoadedCommonLosesMechanicPrefix()
        {
            if (!CommonItemRules.Enabled)
            {
                TestBase.AssertTrue(true, "commonsArePlain off (skipped)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }
            var club = new WeaponItem("Club", 1, 3, 1.3, WeaponType.Mace) { Rarity = "Common", Material = "Bone" };
            club.Modifications.Add(new Modification { Name = "Bone", PrefixCategory = "MATERIAL", Effect = "STRENGTH", RolledValue = 2 });
            club.Modifications.Add(new Modification
            {
                Name = "Dour", PrefixCategory = "ADJECTIVE", Effect = CrossWires.AffixEffect, CrossWire = "ONCASH:CRISIS -> MINT DRAG 50%", RolledValue = 1
            });
            club.Name = "Dour Bone Club";
            bool changed = CommonItemRules.MakePlain(club);
            TestBase.AssertTrue(changed && club.Modifications.Count == 1 && club.Modifications[0].Name == "Bone",
                "the cross-wire prefix is stripped, the material stays", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!club.Name.Contains("Dour", StringComparison.OrdinalIgnoreCase),
                $"name drops the stripped word ({club.Name})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>User 2026-11-15: every mod family names each tier differently (level-1 to level-100 versions).</summary>
        private static void TestEveryTierHasItsOwnName()
        {
            var shared = _cache.ItemMods.Families
                .Where(f => f.Tiers.Select(t => t.Keyword).Distinct(StringComparer.OrdinalIgnoreCase).Count() < f.Tiers.Count)
                .Select(f => f.Family).ToList();
            TestBase.AssertTrue(shared.Count == 0, $"every tier of every family has its own name ({string.Join(", ", shared.Take(5))})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestModifierRulesChangeOtherActions()
        {
            var hero = TestDataBuilders.Character().WithName("ModHero").Build();
            var head = new HeadItem("ModHelm", 1, 1);
            hero.Equipment.Head = head;
            var cleave = new Action("Cleave", ActionType.Attack, TargetType.SingleTarget, cooldown: 0, description: "") { Tags = new List<string> { "class", "warrior", "duelist" } };
            var jab = new Action("Jab", ActionType.Attack, TargetType.SingleTarget, cooldown: 0, description: "") { Tags = new List<string> { "rogue" } };

            head.ImplicitRule = "WHILE IF ACTION Cleave -> DAMAGE 12%";
            TestBase.AssertTrue(Math.Abs(CrossWires.Passive(hero, "DAMAGE", null, cleave) - 12) < 1e-9, "named action: +12% damage with Cleave",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CrossWires.Passive(hero, "DAMAGE", null, jab) == 0 && CrossWires.Passive(hero, "DAMAGE") == 0,
                "and nothing for other actions or without a swing", ref _testsRun, ref _testsPassed, ref _testsFailed);

            head.ImplicitRule = "WHILE IF TAG duelist -> SPEED 10%";
            TestBase.AssertTrue(Math.Abs(CrossWires.ActionSpeedPercent(hero, cleave) - 10) < 1e-9 && CrossWires.ActionSpeedPercent(hero, jab) == 0
                && CrossWires.Passive(hero, "SPEED") == 0, "tag: +10% speed with duelist actions only", ref _testsRun, ref _testsPassed, ref _testsFailed);

            head.ImplicitRule = "WHILE IF CLASS warrior -> DAMAGE 6%";
            TestBase.AssertTrue(Math.Abs(CrossWires.Passive(hero, "DAMAGE", null, cleave) - 6) < 1e-9 && CrossWires.Passive(hero, "DAMAGE", null, jab) == 0,
                "class: +6% damage with Warrior actions", ref _testsRun, ref _testsPassed, ref _testsFailed);

            head.ImplicitRule = "WHILE -> POTENCY bleed 20%";
            var foe = TestDataBuilders.Enemy().WithName("PotencyFoe").Build();
            foe.DotPotency = CrossWires.PotencyAll(hero, jab, foe);
            TestBase.AssertTrue(Math.Abs(foe.DotPotencyMultiplier("bleed") - 1.2) < 1e-9 && foe.DotPotencyMultiplier("burn") == 1.0,
                "status: the hero's bleed on a foe ticks +20%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("+20% bleed damage", ItemTriggerBundleDisplay.FormatModifier(head), "status modifier card text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            head.ImplicitRule = "WHILE IF ACTION Shield_Bash -> DAMAGE 12%";
            TestBase.AssertEqual("+12% damage with Shield Bash", ItemTriggerBundleDisplay.FormatModifier(head), "action modifier card text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
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
                    .Concat(item.Modifications.Where(m => m.ModLine != null).Select(m => (m.ModLine!, (m.Effect ?? "") + "|" + (m.CrossWire ?? ""), m.RolledValue)));
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
                foreach (var (item, spec) in Roll(new ItemRollRequest { ItemLevel = 60, Rarity = rarity.Name, AllowRollUps = false }, 120))
                {
                    var kinds = item.StatBonuses.Select(s => s.ModLine).Concat(item.Modifications.Select(m => m.ModLine))
                        .Where(l => l != null && !string.IsNullOrWhiteSpace(l.SuffixKind))
                        .GroupBy(l => l!.Family).Select(g => g.First()!.SuffixKind!).ToList();
                    // Suffixes fit in the line budget (a 1-line Common is only its action); no kind joins beyond the rarity's.
                    int budget = Math.Max(0, Math.Min(must.Count, spec.Lines.Count - Math.Max(1, item.MinGeneratedActionBonuses)));
                    if (kinds.Count != kinds.Distinct().Count() || must.Count(k => kinds.Contains(k)) < budget
                        || kinds.Any(k => !must.Contains(k) && k != SuffixKinds.Rare))
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

            // Rebuild 2.1 (D0.7, D5.6): a Common is stats and a material only: no action, no modifier, no mechanic lines.
            bool plain = Roll(new ItemRollRequest { ItemLevel = 60, Rarity = "Common", AllowRollUps = false }, 400).All(x =>
            {
                var mods = x.Item.Modifications.Where(m => m.ModLine != null).Select(m => m.ModLine!)
                    .Concat(x.Item.StatBonuses.Where(s => s.ModLine != null).Select(s => s.ModLine!)).ToList();
                return mods.All(m => m.Pool != ModPools.Mechanic && m.Pool != ModPools.Action)
                    && x.Item.ActionBonuses.Count == 0 && string.IsNullOrEmpty(x.Item.GearAction)
                    && string.IsNullOrWhiteSpace(x.Item.ImplicitRule);
            });
            TestBase.AssertTrue(plain, "a Common item is stats and a material only (no action, modifier or mechanic line)", ref _testsRun, ref _testsPassed, ref _testsFailed);

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
                // {n} (the slot prefixes' rolled value) reads as the template's minimum.
                var rule = CrossWireRule.Parse(template!.CrossWire.Replace("{n}",
                    Math.Max(1, (int)Math.Round(template.MinValue)).ToString(System.Globalization.CultureInfo.InvariantCulture)), family.Family);
                if (rule == null || (rule.Verb == "MINT" && RPGGame.Data.MaterialBuildsLoader.FindByKeyword(rule.Store) == null))
                    bad.Add(family.Family);
            }
            // Phase 11 (D11.9): the 12 class cross-wires left loot; the 6 class-less ones stay.
            TestBase.AssertTrue(wires >= 6 && bad.Count == 0,
                $"material-pool cross-wires parse and mint real stores ({wires} wires; bad: {string.Join(", ", bad)})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWeaponsLeanToClassMaterials()
        {
            int total = 0, own = 0;
            foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 30, Weapon = true }, 600))
            {
                total++;
                // Phase 11 (D11.8): the material follows the class affinity (a hybrid's 6 parent materials).
                if (ClassAffinity.Materials(item.ClassAffinity).Contains(item.Material, StringComparer.OrdinalIgnoreCase))
                    own++;
            }
            double share = total == 0 ? 0 : own / (double)total;
            TestBase.AssertTrue(share > 0.85, $"weapons mostly take their affinity's materials ({share:P0})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        // ---------- Phase 11 (Documentation/CORE_DESIGN_PASS_2026-11-17.md, D11.2-D11.12) ----------

        private static readonly string[] Rarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        private static IEnumerable<(Item Item, ItemRollSpec Spec)> RollMix(int perRarity, int itemLevel = 60, int seed0 = 50000)
        {
            int k = 0;
            foreach (string r in Rarities)
                foreach (var x in Roll(new ItemRollRequest { ItemLevel = itemLevel, Rarity = r, AllowRollUps = false }, perRarity, seed0 + 10000 * k++))
                    yield return x;
        }

        private static IEnumerable<(ItemModLineInfo Info, ItemModFamily Family)> MechanicLines(Item item)
        {
            var infos = item.Modifications.Select(m => m.ModLine).Concat(item.StatBonuses.Select(b => b.ModLine))
                .Where(l => l != null && (l.Pool == ModPools.Mechanic || l.Pool == ModPools.Material)).Cast<ItemModLineInfo>();
            var seen = new HashSet<string>();
            foreach (var info in infos)
                if (seen.Add(info.Family) && _cache.ItemMods.Find(info.Family) is { } f)
                    yield return (info, f);
        }

        private static void TestEveryItemHasAffinityAndMaterial()
        {
            int total = 0, ok = 0;
            foreach (var (item, spec) in RollMix(60))
            {
                total++;
                if (ClassAffinity.IsAffinity(item.ClassAffinity) && !string.IsNullOrWhiteSpace(item.Material) && spec.Affinity == item.ClassAffinity)
                    ok++;
            }
            TestBase.AssertTrue(total > 0 && ok == total, $"every item has one of the 10 class affinities and a material ({ok}/{total})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var hybrids = RollMix(60).Count(x => ClassAffinity.Hybrid(x.Item.ClassAffinity) != null);
            TestBase.AssertTrue(hybrids > 0, $"some items have a hybrid affinity ({hybrids})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSlotDomainsGateLines()
        {
            var bad = new List<string>();
            int lines = 0;
            foreach (var (item, spec) in RollMix(80))
                foreach (var l in item.Modifications.Select(m => m.ModLine).Concat(item.StatBonuses.Select(b => b.ModLine)).Where(l => l != null))
                {
                    var f = _cache.ItemMods.Find(l!.Family);
                    if (f == null)
                        continue;
                    lines++;
                    if (!l.OffSlot && !f.AllowsSlot(spec.Slot))
                        bad.Add($"{f.Family}@{spec.Slot}");
                }
            TestBase.AssertTrue(lines > 0 && bad.Count == 0, $"lines roll only in their slot domain unless marked off-slot ({lines} lines; bad: {string.Join(", ", bad.Distinct().Take(8))})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var extra = _cache.ItemMods.Find("Extra Action Slot");
            TestBase.AssertTrue(extra != null && extra.AllowsSlot("head") && !extra.AllowsSlot("weapon"),
                "extra action slots are a Head line", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOffSlotIsRareSingleAndLower()
        {
            int items = 0, withOff = 0, multi = 0, lowRarity = 0;
            foreach (var (item, spec) in RollMix(400, 70, 90000))
            {
                items++;
                int n = spec.RollUps.Count(r => r.StartsWith("offslot:", StringComparison.Ordinal));
                if (n > 0)
                    withOff++;
                if (n > 1)
                    multi++;
                if (n > 0 && RarityOrder.IndexOf(item.Rarity) < 2)
                    lowRarity++;
            }
            double share = items == 0 ? 0 : withOff / (double)items;
            Console.WriteLine($"  off-slot lines: {withOff}/{items} items ({share:P1})");
            TestBase.AssertTrue(multi == 0 && lowRarity == 0, $"at most one off-slot line per item, never below Rare (multi {multi}, low {lowRarity})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(withOff > 0 && share < 0.1, $"off-slot lines are rare but happen ({share:P1})", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static string Relation(string affinity, ItemModFamily f, string material)
        {
            var classes = (f.Classes ?? new List<string>())
                .Select(c => c == "material" ? ClassAffinity.ClassOfMaterial(material) ?? "" : c).Where(c => c.Length > 0).ToList();
            if (classes.Count == 0)
                return "neutral";
            if (classes.Contains(affinity))
                return "own";
            return classes.Any(c => ClassAffinity.Related(affinity, c)) ? "related" : "off";
        }

        private static void TestFirstMechanicLineServesAffinity()
        {
            int items = 0, onClass = 0;
            foreach (var (item, spec) in RollMix(120, 60, 120000).Where(x => RarityOrder.IndexOf(x.Item.Rarity) >= 2))
            {
                var first = spec.Lines.Select(l => _cache.ItemMods.Find(l.Family))
                    .FirstOrDefault(f => f != null && (f.Pool == ModPools.Mechanic || f.Pool == ModPools.Material));
                if (first == null)
                    continue;
                items++;
                string rel = Relation(item.ClassAffinity, first, item.Material);
                if (rel is "own" or "related")
                    onClass++;
            }
            double share = items == 0 ? 0 : onClass / (double)items;
            TestBase.AssertTrue(items > 0 && share > 0.8, $"the first mechanic line serves the item's class or a related one ({share:P0} of {items})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClassAffinityTiltsLines()
        {
            var counts = new Dictionary<string, int> { ["own"] = 0, ["related"] = 0, ["off"] = 0, ["neutral"] = 0 };
            foreach (var (item, _) in RollMix(200, 60, 150000).Where(x => RarityOrder.IndexOf(x.Item.Rarity) >= 1))
                foreach (var (_, f) in MechanicLines(item))
                    counts[Relation(item.ClassAffinity, f, item.Material)]++;
            int classed = counts["own"] + counts["related"] + counts["off"];
            double own = classed == 0 ? 0 : counts["own"] / (double)classed;
            double rel = classed == 0 ? 0 : counts["related"] / (double)classed;
            double off = classed == 0 ? 0 : counts["off"] / (double)classed;
            Console.WriteLine($"  mechanic lines by class: own {own:P0}, related {rel:P0}, off {off:P0} (+{counts["neutral"]} neutral; target ~70/20/10)");
            TestBase.AssertTrue(own > rel && own > off && off > 0, $"own-class lines lead, off-class lines stay possible (own {own:P0}, related {rel:P0}, off {off:P0})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEveryHybridHasLines()
        {
            var thin = HybridTrees.All.Select(h => h.Tag)
                .Where(tag => _cache.ItemMods.Families.Count(f => f.Classes?.Contains(tag) == true) < 2).ToList();
            TestBase.AssertTrue(thin.Count == 0, $"every hybrid has at least 2 composite lines (thin: {string.Join(", ", thin)})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Step 6 (D4.2, D6.5): RANK / TAGAS / SETCOUNT base modifiers only from Epic up.</summary>
        private static void TestModifiersReachOutOnlyFromEpic()
        {
            int low = 0, lowBad = 0, high = 0, highReach = 0;
            foreach (string r in new[] { "Uncommon", "Rare", "Epic", "Legendary" })
                foreach (var (item, _) in Roll(new ItemRollRequest { ItemLevel = 60, Rarity = r, AllowRollUps = false }, 200, 70000 + r.Length * 977))
                {
                    var parts = BaseModifierRoller.SplitRules(item.ImplicitRule);
                    bool reaches = parts.Any(BaseModifierRoller.ReachesOut);
                    if (RarityOrder.IndexOf(item.Rarity) < BaseModifierRoller.EpicIndex) { low++; if (reaches) lowBad++; }
                    else { high++; if (reaches) highReach++; }
                }
            TestBase.AssertTrue(low > 0 && lowBad == 0, $"below Epic, base modifiers are self-contained ({lowBad} of {low} reach out)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(high > 0 && highReach > 0, $"from Epic, base modifiers can reach other systems ({highReach} of {high})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEveryRuleLineParses()
        {
            var bad = new List<string>();
            int rules = 0;
            foreach (var family in _cache.ItemMods.Families.Where(f => f.Kind == "mod"))
            {
                var template = (_cache.Modifications ?? new List<Modification>())
                    .FirstOrDefault(m => string.Equals(m.Name?.Trim(), family.Template, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(template?.CrossWire))
                    continue;
                foreach (string part in template!.CrossWire.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    rules++;
                    string text = part.Replace("{n}", Math.Max(1, (int)Math.Round(template.MinValue)).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    if (CrossWireRule.Parse(text, family.Family) == null)
                        bad.Add($"{family.Family}: {text}");
                }
            }
            TestBase.AssertTrue(rules > 40 && bad.Count == 0, $"every item rule line parses ({rules} rules; bad: {string.Join(" | ", bad)})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
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
