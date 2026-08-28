using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Animal-suffix combat procs are retired. Leftover <c>triggerName</c> on StatBonuses.json
    /// must not stamp item bundles or set bonuses; MATERIAL BUILDS owns gear procs.
    /// </summary>
    public static class StatBonusAnimalSuffixTriggerTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== StatBonus Animal Suffix Trigger Tests ===\n");
            _run = _passed = _failed = 0;

            TestSuffixAttachDoesNotStampCombatTriggers();
            TestTwoShellSuffixesDoNotGrantSetArmor();
            TestAnimalSuffixRowsKeepLegacyTriggerNameButNoMechanics();

            TestBase.PrintSummary("StatBonus Animal Suffix Trigger Tests", _run, _passed, _failed);
        }

        private static void TestSuffixAttachDoesNotStampCombatTriggers()
        {
            TestBase.SetCurrentTestName(nameof(TestSuffixAttachDoesNotStampCombatTriggers));
            var weapon = new WeaponItem("MergeBlade", 1, 5, 1.0, WeaponType.Sword);
            var suffix = new StatBonus
            {
                Name = "of the Tortoise",
                TriggerName = "TortoiseSuffix",
                Tags = new System.Collections.Generic.List<string> { "shell" }
            };
            weapon.StatBonuses.Add(suffix);

            TestBase.AssertTrue(!GameDataTagHelper.HasTag(weapon.Tags, "shell"),
                "taxon tag not copied onto item", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, weapon.TriggerBundles.Count, "no combat bundles from suffix",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, weapon.EquipEffects.Count, "no equip effects from suffix",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestTwoShellSuffixesDoNotGrantSetArmor()
        {
            TestBase.SetCurrentTestName(nameof(TestTwoShellSuffixesDoNotGrantSetArmor));
            var hero = new Character("ShellHero", 10);
            var helm = new HeadItem("ShellHelm", 1, 2);
            helm.StatBonuses.Add(new StatBonus
            {
                Name = "of the Tortoise",
                TriggerName = "TortoiseSuffix",
                Tags = new System.Collections.Generic.List<string> { "shell" }
            });
            hero.TryEquipItem(helm, "Head", out _, out _, ignoreAttributeRequirements: true);

            var chest = new ChestItem("ShellChest", 1, 2);
            chest.StatBonuses.Add(new StatBonus
            {
                Name = "of the Crab",
                TriggerName = "CrabSuffix",
                Tags = new System.Collections.Generic.List<string> { "shell" }
            });
            hero.TryEquipItem(chest, "Body", out _, out _, ignoreAttributeRequirements: true);

            int armor = ItemEquipEffectApplicator.GetEquippedArmorBonus(hero);
            TestBase.AssertEqual(0, armor, "suffixes do not grant shell set armor",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestAnimalSuffixRowsKeepLegacyTriggerNameButNoMechanics()
        {
            TestBase.SetCurrentTestName(nameof(TestAnimalSuffixRowsKeepLegacyTriggerNameButNoMechanics));
            var cache = LootDataCache.Load();
            var tortoise = cache.StatBonuses.FirstOrDefault(s =>
                string.Equals(s.Name, "of the Tortoise", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(tortoise != null, "tortoise row", ref _run, ref _passed, ref _failed);
            if (tortoise == null)
                return;
            TestBase.AssertEqual("TortoiseSuffix", tortoise.TriggerName, "legacy triggerName kept for deserialize",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, tortoise.EnumerateContributions().Count(), "no flat mechanics",
                ref _run, ref _passed, ref _failed);
        }
    }
}
