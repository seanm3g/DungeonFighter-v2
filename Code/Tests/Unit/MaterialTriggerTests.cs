using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
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
            TestMaterialTriggerMergeDoesNotStampOnLoot();
            TestPrefixLotteryExcludesMaterial();
            TestItemTypeConverterClearsCatalogStampsAndRemapsDamascus();
            TestRepairMissingMaterialDoesNotReroll();

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
            TestBase.AssertEqual("Iron", ItemMaterialRules.ResolveWeaponMaterial(WeaponType.Mace, "Epic"), "mace epic→rare Iron", ref _run, ref _passed, ref _failed);
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

        private static void TestMaterialTriggerMergeDoesNotStampOnLoot()
        {
            TestBase.SetCurrentTestName(nameof(TestMaterialTriggerMergeDoesNotStampOnLoot));

            var cache = LootDataCache.Load();
            var applier = new LootBonusApplier(cache, new Random(42));
            var weapon = new WeaponItem("Bone Club", 1, 5, 1.0, WeaponType.Mace) { Rarity = "Common" };
            applier.ApplyAlwaysMaterialAndTrigger(weapon, "Common");
            TestBase.AssertEqual("Bone", weapon.Material, "material set", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, weapon.TriggerBundles?.Count ?? 0, "no combat stamp", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, weapon.EquipEffects?.Count ?? 0, "no equip stamp", ref _run, ref _passed, ref _failed);
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

        private static void TestItemTypeConverterClearsCatalogStampsAndRemapsDamascus()
        {
            TestBase.SetCurrentTestName(nameof(TestItemTypeConverterClearsCatalogStampsAndRemapsDamascus));

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
            TestBase.AssertEqual(0, converted.TriggerBundles?.Count ?? 0, "catalog combat stamp cleared", ref _run, ref _passed, ref _failed);

            var legacy = new Item(ItemType.Weapon, "Old Blade", 1)
            {
                Material = "Damascus",
                WeaponType = WeaponType.Mace,
                Rarity = "Epic",
                TriggerBundles = new List<ActionTriggerBundle>
                {
                    MaterialTriggerMerge.ToBundle(
                        MaterialTriggerCatalog.All.First(d =>
                            string.Equals(d.Material, "Iron", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(d.Material, "Damascus", StringComparison.OrdinalIgnoreCase)))
                },
                Modifications = new List<Modification>
                {
                    new Modification { Name = "Damascus", PrefixCategory = "MATERIAL", ItemRank = "Epic" }
                }
            };
            var fromLegacy = ItemTypeConverter.ConvertItemToProperType(legacy) as WeaponItem;
            TestBase.AssertTrue(fromLegacy != null, "legacy→weapon", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Iron", fromLegacy!.Material, "Damascus remapped to Iron", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Iron", fromLegacy.Modifications.First(m => m.GetPrefixCategory() == ModificationPrefixCategory.Material).Name,
                "prefix remapped", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, fromLegacy.TriggerBundles?.Count ?? 0, "legacy stamps cleared", ref _run, ref _passed, ref _failed);
        }

        private static void TestRepairMissingMaterialDoesNotReroll()
        {
            TestBase.SetCurrentTestName(nameof(TestRepairMissingMaterialDoesNotReroll));

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
            TestBase.AssertEqual(0, (repaired.TriggerBundles?.Count ?? 0) + (repaired.EquipEffects?.Count ?? 0),
                "does not re-roll a pool proc", ref _run, ref _passed, ref _failed);
        }
    }
}
