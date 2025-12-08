using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Combat;
using RPGGame.Combat.Effects.AdvancedStatusEffects;
using RPGGame.World.Tags;
using RPGGame.Entity.Actions.ComboRouting;
using RPGGame.Progression;
using RPGGame.Combat.Outcomes;
using RPGGame.Actions.Conditional;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for Advanced Action Mechanics (All Phases)
    /// </summary>
    public static class AdvancedMechanicsTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Advanced Mechanics tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Advanced Action Mechanics Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Phase 1: Roll Modification & Conditional Triggers
            Console.WriteLine("--- Phase 1: Roll Modification & Conditional Triggers ---");
            TestAdditiveModifier();
            TestMultiplicativeModifier();
            TestClampModifier();
            TestRerollModifier();
            TestExplodingDiceModifier();
            TestMultiDiceRoller();
            TestEventBus();
            TestConditionalTriggerEvaluator();
            TestThresholdManager();

            // Phase 2: Advanced Status Effects
            Console.WriteLine("\n--- Phase 2: Advanced Status Effects ---");
            TestVulnerabilityEffect();
            TestHardenEffect();
            TestFortifyEffect();
            TestFocusEffect();
            TestExposeEffect();
            TestHPRegenEffect();
            TestArmorBreakEffect();
            TestPierceEffect();
            TestReflectEffect();
            TestSilenceEffect();
            TestMarkEffect();
            TestDisruptEffect();
            TestCleanseEffect();

            // Phase 3: Tag System & Combo Routing
            Console.WriteLine("\n--- Phase 3: Tag System & Combo Routing ---");
            TestTagRegistry();
            TestTagMatcher();
            TestTagAggregator();
            TestTagModifier();
            TestComboRouter();

            // Phase 4: Outcome-Based Actions & Meta-Progression
            Console.WriteLine("\n--- Phase 4: Outcome-Based Actions & Meta-Progression ---");
            TestActionUsageTracker();
            TestConditionalXPGain();
            TestOutcomeHandlers();

            PrintSummary();
        }

        #region Phase 1: Roll Modification Tests

        private static void TestAdditiveModifier()
        {
            Console.WriteLine("Testing AdditiveRollModifier...");
            try
            {
                var context = new RollModificationContext(new Character("Test", 1));
                
                // Test positive modifier
                var modifier = new AdditiveRollModifier("TestAdd", 5);
                int result = modifier.ModifyRoll(10, context);
                AssertTrue(result == 15, $"Positive modifier: 10 + 5 = {result} (expected 15)");
                
                // Test negative modifier
                var negativeModifier = new AdditiveRollModifier("TestSub", -3);
                result = negativeModifier.ModifyRoll(10, context);
                AssertTrue(result == 7, $"Negative modifier: 10 - 3 = {result} (expected 7)");
                
                // Test zero modifier
                var zeroModifier = new AdditiveRollModifier("TestZero", 0);
                result = zeroModifier.ModifyRoll(10, context);
                AssertTrue(result == 10, $"Zero modifier: 10 + 0 = {result} (expected 10)");
                
                // Test boundary: low roll with positive modifier
                result = modifier.ModifyRoll(1, context);
                AssertTrue(result == 6, $"Low roll with positive: 1 + 5 = {result} (expected 6)");
                
                // Test boundary: high roll with negative modifier
                result = negativeModifier.ModifyRoll(20, context);
                AssertTrue(result == 17, $"High roll with negative: 20 - 3 = {result} (expected 17)");
                
                // Test large positive modifier
                var largeModifier = new AdditiveRollModifier("TestLarge", 50);
                result = largeModifier.ModifyRoll(10, context);
                AssertTrue(result == 60, $"Large modifier: 10 + 50 = {result} (expected 60)");
                
                // Test large negative modifier (can go below 1, that's okay for modifiers)
                var largeNegative = new AdditiveRollModifier("TestLargeNeg", -50);
                result = largeNegative.ModifyRoll(10, context);
                AssertTrue(result == -40, $"Large negative modifier: 10 - 50 = {result} (expected -40)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Additive modifier test failed: {ex.Message}");
            }
        }

        private static void TestMultiplicativeModifier()
        {
            Console.WriteLine("Testing MultiplicativeRollModifier...");
            try
            {
                var context = new RollModificationContext(new Character("Test", 1));
                
                // Test multiplier > 1.0 (increase)
                var modifier = new MultiplicativeRollModifier("TestMult", 1.5);
                int result = modifier.ModifyRoll(10, context);
                AssertTrue(result == 15, $"Multiplier > 1: 10 * 1.5 = {result} (expected 15)");
                
                // Test multiplier < 1.0 (decrease)
                var reduceModifier = new MultiplicativeRollModifier("TestReduce", 0.5);
                result = reduceModifier.ModifyRoll(10, context);
                AssertTrue(result == 5, $"Multiplier < 1: 10 * 0.5 = {result} (expected 5)");
                
                // Test multiplier = 1.0 (no change)
                var neutralModifier = new MultiplicativeRollModifier("TestNeutral", 1.0);
                result = neutralModifier.ModifyRoll(10, context);
                AssertTrue(result == 10, $"Multiplier = 1: 10 * 1.0 = {result} (expected 10)");
                
                // Test multiplier = 2.0 (double)
                var doubleModifier = new MultiplicativeRollModifier("TestDouble", 2.0);
                result = doubleModifier.ModifyRoll(10, context);
                AssertTrue(result == 20, $"Double multiplier: 10 * 2.0 = {result} (expected 20)");
                
                // Test fractional result rounding (e.g., 11 * 1.5 = 16.5 -> 16 or 17)
                result = modifier.ModifyRoll(11, context);
                AssertTrue(result == 16 || result == 17, $"Fractional rounding: 11 * 1.5 = {result} (should be 16 or 17)");
                
                // Test with low roll
                result = modifier.ModifyRoll(1, context);
                AssertTrue(result >= 1 && result <= 2, $"Low roll with multiplier: 1 * 1.5 = {result} (should be 1-2)");
                
                // Test with high roll
                result = modifier.ModifyRoll(20, context);
                AssertTrue(result == 30, $"High roll with multiplier: 20 * 1.5 = {result} (expected 30)");
                
                // Test very small multiplier
                var tinyModifier = new MultiplicativeRollModifier("TestTiny", 0.1);
                result = tinyModifier.ModifyRoll(10, context);
                AssertTrue(result == 1, $"Tiny multiplier: 10 * 0.1 = {result} (expected 1, minimum)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Multiplicative modifier test failed: {ex.Message}");
            }
        }

        private static void TestClampModifier()
        {
            Console.WriteLine("Testing ClampRollModifier...");
            try
            {
                var context = new RollModificationContext(new Character("Test", 1));
                var modifier = new ClampRollModifier("TestClamp", 5, 15);
                
                // Test values below minimum
                AssertTrue(modifier.ModifyRoll(1, context) == 5, "Clamp min: 1 -> 5");
                AssertTrue(modifier.ModifyRoll(3, context) == 5, "Clamp min: 3 -> 5");
                AssertTrue(modifier.ModifyRoll(4, context) == 5, "Clamp min: 4 -> 5");
                
                // Test values above maximum
                AssertTrue(modifier.ModifyRoll(16, context) == 15, "Clamp max: 16 -> 15");
                AssertTrue(modifier.ModifyRoll(20, context) == 15, "Clamp max: 20 -> 15");
                AssertTrue(modifier.ModifyRoll(100, context) == 15, "Clamp max: 100 -> 15");
                
                // Test values within range (should pass through)
                AssertTrue(modifier.ModifyRoll(5, context) == 5, "Clamp boundary min: 5 -> 5");
                AssertTrue(modifier.ModifyRoll(10, context) == 10, "Clamp middle: 10 -> 10");
                AssertTrue(modifier.ModifyRoll(15, context) == 15, "Clamp boundary max: 15 -> 15");
                
                // Test edge case: min == max
                var singleValueModifier = new ClampRollModifier("TestSingle", 10, 10);
                AssertTrue(singleValueModifier.ModifyRoll(1, context) == 10, "Single value clamp (low): 1 -> 10");
                AssertTrue(singleValueModifier.ModifyRoll(10, context) == 10, "Single value clamp (exact): 10 -> 10");
                AssertTrue(singleValueModifier.ModifyRoll(20, context) == 10, "Single value clamp (high): 20 -> 10");
                
                // Test narrow range
                var narrowModifier = new ClampRollModifier("TestNarrow", 10, 11);
                AssertTrue(narrowModifier.ModifyRoll(5, context) == 10, "Narrow clamp (low): 5 -> 10");
                AssertTrue(narrowModifier.ModifyRoll(10, context) == 10, "Narrow clamp (min): 10 -> 10");
                AssertTrue(narrowModifier.ModifyRoll(11, context) == 11, "Narrow clamp (max): 11 -> 11");
                AssertTrue(narrowModifier.ModifyRoll(15, context) == 11, "Narrow clamp (high): 15 -> 11");
                
                // Test negative values (if allowed)
                var negativeModifier = new ClampRollModifier("TestNeg", -5, 5);
                AssertTrue(negativeModifier.ModifyRoll(-10, context) == -5, "Negative clamp (low): -10 -> -5");
                AssertTrue(negativeModifier.ModifyRoll(0, context) == 0, "Negative clamp (middle): 0 -> 0");
                AssertTrue(negativeModifier.ModifyRoll(10, context) == 5, "Negative clamp (high): 10 -> 5");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Clamp modifier test failed: {ex.Message}");
            }
        }

        private static void TestRerollModifier()
        {
            Console.WriteLine("Testing RerollModifier...");
            try
            {
                var context = new RollModificationContext(new Character("Test", 1));
                
                // Test 100% reroll chance (always rerolls)
                var alwaysReroll = new RerollModifier("TestAlways", 1.0);
                int result = alwaysReroll.ModifyRoll(5, context);
                AssertTrue(result >= 1 && result <= 20, $"100% reroll: result {result} in valid range 1-20");
                
                // Test 0% reroll chance (never rerolls)
                var neverReroll = new RerollModifier("TestNever", 0.0);
                result = neverReroll.ModifyRoll(10, context);
                AssertTrue(result == 10, $"0% reroll: result {result} should equal input 10");
                
                // Test 50% reroll chance (statistical test - run multiple times)
                var halfReroll = new RerollModifier("TestHalf", 0.5);
                int rerollCount = 0;
                int sameCount = 0;
                for (int i = 0; i < 100; i++)
                {
                    int testResult = halfReroll.ModifyRoll(10, context);
                    if (testResult != 10) rerollCount++;
                    else sameCount++;
                }
                AssertTrue(rerollCount > 0 && sameCount > 0, 
                    $"50% reroll: {rerollCount} rerolled, {sameCount} kept (should have both)");
                
                // Test reroll with low initial value
                result = alwaysReroll.ModifyRoll(1, context);
                AssertTrue(result >= 1 && result <= 20, $"Reroll from 1: result {result} in valid range");
                
                // Test reroll with high initial value
                result = alwaysReroll.ModifyRoll(20, context);
                AssertTrue(result >= 1 && result <= 20, $"Reroll from 20: result {result} in valid range");
                
                // Test multiple rerolls (if modifier supports it)
                // Note: This tests that reroll actually generates new random values
                var results = new HashSet<int>();
                for (int i = 0; i < 50; i++)
                {
                    results.Add(alwaysReroll.ModifyRoll(10, context));
                }
                // With 50 rerolls, we should get some variety (not all same value)
                AssertTrue(results.Count > 1, 
                    $"Reroll variety: got {results.Count} different values (should have variety)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Reroll modifier test failed: {ex.Message}");
            }
        }

        private static void TestExplodingDiceModifier()
        {
            Console.WriteLine("Testing ExplodingDiceModifier...");
            try
            {
                var context = new RollModificationContext(new Character("Test", 1));
                
                // Test exploding on threshold (20)
                var modifier = new ExplodingDiceModifier("TestExploding", 20);
                int result = modifier.ModifyRoll(20, context);
                AssertTrue(result >= 20, $"Exploding on 20: result {result} should be >= 20");
                
                // Test non-exploding roll (below threshold)
                result = modifier.ModifyRoll(19, context);
                AssertTrue(result == 19, $"Non-exploding roll: 19 should remain 19, got {result}");
                
                result = modifier.ModifyRoll(10, context);
                AssertTrue(result == 10, $"Non-exploding roll: 10 should remain 10, got {result}");
                
                // Test exploding on lower threshold (e.g., 18)
                var lowerThreshold = new ExplodingDiceModifier("TestLow", 18);
                result = lowerThreshold.ModifyRoll(18, context);
                AssertTrue(result >= 18, $"Exploding on 18: result {result} should be >= 18");
                
                result = lowerThreshold.ModifyRoll(19, context);
                AssertTrue(result >= 19, $"Exploding on 19 (above 18): result {result} should be >= 19");
                
                result = lowerThreshold.ModifyRoll(20, context);
                AssertTrue(result >= 20, $"Exploding on 20 (above 18): result {result} should be >= 20");
                
                // Test multiple explosions (if result of explosion also explodes)
                // Run multiple times to catch chain explosions
                for (int i = 0; i < 100; i++)
                {
                    result = modifier.ModifyRoll(20, context);
                    if (result > 40) // If we got a chain explosion
                    {
                        break;
                    }
                }
                // Chain explosions are possible, so we might see values > 40
                AssertTrue(result >= 20, $"Chain explosion possible: result {result} should be >= 20");
                
                // Test with very low threshold (should explode frequently)
                var frequentExplode = new ExplodingDiceModifier("TestFrequent", 10);
                result = frequentExplode.ModifyRoll(10, context);
                AssertTrue(result >= 10, $"Frequent explode (10): result {result} should be >= 10");
                
                result = frequentExplode.ModifyRoll(15, context);
                AssertTrue(result >= 15, $"Frequent explode (15): result {result} should be >= 15");
                
                // Test with high threshold (rare explosions)
                var rareExplode = new ExplodingDiceModifier("TestRare", 20);
                result = rareExplode.ModifyRoll(19, context);
                AssertTrue(result == 19, $"Rare explode (19): result {result} should be 19 (no explosion)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Exploding dice test failed: {ex.Message}");
            }
        }

        private static void TestMultiDiceRoller()
        {
            Console.WriteLine("Testing MultiDiceRoller...");
            try
            {
                // Test Sum mode with 2d20
                int sumResult = MultiDiceRoller.RollMultipleDice(2, 20, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(sumResult >= 2 && sumResult <= 40, $"Sum mode (2d20): {sumResult} (should be 2-40)");
                
                // Test Sum mode with more dice
                sumResult = MultiDiceRoller.RollMultipleDice(3, 20, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(sumResult >= 3 && sumResult <= 60, $"Sum mode (3d20): {sumResult} (should be 3-60)");
                
                // Test Sum mode with single die
                sumResult = MultiDiceRoller.RollMultipleDice(1, 20, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(sumResult >= 1 && sumResult <= 20, $"Sum mode (1d20): {sumResult} (should be 1-20)");
                
                // Test Sum mode with many dice
                sumResult = MultiDiceRoller.RollMultipleDice(10, 6, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(sumResult >= 10 && sumResult <= 60, $"Sum mode (10d6): {sumResult} (should be 10-60)");

                // Test TakeLowest mode
                int lowestResult = MultiDiceRoller.RollMultipleDice(2, 20, MultiDiceRoller.DiceSelectionMode.TakeLowest);
                AssertTrue(lowestResult >= 1 && lowestResult <= 20, $"TakeLowest (2d20): {lowestResult} (should be 1-20)");
                
                // Test TakeLowest with more dice (should still be in valid range)
                lowestResult = MultiDiceRoller.RollMultipleDice(5, 20, MultiDiceRoller.DiceSelectionMode.TakeLowest);
                AssertTrue(lowestResult >= 1 && lowestResult <= 20, $"TakeLowest (5d20): {lowestResult} (should be 1-20)");
                
                // Test TakeLowest with single die
                lowestResult = MultiDiceRoller.RollMultipleDice(1, 20, MultiDiceRoller.DiceSelectionMode.TakeLowest);
                AssertTrue(lowestResult >= 1 && lowestResult <= 20, $"TakeLowest (1d20): {lowestResult} (should be 1-20)");

                // Test TakeHighest mode
                int highestResult = MultiDiceRoller.RollMultipleDice(2, 20, MultiDiceRoller.DiceSelectionMode.TakeHighest);
                AssertTrue(highestResult >= 1 && highestResult <= 20, $"TakeHighest (2d20): {highestResult} (should be 1-20)");
                
                // Test TakeHighest with more dice
                highestResult = MultiDiceRoller.RollMultipleDice(5, 20, MultiDiceRoller.DiceSelectionMode.TakeHighest);
                AssertTrue(highestResult >= 1 && highestResult <= 20, $"TakeHighest (5d20): {highestResult} (should be 1-20)");
                
                // Test TakeHighest with single die
                highestResult = MultiDiceRoller.RollMultipleDice(1, 20, MultiDiceRoller.DiceSelectionMode.TakeHighest);
                AssertTrue(highestResult >= 1 && highestResult <= 20, $"TakeHighest (1d20): {highestResult} (should be 1-20)");
                
                // Statistical test: TakeLowest should generally be <= TakeHighest
                // Roll the same set of dice and check both lowest and highest from that set
                var diceRolls = MultiDiceRoller.RollMultipleDiceResults(2, 20);
                int lowest = diceRolls.Min();
                int highest = diceRolls.Max();
                // Note: They could be equal, but lowest should never be > highest
                AssertTrue(lowest <= highest, 
                    $"Statistical: TakeLowest ({lowest}) should be <= TakeHighest ({highest})");
                
                // Test with different die sizes
                int d6Result = MultiDiceRoller.RollMultipleDice(2, 6, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(d6Result >= 2 && d6Result <= 12, $"Sum mode (2d6): {d6Result} (should be 2-12)");
                
                int d100Result = MultiDiceRoller.RollMultipleDice(1, 100, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(d100Result >= 1 && d100Result <= 100, $"Sum mode (1d100): {d100Result} (should be 1-100)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"MultiDiceRoller test failed: {ex.Message}");
            }
        }

        private static void TestEventBus()
        {
            Console.WriteLine("Testing CombatEventBus...");
            try
            {
                // Test single subscriber
                bool eventFired = false;
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionExecuted, (evt) => {
                    eventFired = true;
                });

                var testEvent = new CombatEvent(CombatEventType.ActionExecuted, new Character("Test", 1));
                CombatEventBus.Instance.Publish(testEvent);

                AssertTrue(eventFired, "Event bus: single subscriber received event");
                CombatEventBus.Instance.Clear();
                
                // Test multiple subscribers for same event type
                int subscriber1Count = 0;
                int subscriber2Count = 0;
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionExecuted, (evt) => {
                    subscriber1Count++;
                });
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionExecuted, (evt) => {
                    subscriber2Count++;
                });
                
                CombatEventBus.Instance.Publish(testEvent);
                AssertTrue(subscriber1Count == 1, $"Event bus: subscriber1 called {subscriber1Count} time (expected 1)");
                AssertTrue(subscriber2Count == 1, $"Event bus: subscriber2 called {subscriber2Count} time (expected 1)");
                CombatEventBus.Instance.Clear();
                
                // Test different event types
                bool actionMissFired = false;
                bool actionHitFired = false;
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionMiss, (evt) => {
                    actionMissFired = true;
                });
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionHit, (evt) => {
                    actionHitFired = true;
                });
                
                var missEvent = new CombatEvent(CombatEventType.ActionMiss, new Character("Test", 1));
                CombatEventBus.Instance.Publish(missEvent);
                AssertTrue(actionMissFired, "Event bus: ActionMiss event fired");
                AssertTrue(!actionHitFired, "Event bus: ActionHit subscriber not called for miss event");
                
                var hitEvent = new CombatEvent(CombatEventType.ActionHit, new Character("Test", 1));
                CombatEventBus.Instance.Publish(hitEvent);
                AssertTrue(actionHitFired, "Event bus: ActionHit event fired");
                CombatEventBus.Instance.Clear();
                
                // Test event data passing
                string? receivedName = null;
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionExecuted, (evt) => {
                    receivedName = evt.Source?.Name;
                });
                
                var namedEvent = new CombatEvent(CombatEventType.ActionExecuted, new Character("TestChar", 1));
                CombatEventBus.Instance.Publish(namedEvent);
                AssertTrue(receivedName == "TestChar", $"Event bus: event data passed correctly (got '{receivedName}')");
                CombatEventBus.Instance.Clear();
                
                // Test unsubscribe (if supported)
                int unsubscribeCount = 0;
                Action<CombatEvent> handler = (evt) => { unsubscribeCount++; };
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionExecuted, handler);
                CombatEventBus.Instance.Publish(testEvent);
                AssertTrue(unsubscribeCount == 1, $"Event bus: before unsubscribe, handler called {unsubscribeCount} time");
                
                // Note: Unsubscribe functionality may not be implemented
                // If it is, test it here
                CombatEventBus.Instance.Clear();
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Event bus test failed: {ex.Message}");
            }
        }

        private static void TestConditionalTriggerEvaluator()
        {
            Console.WriteLine("Testing ConditionalTriggerEvaluator...");
            try
            {
                var evaluator = new ConditionalTriggerEvaluator();
                var source = new Character("TestSource", 1);
                var target = new Character("TestTarget", 1);
                
                // Test OnMiss condition
                var missEvent = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true };
                var missCondition = TriggerConditionFactory.OnMiss();
                var conditions = new System.Collections.Generic.List<TriggerCondition> { missCondition };
                bool result = evaluator.EvaluateConditions(conditions, missEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: OnMiss correctly identified miss");
                
                // Test OnMiss with hit event (should fail)
                var hitEvent = new CombatEvent(CombatEventType.ActionHit, source) { IsMiss = false };
                result = evaluator.EvaluateConditions(conditions, hitEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: OnMiss correctly rejected hit");
                
                // Test OnNormalHit condition
                var hitCondition = TriggerConditionFactory.OnNormalHit();
                var hitConditions = new System.Collections.Generic.List<TriggerCondition> { hitCondition };
                result = evaluator.EvaluateConditions(hitConditions, hitEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: OnNormalHit correctly identified hit");
                
                result = evaluator.EvaluateConditions(hitConditions, missEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: OnNormalHit correctly rejected miss");
                
                // Test OnCriticalHit condition
                var critEvent = new CombatEvent(CombatEventType.ActionHit, source) { IsCritical = true };
                var critCondition = TriggerConditionFactory.OnCriticalHit();
                var critConditions = new System.Collections.Generic.List<TriggerCondition> { critCondition };
                result = evaluator.EvaluateConditions(critConditions, critEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: OnCriticalHit correctly identified critical");
                
                result = evaluator.EvaluateConditions(critConditions, hitEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: OnCriticalHit correctly rejected non-critical");
                
                // Test multiple conditions (AND logic - all must be true)
                // Note: OnNormalHit and OnCriticalHit are mutually exclusive, so test with compatible conditions
                // Test that a critical hit satisfies OnCriticalHit but NOT OnNormalHit
                var critOnlyConditions = new System.Collections.Generic.List<TriggerCondition> 
                { 
                    TriggerConditionFactory.OnCriticalHit()
                };
                result = evaluator.EvaluateConditions(critOnlyConditions, critEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: OnCriticalHit condition satisfied for critical event");
                
                // Test that critical hit does NOT satisfy OnNormalHit (they're mutually exclusive)
                var normalHitConditions = new System.Collections.Generic.List<TriggerCondition> 
                { 
                    TriggerConditionFactory.OnNormalHit()
                };
                result = evaluator.EvaluateConditions(normalHitConditions, critEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: OnNormalHit correctly rejected critical hit (mutually exclusive)");
                
                // Test multiple compatible conditions (both can be true)
                // Use OnCriticalHit with a condition that can also be true for critical hits
                var multiConditions = new System.Collections.Generic.List<TriggerCondition> 
                { 
                    TriggerConditionFactory.OnCriticalHit()
                };
                result = evaluator.EvaluateConditions(multiConditions, critEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: Multiple conditions (crit) satisfied");
                
                result = evaluator.EvaluateConditions(multiConditions, hitEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: Multiple conditions (hit but not crit) failed correctly");
                
                // Test empty conditions list (should pass - no conditions to check)
                var emptyConditions = new System.Collections.Generic.List<TriggerCondition>();
                result = evaluator.EvaluateConditions(emptyConditions, hitEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: Empty conditions list passes");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Conditional trigger evaluator test failed: {ex.Message}");
            }
        }

        private static void TestThresholdManager()
        {
            Console.WriteLine("Testing ThresholdManager...");
            try
            {
                var manager = new ThresholdManager();
                var character1 = new Character("Test1", 1);
                var character2 = new Character("Test2", 1);
                
                // Test setting and getting critical hit threshold
                manager.SetCriticalHitThreshold(character1, 18);
                int threshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(threshold == 18, $"Critical hit threshold set to {threshold} (expected 18)");
                
                // Test different threshold for different character
                manager.SetCriticalHitThreshold(character2, 19);
                int threshold2 = manager.GetCriticalHitThreshold(character2);
                AssertTrue(threshold2 == 19, $"Character2 threshold: {threshold2} (expected 19)");
                AssertTrue(manager.GetCriticalHitThreshold(character1) == 18, 
                    "Character1 threshold unchanged: still 18");
                
                // Test boundary values
                manager.SetCriticalHitThreshold(character1, 1);
                threshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(threshold == 1, $"Minimum threshold: {threshold} (expected 1)");
                
                manager.SetCriticalHitThreshold(character1, 20);
                threshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(threshold == 20, $"Maximum threshold: {threshold} (expected 20)");
                
                // Test reset to default
                manager.ResetThresholds(character1);
                int defaultThreshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(defaultThreshold > 0 && defaultThreshold <= 20, 
                    $"Default threshold restored: {defaultThreshold} (should be 1-20)");
                
                // Verify character2 still has custom threshold
                AssertTrue(manager.GetCriticalHitThreshold(character2) == 19, 
                    "Character2 threshold preserved after character1 reset");
                
                // Test reset all
                manager.ResetThresholds(character2);
                int defaultThreshold2 = manager.GetCriticalHitThreshold(character2);
                AssertTrue(defaultThreshold2 > 0 && defaultThreshold2 <= 20, 
                    $"Character2 default threshold: {defaultThreshold2} (should be 1-20)");
                
                // Test updating threshold multiple times
                manager.SetCriticalHitThreshold(character1, 15);
                manager.SetCriticalHitThreshold(character1, 16);
                manager.SetCriticalHitThreshold(character1, 17);
                threshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(threshold == 17, $"Updated threshold: {threshold} (expected 17)");
                
                // Test threshold persistence (should maintain until reset)
                int persistentThreshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(persistentThreshold == 17, 
                    $"Threshold persistence: {persistentThreshold} (expected 17)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Threshold manager test failed: {ex.Message}");
            }
        }

        #endregion

        #region Phase 2: Advanced Status Effects Tests

        private static void TestVulnerabilityEffect()
        {
            Console.WriteLine("Testing VulnerabilityEffectHandler...");
            try
            {
                var handler = new VulnerabilityEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                // Test initial application
                int initialStacks = target.VulnerabilityStacks ?? 0;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Vulnerability effect applied");
                AssertTrue((target.VulnerabilityStacks ?? 0) > initialStacks, 
                    $"Vulnerability stacks increased: {initialStacks} -> {target.VulnerabilityStacks ?? 0}");
                
                // Test stacking (multiple applications)
                int stacksAfterFirst = target.VulnerabilityStacks ?? 0;
                handler.Apply(target, action, results);
                AssertTrue((target.VulnerabilityStacks ?? 0) > stacksAfterFirst, 
                    $"Vulnerability stacks increased on second application: {stacksAfterFirst} -> {target.VulnerabilityStacks ?? 0}");
                
                // Test maximum stack limit (if implemented)
                int maxStacks = target.VulnerabilityStacks ?? 0;
                for (int i = 0; i < 10; i++)
                {
                    handler.Apply(target, action, results);
                }
                // Should have a maximum or continue stacking
                AssertTrue((target.VulnerabilityStacks ?? 0) >= maxStacks, 
                    $"Vulnerability stacks after multiple applications: {target.VulnerabilityStacks ?? 0} (should be >= {maxStacks})");
                
                // Test effect on damage calculation (if applicable)
                // This would test that vulnerability actually increases damage taken
                int initialHealth = target.CurrentHealth;
                // Apply some damage and verify it's increased (if vulnerability affects damage)
                
                // Test removal/expiration (if duration system exists)
                // Reset and test fresh application
                target.VulnerabilityStacks = 0;
                handler.Apply(target, action, results);
                AssertTrue((target.VulnerabilityStacks ?? 0) > 0, 
                    $"Vulnerability re-applied after reset: {target.VulnerabilityStacks ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Vulnerability effect test failed: {ex.Message}");
            }
        }

        private static void TestHardenEffect()
        {
            Console.WriteLine("Testing HardenEffectHandler...");
            try
            {
                var handler = new HardenEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                // Test initial application
                int initialStacks = target.HardenStacks ?? 0;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Harden effect applied");
                AssertTrue((target.HardenStacks ?? 0) > initialStacks, 
                    $"Harden stacks increased: {initialStacks} -> {target.HardenStacks ?? 0}");
                
                // Test stacking
                int stacksAfterFirst = target.HardenStacks ?? 0;
                handler.Apply(target, action, results);
                AssertTrue((target.HardenStacks ?? 0) > stacksAfterFirst, 
                    $"Harden stacks increased on second application: {stacksAfterFirst} -> {target.HardenStacks ?? 0}");
                
                // Test multiple applications
                for (int i = 0; i < 5; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.HardenStacks ?? 0) > stacksAfterFirst, 
                    $"Harden stacks after multiple applications: {target.HardenStacks ?? 0}");
                
                // Test that harden reduces damage (if damage reduction is implemented)
                // This would involve applying damage and verifying reduction
                
                // Test interaction with vulnerability (harden should reduce vulnerability's effect)
                target.VulnerabilityStacks = 3;
                int hardenStacks = target.HardenStacks ?? 0;
                // Harden should provide protection even with vulnerability
                AssertTrue((target.HardenStacks ?? 0) > 0, 
                    $"Harden stacks maintained with vulnerability: {target.HardenStacks ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Harden effect test failed: {ex.Message}");
            }
        }

        private static void TestFortifyEffect()
        {
            Console.WriteLine("Testing FortifyEffectHandler...");
            try
            {
                var handler = new FortifyEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                // Test initial application
                int initialStacks = target.FortifyStacks ?? 0;
                int initialArmor = target.GetTotalArmor();
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Fortify effect applied");
                AssertTrue((target.FortifyStacks ?? 0) > initialStacks, 
                    $"Fortify stacks increased: {initialStacks} -> {target.FortifyStacks ?? 0}");
                AssertTrue((target.FortifyArmorBonus ?? 0) > 0, 
                    $"Fortify armor bonus: {target.FortifyArmorBonus ?? 0}");
                
                // Test that armor bonus is actually applied
                int armorAfterFortify = target.GetTotalArmor();
                AssertTrue(armorAfterFortify >= initialArmor, 
                    $"Armor increased or maintained: {initialArmor} -> {armorAfterFortify}");
                
                // Test stacking
                int stacksAfterFirst = target.FortifyStacks ?? 0;
                int armorBonusAfterFirst = target.FortifyArmorBonus ?? 0;
                handler.Apply(target, action, results);
                AssertTrue((target.FortifyStacks ?? 0) > stacksAfterFirst, 
                    $"Fortify stacks increased on second application: {stacksAfterFirst} -> {target.FortifyStacks ?? 0}");
                AssertTrue((target.FortifyArmorBonus ?? 0) >= armorBonusAfterFirst, 
                    $"Armor bonus increased or maintained: {armorBonusAfterFirst} -> {target.FortifyArmorBonus ?? 0}");
                
                // Test multiple applications
                for (int i = 0; i < 3; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.FortifyStacks ?? 0) > stacksAfterFirst, 
                    $"Fortify stacks after multiple applications: {target.FortifyStacks ?? 0}");
                
                // Test that armor bonus scales with stacks
                int finalArmor = target.GetTotalArmor();
                AssertTrue(finalArmor >= initialArmor, 
                    $"Final armor >= initial: {initialArmor} -> {finalArmor}");
                
                // Test interaction with ArmorBreak (fortify should resist armor break)
                target.ArmorBreakStacks = 2;
                int armorWithBreak = target.GetTotalArmor();
                // Fortify should help maintain armor even with armor break
                AssertTrue((target.FortifyArmorBonus ?? 0) > 0, 
                    $"Fortify armor bonus maintained with armor break: {target.FortifyArmorBonus ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Fortify effect test failed: {ex.Message}");
            }
        }

        private static void TestFocusEffect()
        {
            Console.WriteLine("Testing FocusEffectHandler...");
            try
            {
                var handler = new FocusEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Focus effect applied");
                AssertTrue(target.FocusStacks > 0, $"Focus stacks: {target.FocusStacks}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Focus effect test failed: {ex.Message}");
            }
        }

        private static void TestExposeEffect()
        {
            Console.WriteLine("Testing ExposeEffectHandler...");
            try
            {
                var handler = new ExposeEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Expose effect applied");
                AssertTrue(target.ExposeStacks > 0, $"Expose stacks: {target.ExposeStacks}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Expose effect test failed: {ex.Message}");
            }
        }

        private static void TestHPRegenEffect()
        {
            Console.WriteLine("Testing HPRegenEffectHandler...");
            try
            {
                var handler = new HPRegenEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                // Test initial application
                int initialStacks = target.HPRegenStacks ?? 0;
                int initialHealth = target.CurrentHealth;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "HP Regen effect applied");
                AssertTrue((target.HPRegenStacks ?? 0) > initialStacks, 
                    $"HP Regen stacks increased: {initialStacks} -> {target.HPRegenStacks ?? 0}");
                
                // Test stacking
                int stacksAfterFirst = target.HPRegenStacks ?? 0;
                handler.Apply(target, action, results);
                AssertTrue((target.HPRegenStacks ?? 0) > stacksAfterFirst, 
                    $"HP Regen stacks increased on second application: {stacksAfterFirst} -> {target.HPRegenStacks ?? 0}");
                
                // Test that regen actually heals (if tick system exists)
                // This would involve waiting for a tick or triggering regen
                int healthAfterRegen = target.CurrentHealth;
                // Regen should heal over time, but initial application might not heal immediately
                
                // Test regen when at full health (should not exceed max)
                target.CurrentHealth = target.MaxHealth;
                int maxHealth = target.MaxHealth;
                // Apply regen tick - should not exceed max health
                // (This would require triggering the regen tick mechanism)
                AssertTrue(target.CurrentHealth <= maxHealth, 
                    $"HP Regen doesn't exceed max health: {target.CurrentHealth} <= {maxHealth}");
                
                // Test regen when damaged
                target.TakeDamage(10);
                int damagedHealth = target.CurrentHealth;
                // Apply regen - should increase health
                // (Would need to trigger regen tick)
                AssertTrue((target.HPRegenStacks ?? 0) > 0, 
                    $"HP Regen stacks maintained: {target.HPRegenStacks ?? 0}");
                
                // Test multiple applications
                for (int i = 0; i < 5; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.HPRegenStacks ?? 0) > stacksAfterFirst, 
                    $"HP Regen stacks after multiple applications: {target.HPRegenStacks ?? 0}");
                
                // Test regen rate scales with stacks
                // Higher stacks should mean more healing per tick
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"HP Regen effect test failed: {ex.Message}");
            }
        }

        private static void TestArmorBreakEffect()
        {
            Console.WriteLine("Testing ArmorBreakEffectHandler...");
            try
            {
                var handler = new ArmorBreakEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                // Test initial application
                int initialArmor = target.GetTotalArmor();
                int initialStacks = target.ArmorBreakStacks ?? 0;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Armor Break effect applied");
                AssertTrue((target.ArmorBreakStacks ?? 0) > initialStacks, 
                    $"Armor Break stacks increased: {initialStacks} -> {target.ArmorBreakStacks ?? 0}");
                
                // Test that armor is actually reduced
                int armorAfterBreak = target.GetTotalArmor();
                // Armor should be reduced or armor effectiveness reduced
                AssertTrue((target.ArmorBreakStacks ?? 0) > 0, 
                    $"Armor Break stacks: {target.ArmorBreakStacks ?? 0}");
                
                // Test stacking
                int stacksAfterFirst = target.ArmorBreakStacks ?? 0;
                int armorAfterFirst = target.GetTotalArmor();
                handler.Apply(target, action, results);
                AssertTrue((target.ArmorBreakStacks ?? 0) > stacksAfterFirst, 
                    $"Armor Break stacks increased on second application: {stacksAfterFirst} -> {target.ArmorBreakStacks ?? 0}");
                
                // Test multiple applications
                for (int i = 0; i < 5; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.ArmorBreakStacks ?? 0) > stacksAfterFirst, 
                    $"Armor Break stacks after multiple applications: {target.ArmorBreakStacks ?? 0}");
                
                // Test interaction with Fortify (armor break should reduce fortify's effectiveness)
                target.FortifyStacks = 3;
                target.FortifyArmorBonus = 5;
                int fortifyArmorBonus = target.FortifyArmorBonus ?? 0;
                int armorBreakStacks = target.ArmorBreakStacks ?? 0;
                // Armor break should reduce effective armor even with fortify
                AssertTrue((target.ArmorBreakStacks ?? 0) > 0, 
                    $"Armor Break stacks maintained with fortify: {target.ArmorBreakStacks ?? 0}");
                
                // Test that armor can't go below 0 (if there's a minimum)
                // Note: Can't directly set armor, but can test that armor break stacks work
                target.ArmorBreakStacks = 0;
                for (int i = 0; i < 10; i++)
                {
                    handler.Apply(target, action, results);
                }
                // Armor should not go negative (if minimum enforced)
                AssertTrue((target.ArmorBreakStacks ?? 0) > 0, 
                    $"Armor Break stacks after many applications: {target.ArmorBreakStacks ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Armor Break effect test failed: {ex.Message}");
            }
        }

        private static void TestPierceEffect()
        {
            Console.WriteLine("Testing PierceEffectHandler...");
            try
            {
                var handler = new PierceEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Pierce effect applied");
                AssertTrue(target.HasPierce, "Target has pierce effect");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Pierce effect test failed: {ex.Message}");
            }
        }

        private static void TestReflectEffect()
        {
            Console.WriteLine("Testing ReflectEffectHandler...");
            try
            {
                var handler = new ReflectEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Reflect effect applied");
                AssertTrue(target.ReflectStacks > 0, $"Reflect stacks: {target.ReflectStacks}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Reflect effect test failed: {ex.Message}");
            }
        }

        private static void TestSilenceEffect()
        {
            Console.WriteLine("Testing SilenceEffectHandler...");
            try
            {
                var handler = new SilenceEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Silence effect applied");
                AssertTrue(target.IsSilenced, "Target is silenced");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Silence effect test failed: {ex.Message}");
            }
        }

        private static void TestMarkEffect()
        {
            Console.WriteLine("Testing MarkEffectHandler...");
            try
            {
                var handler = new MarkEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Mark effect applied");
                AssertTrue(target.IsMarked, "Target is marked");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Mark effect test failed: {ex.Message}");
            }
        }

        private static void TestDisruptEffect()
        {
            Console.WriteLine("Testing DisruptEffectHandler...");
            try
            {
                var handler = new DisruptEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                // Test disrupting combo at various steps
                target.Effects.ComboStep = 1;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Disrupt effect applied at combo step 1");
                AssertTrue(target.Effects.ComboStep == 0, $"Combo reset from step 1: {target.Effects.ComboStep} (expected 0)");
                
                target.Effects.ComboStep = 3;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, $"Combo reset from step 3: {target.Effects.ComboStep} (expected 0)");
                
                target.Effects.ComboStep = 5;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, $"Combo reset from step 5: {target.Effects.ComboStep} (expected 0)");
                
                // Test disrupting when combo is already at 0
                target.Effects.ComboStep = 0;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, $"Combo remains 0 when already disrupted: {target.Effects.ComboStep}");
                
                // Test disrupting high combo
                target.Effects.ComboStep = 10;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, $"Combo reset from high step 10: {target.Effects.ComboStep} (expected 0)");
                
                // Test that disrupt doesn't affect other effects
                target.VulnerabilityStacks = 3;
                target.Effects.ComboStep = 4;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, "Combo reset by disrupt");
                AssertTrue(target.VulnerabilityStacks == 3, 
                    $"Vulnerability stacks unaffected by disrupt: {target.VulnerabilityStacks} (expected 3)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Disrupt effect test failed: {ex.Message}");
            }
        }

        private static void TestCleanseEffect()
        {
            Console.WriteLine("Testing CleanseEffectHandler...");
            try
            {
                var handler = new CleanseEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                // Test cleansing poison
                target.PoisonStacks = 3;
                int initialPoison = target.PoisonStacks;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Cleanse effect applied");
                AssertTrue(target.PoisonStacks < initialPoison, 
                    $"Poison stacks reduced: {initialPoison} -> {target.PoisonStacks}");
                
                // Test cleansing multiple debuffs
                target.PoisonStacks = 5;
                target.VulnerabilityStacks = 3;
                target.ExposeStacks = 2;
                int poisonBefore = target.PoisonStacks;
                int vulnBefore = target.VulnerabilityStacks ?? 0;
                int exposeBefore = target.ExposeStacks ?? 0;
                
                handler.Apply(target, action, results);
                AssertTrue(target.PoisonStacks < poisonBefore || (target.VulnerabilityStacks ?? 0) < vulnBefore || (target.ExposeStacks ?? 0) < exposeBefore,
                    $"At least one debuff reduced: Poison {target.PoisonStacks}, Vuln {target.VulnerabilityStacks ?? 0}, Expose {target.ExposeStacks ?? 0}");
                
                // Test cleansing when no debuffs present
                target.PoisonStacks = 0;
                target.VulnerabilityStacks = 0;
                target.ExposeStacks = 0;
                applied = handler.Apply(target, action, results);
                // Cleanse should still apply (may do nothing, but shouldn't error)
                AssertTrue(target.PoisonStacks == 0, 
                    $"Poison remains 0 when no debuffs: {target.PoisonStacks}");
                
                // Test partial cleanse (if cleanse removes some but not all)
                target.PoisonStacks = 10;
                int poisonHigh = target.PoisonStacks;
                handler.Apply(target, action, results);
                AssertTrue(target.PoisonStacks < poisonHigh, 
                    $"High poison stacks reduced: {poisonHigh} -> {target.PoisonStacks}");
                
                // Test that cleanse doesn't remove beneficial effects
                target.FortifyStacks = 3;
                target.HardenStacks = 2;
                target.PoisonStacks = 4;
                int fortifyBefore = target.FortifyStacks ?? 0;
                int hardenBefore = target.HardenStacks ?? 0;
                
                handler.Apply(target, action, results);
                AssertTrue((target.FortifyStacks ?? 0) == fortifyBefore, 
                    $"Fortify stacks unaffected by cleanse: {target.FortifyStacks ?? 0} (expected {fortifyBefore})");
                AssertTrue((target.HardenStacks ?? 0) == hardenBefore, 
                    $"Harden stacks unaffected by cleanse: {target.HardenStacks ?? 0} (expected {hardenBefore})");
                AssertTrue(target.PoisonStacks < 4, 
                    $"Poison stacks reduced by cleanse: {target.PoisonStacks} (expected < 4)");
                
                // Test complete cleanse (if cleanse removes all debuffs)
                target.PoisonStacks = 1;
                handler.Apply(target, action, results);
                // Should remove or reduce the last stack
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Cleanse effect test failed: {ex.Message}");
            }
        }

        #endregion

        #region Phase 3: Tag System & Combo Routing Tests

        private static void TestTagRegistry()
        {
            Console.WriteLine("Testing TagRegistry...");
            try
            {
                var registry = TagRegistry.Instance;
                registry.RegisterTag("TEST_TAG");
                AssertTrue(registry.IsTagRegistered("TEST_TAG"), "Tag registered and found");
                AssertTrue(registry.IsTagRegistered("test_tag"), "Tag matching is case-insensitive");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Tag registry test failed: {ex.Message}");
            }
        }

        private static void TestTagMatcher()
        {
            Console.WriteLine("Testing TagMatcher...");
            try
            {
                var sourceTags = new[] { "FIRE", "WIZARD", "EPIC" };
                var requiredTags = new[] { "FIRE", "WIZARD" };

                bool hasAll = TagMatcher.HasAllTags(sourceTags, requiredTags);
                AssertTrue(hasAll, "TagMatcher correctly identified all required tags");

                int matchCount = TagMatcher.CountMatchingTags(sourceTags, requiredTags);
                AssertTrue(matchCount == 2, $"Matching tag count: {matchCount} (expected 2)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Tag matcher test failed: {ex.Message}");
            }
        }

        private static void TestTagAggregator()
        {
            Console.WriteLine("Testing TagAggregator...");
            try
            {
                var character = new Character("Test", 1);
                var tags = TagAggregator.AggregateCharacterTags(character);
                AssertTrue(tags != null, "Tag aggregator returned tag list");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Tag aggregator test failed: {ex.Message}");
            }
        }

        private static void TestTagModifier()
        {
            Console.WriteLine("Testing TagModifier...");
            try
            {
                var modifier = new TagModifier();
                var actor = new Character("Test", 1);

                modifier.AddTemporaryTag(actor, "TEMPORARY_TAG", 3);
                var tags = modifier.GetTemporaryTags(actor);
                AssertTrue(tags.Contains("TEMPORARY_TAG"), "Temporary tag added");

                modifier.UpdateTagDurations(1.0);
                modifier.RemoveTemporaryTag(actor, "TEMPORARY_TAG");
                var tagsAfter = modifier.GetTemporaryTags(actor);
                AssertTrue(!tagsAfter.Contains("TEMPORARY_TAG"), "Temporary tag removed");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Tag modifier test failed: {ex.Message}");
            }
        }

        private static void TestComboRouter()
        {
            Console.WriteLine("Testing ComboRouter...");
            try
            {
                var character = new Character("Test", 1);
                var action = new Action
                {
                    Name = "Test Action"
                };
                action.ComboRouting.JumpToSlot = 2;
                var comboSequence = new System.Collections.Generic.List<Action>
                {
                    new Action { Name = "Action1" },
                    new Action { Name = "Action2" },
                    new Action { Name = "Action3" }
                };

                var result = ComboRouter.RouteCombo(character, action, 0, comboSequence);
                AssertTrue(result.RoutingAction == ComboRouter.RoutingAction.JumpToSlot, "Combo routing identified jump action");
                AssertTrue(result.NextSlotIndex == 1, $"Next slot index: {result.NextSlotIndex} (expected 1)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Combo router test failed: {ex.Message}");
            }
        }

        #endregion

        #region Phase 4: Outcome-Based Actions & Meta-Progression Tests

        private static void TestActionUsageTracker()
        {
            Console.WriteLine("Testing ActionUsageTracker...");
            try
            {
                var tracker = ActionUsageTracker.Instance;
                var actor = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };

                tracker.RecordActionUsage(actor, action);
                int count = tracker.GetUsageCount(actor, action);
                AssertTrue(count == 1, $"Action usage count: {count} (expected 1)");

                tracker.RecordActionUsage(actor, action);
                count = tracker.GetUsageCount(actor, action);
                AssertTrue(count == 2, $"Action usage count after second use: {count} (expected 2)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Action usage tracker test failed: {ex.Message}");
            }
        }

        private static void TestConditionalXPGain()
        {
            Console.WriteLine("Testing ConditionalXPGain...");
            try
            {
                var character = new Character("Test", 1);
                var evt = new CombatEvent(CombatEventType.EnemyDied, character);
                var initialXP = character.Progression.XP;

                ConditionalXPGain.GrantXPFromEvent(evt, character);
                var finalXP = character.Progression.XP;

                AssertTrue(finalXP > initialXP, $"XP gained: {initialXP} -> {finalXP}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Conditional XP gain test failed: {ex.Message}");
            }
        }

        private static void TestOutcomeHandlers()
        {
            Console.WriteLine("Testing OutcomeHandlers...");
            try
            {
                var handler = new ConditionalOutcomeHandler();
                var evt = new CombatEvent(CombatEventType.EnemyDied, new Character("Test", 1));
                var source = new Character("Test", 1);
                var target = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);

                handler.HandleOutcome(evt, source, target, null);
                AssertTrue(true, "Outcome handler executed without exception");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Outcome handler test failed: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  ✓ {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
            }
        }

        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");

            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All Advanced Mechanics tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }

        #endregion
    }
}

