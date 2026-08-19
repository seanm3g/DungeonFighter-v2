using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Combat;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for BattleNarrative
    /// Tests narrative generation, event tracking, and text generation
    /// </summary>
    public static class BattleNarrativeTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BattleNarrative tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BattleNarrative Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestAddEvent();
            TestGetNarratives();
            TestInformationalSummaryExcludesComboCounts();
            TestDisplayGateAfterAnalyzeEvent();
            TestFullFightDisplaysEachNarrativeType();
            TestLiveExecutePathPutsNarrativesInCombatLog();
            TestBiomeTauntBanksLoadAndSelectFromRealRooms();
            TestIceAndSwampRoomsRouteToIceAndSwampTaunts();
            TestCreatureTierBanksLoadAndSelectForRealEnemies();
            TestFirstBloodTechnoEchoFillsNameOnDisplayPath();

            TestBase.PrintSummary("BattleNarrative Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy", "Dungeon", 100, 50);
                
                TestBase.AssertTrue(narrative != null,
                    "BattleNarrative should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"BattleNarrative constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Event Tests

        private static void TestAddEvent()
        {
            Console.WriteLine("\n--- Testing AddEvent ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy");
                var evt = new BattleEvent
                {
                    Actor = "Player",
                    Target = "Enemy",
                    Action = "Attack",
                    Damage = 10,
                    IsSuccess = true
                };
                
                narrative.AddEvent(evt);
                
                TestBase.AssertTrue(true,
                    "AddEvent should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"AddEvent failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestGetNarratives()
        {
            Console.WriteLine("\n--- Testing GetTriggeredNarratives ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy");
                var narratives = narrative.GetTriggeredNarratives();
                
                TestBase.AssertTrue(narratives != null,
                    "GetTriggeredNarratives should return non-null list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GetTriggeredNarratives failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInformationalSummaryExcludesComboCounts()
        {
            Console.WriteLine("\n--- Testing informational summary (no combo line) ---");

            string playerWin = BattleNarrativeGenerator.GenerateInformationalSummary(
                40, 5, playerWon: true, enemyWon: false, "Hero", "Goblin");
            TestBase.AssertTrue(string.IsNullOrEmpty(playerWin),
                "Player victory summary should be empty (no combo or damage line)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string enemyWin = BattleNarrativeGenerator.GenerateInformationalSummary(
                10, 50, playerWon: false, enemyWon: true, "Hero", "Goblin");
            TestBase.AssertTrue(
                enemyWin.Contains("Total damage dealt", StringComparison.Ordinal)
                && enemyWin.Contains("Goblin defeats Hero", StringComparison.Ordinal)
                && !enemyWin.Contains("Combos", StringComparison.OrdinalIgnoreCase),
                "Enemy victory summary should include damage totals but not combo counts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string stalemate = BattleNarrativeGenerator.GenerateInformationalSummary(
                25, 25, playerWon: false, enemyWon: false, "Hero", "Goblin");
            TestBase.AssertTrue(
                stalemate.Contains("stalemate", StringComparison.OrdinalIgnoreCase)
                && !stalemate.Contains("Combos", StringComparison.OrdinalIgnoreCase),
                "Stalemate summary should not include combo counts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// AnalyzeEvent sets one-shot flags; display must still return the text generated for this event.
        /// </summary>
        private static void TestDisplayGateAfterAnalyzeEvent()
        {
            Console.WriteLine("\n--- Testing display gate after AnalyzeEvent mutates flags ---");

            var settings = GameSettings.Instance;
            bool prevEnable = settings.EnableNarrativeEvents;
            double prevBalance = settings.NarrativeBalance;
            settings.EnableNarrativeEvents = true;
            settings.NarrativeBalance = 0.8;

            try
            {
                var narrative = new BattleNarrative("Hero", "Goblin", "Hall", 100, 100);
                narrative.AddEvent(new BattleEvent
                {
                    Actor = "Hero",
                    Target = "Goblin",
                    Damage = 10,
                    IsSuccess = true
                });

                var displayed = narrative.GetTriggeredNarrativesIfSignificant();
                TestBase.AssertTrue(displayed.Count > 0,
                    "First-blood event should display after flags are set",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(displayed.Any(MatchesCombatBank("firstBlood")),
                    "Displayed lines should include firstBlood text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var second = narrative.GetTriggeredNarrativesIfSignificant();
                TestBase.AssertTrue(second.Count > 0,
                    "Re-querying the same event should still return cached display text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                settings.EnableNarrativeEvents = prevEnable;
                settings.NarrativeBalance = prevBalance;
            }
        }

        private static void TestFullFightDisplaysEachNarrativeType()
        {
            Console.WriteLine("\n--- Testing full fight displays each narrative type ---");

            var settings = GameSettings.Instance;
            bool prevEnable = settings.EnableNarrativeEvents;
            double prevBalance = settings.NarrativeBalance;
            settings.EnableNarrativeEvents = true;
            settings.NarrativeBalance = 0.8;

            try
            {
                var collected = new List<string>();
                var narrative = new BattleNarrative("Hero", "Goblin", "Hall", 100, 100);

                void Play(BattleEvent evt)
                {
                    narrative.AddEvent(evt);
                    collected.AddRange(narrative.GetTriggeredNarrativesIfSignificant());
                }

                // firstBlood + healthLeadChange (Hero 100, Goblin 90)
                Play(new BattleEvent { Actor = "Hero", Target = "Goblin", Damage = 10, IsSuccess = true });

                // criticalHit
                Play(new BattleEvent
                {
                    Actor = "Hero",
                    Target = "Goblin",
                    Damage = 12,
                    IsSuccess = true,
                    IsCritical = true,
                    Roll = 18
                });

                // criticalMiss
                Play(new BattleEvent
                {
                    Actor = "Hero",
                    Target = "Goblin",
                    Damage = 0,
                    IsSuccess = false,
                    NaturalRoll = 1
                });

                // below50Percent (Hero 100 → 49)
                Play(new BattleEvent { Actor = "Goblin", Target = "Hero", Damage = 51, IsSuccess = true });

                // below10Percent (Hero 49 → 9)
                Play(new BattleEvent { Actor = "Goblin", Target = "Hero", Damage = 40, IsSuccess = true });

                // Pad action counts to taunt thresholds (player ~11, enemy ~9 at balance 0.8).
                // Zero damage so health/threshold flags stay put.
                for (int i = 0; i < 14; i++)
                    Play(new BattleEvent { Actor = "Hero", Target = "Goblin", Damage = 0, IsSuccess = true });
                for (int i = 0; i < 12; i++)
                    Play(new BattleEvent { Actor = "Goblin", Target = "Hero", Damage = 0, IsSuccess = true });

                TestBase.AssertTrue(collected.Any(MatchesCombatBank("firstBlood")),
                    "full fight should display firstBlood",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("criticalHit", ("name", "Hero"))(line)),
                    "full fight should display criticalHit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("criticalMiss", ("name", "Hero"))(line)),
                    "full fight should display criticalMiss",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("below50Percent", ("name", "Hero"))(line)),
                    "full fight should display below50Percent",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("below10Percent", ("name", "Hero"))(line)),
                    "full fight should display below10Percent",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("healthLeadChange", ("name", "Hero"))(line)),
                    "full fight should display healthLeadChange",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(IsPlayerTauntLine),
                    "full fight should display playerTaunt",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(IsEnemyTauntLine),
                    "full fight should display enemyTaunt",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                settings.EnableNarrativeEvents = prevEnable;
                settings.NarrativeBalance = prevBalance;
            }
        }

        /// <summary>
        /// Live combat path: ExecuteActionWithUIAndStatusEffectsColored → GetTriggeredNarrativesIfSignificant
        /// → BlockMessageCollector (the same collector CombatTurnHandler feeds the combat log).
        /// </summary>
        private static void TestLiveExecutePathPutsNarrativesInCombatLog()
        {
            Console.WriteLine("\n--- Testing live execute path puts narratives in combat log ---");

            var settings = GameSettings.Instance;
            bool prevEnable = settings.EnableNarrativeEvents;
            double prevBalance = settings.NarrativeBalance;
            settings.EnableNarrativeEvents = true;
            settings.NarrativeBalance = 0.8;

            ActionSelector.ClearStoredRolls();
            try
            {
                var hero = TestDataBuilders.Character().WithName("Hero").WithStats(20, 20, 20, 20).Build();
                hero.MaxHealth = 100;
                hero.CurrentHealth = 100;
                var goblin = TestDataBuilders.Enemy().WithName("Goblin").WithHealth(200).WithStats(12, 5, 5, 5).Build();
                var jab = TestDataBuilders.CreateMockAction("JAB");
                var narrative = new BattleNarrative("Hero", "Goblin", "Hall", 100, goblin.CurrentHealth);
                var combatLog = new List<string>();

                void Play(Character source, Character target, int roll)
                {
                    ActionSelector.SetStoredActionRoll(source, roll);
                    var ((actionText, rollInfo), statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                        source, target, jab, null, null, narrative);
                    var displayed = narrative.GetTriggeredNarrativesIfSignificant();
                    var narrativeColored = new List<List<ColoredText>>();
                    foreach (var line in displayed)
                    {
                        if (string.IsNullOrEmpty(line))
                            continue;
                        var parsed = ColoredTextParser.Parse(line);
                        if (parsed.Count > 0)
                            narrativeColored.Add(parsed);
                    }
                    var messages = BlockMessageCollector.CollectActionBlockMessages(
                        actionText, rollInfo, statusEffects, null, narrativeColored);
                    foreach (var (segments, _) in messages)
                        combatLog.Add(ColoredTextRenderer.RenderAsPlainText(segments));
                }

                Play(hero, goblin, 20);
                Play(hero, goblin, 20);
                for (int i = 0; i < 8 && !combatLog.Any(line => MatchesCombatBank("below50Percent", ("name", "Hero"))(line)); i++)
                    Play(goblin, hero, 16);
                for (int i = 0; i < 14; i++)
                    Play(hero, goblin, 2);
                for (int i = 0; i < 12; i++)
                    Play(goblin, hero, 2);

                TestBase.AssertTrue(combatLog.Any(MatchesCombatBank("firstBlood")),
                    "live combat log should display firstBlood",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(combatLog.Any(line => MatchesCombatBank("criticalHit", ("name", "Hero"))(line)),
                    "live combat log should display criticalHit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(combatLog.Any(line => MatchesCombatBank("below50Percent", ("name", "Hero"))(line)),
                    "live combat log should display below50Percent",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(combatLog.Any(IsEnemyTauntLine),
                    "live combat log should display enemyTaunt",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                ActionSelector.ClearStoredRolls();
                settings.EnableNarrativeEvents = prevEnable;
                settings.NarrativeBalance = prevBalance;
            }
        }

        /// <summary>
        /// Biome taunts: FlavorText banks load, GetLocationType matches real Rooms.json
        /// display names, and GetLocationSpecificTaunt fills tokens from the matching bank.
        /// </summary>
        private static void TestBiomeTauntBanksLoadAndSelectFromRealRooms()
        {
            Console.WriteLine("\n--- Testing biome taunt banks vs real room names ---");

            FlavorText.Reload();
            var data = FlavorText.GetData();
            var tauntSystem = new TauntSystem(new NarrativeTextProvider());

            var cases = new (string Room, string LocationType, string PlayerBank, string EnemyBank)[]
            {
                ("Crypt Passage", "crypt", "playerTaunt_crypt", "enemyTaunt_crypt"),
                ("Crystal Garden", "crystal", "playerTaunt_crystal", "enemyTaunt_crystal"),
                ("Geode Chamber", "crystal", "playerTaunt_crystal", "enemyTaunt_crystal"),
                ("Lava Chamber", "lava", "playerTaunt_lava", "enemyTaunt_lava"),
                ("Magma Pool", "lava", "playerTaunt_lava", "enemyTaunt_lava"),
                ("Volcanic Vent", "lava", "playerTaunt_lava", "enemyTaunt_lava"),
                ("Sacred Altar", "temple", "playerTaunt_temple", "enemyTaunt_temple"),
                ("Library", "library", "playerTaunt_library", "enemyTaunt_library"),
                ("Underwater Cavern", "underwater", "playerTaunt_underwater", "enemyTaunt_underwater"),
                ("Frozen Cavern", "ice", "playerTaunt_ice", "enemyTaunt_ice"),
                ("Glacial Chamber", "ice", "playerTaunt_ice", "enemyTaunt_ice"),
                ("Marsh Sanctuary", "swamp", "playerTaunt_swamp", "enemyTaunt_swamp"),
                ("Bog Clearing", "swamp", "playerTaunt_swamp", "enemyTaunt_swamp"),
            };

            foreach (var (room, locationType, playerBank, enemyBank) in cases)
            {
                TestBase.AssertTrue(
                    data.CombatNarratives.TryGetValue(playerBank, out var playerLines)
                    && playerLines != null && playerLines.Length == 4,
                    $"{playerBank} should load 4 lines",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(
                    data.CombatNarratives.TryGetValue(enemyBank, out var enemyLines)
                    && enemyLines != null && enemyLines.Length == 4,
                    $"{enemyBank} should load 4 lines",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(locationType, tauntSystem.GetLocationType(room),
                    $"{room} should map to {locationType}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                string playerTaunt = tauntSystem.GetLocationSpecificTaunt("player", "Hero", "Goblin", room);
                TestBase.AssertTrue(
                    MatchesCombatBank(playerBank, ("name", "Hero"), ("enemy", "Goblin"))(playerTaunt)
                    && !playerTaunt.Contains("{enemy}", StringComparison.Ordinal)
                    && !playerTaunt.Contains("{name}", StringComparison.Ordinal),
                    $"{room} player taunt should come from {playerBank} with tokens filled",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                string enemyTaunt = tauntSystem.GetLocationSpecificTaunt("enemy", "Goblin", "Hero", room);
                TestBase.AssertTrue(
                    MatchesCombatBank(enemyBank, ("name", "Goblin"), ("player", "Hero"))(enemyTaunt)
                    && !enemyTaunt.Contains("{player}", StringComparison.Ordinal)
                    && !enemyTaunt.Contains("{name}", StringComparison.Ordinal),
                    $"{room} enemy taunt should come from {enemyBank} with tokens filled",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.AssertTrue(
                data.CombatNarratives.TryGetValue("enemyTaunt_crystal", out var crystalEnemy)
                && crystalEnemy != null
                && crystalEnemy.Any(line => line.Contains("—", StringComparison.Ordinal)),
                "enemyTaunt_crystal should preserve the em-dash line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Ice/Swamp rooms must use ice/swamp banks, not Crystal/Temple (cavern/sanctuary)
        /// or generic. Crystal/Temple/crypt/forest/library/lava/underwater stay put.
        /// </summary>
        private static void TestIceAndSwampRoomsRouteToIceAndSwampTaunts()
        {
            Console.WriteLine("\n--- Testing Ice/Swamp rooms route to ice/swamp taunts ---");

            FlavorText.Reload();
            var tauntSystem = new TauntSystem(new NarrativeTextProvider());

            TestBase.AssertEqual("ice", tauntSystem.GetLocationType("Frozen Cavern"),
                "Frozen Cavern should match ice (frozen), not crystal via cavern",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("ice", tauntSystem.GetLocationType("Glacial Chamber"),
                "Glacial Chamber should match ice via glacial",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("ice", tauntSystem.GetLocationType("Frozen Lake"),
                "Frozen Lake should match ice via frozen",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("swamp", tauntSystem.GetLocationType("Marsh Sanctuary"),
                "Marsh Sanctuary should match swamp (marsh), not temple via sanctuary",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("swamp", tauntSystem.GetLocationType("Bog Clearing"),
                "Bog Clearing should match swamp via bog",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("crystal", tauntSystem.GetLocationType("Crystal Garden"),
                "Crystal Garden should stay crystal",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("crystal", tauntSystem.GetLocationType("Geode Chamber"),
                "Geode Chamber should stay crystal",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("crystal", tauntSystem.GetLocationType("Crystal Cave"),
                "Crystal Cave should still match crystal by crystal, not cave",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("temple", tauntSystem.GetLocationType("Sacred Altar"),
                "Sacred Altar should stay temple",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("temple", tauntSystem.GetLocationType("Lost Shrine"),
                "Lost Shrine should stay temple",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("crypt", tauntSystem.GetLocationType("Crypt Passage"),
                "Crypt Passage should stay crypt",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("library", tauntSystem.GetLocationType("Library"),
                "Library should stay library",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("lava", tauntSystem.GetLocationType("Lava Chamber"),
                "Lava Chamber should stay lava",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("underwater", tauntSystem.GetLocationType("Underwater Cavern"),
                "Underwater Cavern should stay underwater",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("forest", tauntSystem.GetLocationType("Dark Forest"),
                "Dark Forest should stay forest",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string frozenPlayer = tauntSystem.GetLocationSpecificTaunt("player", "Hero", "Goblin", "Frozen Cavern");
            TestBase.AssertTrue(
                MatchesCombatBank("playerTaunt_ice", ("name", "Hero"), ("enemy", "Goblin"))(frozenPlayer)
                && !frozenPlayer.Contains("{enemy}", StringComparison.Ordinal)
                && !frozenPlayer.Contains("{name}", StringComparison.Ordinal)
                && !MatchesCombatBank("playerTaunt_crystal", ("name", "Hero"), ("enemy", "Goblin"))(frozenPlayer)
                && !MatchesCombatBank("playerTaunt", ("name", "Hero"), ("enemy", "Goblin"))(frozenPlayer),
                "Frozen Cavern player taunt should be ice, not crystal or generic",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string frozenEnemy = tauntSystem.GetLocationSpecificTaunt("enemy", "Goblin", "Hero", "Frozen Cavern");
            TestBase.AssertTrue(
                MatchesCombatBank("enemyTaunt_ice", ("name", "Goblin"), ("player", "Hero"))(frozenEnemy)
                && !frozenEnemy.Contains("{player}", StringComparison.Ordinal)
                && !frozenEnemy.Contains("{name}", StringComparison.Ordinal)
                && !MatchesCombatBank("enemyTaunt_crystal", ("name", "Goblin"), ("player", "Hero"))(frozenEnemy)
                && !MatchesCombatBank("enemyTaunt", ("name", "Goblin"), ("player", "Hero"))(frozenEnemy),
                "Frozen Cavern enemy taunt should be ice, not crystal or generic",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string marshPlayer = tauntSystem.GetLocationSpecificTaunt("player", "Hero", "Goblin", "Marsh Sanctuary");
            TestBase.AssertTrue(
                MatchesCombatBank("playerTaunt_swamp", ("name", "Hero"), ("enemy", "Goblin"))(marshPlayer)
                && !marshPlayer.Contains("{enemy}", StringComparison.Ordinal)
                && !marshPlayer.Contains("{name}", StringComparison.Ordinal)
                && !MatchesCombatBank("playerTaunt_temple", ("name", "Hero"), ("enemy", "Goblin"))(marshPlayer)
                && !MatchesCombatBank("playerTaunt", ("name", "Hero"), ("enemy", "Goblin"))(marshPlayer),
                "Marsh Sanctuary player taunt should be swamp, not temple or generic",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string marshEnemy = tauntSystem.GetLocationSpecificTaunt("enemy", "Goblin", "Hero", "Marsh Sanctuary");
            TestBase.AssertTrue(
                MatchesCombatBank("enemyTaunt_swamp", ("name", "Goblin"), ("player", "Hero"))(marshEnemy)
                && !marshEnemy.Contains("{player}", StringComparison.Ordinal)
                && !marshEnemy.Contains("{name}", StringComparison.Ordinal)
                && !MatchesCombatBank("enemyTaunt_temple", ("name", "Goblin"), ("player", "Hero"))(marshEnemy)
                && !MatchesCombatBank("enemyTaunt", ("name", "Goblin"), ("player", "Hero"))(marshEnemy),
                "Marsh Sanctuary enemy taunt should be swamp, not temple or generic",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Creature-tier combat banks load from FlavorText and are selected for real Enemies.json rows
        /// (Spider / Wolf / Goblin) instead of the unsuffixed generic fallback.
        /// </summary>
        private static void TestCreatureTierBanksLoadAndSelectForRealEnemies()
        {
            Console.WriteLine("\n--- Testing creature-tier banks vs real enemies ---");

            FlavorText.Reload();
            EnemyLoader.LoadEnemies();
            var data = FlavorText.GetData();
            var textProvider = new NarrativeTextProvider();
            var tauntSystem = new TauntSystem(textProvider);
            var settings = new GameSettings { NarrativeBalance = 0 };
            int tauntActions = tauntSystem.GetEnemyTauntThreshold(0, settings);

            var cases = new (string EnemyName, string Tier)[]
            {
                ("Spider", CreatureTierIds.NativeFauna),
                ("Wolf", CreatureTierIds.FeralStock),
                ("Goblin", CreatureTierIds.TechnoEcho),
            };

            foreach (var (enemyName, tier) in cases)
            {
                var enemyData = EnemyLoader.GetEnemyData(enemyName);
                TestBase.AssertEqual(tier, enemyData?.CreatureTier,
                    $"{enemyName} should load creatureTier {tier}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                foreach (var bank in CreatureTierIds.NarrativeBanks)
                {
                    string key = CreatureTierIds.SuffixedBank(bank, tier);
                    TestBase.AssertTrue(
                        data.CombatNarratives.TryGetValue(key, out var lines)
                        && lines != null && lines.Length == 3
                        && lines.All(l => !string.IsNullOrWhiteSpace(l)),
                        $"{key} should load 3 authored lines",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    if (bank == "enemyTaunt")
                        continue;

                    string narrative = textProvider.ReplacePlaceholders(
                        textProvider.GetCreatureTieredNarrative(bank, tier),
                        new Dictionary<string, string> { ["name"] = enemyName, ["player"] = "Hero" });
                    TestBase.AssertTrue(
                        LineEqualsFilledBank(narrative, key, ("name", enemyName), ("player", "Hero"))
                        && !LineEqualsFilledBank(narrative, bank, ("name", enemyName), ("player", "Hero"))
                        && !narrative.Contains("{name}", StringComparison.Ordinal)
                        && !narrative.Contains("{player}", StringComparison.Ordinal),
                        $"{enemyName} {bank} should come from {key}, not generic, with tokens filled",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }

                var (shouldTaunt, tauntText) = tauntSystem.CheckEnemyTaunt(
                    tauntActions, 0, enemyName, "Hero", "Frozen Cavern", settings, tier);
                string tauntKey = CreatureTierIds.SuffixedBank("enemyTaunt", tier);
                TestBase.AssertTrue(
                    shouldTaunt
                    && LineEqualsFilledBank(tauntText, tauntKey, ("name", enemyName), ("player", "Hero"))
                    && !LineEqualsFilledBank(tauntText, "enemyTaunt", ("name", enemyName), ("player", "Hero"))
                    && !tauntText.Contains("{player}", StringComparison.Ordinal)
                    && !tauntText.Contains("{name}", StringComparison.Ordinal),
                    $"{enemyName} enemy taunt should come from {tauntKey}, not generic",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        /// <summary>
        /// Combat log shows AnalyzeEvent strings via GetTriggeredNarrativesIfSignificant.
        /// firstBlood_technoEcho authors {name}; the display path must fill it (Goblin), not dump the template.
        /// </summary>
        private static void TestFirstBloodTechnoEchoFillsNameOnDisplayPath()
        {
            Console.WriteLine("\n--- Testing firstBlood_technoEcho fills {name} on the combat-log display path ---");

            const string liveLine = "The cut opens clean and even, like {name} measured it first.";
            var settings = GameSettings.Instance;
            bool prevEnable = settings.EnableNarrativeEvents;
            double prevBalance = settings.NarrativeBalance;
            settings.EnableNarrativeEvents = true;
            settings.NarrativeBalance = 0.8;

            var data = FlavorText.GetData();
            data.CombatNarratives.TryGetValue("firstBlood_technoEcho", out var previous);

            try
            {
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.firstBlood_technoEcho", new[] { liveLine });
                var narrative = new BattleNarrative(
                    "Seamus Ashwhisper", "Goblin", "Geode Chamber", 100, 100, CreatureTierIds.TechnoEcho);
                narrative.AddEvent(new BattleEvent
                {
                    Actor = "Seamus Ashwhisper",
                    Target = "Goblin",
                    Damage = 10,
                    IsSuccess = true
                });

                var displayed = narrative.GetTriggeredNarrativesIfSignificant();
                TestBase.AssertTrue(
                    displayed.Any(l => l.Contains("Goblin", StringComparison.Ordinal)
                        && l.Contains("measured it first", StringComparison.Ordinal)
                        && !l.Contains("{name}", StringComparison.Ordinal)),
                    "combat-log firstBlood_technoEcho must fill {name} with Goblin",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!displayed.Any(l => l.Contains("{name}", StringComparison.Ordinal)),
                    "combat-log firstBlood must never leave {name} unsubstituted",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                FlavorTextBankCatalog.SetBank(data, "combatNarratives.firstBlood_technoEcho", previous ?? Array.Empty<string>());
                settings.EnableNarrativeEvents = prevEnable;
                settings.NarrativeBalance = prevBalance;
            }
        }

        private static bool LineEqualsFilledBank(string line, string key, params (string Token, string Value)[] replacements)
        {
            if (string.IsNullOrEmpty(line))
                return false;
            if (!FlavorText.GetData().CombatNarratives.TryGetValue(key, out var bank) || bank == null)
                return false;
            foreach (var template in bank)
            {
                string filled = template;
                foreach (var (token, value) in replacements)
                    filled = filled.Replace("{" + token + "}", value);
                if (string.Equals(line, filled, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static Func<string, bool> MatchesCombatBank(string key, params (string Token, string Value)[] replacements)
        {
            return line =>
            {
                if (string.IsNullOrEmpty(line))
                    return false;
                if (!FlavorText.GetData().CombatNarratives.TryGetValue(key, out var bank) || bank == null)
                    return false;
                foreach (var template in bank)
                {
                    string filled = template;
                    foreach (var (token, value) in replacements)
                        filled = filled.Replace("{" + token + "}", value);
                    if (string.Equals(line, filled, StringComparison.Ordinal)
                        || line.Contains(filled, StringComparison.Ordinal))
                        return true;
                }
                return false;
            };
        }

        private static bool IsPlayerTauntLine(string line)
        {
            if (MatchesCombatBank("playerTaunt", ("name", "Hero"), ("enemy", "Goblin"))(line))
                return true;
            foreach (var key in FlavorText.GetData().CombatNarratives.Keys)
            {
                if (key.StartsWith("playerTaunt", StringComparison.OrdinalIgnoreCase)
                    && MatchesCombatBank(key, ("name", "Hero"), ("enemy", "Goblin"))(line))
                    return true;
            }
            return false;
        }

        private static bool IsEnemyTauntLine(string line)
        {
            if (MatchesCombatBank("enemyTaunt", ("name", "Goblin"), ("player", "Hero"))(line))
                return true;
            foreach (var key in FlavorText.GetData().CombatNarratives.Keys)
            {
                if (key.StartsWith("enemyTaunt", StringComparison.OrdinalIgnoreCase)
                    && MatchesCombatBank(key, ("name", "Goblin"), ("player", "Hero"))(line))
                    return true;
            }
            return false;
        }

        #endregion
    }
}
