using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;
using RPGGame;
using RPGGame.Config;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for BattleEventAnalyzer
    /// Tests event analysis, narrative triggering, and health tracking
    /// </summary>
    public static class BattleEventAnalyzerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BattleEventAnalyzer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BattleEventAnalyzer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestInitialize();
            TestUpdateFinalHealth();
            TestAnalyzeEvent();
            TestIsSignificantEventAfterAnalyzeEvent();
            TestCreatureTierBankThenGenericFallback();
            TestFirstBloodCreatureTierFillsNameToken();
            TestFirstBloodVictimWordingForPlayerAndEnemyFirstHit();
            TestCreatureTierCritUsesEnemyActorOnly();
            TestEnemyTauntTierBankThenBiomeFallback();

            TestBase.PrintSummary("BattleEventAnalyzer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var textProvider = new NarrativeTextProvider();
                var stateManager = new NarrativeStateManager();
                var tauntSystem = new TauntSystem(textProvider);
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
                
                TestBase.AssertTrue(analyzer != null,
                    "BattleEventAnalyzer should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"BattleEventAnalyzer constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInitialize()
        {
            Console.WriteLine("\n--- Testing Initialize ---");

            try
            {
                var textProvider = new NarrativeTextProvider();
                var stateManager = new NarrativeStateManager();
                var tauntSystem = new TauntSystem(textProvider);
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
                
                analyzer.Initialize("Player", "Enemy", "Dungeon", 100, 50);
                
                TestBase.AssertTrue(true,
                    "Initialize should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Initialize failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestUpdateFinalHealth()
        {
            Console.WriteLine("\n--- Testing UpdateFinalHealth ---");

            try
            {
                var textProvider = new NarrativeTextProvider();
                var stateManager = new NarrativeStateManager();
                var tauntSystem = new TauntSystem(textProvider);
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
                
                analyzer.UpdateFinalHealth(80, 30);
                
                TestBase.AssertTrue(true,
                    "UpdateFinalHealth should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"UpdateFinalHealth failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestAnalyzeEvent()
        {
            Console.WriteLine("\n--- Testing AnalyzeEvent ---");

            try
            {
                var textProvider = new NarrativeTextProvider();
                var stateManager = new NarrativeStateManager();
                var tauntSystem = new TauntSystem(textProvider);
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
                analyzer.Initialize("Player", "Enemy", "Dungeon", 100, 50);
                
                var evt = new BattleEvent
                {
                    Actor = "Player",
                    Target = "Enemy",
                    Damage = 10,
                    IsSuccess = true
                };
                var settings = new GameSettings();
                var narratives = analyzer.AnalyzeEvent(evt, settings);
                
                TestBase.AssertTrue(narratives != null,
                    "AnalyzeEvent should return non-null list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"AnalyzeEvent failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestIsSignificantEventAfterAnalyzeEvent()
        {
            Console.WriteLine("\n--- Testing IsSignificantEvent after AnalyzeEvent ---");

            var textProvider = new NarrativeTextProvider();
            var stateManager = new NarrativeStateManager();
            var tauntSystem = new TauntSystem(textProvider);
            var analyzer = new BattleEventAnalyzer(textProvider, stateManager, tauntSystem);
            analyzer.Initialize("Player", "Enemy", "Hall", 100, 50);
            analyzer.UpdateFinalHealth(100, 40);

            var evt = new BattleEvent
            {
                Actor = "Player",
                Target = "Enemy",
                Damage = 10,
                IsSuccess = true
            };
            var settings = GameSettings.Instance;
            var narratives = analyzer.AnalyzeEvent(evt, settings);

            TestBase.AssertTrue(narratives.Count > 0,
                "AnalyzeEvent should generate first-blood text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(stateManager.HasFirstBloodOccurred,
                "HasFirstBloodOccurred should be set after generation",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(analyzer.IsSignificantEvent(evt, settings),
                "IsSignificantEvent should still be true for the event that just generated text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCreatureTierBankThenGenericFallback()
        {
            Console.WriteLine("\n--- Testing creature-tier bank then generic fallback ---");

            var data = FlavorText.GetData();
            string unique = "TIER_FIRST_BLOOD_MARKER_technoEcho";
            data.CombatNarratives.TryGetValue("firstBlood_technoEcho", out var previous);
            FlavorTextBankCatalog.SetBank(data, "combatNarratives.firstBlood_technoEcho", new[] { unique });

            try
            {
                var textProvider = new NarrativeTextProvider();
                string tiered = textProvider.GetCreatureTieredNarrative("firstBlood", CreatureTierIds.TechnoEcho);
                TestBase.AssertEqual(unique, tiered,
                    "Non-empty tier bank should win over generic firstBlood",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                FlavorTextBankCatalog.SetBank(data, "combatNarratives.firstBlood_technoEcho", Array.Empty<string>());
                string fallback = textProvider.GetCreatureTieredNarrative("firstBlood", CreatureTierIds.TechnoEcho);
                TestBase.AssertTrue(fallback != unique && !string.IsNullOrEmpty(fallback),
                    "Empty tier bank should fall back to generic firstBlood",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                string missingTag = textProvider.GetCreatureTieredNarrative("firstBlood", null);
                TestBase.AssertTrue(!string.IsNullOrEmpty(missingTag),
                    "Missing creatureTier should use generic firstBlood",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var stateManager = new NarrativeStateManager();
                var analyzer = new BattleEventAnalyzer(textProvider, stateManager, new TauntSystem(textProvider));
                analyzer.Initialize("Hero", "Goblin", "Hall", 100, 100, CreatureTierIds.TechnoEcho);
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.firstBlood_technoEcho", new[] { unique });
                var lines = analyzer.AnalyzeEvent(new BattleEvent
                {
                    Actor = "Hero",
                    Target = "Goblin",
                    Damage = 10,
                    IsSuccess = true
                }, GameSettings.Instance);
                TestBase.AssertTrue(lines.Contains(unique),
                    "AnalyzeEvent firstBlood should use the enemy creature-tier bank",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.firstBlood_technoEcho", previous ?? Array.Empty<string>());
            }
        }

        /// <summary>
        /// firstBlood is the only creature-tier bank AnalyzeEvent used to add without
        /// ReplacePlaceholders. Live combat then showed authored {name} templates verbatim.
        /// </summary>
        private static void TestFirstBloodCreatureTierFillsNameToken()
        {
            Console.WriteLine("\n--- Testing firstBlood AnalyzeEvent fills {name} from the creature-tier bank ---");

            const string liveLine = "The cut opens clean and even on {name}, like something measured it first.";
            var data = FlavorText.GetData();
            data.CombatNarratives.TryGetValue("firstBlood_technoEcho", out var previous);

            try
            {
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.firstBlood_technoEcho", new[] { liveLine });
                var analyzer = new BattleEventAnalyzer(
                    new NarrativeTextProvider(), new NarrativeStateManager(), new TauntSystem(new NarrativeTextProvider()));
                analyzer.Initialize("Seamus Ashwhisper", "Goblin", "Geode Chamber", 100, 100, CreatureTierIds.TechnoEcho);
                var lines = analyzer.AnalyzeEvent(new BattleEvent
                {
                    Actor = "Seamus Ashwhisper",
                    Target = "Goblin",
                    Damage = 10,
                    IsSuccess = true
                }, GameSettings.Instance);

                TestBase.AssertTrue(lines.Any(l => l.Contains("on Goblin", StringComparison.Ordinal)
                    && l.Contains("measured it first", StringComparison.Ordinal)),
                    "AnalyzeEvent firstBlood should fill {name} with the enemy (Goblin)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!lines.Any(l => l.Contains("{name}", StringComparison.Ordinal)),
                    "AnalyzeEvent firstBlood must not emit unsubstituted {name}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.firstBlood_technoEcho", previous ?? Array.Empty<string>());
            }

            foreach (var tier in CreatureTierIds.All)
            {
                string key = CreatureTierIds.SuffixedBank("firstBlood", tier);
                if (!data.CombatNarratives.TryGetValue(key, out var authored) || authored == null)
                    continue;

                foreach (var template in authored)
                {
                    if (!template.Contains("{name}", StringComparison.Ordinal))
                        continue;

                    data.CombatNarratives.TryGetValue(key, out var prior);
                    try
                    {
                        FlavorTextBankCatalog.SetBank(data, $"combatNarratives.{key}", new[] { template });
                        var analyzer = new BattleEventAnalyzer(
                            new NarrativeTextProvider(), new NarrativeStateManager(), new TauntSystem(new NarrativeTextProvider()));
                        analyzer.Initialize("Hero", "Goblin", "Hall", 100, 100, tier);
                        var lines = analyzer.AnalyzeEvent(new BattleEvent
                        {
                            Actor = "Hero",
                            Target = "Goblin",
                            Damage = 10,
                            IsSuccess = true
                        }, GameSettings.Instance);
                        TestBase.AssertTrue(lines.Any(l => l.Contains("Goblin", StringComparison.Ordinal)
                            && !l.Contains("{name}", StringComparison.Ordinal)),
                            $"AnalyzeEvent firstBlood_{tier} should fill {{name}} for authored line",
                            ref _testsRun, ref _testsPassed, ref _testsFailed);
                    }
                    finally
                    {
                        FlavorTextBankCatalog.SetBank(data, $"combatNarratives.{key}", prior ?? Array.Empty<string>());
                    }
                }
            }
        }

        /// <summary>
        /// firstBlood fires on whichever side hits first, but {name} is always the enemy
        /// (victim-oriented copy). Magma Beast is technoEcho — the live screenshot case.
        /// </summary>
        private static void TestFirstBloodVictimWordingForPlayerAndEnemyFirstHit()
        {
            Console.WriteLine("\n--- Testing firstBlood victim wording (player-first and enemy-first) ---");

            FlavorText.Reload();
            EnemyLoader.LoadEnemies();
            var magma = EnemyLoader.GetEnemyData("Magma Beast");
            TestBase.AssertEqual(CreatureTierIds.TechnoEcho, magma?.CreatureTier,
                "Magma Beast should load creatureTier technoEcho",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var data = FlavorText.GetData();
            foreach (var tier in CreatureTierIds.All)
            {
                string key = CreatureTierIds.SuffixedBank("firstBlood", tier);
                TestBase.AssertTrue(
                    data.CombatNarratives.TryGetValue(key, out var authored)
                    && authored != null
                    && authored.Length == 3
                    && !authored.Any(l =>
                        l.Contains("draws blood", StringComparison.Ordinal)
                        || l.Contains("like {name} measured it first", StringComparison.Ordinal)),
                    $"{key} should be 3 victim-oriented lines (no attacker-as-name copy)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.AssertTrue(
                data.CombatNarratives.TryGetValue("firstBlood_technoEcho", out var techno)
                && techno != null
                && techno.Any(l => l.Contains("The cut opens clean and even on {name}", StringComparison.Ordinal)),
                "firstBlood_technoEcho should author 'on {name}', not 'like {name} measured'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            AssertFirstBloodFilledVictimLine(
                "Gavin Quickstrike", "Magma Beast",
                "player-first Magma Beast firstBlood should fill {name} to Magma Beast from firstBlood_technoEcho");
            AssertFirstBloodFilledVictimLine(
                "Magma Beast", "Gavin Quickstrike",
                "enemy-first Magma Beast firstBlood should still fill {name} to Magma Beast");
        }

        private static void AssertFirstBloodFilledVictimLine(string actor, string target, string message)
        {
            const string enemyName = "Magma Beast";
            var analyzer = new BattleEventAnalyzer(
                new NarrativeTextProvider(), new NarrativeStateManager(), new TauntSystem(new NarrativeTextProvider()));
            analyzer.Initialize("Gavin Quickstrike", enemyName, "Volcanic Chamber", 100, 100, CreatureTierIds.TechnoEcho);
            var lines = analyzer.AnalyzeEvent(new BattleEvent
            {
                Actor = actor,
                Target = target,
                Damage = 10,
                IsSuccess = true
            }, GameSettings.Instance);

            string key = CreatureTierIds.SuffixedBank("firstBlood", CreatureTierIds.TechnoEcho);
            bool matchesBank = false;
            string filledSample = "";
            if (FlavorText.GetData().CombatNarratives.TryGetValue(key, out var bank) && bank != null)
            {
                foreach (var template in bank)
                {
                    string filled = template.Replace("{name}", enemyName);
                    if (lines.Any(l => string.Equals(l, filled, StringComparison.Ordinal)))
                    {
                        matchesBank = true;
                        filledSample = filled;
                        break;
                    }
                }
            }

            TestBase.AssertTrue(
                matchesBank
                && !string.IsNullOrEmpty(filledSample)
                && filledSample.Contains(enemyName, StringComparison.Ordinal)
                && !filledSample.Contains("{name}", StringComparison.Ordinal)
                && !filledSample.Contains("draws blood", StringComparison.Ordinal)
                && !lines.Any(l => l.Contains("{name}", StringComparison.Ordinal)),
                message,
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            Console.WriteLine($"    {actor} vs {target}: {filledSample}");
        }

        private static void TestCreatureTierCritUsesEnemyActorOnly()
        {
            Console.WriteLine("\n--- Testing creature-tier crit banks apply to the enemy actor only ---");

            var data = FlavorText.GetData();
            string unique = "TIER_CRIT_MARKER_technoEcho {name}";
            data.CombatNarratives.TryGetValue("criticalHit_technoEcho", out var previous);
            FlavorTextBankCatalog.SetBank(data, "combatNarratives.criticalHit_technoEcho", new[] { unique });

            try
            {
                var settings = new GameSettings { NarrativeBalance = 1.0 };
                var textProvider = new NarrativeTextProvider();

                var playerAnalyzer = new BattleEventAnalyzer(textProvider, new NarrativeStateManager(), new TauntSystem(textProvider));
                playerAnalyzer.Initialize("Hero", "Goblin", "Hall", 100, 100, CreatureTierIds.TechnoEcho);
                var playerLines = playerAnalyzer.AnalyzeEvent(new BattleEvent
                {
                    Actor = "Hero",
                    Target = "Goblin",
                    Damage = 20,
                    IsSuccess = true,
                    IsCritical = true,
                    Roll = 20
                }, settings);
                TestBase.AssertTrue(!playerLines.Any(l => l.Contains("TIER_CRIT_MARKER_technoEcho", StringComparison.Ordinal)),
                    "Player crit should stay on the generic criticalHit bank",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var enemyAnalyzer = new BattleEventAnalyzer(textProvider, new NarrativeStateManager(), new TauntSystem(textProvider));
                enemyAnalyzer.Initialize("Hero", "Goblin", "Hall", 100, 100, CreatureTierIds.TechnoEcho);
                var enemyLines = enemyAnalyzer.AnalyzeEvent(new BattleEvent
                {
                    Actor = "Goblin",
                    Target = "Hero",
                    Damage = 20,
                    IsSuccess = true,
                    IsCritical = true,
                    Roll = 20
                }, settings);
                TestBase.AssertTrue(enemyLines.Any(l => l.Contains("TIER_CRIT_MARKER_technoEcho", StringComparison.Ordinal)),
                    "Enemy crit should use criticalHit_{creatureTier}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.criticalHit_technoEcho", previous ?? Array.Empty<string>());
            }
        }

        private static void TestEnemyTauntTierBankThenBiomeFallback()
        {
            Console.WriteLine("\n--- Testing enemy taunt: creature-tier bank then biome, never multiplied ---");

            var data = FlavorText.GetData();
            string unique = "TIER_TAUNT_MARKER You cannot hide, {player}!";
            string forestPinned = "FOREST_TAUNT_MARKER Stay out of my woods, {player}!";
            data.CombatNarratives.TryGetValue("enemyTaunt_technoEcho", out var previous);
            data.CombatNarratives.TryGetValue("enemyTaunt_forest", out var previousForest);
            var textProvider = new NarrativeTextProvider();
            var tauntSystem = new TauntSystem(textProvider);
            var settings = new GameSettings { NarrativeBalance = 0 };
            int actions = tauntSystem.GetEnemyTauntThreshold(0, settings);

            try
            {
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.enemyTaunt_forest", new[] { forestPinned });
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.enemyTaunt_technoEcho", Array.Empty<string>());
                var (shouldBiome, biomeText) = tauntSystem.CheckEnemyTaunt(
                    actions, 0, "Goblin", "Hero", "Dark Forest", settings, CreatureTierIds.TechnoEcho);
                TestBase.AssertTrue(shouldBiome && biomeText.Contains("FOREST_TAUNT_MARKER", StringComparison.Ordinal) && biomeText.Contains("Hero"),
                    "Empty enemyTaunt_{tier} should use the existing biome matcher (forest), not a multiplied key",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var (shouldHall, hallText) = tauntSystem.CheckEnemyTaunt(
                    actions, 0, "Goblin", "Hero", "Hall", settings, CreatureTierIds.TechnoEcho);
                TestBase.AssertTrue(shouldHall && !string.IsNullOrEmpty(hallText) && !hallText.Contains("FOREST_TAUNT_MARKER", StringComparison.Ordinal),
                    "Hall has no biome key; empty tier bank falls back to generic enemyTaunt",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                FlavorTextBankCatalog.SetBank(data, "combatNarratives.enemyTaunt_technoEcho", new[] { unique });
                var (shouldTier, tierText) = tauntSystem.CheckEnemyTaunt(
                    actions, 0, "Goblin", "Hero", "Dark Forest", settings, CreatureTierIds.TechnoEcho);
                TestBase.AssertTrue(shouldTier && tierText.Contains("TIER_TAUNT_MARKER", StringComparison.Ordinal) && tierText.Contains("Hero"),
                    "Non-empty enemyTaunt_{tier} should win over biome taunts without creating enemyTaunt_forest_technoEcho",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.enemyTaunt_technoEcho", previous ?? Array.Empty<string>());
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.enemyTaunt_forest", previousForest ?? Array.Empty<string>());
            }
        }

        #endregion
    }
}
