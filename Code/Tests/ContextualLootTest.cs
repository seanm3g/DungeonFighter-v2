using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tests
{
    /// <summary>
    /// Test for the contextual loot system (modifications and actions with 80/20 and 70/30 splits)
    /// </summary>
    public class ContextualLootTest
    {
        public static void RunTests()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║     CONTEXTUAL LOOT SYSTEM TEST                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

            TestWandActionSplit();
            TestSwordActionSplit();
            TestDaggerActionSplit();
            TestModificationContextualBias();

            Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║     ALL TESTS COMPLETE                                 ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        }

        private static void TestWandActionSplit()
        {
            Console.WriteLine("TEST 1: Wand Action Split (80% Wizard, 20% Any)");
            Console.WriteLine("──────────────────────────────────────────────\n");

            var random = new Random(42); // Fixed seed for reproducibility
            var player = new Character("TestWizard");
            var context = LootContext.Create(player, dungeonTheme: "Lava");

            var actionCounts = new Dictionary<string, int>();
            int totalActions = 0;

            // Generate 100 wands with actions
            for (int i = 0; i < 100; i++)
            {
                var wand = new WeaponItem("Test Wand", 1, 10, 1.0, WeaponType.Wand);
                var tuning = GameConfiguration.Instance;

                // Simulate ROLL 6-7: Rarity and bonuses
                double magicFind = player.GetMagicFind();
                var rarity = new RarityData {
                    Name = "Uncommon",
                    Weight = 1,
                    StatBonuses = 0,
                    ActionBonuses = 1,
                    Modifications = 0
                };

                context.WeaponType = wand.WeaponType.ToString();

                // Apply bonuses with context
                var bonusApplier = new LootBonusApplier(LootDataCache.Load(), random);
                bonusApplier.ApplyBonuses(wand, rarity, context);

                // Count actions
                totalActions += wand.ActionBonuses.Count;
                foreach (var action in wand.ActionBonuses)
                {
                    if (!actionCounts.ContainsKey(action.Name))
                        actionCounts[action.Name] = 0;
                    actionCounts[action.Name]++;
                }
            }

            // Display results
            Console.WriteLine($"Generated 100 wands with {totalActions} total actions\n");
            Console.WriteLine("Action Distribution:");
            var wizardActions = new[] { "ARCANE BLAST", "MAGIC BOLT", "ARCANE CASCADE" };
            int wizardCount = actionCounts.Where(kvp => wizardActions.Contains(kvp.Key)).Sum(kvp => kvp.Value);
            int otherCount = totalActions - wizardCount;

            foreach (var kvp in actionCounts.OrderByDescending(x => x.Value))
            {
                double percentage = (kvp.Value * 100.0) / totalActions;
                string isWizard = wizardActions.Contains(kvp.Key) ? "[WIZARD]" : "[OTHER]";
                Console.WriteLine($"  {kvp.Key,-30} {isWizard,-10} {kvp.Value:3} ({percentage:F1}%)");
            }

            Console.WriteLine($"\n  Wizard Actions: {wizardCount}/{totalActions} ({(wizardCount * 100.0 / totalActions):F1}%)");
            Console.WriteLine($"  Other Actions:  {otherCount}/{totalActions} ({(otherCount * 100.0 / totalActions):F1}%)");
            Console.WriteLine($"  Expected: ~80% Wizard, ~20% Other");
            Console.WriteLine($"  Status: {(wizardCount > totalActions * 0.70 && wizardCount < totalActions * 0.90 ? "✓ PASS" : "✗ FAIL")}\n");
        }

        private static void TestSwordActionSplit()
        {
            Console.WriteLine("TEST 2: Sword Action Split (80% Warrior, 20% Any)");
            Console.WriteLine("──────────────────────────────────────────────\n");

            var random = new Random(43);
            var player = new Character("TestWarrior");
            var context = LootContext.Create(player, dungeonTheme: "Sky");

            var actionCounts = new Dictionary<string, int>();
            int totalActions = 0;

            for (int i = 0; i < 100; i++)
            {
                var sword = new WeaponItem("Test Sword", 1, 15, 1.0, WeaponType.Sword);
                var rarity = new RarityData
                {
                    Name = "Rare",
                    Weight = 1,
                    StatBonuses = 0,
                    ActionBonuses = 1,
                    Modifications = 0
                };

                context.WeaponType = sword.WeaponType.ToString();

                var bonusApplier = new LootBonusApplier(LootDataCache.Load(), random);
                bonusApplier.ApplyBonuses(sword, rarity, context);

                totalActions += sword.ActionBonuses.Count;
                foreach (var action in sword.ActionBonuses)
                {
                    if (!actionCounts.ContainsKey(action.Name))
                        actionCounts[action.Name] = 0;
                    actionCounts[action.Name]++;
                }
            }

            Console.WriteLine($"Generated 100 swords with {totalActions} total actions\n");
            Console.WriteLine("Action Distribution:");
            var warriorActions = new[] { "PRECISION STRIKE", "SHIELD BASH", "COUNTER ATTACK" };
            int warriorCount = actionCounts.Where(kvp => warriorActions.Contains(kvp.Key)).Sum(kvp => kvp.Value);
            int otherCount = totalActions - warriorCount;

            foreach (var kvp in actionCounts.OrderByDescending(x => x.Value))
            {
                double percentage = (kvp.Value * 100.0) / totalActions;
                string isWarrior = warriorActions.Contains(kvp.Key) ? "[WARRIOR]" : "[OTHER]";
                Console.WriteLine($"  {kvp.Key,-30} {isWarrior,-10} {kvp.Value:3} ({percentage:F1}%)");
            }

            Console.WriteLine($"\n  Warrior Actions: {warriorCount}/{totalActions} ({(warriorCount * 100.0 / totalActions):F1}%)");
            Console.WriteLine($"  Other Actions:   {otherCount}/{totalActions} ({(otherCount * 100.0 / totalActions):F1}%)");
            Console.WriteLine($"  Expected: ~80% Warrior, ~20% Other");
            Console.WriteLine($"  Status: {(warriorCount > totalActions * 0.70 && warriorCount < totalActions * 0.90 ? "✓ PASS" : "✗ FAIL")}\n");
        }

        private static void TestDaggerActionSplit()
        {
            Console.WriteLine("TEST 3: Dagger Action Split (80% Rogue, 20% Any)");
            Console.WriteLine("───────────────────────────────────────────────\n");

            var random = new Random(44);
            var player = new Character("TestRogue");
            var context = LootContext.Create(player, dungeonTheme: "Crypt", enemyArchetype: "Assassin");

            var actionCounts = new Dictionary<string, int>();
            int totalActions = 0;

            for (int i = 0; i < 100; i++)
            {
                var dagger = new WeaponItem("Test Dagger", 1, 8, 1.0, WeaponType.Dagger);
                var rarity = new RarityData
                {
                    Name = "Epic",
                    Weight = 1,
                    StatBonuses = 0,
                    ActionBonuses = 1,
                    Modifications = 0
                };

                context.WeaponType = dagger.WeaponType.ToString();

                var bonusApplier = new LootBonusApplier(LootDataCache.Load(), random);
                bonusApplier.ApplyBonuses(dagger, rarity, context);

                totalActions += dagger.ActionBonuses.Count;
                foreach (var action in dagger.ActionBonuses)
                {
                    if (!actionCounts.ContainsKey(action.Name))
                        actionCounts[action.Name] = 0;
                    actionCounts[action.Name]++;
                }
            }

            Console.WriteLine($"Generated 100 daggers with {totalActions} total actions\n");
            Console.WriteLine("Action Distribution:");
            var rogueActions = new[] { "DEADLY THRUST", "SHADOW STRIKE", "BACKSTAB" };
            int rogueCount = actionCounts.Where(kvp => rogueActions.Contains(kvp.Key)).Sum(kvp => kvp.Value);
            int otherCount = totalActions - rogueCount;

            foreach (var kvp in actionCounts.OrderByDescending(x => x.Value))
            {
                double percentage = (kvp.Value * 100.0) / totalActions;
                string isRogue = rogueActions.Contains(kvp.Key) ? "[ROGUE]" : "[OTHER]";
                Console.WriteLine($"  {kvp.Key,-30} {isRogue,-10} {kvp.Value:3} ({percentage:F1}%)");
            }

            Console.WriteLine($"\n  Rogue Actions: {rogueCount}/{totalActions} ({(rogueCount * 100.0 / totalActions):F1}%)");
            Console.WriteLine($"  Other Actions: {otherCount}/{totalActions} ({(otherCount * 100.0 / totalActions):F1}%)");
            Console.WriteLine($"  Expected: ~80% Rogue, ~20% Other");
            Console.WriteLine($"  Status: {(rogueCount > totalActions * 0.70 && rogueCount < totalActions * 0.90 ? "✓ PASS" : "✗ FAIL")}\n");
        }

        private static void TestModificationContextualBias()
        {
            Console.WriteLine("TEST 4: Modification Contextual Bias (70% Theme/Archetype, 30% Random)");
            Console.WriteLine("──────────────────────────────────────────────────────────────────\n");

            var random = new Random(45);
            var player = new Character("TestPlayer");
            var context = LootContext.Create(player, dungeonTheme: "Lava", enemyArchetype: "Berserker");

            var modCounts = new Dictionary<string, int>();
            int totalMods = 0;
            var lavaMods = new[] { "Burning", "Molten", "Searing", "Infernal" };
            var berserkerMods = new[] { "Brutal", "Savage" };

            for (int i = 0; i < 100; i++)
            {
                var item = new WeaponItem("Test Weapon", 1, 10, 1.0, WeaponType.Mace);
                var rarity = new RarityData
                {
                    Name = "Legendary",
                    Weight = 1,
                    StatBonuses = 0,
                    ActionBonuses = 0,
                    Modifications = 1
                };

                context.WeaponType = item.WeaponType.ToString();

                var bonusApplier = new LootBonusApplier(LootDataCache.Load(), random);
                bonusApplier.ApplyBonuses(item, rarity, context);

                totalMods += item.Modifications.Count;
                foreach (var mod in item.Modifications)
                {
                    if (!modCounts.ContainsKey(mod.Name))
                        modCounts[mod.Name] = 0;
                    modCounts[mod.Name]++;
                }
            }

            Console.WriteLine($"Generated 100 items in Lava dungeon vs Berserker with {totalMods} total modifications\n");
            Console.WriteLine("Modification Distribution:");
            int thematicCount = modCounts.Where(kvp => lavaMods.Contains(kvp.Key) || berserkerMods.Contains(kvp.Key)).Sum(kvp => kvp.Value);
            int randomCount = totalMods - thematicCount;

            foreach (var kvp in modCounts.OrderByDescending(x => x.Value))
            {
                double percentage = (kvp.Value * 100.0) / totalMods;
                string isThematic = lavaMods.Contains(kvp.Key) || berserkerMods.Contains(kvp.Key) ? "[THEMATIC]" : "[RANDOM]";
                Console.WriteLine($"  {kvp.Key,-30} {isThematic,-12} {kvp.Value:3} ({percentage:F1}%)");
            }

            Console.WriteLine($"\n  Thematic Mods (Lava/Berserker): {thematicCount}/{totalMods} ({(thematicCount * 100.0 / totalMods):F1}%)");
            Console.WriteLine($"  Random Mods:                     {randomCount}/{totalMods} ({(randomCount * 100.0 / totalMods):F1}%)");
            Console.WriteLine($"  Expected: ~70% Thematic, ~30% Random");
            Console.WriteLine($"  Status: {(thematicCount > totalMods * 0.60 && thematicCount < totalMods * 0.80 ? "✓ PASS" : "✗ FAIL")}\n");
        }
    }
}
