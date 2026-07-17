using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Combat.Formatting;
using RPGGame.Entity.Services;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.ActionInteractionLab;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.ActionInteractionLab
{
    public static class ActionInteractionLabGearTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            ActionLabWeaponFactory_BuildsWithPrefixSuffix(ref run, ref pass, ref fail);
            ActionLabWeaponFactory_BuildsWithMultiplePrefixesAndSuffixes(ref run, ref pass, ref fail);
            ActionLabWeaponFactory_FindIndexMatchesTypeAndTier(ref run, ref pass, ref fail);
            ActionLabArmorFactory_BuildsWithPrefixSuffix(ref run, ref pass, ref fail);
            ActionLabArmorFactory_BuildsWithMultiplePrefixesAndSuffixes(ref run, ref pass, ref fail);
            ActionLabArmorFactory_FindIndexMatchesSlotAndTier(ref run, ref pass, ref fail);
            ActionLabArmorFactory_FilterMapsBodyToChest(ref run, ref pass, ref fail);
            ActionLabGearCatalogFilter_Basics(ref run, ref pass, ref fail);
            ClearLabGear_UnequipsSlot(ref run, ref pass, ref fail);
        }



        internal static void ActionLabWeaponFactory_BuildsWithPrefixSuffix(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var data = new WeaponData
            {
                Type = "Sword",
                Name = "Test Blade",
                BaseDamage = 10,
                AttackSpeed = 1.0,
                Tier = 1,
            };
            var prefix = new Modification
            {
                Name = "Sharp",
                ItemRank = "Uncommon",
                Effect = "damage",
                MinValue = 2,
                MaxValue = 2,
                DiceResult = 5,
            };
            var suffix = new StatBonus { Name = "of Power", StatType = "Damage", Value = 3 };
            var w = ActionLabWeaponFactory.CreateWeapon(data, prefix, suffix);
            TestBase.AssertTrue(w.Name.Contains("Sharp", StringComparison.Ordinal), "prefix in generated name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(w.Name.Contains("of Power", StringComparison.Ordinal), "suffix in generated name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, w.Modifications.Count, "one modification", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, w.StatBonuses.Count, "one stat bonus", ref run, ref passed, ref failed);
        }


        internal static void ActionLabWeaponFactory_BuildsWithMultiplePrefixesAndSuffixes(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var data = new WeaponData
            {
                Type = "Sword",
                Name = "Test Blade",
                BaseDamage = 10,
                AttackSpeed = 1.0,
                Tier = 1,
            };
            var prefixes = new[]
            {
                new Modification
                {
                    Name = "Sharp",
                    ItemRank = "Uncommon",
                    Effect = "damage",
                    MinValue = 1,
                    MaxValue = 1,
                    DiceResult = 5,
                },
                new Modification
                {
                    Name = "Heavy",
                    ItemRank = "Common",
                    Effect = "damage",
                    MinValue = 1,
                    MaxValue = 1,
                    DiceResult = 3,
                },
            };
            var suffixes = new[]
            {
                new StatBonus { Name = "of Power", StatType = "Damage", Value = 2 },
                new StatBonus { Name = "of Speed", StatType = "Agility", Value = 1 },
            };
            var w = ActionLabWeaponFactory.CreateWeapon(data, prefixes, suffixes);
            TestBase.AssertTrue(w.Name.Contains("Sharp", StringComparison.Ordinal), "first prefix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(w.Name.Contains("Heavy", StringComparison.Ordinal), "second prefix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(w.Name.Contains("of Power", StringComparison.Ordinal), "first suffix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(w.Name.Contains("of Speed", StringComparison.Ordinal), "second suffix in name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, w.Modifications.Count, "two modifications", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, w.StatBonuses.Count, "two stat bonuses", ref run, ref passed, ref failed);
        }


        internal static void ActionLabWeaponFactory_FindIndexMatchesTypeAndTier(ref int run, ref int passed, ref int failed)
        {
            var weapons = new[]
            {
                new WeaponData { Type = "Mace", Name = "Crusher", BaseDamage = 5, AttackSpeed = 1.0, Tier = 1 },
                new WeaponData { Type = "Sword", Name = "Steel Sword", BaseDamage = 8, AttackSpeed = 1.0, Tier = 2 },
            };
            var equipped = new WeaponItem("Steel Sword", 2, 8, 1.0, WeaponType.Sword);
            int idx = ActionLabWeaponFactory.FindBestWeaponDataIndex(weapons, equipped);
            TestBase.AssertEqual(1, idx, "FindBestWeaponDataIndex matches type+tier", ref run, ref passed, ref failed);
        }


        internal static void ActionLabArmorFactory_BuildsWithPrefixSuffix(ref int run, ref int passed, ref int failed)
        {
            var data = new ArmorData
            {
                Slot = "head",
                Name = "Test Helm",
                Armor = 5,
                Tier = 2,
            };
            var prefix = new Modification
            {
                Name = "Sturdy",
                ItemRank = "Common",
                Effect = "armor",
                MinValue = 1,
                MaxValue = 1,
                DiceResult = 0,
            };
            var suffix = new StatBonus { Name = "of Warding", StatType = "Armor", Value = 1 };
            var item = ActionLabArmorFactory.CreateArmor(data, prefix, suffix);
            TestBase.AssertTrue(item.Name.Contains("Sturdy", StringComparison.Ordinal), "prefix in generated name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item.Name.Contains("of Warding", StringComparison.Ordinal), "suffix in generated name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, item.Modifications.Count, "one modification", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, item.StatBonuses.Count, "one stat bonus", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item is HeadItem, "head armor type", ref run, ref passed, ref failed);
        }


        internal static void ActionLabArmorFactory_BuildsWithMultiplePrefixesAndSuffixes(ref int run, ref int passed, ref int failed)
        {
            var data = new ArmorData
            {
                Slot = "head",
                Name = "Test Helm",
                Armor = 5,
                Tier = 2,
            };
            var prefixes = new[]
            {
                new Modification
                {
                    Name = "Sturdy",
                    ItemRank = "Common",
                    Effect = "armor",
                    MinValue = 1,
                    MaxValue = 1,
                    DiceResult = 0,
                },
                new Modification
                {
                    Name = "Reinforced",
                    ItemRank = "Uncommon",
                    Effect = "armor",
                    MinValue = 1,
                    MaxValue = 1,
                    DiceResult = 0,
                },
            };
            var suffixes = new[]
            {
                new StatBonus { Name = "of Warding", StatType = "Armor", Value = 1 },
                new StatBonus { Name = "of Health", StatType = "Health", Value = 5 },
            };
            var item = ActionLabArmorFactory.CreateArmor(data, prefixes, suffixes);
            TestBase.AssertTrue(item.Name.Contains("Sturdy", StringComparison.Ordinal), "first prefix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item.Name.Contains("Reinforced", StringComparison.Ordinal), "second prefix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item.Name.Contains("of Warding", StringComparison.Ordinal), "first suffix in name", ref run, ref passed, ref failed);
            TestBase.AssertTrue(item.Name.Contains("of Health", StringComparison.Ordinal), "second suffix in name", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, item.Modifications.Count, "two modifications", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, item.StatBonuses.Count, "two stat bonuses", ref run, ref passed, ref failed);
        }


        internal static void ActionLabArmorFactory_FindIndexMatchesSlotAndTier(ref int run, ref int passed, ref int failed)
        {
            var armors = new[]
            {
                new ArmorData { Slot = "head", Name = "Cap", Armor = 1, Tier = 1 },
                new ArmorData { Slot = "head", Name = "Helm", Armor = 3, Tier = 2 },
            };
            var equipped = new HeadItem("Helm", 2, 3);
            int idx = ActionLabArmorFactory.FindBestArmorDataIndex(armors, equipped);
            TestBase.AssertEqual(1, idx, "FindBestArmorDataIndex matches slot+tier", ref run, ref passed, ref failed);
        }


        internal static void ActionLabArmorFactory_FilterMapsBodyToChest(ref int run, ref int passed, ref int failed)
        {
            var all = new List<ArmorData>
            {
                new ArmorData { Slot = "head", Name = "H", Armor = 1, Tier = 1 },
                new ArmorData { Slot = "chest", Name = "C", Armor = 2, Tier = 1 },
                new ArmorData { Slot = "feet", Name = "F", Armor = 1, Tier = 1 },
            };
            var head = ActionLabArmorFactory.FilterArmorDataForEquipSlot(all, "head");
            TestBase.AssertEqual(1, head.Count, "one head row", ref run, ref passed, ref failed);
            var body = ActionLabArmorFactory.FilterArmorDataForEquipSlot(all, "body");
            TestBase.AssertEqual(1, body.Count, "one chest row for body slot", ref run, ref passed, ref failed);
            TestBase.AssertEqual("chest", body[0].Slot, "body equip maps to chest JSON", ref run, ref passed, ref failed);
        }


        internal static void ActionLabGearCatalogFilter_Basics(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(ActionLabGearCatalogFilter_Basics));
            TestBase.AssertEqual("Common", ActionLabGearCatalogFilter.GetWeaponTierRarityBand(1), "T1 band", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Legendary", ActionLabGearCatalogFilter.GetWeaponTierRarityBand(5), "T5 band", ref run, ref passed, ref failed);
            var wLow = new WeaponData { Tier = 1, Type = "Sword", Name = "Rusty" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wLow, null), "weapon rarity null", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wLow, "Common"), "T1 common", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wLow, "Rare"), "T1 not rare", ref run, ref passed, ref failed);
            var wHigh = new WeaponData { Tier = 5, Type = "Mace", Name = "Smash" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wHigh, "Legendary"), "T5 legendary", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(wHigh, "Mythic"), "T5 mythic", ref run, ref passed, ref failed);
            var mod = new Modification { ItemRank = "Rare", Name = "Keen" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ModificationMatchesRarityFilter(mod, "Rare"), "mod rare", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.ModificationMatchesRarityFilter(mod, "Common"), "mod not common", ref run, ref passed, ref failed);
            var sbOpen = new StatBonus { Name = "of Open", ItemRank = "" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.StatBonusMatchesRarityFilter(sbOpen, "Epic"), "blank suffix rank wildcard", ref run, ref passed, ref failed);
            var sbTagged = new StatBonus { Name = "of Tagged", ItemRank = "Legendary" };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.StatBonusMatchesRarityFilter(sbTagged, "Legendary"), "suffix legendary", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.StatBonusMatchesRarityFilter(sbTagged, "Rare"), "suffix not rare", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.WeaponMatchesTypeFilter(wHigh, "Mace"), "type mace", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.WeaponMatchesTypeFilter(wHigh, "Sword"), "type not sword", ref run, ref passed, ref failed);

            TestBase.AssertEqual("Cloth", ActionLabGearCatalogFilter.GetArmorCatalogClass("Cloth Cap"), "armor class cloth cap", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Studded Leather", ActionLabGearCatalogFilter.GetArmorCatalogClass("Studded Leather Boots"), "armor class studded leather", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Helm", ActionLabGearCatalogFilter.GetArmorCatalogClass("Helm"), "armor class single word", ref run, ref passed, ref failed);
            var aLow = new ArmorData { Slot = "head", Name = "Cloth Cap", Armor = 1, Tier = 1 };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(aLow, null), "armor rarity null", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(aLow, "Common"), "armor T1 common", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(aLow, "Rare"), "armor T1 not rare", ref run, ref passed, ref failed);
            var aHigh = new ArmorData { Slot = "chest", Name = "Plate Mail", Armor = 5, Tier = 5 };
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(aHigh, "Mythic"), "armor T5 mythic", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesClassFilter(aLow, null), "armor class filter null", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ArmorMatchesClassFilter(aLow, "Cloth"), "armor class cloth", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.ArmorMatchesClassFilter(aLow, "Iron"), "armor class not iron", ref run, ref passed, ref failed);

            TestBase.AssertTrue(ActionLabGearCatalogFilter.ItemMatchesTierFilter(3, null), "tier filter null matches", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionLabGearCatalogFilter.ItemMatchesTierFilter(3, 3), "tier filter exact", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionLabGearCatalogFilter.ItemMatchesTierFilter(2, 3), "tier filter mismatch", ref run, ref passed, ref failed);
        }


        internal static void ClearLabGear_UnequipsSlot(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var data = new ArmorData { Slot = "head", Name = "LabClearHelm", Armor = 2, Tier = 1 };
            var head = ActionLabArmorFactory.CreateArmorWithoutAffixes(data);
            var hero = TestDataBuilders.Character().WithName("LabClearGear").Build();
            var combatManager = new CombatManager();
            ActionInteractionLabSession.Begin(hero, combatManager, () => { }, null);
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
            {
                ActionInteractionLabSession.EndSession();
                TestBase.AssertTrue(false, "ClearLabGear session exists", ref run, ref passed, ref failed);
                return;
            }

            lab.ApplyLabGear(head, "head");
            TestBase.AssertTrue(lab.LabPlayer.Head != null, "ClearLabGear pre: head equipped", ref run, ref passed, ref failed);
            lab.ClearLabGear("head");
            TestBase.AssertTrue(lab.LabPlayer.Head == null, "ClearLabGear post: head empty", ref run, ref passed, ref failed);
            ActionInteractionLabSession.EndSession();
        }
    }
}
