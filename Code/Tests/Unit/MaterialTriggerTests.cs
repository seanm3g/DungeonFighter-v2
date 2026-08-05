using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.Tests.Unit
{
    /// <summary>Always-material + material trigger pool coverage.</summary>
    public static class MaterialTriggerTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Material Trigger Tests ===\n");
            _run = _passed = _failed = 0;

            TestEveryMaterialPoolHasAtLeastTwo();
            TestWeaponClassLadder();
            TestEnsureMaterialWeaponForced();
            TestEnsureMaterialArmorAlways();
            TestMaterialTriggerMergePicksFromPool();
            TestPrefixLotteryExcludesMaterial();
            TestItemTypeConverterPreservesMaterialTriggers();
            TestRepairMissingMaterialTriggerFromPrefix();

            TestBase.PrintSummary("Material Trigger Tests", _run, _passed, _failed);
        }

        private static void TestEveryMaterialPoolHasAtLeastTwo()
        {
            TestBase.SetCurrentTestName(nameof(TestEveryMaterialPoolHasAtLeastTwo));
            foreach (var mat in ItemMaterialRules.AllMaterials)
            {
                TestBase.AssertTrue(
                    MaterialTriggerCatalog.TryGetPool(mat, out var pool) && pool.Count >= 2,
                    $"{mat} pool >= 2 (got {(MaterialTriggerCatalog.TryGetPool(mat, out var p) ? p.Count : 0)})",
                    ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestWeaponClassLadder()
        {
            TestBase.SetCurrentTestName(nameof(TestWeaponClassLadder));
            TestBase.AssertEqual("Bone", ItemMaterialRules.ResolveWeaponMaterial(WeaponType.Mace, "Common"), "mace common", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Steel", ItemMaterialRules.ResolveWeaponMaterial(WeaponType.Mace, "Uncommon"), "mace uncommon", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Damascus", ItemMaterialRules.ResolveWeaponMaterial(WeaponType.Mace, "Epic"), "mace epic→rare", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Bronze", ItemMaterialRules.ResolveWeaponMaterial(WeaponType.Sword, "Common"), "sword", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Glass", ItemMaterialRules.ResolveWeaponMaterial(WeaponType.Dagger, "Common"), "dagger", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Willow", ItemMaterialRules.ResolveWeaponMaterial(WeaponType.Wand, "Common"), "wand", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Crystal", ItemMaterialRules.ResolveWeaponMaterial(WeaponType.Wand, "Mythic"), "wand mythic", ref _run, ref _passed, ref _failed);
        }

        private static void TestEnsureMaterialWeaponForced()
        {
            TestBase.SetCurrentTestName(nameof(TestEnsureMaterialWeaponForced));
            var cache = LootDataCache.Load();
            var applier = new LootBonusApplier(cache, new Random(1));
            var weapon = new WeaponItem("Test Mace", 1, 5, 1.0, WeaponType.Mace) { Rarity = "Uncommon" };
            applier.EnsureMaterial(weapon, "Uncommon");
            TestBase.AssertEqual("Steel", weapon.Material, "weapon material", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(weapon.Modifications.Any(m =>
                    m.GetPrefixCategory() == ModificationPrefixCategory.Material
                    && string.Equals(m.Name, "Steel", StringComparison.OrdinalIgnoreCase)),
                "steel mod present", ref _run, ref _passed, ref _failed);
        }

        private static void TestEnsureMaterialArmorAlways()
        {
            TestBase.SetCurrentTestName(nameof(TestEnsureMaterialArmorAlways));
            var cache = LootDataCache.Load();
            var applier = new LootBonusApplier(cache, new Random(2));
            var helm = new HeadItem("Helm", 1, 2) { Rarity = "Common" };
            applier.EnsureMaterial(helm, "Common");
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(helm.Material), "armor material set", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                ItemMaterialRules.AllMaterials.Any(m =>
                    string.Equals(m, helm.Material, StringComparison.OrdinalIgnoreCase)),
                "armor material known", ref _run, ref _passed, ref _failed);
        }

        private static void TestMaterialTriggerMergePicksFromPool()
        {
            TestBase.SetCurrentTestName(nameof(TestMaterialTriggerMergePicksFromPool));

            var weapon = new WeaponItem("Bone Club", 1, 5, 1.0, WeaponType.Mace)
            {
                Material = "Bone",
                Rarity = "Common"
            };
            MaterialTriggerMerge.ClearCatalogTriggerStamp(weapon);
            MaterialTriggerMerge.ApplyMaterialTrigger(weapon, new Random(42));
            TestBase.AssertTrue(
                (weapon.TriggerBundles?.Count ?? 0) + (weapon.EquipEffects?.Count ?? 0) >= 1,
                "material trigger applied", ref _run, ref _passed, ref _failed);

            MaterialTriggerCatalog.TryGetPool("Bone", out var bonePool);
            bool matched = false;
            if (weapon.TriggerBundles != null)
            {
                foreach (var b in weapon.TriggerBundles)
                {
                    foreach (var def in bonePool)
                    {
                        var expected = MaterialTriggerMerge.ToBundle(def);
                        if (string.Equals(expected.When, b.When, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(expected.Mechanics, b.Mechanics, StringComparison.OrdinalIgnoreCase)
                            && Nullable.Equals(expected.Value, b.Value))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                        break;
                }
            }

            TestBase.AssertTrue(matched, "bundle from bone pool", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(bonePool.Count >= 2, "bone pool size", ref _run, ref _passed, ref _failed);
        }

        private static void TestPrefixLotteryExcludesMaterial()
        {
            TestBase.SetCurrentTestName(nameof(TestPrefixLotteryExcludesMaterial));
            var cache = LootDataCache.CreateEmpty();
            cache.Modifications.Add(new Modification { Name = "Qc", PrefixCategory = "QUALITY", ItemRank = "Common", DiceResult = 1, MinValue = 1, MaxValue = 1 });
            cache.Modifications.Add(new Modification { Name = "Ac", PrefixCategory = "ADJECTIVE", ItemRank = "Common", DiceResult = 2, MinValue = 1, MaxValue = 1 });
            cache.Modifications.Add(new Modification { Name = "Mc", PrefixCategory = "MATERIAL", ItemRank = "Common", DiceResult = 3, MinValue = 1, MaxValue = 1 });
            cache.RarityData.Add(new RarityData { Name = "Common", Weight = 1, StatBonuses = 0, ActionBonuses = 0, Modifications = 0 });

            var item = TestDataBuilders.Item().WithName("X").Build();
            var applier = new LootBonusApplier(cache, new Random(0));
            applier.ApplyPrefixSlots(item, 2, null);
            TestBase.AssertTrue(
                item.Modifications.All(m => m.GetPrefixCategory() != ModificationPrefixCategory.Material),
                "prefix lottery has no material", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, item.Modifications.Count, "Q+A only", ref _run, ref _passed, ref _failed);
        }

        private static void TestItemTypeConverterPreservesMaterialTriggers()
        {
            TestBase.SetCurrentTestName(nameof(TestItemTypeConverterPreservesMaterialTriggers));

            var weapon = new WeaponItem("Bone Log", 1, 6, 1.35, WeaponType.Mace)
            {
                Material = "Bone",
                Rarity = "Common",
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    MaterialTriggerMerge.ToBundle(
                        MaterialTriggerCatalog.All.First(d =>
                            string.Equals(d.Material, "Bone", StringComparison.OrdinalIgnoreCase)))
                }
            };
            weapon.Modifications.Add(new Modification
            {
                Name = "Bone",
                PrefixCategory = "MATERIAL",
                ItemRank = "Common"
            });

            var converted = ItemTypeConverter.ConvertItemToProperType(weapon) as WeaponItem;
            TestBase.AssertTrue(converted != null, "converted weapon", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Bone", converted!.Material, "material preserved", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                converted.TriggerBundles != null && converted.TriggerBundles.Count == 1,
                "trigger bundles preserved", ref _run, ref _passed, ref _failed);

            // Legacy path: base Item typed as weapon (no derived fields beyond Type).
            var legacy = new Item(ItemType.Weapon, "Old Blade", 1)
            {
                Material = "Steel",
                WeaponType = WeaponType.Mace,
                Rarity = "Uncommon",
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    MaterialTriggerMerge.ToBundle(
                        MaterialTriggerCatalog.All.First(d =>
                            string.Equals(d.Material, "Steel", StringComparison.OrdinalIgnoreCase)))
                },
                Modifications = new List<Modification>
                {
                    new Modification { Name = "Steel", PrefixCategory = "MATERIAL", ItemRank = "Uncommon" }
                }
            };
            var fromLegacy = ItemTypeConverter.ConvertItemToProperType(legacy) as WeaponItem;
            TestBase.AssertTrue(fromLegacy != null, "legacy→weapon", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Steel", fromLegacy!.Material, "legacy material copied", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                fromLegacy.TriggerBundles != null && fromLegacy.TriggerBundles.Count == 1,
                "legacy triggers copied", ref _run, ref _passed, ref _failed);
        }

        private static void TestRepairMissingMaterialTriggerFromPrefix()
        {
            TestBase.SetCurrentTestName(nameof(TestRepairMissingMaterialTriggerFromPrefix));

            var broken = new WeaponItem("Bone Log", 1, 6, 1.35, WeaponType.Mace)
            {
                Material = "",
                Rarity = "Common",
                TriggerBundles = new List<ActionTriggerBundle>(),
                EquipEffects = new List<ActionTriggerBundle>(),
                Modifications = new List<Modification>
                {
                    new Modification { Name = "Bone", PrefixCategory = "MATERIAL", ItemRank = "Common" }
                }
            };

            var repaired = ItemTypeConverter.ConvertItemToProperType(broken) as WeaponItem;
            TestBase.AssertTrue(repaired != null, "repaired weapon", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Bone", repaired!.Material, "material restored from prefix", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                (repaired.TriggerBundles?.Count ?? 0) + (repaired.EquipEffects?.Count ?? 0) >= 1,
                "material trigger restored", ref _run, ref _passed, ref _failed);

            string summary = ItemTriggerBundleDisplay.FormatSummary(repaired.TriggerBundles![0]);
            TestBase.AssertTrue(
                !string.IsNullOrWhiteSpace(summary) && summary.Contains("—", StringComparison.Ordinal),
                "tooltip summary names material identity", ref _run, ref _passed, ref _failed);
        }
    }
}
