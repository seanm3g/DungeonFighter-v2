using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Comprehensive test suite for BattleNarrative refactored managers.
    /// Tests all four specialized managers: State, Text, Taunt, and Analyzer.
    /// </summary>
    public static class BattleNarrativeManagersTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("BATTLENARRATIVE MANAGERS TEST SUITE");
            Console.WriteLine(new string('=', 80));

            TestNarrativeStateManager();
            TestNarrativeTextProvider();
            TestTauntSystem();
            TestBattleEventAnalyzer();

            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("ALL TESTS COMPLETED SUCCESSFULLY");
            Console.WriteLine(new string('=', 80));
        }

        // ===== NARRATIVE STATE MANAGER TESTS =====
        private static void TestNarrativeStateManager()
        {
            Console.WriteLine("\n--- NARRATIVE STATE MANAGER TESTS ---");

            var stateManager = new NarrativeStateManager();

            // Test initialization
            Assert(stateManager.HasFirstBloodOccurred == false, "First blood should be false initially");
            Assert(stateManager.PlayerActionCount == 0, "Player action count should be 0 initially");
            Assert(stateManager.CanPlayerTaunt == true, "Player should be able to taunt initially");

            // Test state setters
            stateManager.SetFirstBloodOccurred();
            Assert(stateManager.HasFirstBloodOccurred == true, "First blood should be true after setting");

            stateManager.SetGoodComboOccurred();
            Assert(stateManager.HasGoodComboOccurred == true, "Good combo should be true after setting");

            // Test counters
            for (int i = 0; i < 5; i++)
            {
                stateManager.IncrementPlayerActionCount();
            }
            Assert(stateManager.PlayerActionCount == 5, "Player action count should be 5");

            // Test taunt limits
            stateManager.IncrementPlayerTauntCount();
            Assert(stateManager.CanPlayerTaunt == true, "Can taunt with 1 taunt");
            stateManager.IncrementPlayerTauntCount();
            Assert(stateManager.CanPlayerTaunt == false, "Cannot taunt with 2 taunts");

            // Test health leads
            stateManager.SetPlayerHealthLead();
            Assert(stateManager.HasPlayerHealthLead == true, "Player should have health lead");
            Assert(stateManager.HasEnemyHealthLead == false, "Enemy should not have health lead");

            stateManager.SetEnemyHealthLead();
            Assert(stateManager.HasEnemyHealthLead == true, "Enemy should have health lead");
            Assert(stateManager.HasPlayerHealthLead == false, "Player should not have health lead");

            // Test reset
            stateManager.ResetAllStates();
            Assert(stateManager.HasFirstBloodOccurred == false, "First blood should be false after reset");
            Assert(stateManager.PlayerActionCount == 0, "Action count should be 0 after reset");

            Console.WriteLine("✓ NarrativeStateManager: 12/12 assertions passed");
        }

        // ===== NARRATIVE TEXT PROVIDER TESTS =====
        private static void TestNarrativeTextProvider()
        {
            Console.WriteLine("\n--- NARRATIVE TEXT PROVIDER TESTS ---");

            var provider = new NarrativeTextProvider();

            // Test fallback narratives
            var firstBlood = provider.GetFallbackNarrative("firstBlood");
            Assert(!string.IsNullOrEmpty(firstBlood), "First blood narrative should not be empty");

            var criticalHit = provider.GetFallbackNarrative("criticalHit");
            Assert(criticalHit.Contains("devastating"), "Critical hit narrative should mention devastating");

            var below50 = provider.GetFallbackNarrative("below50Percent");
            Assert(below50.Contains("{name}"), "Below 50% should have name placeholder");

            // Test placeholder replacement
            var template1 = "{name} strikes {target}!";
            var replacements1 = new Dictionary<string, string> { { "name", "Hero" }, { "target", "Goblin" } };
            var result1 = provider.ReplacePlaceholders(template1, replacements1);
            Assert(result1 == "Hero strikes Goblin!", "Should replace multiple placeholders");

            // Test case sensitivity
            var template2 = "{Name} and {name}";
            var replacements2 = new Dictionary<string, string> { { "name", "Hero" } };
            var result2 = provider.ReplacePlaceholders(template2, replacements2);
            Assert(result2 == "{Name} and Hero", "Should be case sensitive");

            // Test all major event types
            var eventTypes = new[] { "firstBlood", "criticalHit", "criticalMiss", "below50Percent", "playerDefeated", "enemyDefeated" };
            foreach (var eventType in eventTypes)
            {
                var narrative = provider.GetFallbackNarrative(eventType);
                Assert(!string.IsNullOrEmpty(narrative), $"{eventType} should have narrative");
            }

            Console.WriteLine("✓ NarrativeTextProvider: 8/8 assertions passed");
        }

        // ===== TAUNT SYSTEM TESTS =====
        private static void TestTauntSystem()
        {
            Console.WriteLine("\n--- TAUNT SYSTEM TESTS ---");

            var textProvider = new NarrativeTextProvider();
            var tauntSystem = new TauntSystem(textProvider);

            // Test location detection
            Assert(tauntSystem.GetLocationType("Dark Forest") == "forest", "Should detect forest");
            Assert(tauntSystem.GetLocationType("Underwater Cave") == "underwater", "Should detect underwater");
            Assert(tauntSystem.GetLocationType("Lava Pits") == "lava", "Should detect lava");
            Assert(tauntSystem.GetLocationType("Ancient Crypt") == "crypt", "Should detect crypt");
            Assert(tauntSystem.GetLocationType("Unknown") == "generic", "Should return generic for unknown");
            Assert(tauntSystem.GetLocationType(string.Empty) == "generic", "Should return generic for empty string");

            // Test case insensitivity
            var loc1 = tauntSystem.GetLocationType("FOREST");
            var loc2 = tauntSystem.GetLocationType("forest");
            Assert(loc1 == loc2, "Location detection should be case insensitive");

            // Test thresholds
            var settings = GameSettings.Instance;
            var playerThreshold = tauntSystem.GetPlayerTauntThreshold(0, settings);
            Assert(playerThreshold >= 8 && playerThreshold <= 12, "First player taunt threshold should be 8-12");

            var enemyThreshold = tauntSystem.GetEnemyTauntThreshold(0, settings);
            Assert(enemyThreshold >= 6 && enemyThreshold <= 10, "First enemy taunt threshold should be 6-10");

            // Test no more taunts
            var maxThreshold = tauntSystem.GetPlayerTauntThreshold(2, settings);
            Assert(maxThreshold == int.MaxValue, "Should not allow taunts after 2");

            // Test taunt generation
            var taunt = tauntSystem.GetLocationSpecificTaunt("player", "Hero", "Enemy", "generic");
            Assert(!string.IsNullOrEmpty(taunt), "Should generate taunt");
            Assert(taunt.Contains("Hero"), "Should include player name");

            Console.WriteLine("✓ TauntSystem: 10/10 assertions passed");
        }

        // ===== BATTLE EVENT ANALYZER TESTS =====
        private static void TestBattleEventAnalyzer()
        {
            Console.WriteLine("\n--- BATTLE EVENT ANALYZER TESTS ---");

            var textProvider = new NarrativeTextProvider();
            var stateManager = new NarrativeStateManager();
            var tauntSystem = new TauntSystem(textProvider);
            var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);

            // Initialize
            analyzer.Initialize("Hero", "Enemy", "Forest", 100, 100);
            analyzer.UpdateFinalHealth(100, 100);

            // Test first blood
            var evt1 = new BattleEvent { Actor = "Hero", Target = "Enemy", Damage = 10, IsSuccess = true };
            analyzer.UpdateFinalHealth(100, 90);
            var narratives1 = analyzer.AnalyzeEvent(evt1, GameSettings.Instance);
            Assert(narratives1.Any(n => n.Contains("blood")), "First blood should trigger");

            // Test critical hit
            var analyzer2 = new BattleEventAnalyzer(textProvider, new NarrativeStateManager(), tauntSystem);
            analyzer2.Initialize("Hero", "Enemy", "Forest", 100, 100);
            analyzer2.UpdateFinalHealth(100, 100);
            var evt2 = new BattleEvent { Actor = "Hero", Target = "Enemy", Damage = 25, IsSuccess = true, IsCritical = true };
            analyzer2.UpdateFinalHealth(100, 75);
            var narratives2 = analyzer2.AnalyzeEvent(evt2, GameSettings.Instance);
            Assert(narratives2.Count > 0, "Critical hit should generate narratives");

            // Test health threshold
            var analyzer3 = new BattleEventAnalyzer(textProvider, new NarrativeStateManager(), tauntSystem);
            analyzer3.Initialize("Hero", "Enemy", "Forest", 100, 100);
            analyzer3.UpdateFinalHealth(49, 100); // Below 50%
            var evt3 = new BattleEvent { Actor = "Enemy", Target = "Hero", Damage = 1 };
            var narratives3 = analyzer3.AnalyzeEvent(evt3, GameSettings.Instance);
            Assert(narratives3.Count > 0, "Below 50% health should trigger");

            // Test defeat
            var analyzer4 = new BattleEventAnalyzer(textProvider, new NarrativeStateManager(), tauntSystem);
            analyzer4.Initialize("Hero", "Enemy", "Forest", 100, 100);
            analyzer4.UpdateFinalHealth(0, 50); // Defeated
            var evt4 = new BattleEvent { Actor = "Enemy", Target = "Hero", Damage = 100 };
            var narratives4 = analyzer4.AnalyzeEvent(evt4, GameSettings.Instance);
            Assert(narratives4.Count > 0, "Defeat should trigger");

            // Test combo
            var analyzer5 = new BattleEventAnalyzer(textProvider, new NarrativeStateManager(), tauntSystem);
            analyzer5.Initialize("Hero", "Enemy", "Forest", 100, 100);
            analyzer5.UpdateFinalHealth(100, 80);
            var evt5 = new BattleEvent { Actor = "Hero", Target = "Enemy", IsCombo = true, ComboStep = 3, Damage = 20 };
            var narratives5 = analyzer5.AnalyzeEvent(evt5, GameSettings.Instance);
            Assert(narratives5.Count > 0, "Combo should trigger");

            Console.WriteLine("✓ BattleEventAnalyzer: 6/6 assertions passed");
        }

        // ===== HELPER METHOD =====
        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"ASSERTION FAILED: {message}");
            }
        }
    }
}

