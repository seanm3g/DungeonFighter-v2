using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.World.Tags;

namespace RPGGame.Tests.Unit
{
    public static class StatBonusAnimalSuffixTriggerTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== StatBonus Animal Suffix Trigger Tests ===\n");
            _run = _passed = _failed = 0;

            TestTaxonTagsRegistered();
            TestCatalogHasAnimalsAndSynergies();
            TestSuffixMergeAddsTagsAndBundles();
            TestStaleSuffixNameStillResolvesTriggers();
            TestShellSetRequiresTwoPieces();
            TestIdentityDedupeAcrossTwoShellPieces();
            TestAnimalSuffixRowsAreTriggerOnly();

            TestBase.PrintSummary("StatBonus Animal Suffix Trigger Tests", _run, _passed, _failed);
        }

        private static void TestTaxonTagsRegistered()
        {
            TestBase.SetCurrentTestName(nameof(TestTaxonTagsRegistered));
            foreach (var t in StatBonusTriggerMerge.TaxonTags)
            {
                TestBase.AssertTrue(TagDefinitions.IsKnownTag(t), $"taxon {t} known", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(TagDefinitions.IsAllowedOn(TagEntityScope.Item, t), $"taxon {t} on item", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestCatalogHasAnimalsAndSynergies()
        {
            TestBase.SetCurrentTestName(nameof(TestCatalogHasAnimalsAndSynergies));
            TestBase.AssertTrue(AnimalSuffixCatalog.All.Count >= 100, "animal defs", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(TriggersLoader.TryGetByName("TortoiseSuffix", out _), "TortoiseSuffix", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(TriggersLoader.TryGetByName("BisonSuffix", out var bison), "BisonSuffix", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("beast", AnimalSuffixCatalog.All.First(a => a.SuffixName == "of the Bison").Taxon,
                "bison taxon", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("ONFIRSTHIT", bison.When, "bison when", ref _run, ref _passed, ref _failed);
            foreach (var taxon in StatBonusTriggerMerge.TaxonTags)
            {
                TestBase.AssertTrue(TriggersLoader.TryGetByName(StatBonusTriggerMerge.TaxonSetFromName(taxon), out _),
                    $"{taxon} SetFrom", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(TriggersLoader.TryGetByName(StatBonusTriggerMerge.TaxonAmpToName(taxon), out _),
                    $"{taxon} AmpTo", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestSuffixMergeAddsTagsAndBundles()
        {
            TestBase.SetCurrentTestName(nameof(TestSuffixMergeAddsTagsAndBundles));
            TriggersLoader.ClearCache();
            var weapon = new WeaponItem("MergeBlade", 1, 5, 1.0, WeaponType.Sword);
            var suffix = new StatBonus
            {
                Name = "of the Tortoise",
                TriggerName = "TortoiseSuffix",
                Tags = new List<string> { "shell" }
            };
            StatBonusTriggerMerge.ApplySuffixToItem(weapon, suffix);
            TestBase.AssertTrue(GameDataTagHelper.HasTag(weapon.Tags, "shell"), "shell tag merged", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(weapon.TriggerBundles.Any(b =>
                    string.Equals(b.IdentityName, "TortoiseSuffix", StringComparison.OrdinalIgnoreCase)),
                "tortoise bundle", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(weapon.TriggerBundles.Any(b =>
                    string.Equals(b.IdentityName, "ShellAmpTo", StringComparison.OrdinalIgnoreCase)),
                "ShellAmpTo merged", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(weapon.EquipEffects.Any(b =>
                    string.Equals(b.IdentityName, "ShellSetFrom", StringComparison.OrdinalIgnoreCase)),
                "ShellSetFrom equip", ref _run, ref _passed, ref _failed);
        }

        private static void TestStaleSuffixNameStillResolvesTriggers()
        {
            TestBase.SetCurrentTestName(nameof(TestStaleSuffixNameStillResolvesTriggers));
            TriggersLoader.ClearCache();
            var weapon = new WeaponItem("StaleOrangutan", 1, 5, 1.0, WeaponType.Wand);
            // Simulate loot from a cache that still had the animal name but no triggerName/tags.
            weapon.StatBonuses.Add(new StatBonus { Name = "of the Orangutan" });
            StatBonusTriggerMerge.RefreshFromItemSuffixes(weapon);
            TestBase.AssertTrue(GameDataTagHelper.HasTag(weapon.Tags, "beast"), "beast tag from catalog", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(weapon.TriggerBundles.Any(b =>
                    string.Equals(b.IdentityName, "OrangutanSuffix", StringComparison.OrdinalIgnoreCase)),
                "OrangutanSuffix resolved", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(weapon.TriggerBundles.Any(b =>
                    string.Equals(b.IdentityName, "BeastAmpTo", StringComparison.OrdinalIgnoreCase)),
                "BeastAmpTo resolved", ref _run, ref _passed, ref _failed);

            var lines = ItemTooltipFormatter.BuildItemTooltipLines(null, weapon, "Inventory", 40);
            string flat = string.Join("\n", lines.Select(l => string.Concat(l.Select(c => c.Text))));
            TestBase.AssertTrue(flat.Contains("Triggers", StringComparison.OrdinalIgnoreCase)
                                || flat.Contains("Orangutan", StringComparison.OrdinalIgnoreCase),
                "tooltip surfaces orangutan trigger", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(flat.Contains("jump", StringComparison.OrdinalIgnoreCase)
                                || flat.Contains("strip", StringComparison.OrdinalIgnoreCase)
                                || flat.Contains("Orangutan", StringComparison.OrdinalIgnoreCase),
                "tooltip has orangutan trigger body text", ref _run, ref _passed, ref _failed);
            var orangutan = weapon.TriggerBundles.First(b =>
                string.Equals(b.IdentityName, "OrangutanSuffix", StringComparison.OrdinalIgnoreCase));
            string orangSummary = ItemTriggerBundleDisplay.FormatSummary(orangutan);
            TestBase.AssertTrue(orangSummary.Contains("Orangutan", StringComparison.OrdinalIgnoreCase),
                "OrangutanSuffix keeps its identity name", ref _run, ref _passed, ref _failed);
            int dash = orangSummary.IndexOf(" — ", StringComparison.Ordinal);
            TestBase.AssertTrue(dash > 0 && !string.IsNullOrWhiteSpace(orangSummary.Substring(dash + 3)),
                "OrangutanSuffix summary has text after em dash", ref _run, ref _passed, ref _failed);
        }

        private static void TestShellSetRequiresTwoPieces()
        {
            TestBase.SetCurrentTestName(nameof(TestShellSetRequiresTwoPieces));
            TriggersLoader.ClearCache();
            var hero = new Character("ShellHero", 10);
            var helm = new HeadItem("ShellHelm", 1, 2);
            StatBonusTriggerMerge.ApplySuffixToItem(helm, new StatBonus
            {
                Name = "of the Tortoise",
                TriggerName = "TortoiseSuffix",
                Tags = new List<string> { "shell" }
            });
            hero.TryEquipItem(helm, "Head", out _, out _, ignoreAttributeRequirements: true);
            int onePiece = ItemEquipEffectApplicator.GetEquippedArmorBonus(hero);
            TestBase.AssertEqual(0, onePiece, "1 shell piece no set armor", ref _run, ref _passed, ref _failed);

            var chest = new ChestItem("ShellChest", 1, 2);
            StatBonusTriggerMerge.ApplySuffixToItem(chest, new StatBonus
            {
                Name = "of the Crab",
                TriggerName = "CrabSuffix",
                Tags = new List<string> { "shell" }
            });
            hero.TryEquipItem(chest, "Body", out _, out _, ignoreAttributeRequirements: true);
            int twoPiece = ItemEquipEffectApplicator.GetEquippedArmorBonus(hero);
            TestBase.AssertEqual(2, twoPiece, "2 shell pieces set armor +2", ref _run, ref _passed, ref _failed);
        }

        private static void TestIdentityDedupeAcrossTwoShellPieces()
        {
            TestBase.SetCurrentTestName(nameof(TestIdentityDedupeAcrossTwoShellPieces));
            TriggersLoader.ClearCache();
            var hero = new Character("DedupeHero", 10);
            var foe = new Character("Foe", 20);

            var helm = new HeadItem("H2", 1, 1);
            StatBonusTriggerMerge.ApplySuffixToItem(helm, new StatBonus
            {
                Name = "of the Tortoise",
                TriggerName = "TortoiseSuffix",
                Tags = new List<string> { "shell" }
            });
            var chest = new ChestItem("C2", 1, 1);
            StatBonusTriggerMerge.ApplySuffixToItem(chest, new StatBonus
            {
                Name = "of the Pangolin",
                TriggerName = "PangolinSuffix",
                Tags = new List<string> { "shell" }
            });
            hero.TryEquipItem(helm, "Head", out _, out _, ignoreAttributeRequirements: true);
            hero.TryEquipItem(chest, "Body", out _, out _, ignoreAttributeRequirements: true);

            int armor = ItemEquipEffectApplicator.GetEquippedArmorBonus(hero);
            TestBase.AssertEqual(2, armor, "set armor deduped to +2 not +4", ref _run, ref _passed, ref _failed);

            var msgs = new List<string>();
            var hit = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = foe,
                Action = new Action { Name = "Swing" },
                IsMiss = false
            };
            EquippedItemTriggerApplicator.ApplyFromAttacker(hero, foe, hit, msgs);
            TestBase.AssertEqual(1, hero.HardenStacks ?? 0,
                "ShellAmpTo harden once across two pieces", ref _run, ref _passed, ref _failed);
        }

        private static void TestAnimalSuffixRowsAreTriggerOnly()
        {
            TestBase.SetCurrentTestName(nameof(TestAnimalSuffixRowsAreTriggerOnly));
            var cache = LootDataCache.Load();
            var tortoise = cache.StatBonuses.FirstOrDefault(s =>
                string.Equals(s.Name, "of the Tortoise", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(tortoise != null, "tortoise row", ref _run, ref _passed, ref _failed);
            if (tortoise == null)
                return;
            TestBase.AssertEqual("TortoiseSuffix", tortoise.TriggerName, "triggerName", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tortoise.Tags != null && tortoise.Tags.Contains("shell"), "shell tag", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, tortoise.EnumerateContributions().Count(), "no flat mechanics", ref _run, ref _passed, ref _failed);
        }
    }
}
