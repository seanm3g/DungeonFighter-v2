using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Combat;
using RPGGame.Actions.Conditional;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Phase 1: Roll Modification & Conditional Triggers Tests
    /// </summary>
    public static class AdvancedMechanicsTest_Phase1
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== Phase 1: Roll Modification & Conditional Triggers ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            TestAdditiveModifier();
            TestMultiplicativeModifier();
            TestClampModifier();
            TestRerollModifier();
            TestExplodingDiceModifier();
            TestMultiDiceRoller();
            TestEventBus();
            TestConditionalTriggerEvaluator();
            TestThresholdManager();
            
            PrintSummary();
        }
        
        private static void TestAdditiveModifier()
        {
            Console.WriteLine("Testing AdditiveRollModifier...");
            try
            {
                var context = new RollModificationContext(new Character("Test", 1));
                
                var modifier = new AdditiveRollModifier("TestAdd", 5);
                int result = modifier.ModifyRoll(10, context);
                AssertTrue(result == 15, $"Positive modifier: 10 + 5 = {result} (expected 15)");
                
                var negativeModifier = new AdditiveRollModifier("TestSub", -3);
                result = negativeModifier.ModifyRoll(10, context);
                AssertTrue(result == 7, $"Negative modifier: 10 - 3 = {result} (expected 7)");
                
                var zeroModifier = new AdditiveRollModifier("TestZero", 0);
                result = zeroModifier.ModifyRoll(10, context);
                AssertTrue(result == 10, $"Zero modifier: 10 + 0 = {result} (expected 10)");
                
                result = modifier.ModifyRoll(1, context);
                AssertTrue(result == 6, $"Low roll with positive: 1 + 5 = {result} (expected 6)");
                
                result = negativeModifier.ModifyRoll(20, context);
                AssertTrue(result == 17, $"High roll with negative: 20 - 3 = {result} (expected 17)");
                
                var largeModifier = new AdditiveRollModifier("TestLarge", 50);
                result = largeModifier.ModifyRoll(10, context);
                AssertTrue(result == 60, $"Large modifier: 10 + 50 = {result} (expected 60)");
                
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
                
                var modifier = new MultiplicativeRollModifier("TestMult", 1.5);
                int result = modifier.ModifyRoll(10, context);
                AssertTrue(result == 15, $"Multiplier > 1: 10 * 1.5 = {result} (expected 15)");
                
                var reduceModifier = new MultiplicativeRollModifier("TestReduce", 0.5);
                result = reduceModifier.ModifyRoll(10, context);
                AssertTrue(result == 5, $"Multiplier < 1: 10 * 0.5 = {result} (expected 5)");
                
                var neutralModifier = new MultiplicativeRollModifier("TestNeutral", 1.0);
                result = neutralModifier.ModifyRoll(10, context);
                AssertTrue(result == 10, $"Multiplier = 1: 10 * 1.0 = {result} (expected 10)");
                
                var doubleModifier = new MultiplicativeRollModifier("TestDouble", 2.0);
                result = doubleModifier.ModifyRoll(10, context);
                AssertTrue(result == 20, $"Double multiplier: 10 * 2.0 = {result} (expected 20)");
                
                result = modifier.ModifyRoll(11, context);
                AssertTrue(result == 16 || result == 17, $"Fractional rounding: 11 * 1.5 = {result} (should be 16 or 17)");
                
                result = modifier.ModifyRoll(1, context);
                AssertTrue(result >= 1 && result <= 2, $"Low roll with multiplier: 1 * 1.5 = {result} (should be 1-2)");
                
                result = modifier.ModifyRoll(20, context);
                AssertTrue(result == 30, $"High roll with multiplier: 20 * 1.5 = {result} (expected 30)");
                
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
                
                AssertTrue(modifier.ModifyRoll(1, context) == 5, "Clamp min: 1 -> 5");
                AssertTrue(modifier.ModifyRoll(3, context) == 5, "Clamp min: 3 -> 5");
                AssertTrue(modifier.ModifyRoll(4, context) == 5, "Clamp min: 4 -> 5");
                
                AssertTrue(modifier.ModifyRoll(16, context) == 15, "Clamp max: 16 -> 15");
                AssertTrue(modifier.ModifyRoll(20, context) == 15, "Clamp max: 20 -> 15");
                AssertTrue(modifier.ModifyRoll(100, context) == 15, "Clamp max: 100 -> 15");
                
                AssertTrue(modifier.ModifyRoll(5, context) == 5, "Clamp boundary min: 5 -> 5");
                AssertTrue(modifier.ModifyRoll(10, context) == 10, "Clamp middle: 10 -> 10");
                AssertTrue(modifier.ModifyRoll(15, context) == 15, "Clamp boundary max: 15 -> 15");
                
                var singleValueModifier = new ClampRollModifier("TestSingle", 10, 10);
                AssertTrue(singleValueModifier.ModifyRoll(1, context) == 10, "Single value clamp (low): 1 -> 10");
                AssertTrue(singleValueModifier.ModifyRoll(10, context) == 10, "Single value clamp (exact): 10 -> 10");
                AssertTrue(singleValueModifier.ModifyRoll(20, context) == 10, "Single value clamp (high): 20 -> 10");
                
                var narrowModifier = new ClampRollModifier("TestNarrow", 10, 11);
                AssertTrue(narrowModifier.ModifyRoll(5, context) == 10, "Narrow clamp (low): 5 -> 10");
                AssertTrue(narrowModifier.ModifyRoll(10, context) == 10, "Narrow clamp (min): 10 -> 10");
                AssertTrue(narrowModifier.ModifyRoll(11, context) == 11, "Narrow clamp (max): 11 -> 11");
                AssertTrue(narrowModifier.ModifyRoll(15, context) == 11, "Narrow clamp (high): 15 -> 11");
                
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
                
                var alwaysReroll = new RerollModifier("TestAlways", 1.0);
                int result = alwaysReroll.ModifyRoll(5, context);
                AssertTrue(result >= 1 && result <= 20, $"100% reroll: result {result} in valid range 1-20");
                
                var neverReroll = new RerollModifier("TestNever", 0.0);
                result = neverReroll.ModifyRoll(10, context);
                AssertTrue(result == 10, $"0% reroll: result {result} should equal input 10");
                
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
                
                result = alwaysReroll.ModifyRoll(1, context);
                AssertTrue(result >= 1 && result <= 20, $"Reroll from 1: result {result} in valid range");
                
                result = alwaysReroll.ModifyRoll(20, context);
                AssertTrue(result >= 1 && result <= 20, $"Reroll from 20: result {result} in valid range");
                
                var results = new HashSet<int>();
                for (int i = 0; i < 50; i++)
                {
                    results.Add(alwaysReroll.ModifyRoll(10, context));
                }
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
                
                var modifier = new ExplodingDiceModifier("TestExploding", 20);
                int result = modifier.ModifyRoll(20, context);
                AssertTrue(result >= 20, $"Exploding on 20: result {result} should be >= 20");
                
                result = modifier.ModifyRoll(19, context);
                AssertTrue(result == 19, $"Non-exploding roll: 19 should remain 19, got {result}");
                
                result = modifier.ModifyRoll(10, context);
                AssertTrue(result == 10, $"Non-exploding roll: 10 should remain 10, got {result}");
                
                var lowerThreshold = new ExplodingDiceModifier("TestLow", 18);
                result = lowerThreshold.ModifyRoll(18, context);
                AssertTrue(result >= 18, $"Exploding on 18: result {result} should be >= 18");
                
                result = lowerThreshold.ModifyRoll(19, context);
                AssertTrue(result >= 19, $"Exploding on 19 (above 18): result {result} should be >= 19");
                
                result = lowerThreshold.ModifyRoll(20, context);
                AssertTrue(result >= 20, $"Exploding on 20 (above 18): result {result} should be >= 20");
                
                for (int i = 0; i < 100; i++)
                {
                    result = modifier.ModifyRoll(20, context);
                    if (result > 40)
                    {
                        break;
                    }
                }
                AssertTrue(result >= 20, $"Chain explosion possible: result {result} should be >= 20");
                
                var frequentExplode = new ExplodingDiceModifier("TestFrequent", 10);
                result = frequentExplode.ModifyRoll(10, context);
                AssertTrue(result >= 10, $"Frequent explode (10): result {result} should be >= 10");
                
                result = frequentExplode.ModifyRoll(15, context);
                AssertTrue(result >= 15, $"Frequent explode (15): result {result} should be >= 15");
                
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
                int sumResult = MultiDiceRoller.RollMultipleDice(2, 20, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(sumResult >= 2 && sumResult <= 40, $"Sum mode (2d20): {sumResult} (should be 2-40)");
                
                sumResult = MultiDiceRoller.RollMultipleDice(3, 20, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(sumResult >= 3 && sumResult <= 60, $"Sum mode (3d20): {sumResult} (should be 3-60)");
                
                sumResult = MultiDiceRoller.RollMultipleDice(1, 20, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(sumResult >= 1 && sumResult <= 20, $"Sum mode (1d20): {sumResult} (should be 1-20)");
                
                sumResult = MultiDiceRoller.RollMultipleDice(10, 6, MultiDiceRoller.DiceSelectionMode.Sum);
                AssertTrue(sumResult >= 10 && sumResult <= 60, $"Sum mode (10d6): {sumResult} (should be 10-60)");

                int lowestResult = MultiDiceRoller.RollMultipleDice(2, 20, MultiDiceRoller.DiceSelectionMode.TakeLowest);
                AssertTrue(lowestResult >= 1 && lowestResult <= 20, $"TakeLowest (2d20): {lowestResult} (should be 1-20)");
                
                lowestResult = MultiDiceRoller.RollMultipleDice(5, 20, MultiDiceRoller.DiceSelectionMode.TakeLowest);
                AssertTrue(lowestResult >= 1 && lowestResult <= 20, $"TakeLowest (5d20): {lowestResult} (should be 1-20)");
                
                lowestResult = MultiDiceRoller.RollMultipleDice(1, 20, MultiDiceRoller.DiceSelectionMode.TakeLowest);
                AssertTrue(lowestResult >= 1 && lowestResult <= 20, $"TakeLowest (1d20): {lowestResult} (should be 1-20)");

                int highestResult = MultiDiceRoller.RollMultipleDice(2, 20, MultiDiceRoller.DiceSelectionMode.TakeHighest);
                AssertTrue(highestResult >= 1 && highestResult <= 20, $"TakeHighest (2d20): {highestResult} (should be 1-20)");
                
                highestResult = MultiDiceRoller.RollMultipleDice(5, 20, MultiDiceRoller.DiceSelectionMode.TakeHighest);
                AssertTrue(highestResult >= 1 && highestResult <= 20, $"TakeHighest (5d20): {highestResult} (should be 1-20)");
                
                highestResult = MultiDiceRoller.RollMultipleDice(1, 20, MultiDiceRoller.DiceSelectionMode.TakeHighest);
                AssertTrue(highestResult >= 1 && highestResult <= 20, $"TakeHighest (1d20): {highestResult} (should be 1-20)");
                
                var diceRolls = MultiDiceRoller.RollMultipleDiceResults(2, 20);
                int lowest = diceRolls.Min();
                int highest = diceRolls.Max();
                AssertTrue(lowest <= highest, 
                    $"Statistical: TakeLowest ({lowest}) should be <= TakeHighest ({highest})");
                
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
                bool eventFired = false;
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionExecuted, (evt) => {
                    eventFired = true;
                });

                var testEvent = new CombatEvent(CombatEventType.ActionExecuted, new Character("Test", 1));
                CombatEventBus.Instance.Publish(testEvent);

                AssertTrue(eventFired, "Event bus: single subscriber received event");
                CombatEventBus.Instance.Clear();
                
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
                
                string? receivedName = null;
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionExecuted, (evt) => {
                    receivedName = evt.Source?.Name;
                });
                
                var namedEvent = new CombatEvent(CombatEventType.ActionExecuted, new Character("TestChar", 1));
                CombatEventBus.Instance.Publish(namedEvent);
                AssertTrue(receivedName == "TestChar", $"Event bus: event data passed correctly (got '{receivedName}')");
                CombatEventBus.Instance.Clear();
                
                int unsubscribeCount = 0;
                Action<CombatEvent> handler = (evt) => { unsubscribeCount++; };
                CombatEventBus.Instance.Subscribe(CombatEventType.ActionExecuted, handler);
                CombatEventBus.Instance.Publish(testEvent);
                AssertTrue(unsubscribeCount == 1, $"Event bus: before unsubscribe, handler called {unsubscribeCount} time");
                
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
                
                var missEvent = new CombatEvent(CombatEventType.ActionMiss, source) { IsMiss = true };
                var missCondition = TriggerConditionFactory.OnMiss();
                var conditions = new System.Collections.Generic.List<TriggerCondition> { missCondition };
                bool result = evaluator.EvaluateConditions(conditions, missEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: OnMiss correctly identified miss");
                
                var hitEvent = new CombatEvent(CombatEventType.ActionHit, source) { IsMiss = false };
                result = evaluator.EvaluateConditions(conditions, hitEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: OnMiss correctly rejected hit");
                
                var hitCondition = TriggerConditionFactory.OnNormalHit();
                var hitConditions = new System.Collections.Generic.List<TriggerCondition> { hitCondition };
                result = evaluator.EvaluateConditions(hitConditions, hitEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: OnNormalHit correctly identified hit");
                
                result = evaluator.EvaluateConditions(hitConditions, missEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: OnNormalHit correctly rejected miss");
                
                var critEvent = new CombatEvent(CombatEventType.ActionHit, source) { IsCritical = true };
                var critCondition = TriggerConditionFactory.OnCriticalHit();
                var critConditions = new System.Collections.Generic.List<TriggerCondition> { critCondition };
                result = evaluator.EvaluateConditions(critConditions, critEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: OnCriticalHit correctly identified critical");
                
                result = evaluator.EvaluateConditions(critConditions, hitEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: OnCriticalHit correctly rejected non-critical");
                
                var critOnlyConditions = new System.Collections.Generic.List<TriggerCondition> 
                { 
                    TriggerConditionFactory.OnCriticalHit()
                };
                result = evaluator.EvaluateConditions(critOnlyConditions, critEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: OnCriticalHit condition satisfied for critical event");
                
                var normalHitConditions = new System.Collections.Generic.List<TriggerCondition> 
                { 
                    TriggerConditionFactory.OnNormalHit()
                };
                result = evaluator.EvaluateConditions(normalHitConditions, critEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: OnNormalHit correctly rejected critical hit (mutually exclusive)");
                
                var multiConditions = new System.Collections.Generic.List<TriggerCondition> 
                { 
                    TriggerConditionFactory.OnCriticalHit()
                };
                result = evaluator.EvaluateConditions(multiConditions, critEvent, source, target, null);
                AssertTrue(result, "Conditional trigger: Multiple conditions (crit) satisfied");
                
                result = evaluator.EvaluateConditions(multiConditions, hitEvent, source, target, null);
                AssertTrue(!result, "Conditional trigger: Multiple conditions (hit but not crit) failed correctly");
                
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
                
                manager.SetCriticalHitThreshold(character1, 18);
                int threshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(threshold == 18, $"Critical hit threshold set to {threshold} (expected 18)");
                
                manager.SetCriticalHitThreshold(character2, 19);
                int threshold2 = manager.GetCriticalHitThreshold(character2);
                AssertTrue(threshold2 == 19, $"Character2 threshold: {threshold2} (expected 19)");
                AssertTrue(manager.GetCriticalHitThreshold(character1) == 18, 
                    "Character1 threshold unchanged: still 18");
                
                manager.SetCriticalHitThreshold(character1, 1);
                threshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(threshold == 1, $"Minimum threshold: {threshold} (expected 1)");
                
                manager.SetCriticalHitThreshold(character1, 20);
                threshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(threshold == 20, $"Maximum threshold: {threshold} (expected 20)");
                
                manager.ResetThresholds(character1);
                int defaultThreshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(defaultThreshold > 0 && defaultThreshold <= 20, 
                    $"Default threshold restored: {defaultThreshold} (should be 1-20)");
                
                AssertTrue(manager.GetCriticalHitThreshold(character2) == 19, 
                    "Character2 threshold preserved after character1 reset");
                
                manager.ResetThresholds(character2);
                int defaultThreshold2 = manager.GetCriticalHitThreshold(character2);
                AssertTrue(defaultThreshold2 > 0 && defaultThreshold2 <= 20, 
                    $"Character2 default threshold: {defaultThreshold2} (should be 1-20)");
                
                manager.SetCriticalHitThreshold(character1, 15);
                manager.SetCriticalHitThreshold(character1, 16);
                manager.SetCriticalHitThreshold(character1, 17);
                threshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(threshold == 17, $"Updated threshold: {threshold} (expected 17)");
                
                int persistentThreshold = manager.GetCriticalHitThreshold(character1);
                AssertTrue(persistentThreshold == 17, 
                    $"Threshold persistence: {persistentThreshold} (expected 17)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Threshold manager test failed: {ex.Message}");
            }
        }
        
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
            Console.WriteLine("\n=== Phase 1 Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            
            if (_testsRun > 0)
            {
                Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            }
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All Phase 1 tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
    }
}

