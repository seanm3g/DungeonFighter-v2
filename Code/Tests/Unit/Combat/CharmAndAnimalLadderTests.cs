using System;
using RPGGame;
using RPGGame.Data;
using RPGGame.Entity.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    public static class CharmAndAnimalLadderTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Charm Slot + Animal Ladder Tests ===\n");
            _run = _passed = _failed = 0;

            TestCharmEquipSaveRoundTrip();
            TestCharmAmplifiesMaterialConvertOnly();
            TestAnimalLadderThresholds();
            TestMaterialCountsIgnoreCharm();
            TestTaxonSetBonuses();

            TestBase.PrintSummary("Charm Slot + Animal Ladder Tests", _run, _passed, _failed);
        }

        private static void TestCharmEquipSaveRoundTrip()
        {
            TestBase.SetCurrentTestName(nameof(TestCharmEquipSaveRoundTrip));
            var hero = new Character("CharmHero", 5);
            var charm = CharmsLoader.CreateItemByName("Menagerie Charm");
            TestBase.AssertTrue(charm != null, "catalog has Menagerie Charm", ref _run, ref _passed, ref _failed);
            if (charm == null)
                return;

            TestBase.AssertTrue(hero.TryEquipItem(charm, "charm", out _, out _, ignoreAttributeRequirements: true),
                "equip charm", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(hero.Charm is CharmItem, "charm slot filled", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("animal", (hero.Charm as CharmItem)!.Layer, "layer", ref _run, ref _passed, ref _failed);

            string json = new CharacterSerializer().Serialize(hero);
            TestBase.AssertTrue(json.Contains("Menagerie Charm", StringComparison.OrdinalIgnoreCase)
                || json.Contains("\"charm\"", StringComparison.OrdinalIgnoreCase),
                "serialized charm", ref _run, ref _passed, ref _failed);
        }

        private static void TestCharmAmplifiesMaterialConvertOnly()
        {
            TestBase.SetCurrentTestName(nameof(TestCharmAmplifiesMaterialConvertOnly));
            var hero = new Character("ForgeHero", 5);
            var forge = CharmsLoader.CreateItemByName("Forge Sigil");
            TestBase.AssertTrue(forge != null, "Forge Sigil", ref _run, ref _passed, ref _failed);
            if (forge == null)
                return;
            hero.TryEquipItem(forge, "charm", out _, out _, ignoreAttributeRequirements: true);

            TestBase.AssertEqual(1.5, CharmBonusController.GetMintMultiplier(hero), "mint mult", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1.5, CharmBonusController.GetConvertMultiplier(hero), "convert mult", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1.0, CharmBonusController.GetClassDefenseMultiplier(hero), "class defense untouched", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1.0, CharmBonusController.GetAnimalLadderMultiplier(hero), "animal untouched", ref _run, ref _passed, ref _failed);
        }

        private static void TestAnimalLadderThresholds()
        {
            TestBase.SetCurrentTestName(nameof(TestAnimalLadderThresholds));
            var hero = new Character("BirdHero", 5);

            void EquipBird(string slot, ItemType type, string name)
            {
                Item item = type switch
                {
                    ItemType.Head => new HeadItem(name, 1, 1),
                    ItemType.Chest => new ChestItem(name, 1, 1),
                    ItemType.Legs => new LegsItem(name, 1, 1),
                    ItemType.Feet => new FeetItem(name, 1, 1),
                    _ => new HeadItem(name, 1, 1)
                };
                item.StatBonuses.Add(new StatBonus
                {
                    Name = "of the Hummingbird",
                    Tags = new System.Collections.Generic.List<string> { "bird" }
                });
                AnimalTagHelper.SyncSuffixTags(item);
                string slotKey = slot;
                hero.TryEquipItem(item, slotKey, out _, out _, ignoreAttributeRequirements: true);
            }

            EquipBird("head", ItemType.Head, "BirdHelm");
            TestBase.AssertEqual(0, AnimalSetController.GetGeneralSpeedPct(hero), "1 animal no general", ref _run, ref _passed, ref _failed);

            EquipBird("body", ItemType.Chest, "BirdChest");
            TestBase.AssertTrue(AnimalSetController.CountAnimals(hero) >= 2, "2 animals", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(AnimalSetController.GetGeneralSpeedPct(hero) > 0, "general speed", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(AnimalSetController.HasTaxonSet(hero, "bird"), "bird taxon", ref _run, ref _passed, ref _failed);

            EquipBird("legs", ItemType.Legs, "BirdLegs");
            TestBase.AssertTrue(AnimalSetController.HasSpecificHeighten(hero), "3 hummingbird specific", ref _run, ref _passed, ref _failed);

            var menagerie = CharmsLoader.CreateItemByName("Menagerie Charm");
            if (menagerie != null)
            {
                hero.TryEquipItem(menagerie, "charm", out _, out _, ignoreAttributeRequirements: true);
                AnimalSetController.SyncAnimalActionsToPool(hero);
                bool hasAction = false;
                foreach (var entry in hero.ActionPool)
                {
                    if (entry.action?.Name != null
                        && entry.action.Name.StartsWith("ANIMAL:", StringComparison.OrdinalIgnoreCase))
                    {
                        hasAction = true;
                        break;
                    }
                }
                TestBase.AssertTrue(hasAction, "menagerie unlocks animal action", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestTaxonSetBonuses()
        {
            TestBase.SetCurrentTestName(nameof(TestTaxonSetBonuses));
            var hero = new Character("BeastHero", 5);

            void EquipBeast(string slot, Item item, string suffix)
            {
                item.StatBonuses.Add(new StatBonus
                {
                    Name = suffix,
                    Tags = new System.Collections.Generic.List<string> { "beast" }
                });
                AnimalTagHelper.SyncSuffixTags(item);
                hero.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true);
            }

            TestBase.AssertEqual(1.0, AnimalSetController.GetOutgoingDamageMultiplier(hero), "no set: damage x1", ref _run, ref _passed, ref _failed);
            EquipBeast("head", new HeadItem("WolfHelm", 1, 1), "of the Wolf");
            TestBase.AssertEqual(1.0, AnimalSetController.GetOutgoingDamageMultiplier(hero), "1 beast piece: no bonus", ref _run, ref _passed, ref _failed);
            EquipBeast("body", new ChestItem("BearChest", 1, 1), "of the Bear");
            TestBase.AssertTrue(Math.Abs(AnimalSetController.GetOutgoingDamageMultiplier(hero) - 1.06) < 0.001,
                "2 beast pieces: Ferocity +6% damage", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1.0, AnimalSetController.GetIncomingDamageMultiplier(hero), "beast does not reduce damage taken", ref _run, ref _passed, ref _failed);

            var enemy = new Enemy("Dummy", 1, 100, 5, 5, 5, 5, 0);
            TestBase.AssertEqual(1.0, AnimalSetController.GetOutgoingDamageMultiplier(enemy), "enemies never get set bonuses", ref _run, ref _passed, ref _failed);

            string tip = string.Join("\n", AnimalSetController.DescribeSetBonuses(hero));
            TestBase.AssertTrue(tip.Contains("Active - Beast 2", StringComparison.Ordinal) && tip.Contains("Ferocity", StringComparison.Ordinal),
                "set tooltip lists the active beast bonus", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tip.Contains("Shell 0/2", StringComparison.Ordinal),
                "set tooltip lists inactive taxa with progress", ref _run, ref _passed, ref _failed);
        }

        private static void TestMaterialCountsIgnoreCharm()
        {
            TestBase.SetCurrentTestName(nameof(TestMaterialCountsIgnoreCharm));
            var hero = new Character("IronHero", 5);
            var helm = new HeadItem("IronHelm", 1, 1) { Material = "Iron" };
            hero.TryEquipItem(helm, "head", out _, out _, ignoreAttributeRequirements: true);
            var charm = CharmsLoader.CreateItemByName("Forge Sigil");
            if (charm != null)
            {
                charm.Material = "Iron";
                hero.TryEquipItem(charm, "charm", out _, out _, ignoreAttributeRequirements: true);
            }

            int iron = MaterialSetController.CountEquipped(hero, "Iron");
            TestBase.AssertEqual(1, iron, "charm does not count as material piece", ref _run, ref _passed, ref _failed);
        }
    }
}
