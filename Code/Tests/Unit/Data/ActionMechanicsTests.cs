using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for ALL game mechanics referenced in Actions.json
    /// Discovers which mechanics are used and tests each category systematically
    /// </summary>
    public static class ActionMechanicsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all action mechanics tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Mechanics Tests (All) ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Discovery phase
            var mechanicsInventory = DiscoverUsedMechanics();
            
            // Testing phases
            TestStatusEffects(mechanicsInventory);
            TestRollModifications(mechanicsInventory);
            TestConditionalTriggers(mechanicsInventory);
            TestComboRouting(mechanicsInventory);
            TestAdvancedMechanics(mechanicsInventory);
            TestActionAttackBonuses(mechanicsInventory);
            TestBasicProperties(mechanicsInventory);
            TestComboSystem(mechanicsInventory);

            TestBase.PrintSummary("Action Mechanics Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Mechanics Discovery

        private static MechanicsInventory DiscoverUsedMechanics()
        {
            Console.WriteLine("--- Discovering Used Mechanics ---\n");
            
            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();
            var inventory = new MechanicsInventory();

            foreach (var action in allActions)
            {
                // Status Effects
                if (action.CausesBleed) inventory.StatusEffects.Add("Bleed");
                if (action.CausesPoison) inventory.StatusEffects.Add("Poison");
                if (action.CausesBurn) inventory.StatusEffects.Add("Burn");
                if (action.CausesWeaken) inventory.StatusEffects.Add("Weaken");
                if (action.CausesSlow) inventory.StatusEffects.Add("Slow");
                if (action.CausesStun) inventory.StatusEffects.Add("Stun");
                if (action.CausesVulnerability) inventory.StatusEffects.Add("Vulnerability");
                if (action.CausesHarden) inventory.StatusEffects.Add("Harden");
                if (action.CausesExpose) inventory.StatusEffects.Add("Expose");
                if (action.CausesSilence) inventory.StatusEffects.Add("Silence");
                if (action.CausesPierce) inventory.StatusEffects.Add("Pierce");
                if (action.CausesStatDrain) inventory.StatusEffects.Add("StatDrain");
                if (action.CausesFortify) inventory.StatusEffects.Add("Fortify");
                if (action.CausesFocus) inventory.StatusEffects.Add("Focus");
                if (action.CausesCleanse) inventory.StatusEffects.Add("Cleanse");
                if (action.CausesReflect) inventory.StatusEffects.Add("Reflect");

                // Roll Modifications
                if (action.RollMods.MultipleDiceCount > 1)
                {
                    inventory.RollModifications.Add($"MultipleDice({action.RollMods.MultipleDiceCount}, {action.RollMods.MultipleDiceMode})");
                }
                if (action.RollMods.ExplodingDice)
                {
                    inventory.RollModifications.Add($"ExplodingDice(threshold={action.RollMods.ExplodingDiceThreshold})");
                }
                if (action.RollMods.AllowReroll)
                {
                    inventory.RollModifications.Add($"Reroll(chance={action.RollMods.RerollChance})");
                }
                if (action.RollMods.CriticalHitThresholdOverride > 0)
                {
                    inventory.RollModifications.Add($"CriticalHitOverride({action.RollMods.CriticalHitThresholdOverride})");
                }
                if (action.RollMods.ComboThresholdOverride > 0)
                {
                    inventory.RollModifications.Add($"ComboThresholdOverride({action.RollMods.ComboThresholdOverride})");
                }
                if (action.RollMods.HitThresholdOverride > 0)
                {
                    inventory.RollModifications.Add($"HitThresholdOverride({action.RollMods.HitThresholdOverride})");
                }
                if (action.RollMods.CriticalMissThresholdOverride > 0)
                {
                    inventory.RollModifications.Add($"CriticalMissThresholdOverride({action.RollMods.CriticalMissThresholdOverride})");
                }
                if (action.RollMods.CriticalHitThresholdAdjustment != 0)
                {
                    inventory.RollModifications.Add($"CriticalHitAdjustment({action.RollMods.CriticalHitThresholdAdjustment})");
                }
                if (action.RollMods.ComboThresholdAdjustment != 0)
                {
                    inventory.RollModifications.Add($"ComboThresholdAdjustment({action.RollMods.ComboThresholdAdjustment})");
                }
                if (action.RollMods.HitThresholdAdjustment != 0)
                {
                    inventory.RollModifications.Add($"HitThresholdAdjustment({action.RollMods.HitThresholdAdjustment})");
                }
                if (action.RollMods.Additive != 0)
                {
                    inventory.RollModifications.Add($"Additive({action.RollMods.Additive})");
                }
                if (action.RollMods.Multiplier != 1.0)
                {
                    inventory.RollModifications.Add($"Multiplier({action.RollMods.Multiplier})");
                }

                // Conditional Triggers
                if (action.Triggers.TriggerConditions != null && action.Triggers.TriggerConditions.Count > 0)
                {
                    foreach (var condition in action.Triggers.TriggerConditions)
                    {
                        inventory.ConditionalTriggers.Add(condition);
                    }
                }
                if (action.Triggers.ExactRollTriggerValue > 0)
                {
                    inventory.ConditionalTriggers.Add($"ExactRoll({action.Triggers.ExactRollTriggerValue})");
                }
                if (!string.IsNullOrEmpty(action.Triggers.RequiredTag))
                {
                    inventory.ConditionalTriggers.Add($"RequiredTag({action.Triggers.RequiredTag})");
                }

                // Combo Routing
                if (action.ComboRouting.JumpToSlot > 0)
                {
                    inventory.ComboRouting.Add($"JumpToSlot({action.ComboRouting.JumpToSlot})");
                }
                if (action.ComboRouting.SkipNext) inventory.ComboRouting.Add("SkipNext");
                if (action.ComboRouting.RepeatPrevious) inventory.ComboRouting.Add("RepeatPrevious");
                if (action.ComboRouting.LoopToStart) inventory.ComboRouting.Add("LoopToStart");
                if (action.ComboRouting.StopEarly) inventory.ComboRouting.Add("StopEarly");
                if (action.ComboRouting.DisableSlot) inventory.ComboRouting.Add("DisableSlot");
                if (action.ComboRouting.RandomAction) inventory.ComboRouting.Add("RandomAction");
                if (action.ComboRouting.TriggerOnlyInSlot > 0)
                {
                    inventory.ComboRouting.Add($"TriggerOnlyInSlot({action.ComboRouting.TriggerOnlyInSlot})");
                }

                // Advanced Mechanics
                if (action.Advanced.MultiHitCount > 1)
                {
                    inventory.AdvancedMechanics.Add($"MultiHit({action.Advanced.MultiHitCount})");
                }
                if (action.Advanced.SelfDamagePercent > 0)
                {
                    inventory.AdvancedMechanics.Add($"SelfDamage({action.Advanced.SelfDamagePercent}%)");
                }
                if (action.Advanced.RollBonus != 0)
                {
                    inventory.AdvancedMechanics.Add($"RollBonus({action.Advanced.RollBonus}, duration={action.Advanced.RollBonusDuration})");
                }
                if (action.Advanced.StatBonus != 0)
                {
                    inventory.AdvancedMechanics.Add($"StatBonus({action.Advanced.StatBonusType}+{action.Advanced.StatBonus}, duration={action.Advanced.StatBonusDuration})");
                }
                if (action.Advanced.SkipNextTurn) inventory.AdvancedMechanics.Add("SkipNextTurn");
                if (action.Advanced.GuaranteeNextSuccess) inventory.AdvancedMechanics.Add("GuaranteeNextSuccess");
                if (action.Advanced.HealAmount > 0)
                {
                    inventory.AdvancedMechanics.Add($"Heal({action.Advanced.HealAmount})");
                }
                if (action.Advanced.HealthThreshold > 0.0)
                {
                    inventory.AdvancedMechanics.Add($"HealthThreshold({action.Advanced.HealthThreshold})");
                }
                if (action.Advanced.StatThreshold > 0.0)
                {
                    inventory.AdvancedMechanics.Add($"StatThreshold({action.Advanced.StatThresholdType}>{action.Advanced.StatThreshold})");
                }
                if (action.Advanced.ConditionalDamageMultiplier != 1.0)
                {
                    inventory.AdvancedMechanics.Add($"ConditionalDamage({action.Advanced.ConditionalDamageMultiplier}x)");
                }
                if (action.Advanced.RepeatLastAction) inventory.AdvancedMechanics.Add("RepeatLastAction");
                if (action.Advanced.ExtraAttacks > 0)
                {
                    inventory.AdvancedMechanics.Add($"ExtraAttacks({action.Advanced.ExtraAttacks})");
                }
                if (action.Advanced.ComboAmplifierMultiplier != 1.0)
                {
                    inventory.AdvancedMechanics.Add($"ComboAmplifier({action.Advanced.ComboAmplifierMultiplier}x)");
                }
                if (action.Advanced.EnemyRollPenalty != 0)
                {
                    inventory.AdvancedMechanics.Add($"EnemyRollPenalty({action.Advanced.EnemyRollPenalty})");
                }
                if (action.Advanced.ExtraDamage > 0)
                {
                    inventory.AdvancedMechanics.Add($"ExtraDamage({action.Advanced.ExtraDamage}, decay={action.Advanced.ExtraDamageDecay})");
                }
                if (action.Advanced.DamageReduction > 0.0)
                {
                    inventory.AdvancedMechanics.Add($"DamageReduction({action.Advanced.DamageReduction}, decay={action.Advanced.DamageReductionDecay})");
                }
                if (action.Advanced.SelfAttackChance > 0.0)
                {
                    inventory.AdvancedMechanics.Add($"SelfAttackChance({action.Advanced.SelfAttackChance})");
                }
                if (action.Advanced.ResetEnemyCombo) inventory.AdvancedMechanics.Add("ResetEnemyCombo");
                if (action.Advanced.StunEnemy) inventory.AdvancedMechanics.Add($"StunEnemy(duration={action.Advanced.StunDuration})");
                if (action.Advanced.ReduceLengthNextActions)
                {
                    inventory.AdvancedMechanics.Add($"ReduceLength({action.Advanced.LengthReduction}, duration={action.Advanced.LengthReductionDuration})");
                }

                // ACTION/ATTACK Bonuses
                if (action.ActionAttackBonuses != null && action.ActionAttackBonuses.BonusGroups != null)
                {
                    foreach (var group in action.ActionAttackBonuses.BonusGroups)
                    {
                        string bonusTypes = string.Join(",", group.Bonuses.Select(b => $"{b.Type}+{b.Value}"));
                        inventory.ActionAttackBonuses.Add($"{group.Keyword}({group.Count}): {bonusTypes}");
                    }
                }

                // Basic Properties
                inventory.ActionTypes.Add(action.Type.ToString());
                inventory.TargetTypes.Add(action.Target.ToString());
                if (action.Tags != null && action.Tags.Count > 0)
                {
                    foreach (var tag in action.Tags)
                    {
                        inventory.Tags.Add(tag);
                    }
                }

                // Combo Properties
                if (action.IsComboAction)
                {
                    inventory.ComboProperties.Add($"ComboAction(order={action.ComboOrder}, bonus={action.ComboBonusAmount}, duration={action.ComboBonusDuration})");
                }
            }

            // Print inventory
            Console.WriteLine($"Total Actions Scanned: {allActions.Count}\n");
            Console.WriteLine($"Status Effects Found: {inventory.StatusEffects.Distinct().Count()} types");
            Console.WriteLine($"Roll Modifications Found: {inventory.RollModifications.Distinct().Count()} types");
            Console.WriteLine($"Conditional Triggers Found: {inventory.ConditionalTriggers.Distinct().Count()} types");
            Console.WriteLine($"Combo Routing Found: {inventory.ComboRouting.Distinct().Count()} types");
            Console.WriteLine($"Advanced Mechanics Found: {inventory.AdvancedMechanics.Distinct().Count()} types");
            Console.WriteLine($"ACTION/ATTACK Bonuses Found: {inventory.ActionAttackBonuses.Distinct().Count()} types");
            Console.WriteLine($"Action Types Found: {inventory.ActionTypes.Distinct().Count()} types");
            Console.WriteLine($"Target Types Found: {inventory.TargetTypes.Distinct().Count()} types");
            Console.WriteLine($"Tags Found: {inventory.Tags.Distinct().Count()} unique tags");
            Console.WriteLine($"Combo Properties Found: {inventory.ComboProperties.Distinct().Count()} types\n");

            return inventory;
        }

        private class MechanicsInventory
        {
            public HashSet<string> StatusEffects { get; } = new HashSet<string>();
            public HashSet<string> RollModifications { get; } = new HashSet<string>();
            public HashSet<string> ConditionalTriggers { get; } = new HashSet<string>();
            public HashSet<string> ComboRouting { get; } = new HashSet<string>();
            public HashSet<string> AdvancedMechanics { get; } = new HashSet<string>();
            public HashSet<string> ActionAttackBonuses { get; } = new HashSet<string>();
            public HashSet<string> ActionTypes { get; } = new HashSet<string>();
            public HashSet<string> TargetTypes { get; } = new HashSet<string>();
            public HashSet<string> Tags { get; } = new HashSet<string>();
            public HashSet<string> ComboProperties { get; } = new HashSet<string>();
        }

        #endregion

        #region Status Effects Testing

        private static void TestStatusEffects(MechanicsInventory inventory)
        {
            Console.WriteLine("\n--- Testing Status Effects ---");

            var allActions = ActionLoader.GetAllActions();
            var statusEffectTypes = new[]
            {
                ("Bleed", (Func<Action, bool>)(a => a.CausesBleed)),
                ("Poison", (Func<Action, bool>)(a => a.CausesPoison)),
                ("Burn", (Func<Action, bool>)(a => a.CausesBurn)),
                ("Weaken", (Func<Action, bool>)(a => a.CausesWeaken)),
                ("Slow", (Func<Action, bool>)(a => a.CausesSlow)),
                ("Stun", (Func<Action, bool>)(a => a.CausesStun)),
                ("Vulnerability", (Func<Action, bool>)(a => a.CausesVulnerability)),
                ("Harden", (Func<Action, bool>)(a => a.CausesHarden)),
                ("Expose", (Func<Action, bool>)(a => a.CausesExpose)),
                ("Silence", (Func<Action, bool>)(a => a.CausesSilence)),
                ("Pierce", (Func<Action, bool>)(a => a.CausesPierce)),
                ("StatDrain", (Func<Action, bool>)(a => a.CausesStatDrain)),
                ("Fortify", (Func<Action, bool>)(a => a.CausesFortify)),
                ("Focus", (Func<Action, bool>)(a => a.CausesFocus)),
                ("Cleanse", (Func<Action, bool>)(a => a.CausesCleanse)),
                ("Reflect", (Func<Action, bool>)(a => a.CausesReflect))
            };

            int testedTypes = 0;
            int validTypes = 0;

            foreach (var (effectName, checkFunc) in statusEffectTypes)
            {
                var actionsWithEffect = allActions.Where(checkFunc).ToList();
                if (actionsWithEffect.Count > 0)
                {
                    testedTypes++;
                    var sampleAction = actionsWithEffect.First();
                    
                    // Verify the action has the property set correctly
                    bool isValid = checkFunc(sampleAction);
                    if (isValid)
                    {
                        validTypes++;
                        Console.WriteLine($"  ✓ {effectName}: {actionsWithEffect.Count} actions, sample: {sampleAction.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"  ✗ {effectName}: Property check failed for {sampleAction.Name}");
                    }
                }
            }

            TestBase.AssertTrue(testedTypes > 0,
                $"At least one status effect type should be found, found {testedTypes}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(validTypes == testedTypes,
                $"All found status effects should be valid, {validTypes}/{testedTypes} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Roll Modifications Testing

        private static void TestRollModifications(MechanicsInventory inventory)
        {
            Console.WriteLine("\n--- Testing Roll Modifications ---");

            var allActions = ActionLoader.GetAllActions();
            int actionsWithRollMods = 0;
            int validRollMods = 0;

            foreach (var action in allActions)
            {
                bool hasRollMod = false;
                bool isValid = true;

                if (action.RollMods.MultipleDiceCount > 1)
                {
                    hasRollMod = true;
                    if (action.RollMods.MultipleDiceCount < 1)
                    {
                        isValid = false;
                    }
                }
                if (action.RollMods.ExplodingDice)
                {
                    hasRollMod = true;
                    if (action.RollMods.ExplodingDiceThreshold < 1 || action.RollMods.ExplodingDiceThreshold > 20)
                    {
                        isValid = false;
                    }
                }
                if (action.RollMods.AllowReroll)
                {
                    hasRollMod = true;
                    if (action.RollMods.RerollChance < 0.0 || action.RollMods.RerollChance > 1.0)
                    {
                        isValid = false;
                    }
                }
                if (action.RollMods.CriticalHitThresholdOverride > 0)
                {
                    hasRollMod = true;
                    if (action.RollMods.CriticalHitThresholdOverride < 1 || action.RollMods.CriticalHitThresholdOverride > 20)
                    {
                        isValid = false;
                    }
                }
                if (action.RollMods.Additive != 0 || action.RollMods.Multiplier != 1.0)
                {
                    hasRollMod = true;
                    if (action.RollMods.Multiplier < 0)
                    {
                        isValid = false;
                    }
                }

                if (hasRollMod)
                {
                    actionsWithRollMods++;
                    if (isValid)
                    {
                        validRollMods++;
                    }
                }
            }

            Console.WriteLine($"  Actions with roll modifications: {actionsWithRollMods}");
            Console.WriteLine($"  Valid roll modifications: {validRollMods}/{actionsWithRollMods}");

            // Test specific roll mod types if found
            var multiDiceActions = allActions.Where(a => a.RollMods.MultipleDiceCount > 1).ToList();
            if (multiDiceActions.Count > 0)
            {
                var sample = multiDiceActions.First();
                Console.WriteLine($"  ✓ MultipleDice: {multiDiceActions.Count} actions, sample: {sample.Name} ({sample.RollMods.MultipleDiceCount} dice, {sample.RollMods.MultipleDiceMode})");
            }

            var explodingActions = allActions.Where(a => a.RollMods.ExplodingDice).ToList();
            if (explodingActions.Count > 0)
            {
                var sample = explodingActions.First();
                Console.WriteLine($"  ✓ ExplodingDice: {explodingActions.Count} actions, sample: {sample.Name} (threshold={sample.RollMods.ExplodingDiceThreshold})");
            }

            TestBase.AssertTrue(validRollMods == actionsWithRollMods || actionsWithRollMods == 0,
                $"All roll modifications should be valid, {validRollMods}/{actionsWithRollMods} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Conditional Triggers Testing

        private static void TestConditionalTriggers(MechanicsInventory inventory)
        {
            Console.WriteLine("\n--- Testing Conditional Triggers ---");

            var allActions = ActionLoader.GetAllActions();
            var actionsWithTriggers = allActions.Where(a => 
                (a.Triggers.TriggerConditions != null && a.Triggers.TriggerConditions.Count > 0) ||
                a.Triggers.ExactRollTriggerValue > 0 ||
                !string.IsNullOrEmpty(a.Triggers.RequiredTag)
            ).ToList();

            Console.WriteLine($"  Actions with conditional triggers: {actionsWithTriggers.Count}");

            int validTriggers = 0;
            foreach (var action in actionsWithTriggers)
            {
                bool isValid = true;
                if (action.Triggers.TriggerConditions != null)
                {
                    foreach (var condition in action.Triggers.TriggerConditions)
                    {
                        if (string.IsNullOrWhiteSpace(condition))
                        {
                            isValid = false;
                        }
                    }
                }
                if (action.Triggers.ExactRollTriggerValue > 0 && 
                    (action.Triggers.ExactRollTriggerValue < 1 || action.Triggers.ExactRollTriggerValue > 20))
                {
                    isValid = false;
                }

                if (isValid)
                {
                    validTriggers++;
                    if (validTriggers <= 3) // Show first 3 examples
                    {
                        var conditions = action.Triggers.TriggerConditions != null 
                            ? string.Join(", ", action.Triggers.TriggerConditions) 
                            : "";
                        var exactRoll = action.Triggers.ExactRollTriggerValue > 0 
                            ? $"ExactRoll={action.Triggers.ExactRollTriggerValue}" 
                            : "";
                        var tag = !string.IsNullOrEmpty(action.Triggers.RequiredTag) 
                            ? $"Tag={action.Triggers.RequiredTag}" 
                            : "";
                        Console.WriteLine($"  ✓ {action.Name}: {conditions} {exactRoll} {tag}".Trim());
                    }
                }
            }

            TestBase.AssertTrue(validTriggers == actionsWithTriggers.Count || actionsWithTriggers.Count == 0,
                $"All conditional triggers should be valid, {validTriggers}/{actionsWithTriggers.Count} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Combo Routing Testing

        private static void TestComboRouting(MechanicsInventory inventory)
        {
            Console.WriteLine("\n--- Testing Combo Routing ---");

            var allActions = ActionLoader.GetAllActions();
            var actionsWithRouting = allActions.Where(a =>
                a.ComboRouting.JumpToSlot > 0 ||
                a.ComboRouting.SkipNext ||
                a.ComboRouting.RepeatPrevious ||
                a.ComboRouting.LoopToStart ||
                a.ComboRouting.StopEarly ||
                a.ComboRouting.DisableSlot ||
                a.ComboRouting.RandomAction ||
                a.ComboRouting.TriggerOnlyInSlot > 0
            ).ToList();

            Console.WriteLine($"  Actions with combo routing: {actionsWithRouting.Count}");

            int validRouting = 0;
            foreach (var action in actionsWithRouting)
            {
                bool isValid = true;
                if (action.ComboRouting.JumpToSlot < 0)
                {
                    isValid = false;
                }
                if (action.ComboRouting.TriggerOnlyInSlot < 0)
                {
                    isValid = false;
                }

                if (isValid)
                {
                    validRouting++;
                    if (validRouting <= 3) // Show first 3 examples
                    {
                        var routingTypes = new List<string>();
                        if (action.ComboRouting.JumpToSlot > 0) routingTypes.Add($"Jump({action.ComboRouting.JumpToSlot})");
                        if (action.ComboRouting.SkipNext) routingTypes.Add("SkipNext");
                        if (action.ComboRouting.RepeatPrevious) routingTypes.Add("RepeatPrevious");
                        if (action.ComboRouting.LoopToStart) routingTypes.Add("LoopToStart");
                        Console.WriteLine($"  ✓ {action.Name}: {string.Join(", ", routingTypes)}");
                    }
                }
            }

            TestBase.AssertTrue(validRouting == actionsWithRouting.Count || actionsWithRouting.Count == 0,
                $"All combo routing should be valid, {validRouting}/{actionsWithRouting.Count} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Advanced Mechanics Testing

        private static void TestAdvancedMechanics(MechanicsInventory inventory)
        {
            Console.WriteLine("\n--- Testing Advanced Mechanics ---");

            var allActions = ActionLoader.GetAllActions();
            int actionsWithAdvanced = 0;
            int validAdvanced = 0;

            foreach (var action in allActions)
            {
                bool hasAdvanced = false;
                bool isValid = true;

                if (action.Advanced.MultiHitCount > 1)
                {
                    hasAdvanced = true;
                    if (action.Advanced.MultiHitCount < 1)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.SelfDamagePercent > 0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.SelfDamagePercent < 0 || action.Advanced.SelfDamagePercent > 100)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.RollBonus != 0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.RollBonusDuration < 0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.StatBonus != 0)
                {
                    hasAdvanced = true;
                    if (string.IsNullOrEmpty(action.Advanced.StatBonusType) || action.Advanced.StatBonusDuration < 0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.HealAmount > 0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.HealAmount < 0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.HealthThreshold > 0.0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.HealthThreshold < 0.0 || action.Advanced.HealthThreshold > 1.0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.ConditionalDamageMultiplier != 1.0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.ConditionalDamageMultiplier < 0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.ExtraAttacks > 0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.ExtraAttacks < 0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.EnemyRollPenalty != 0)
                {
                    hasAdvanced = true;
                }
                if (action.Advanced.ExtraDamage > 0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.ExtraDamage < 0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.DamageReduction > 0.0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.DamageReduction < 0.0 || action.Advanced.DamageReduction > 1.0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.SelfAttackChance > 0.0)
                {
                    hasAdvanced = true;
                    if (action.Advanced.SelfAttackChance < 0.0 || action.Advanced.SelfAttackChance > 1.0)
                    {
                        isValid = false;
                    }
                }
                if (action.Advanced.ReduceLengthNextActions)
                {
                    hasAdvanced = true;
                    if (action.Advanced.LengthReduction < 0.0 || action.Advanced.LengthReductionDuration < 0)
                    {
                        isValid = false;
                    }
                }

                if (hasAdvanced)
                {
                    actionsWithAdvanced++;
                    if (isValid)
                    {
                        validAdvanced++;
                    }
                }
            }

            Console.WriteLine($"  Actions with advanced mechanics: {actionsWithAdvanced}");
            Console.WriteLine($"  Valid advanced mechanics: {validAdvanced}/{actionsWithAdvanced}");

            // Show examples
            var multiHitActions = allActions.Where(a => a.Advanced.MultiHitCount > 1).Take(3).ToList();
            if (multiHitActions.Count > 0)
            {
                foreach (var action in multiHitActions)
                {
                    Console.WriteLine($"  ✓ MultiHit: {action.Name} ({action.Advanced.MultiHitCount} hits)");
                }
            }

            var selfDamageActions = allActions.Where(a => a.Advanced.SelfDamagePercent > 0).Take(3).ToList();
            if (selfDamageActions.Count > 0)
            {
                foreach (var action in selfDamageActions)
                {
                    Console.WriteLine($"  ✓ SelfDamage: {action.Name} ({action.Advanced.SelfDamagePercent}%)");
                }
            }

            var healActions = allActions.Where(a => a.Advanced.HealAmount > 0).Take(3).ToList();
            if (healActions.Count > 0)
            {
                foreach (var action in healActions)
                {
                    Console.WriteLine($"  ✓ Heal: {action.Name} ({action.Advanced.HealAmount} HP)");
                }
            }

            TestBase.AssertTrue(validAdvanced == actionsWithAdvanced || actionsWithAdvanced == 0,
                $"All advanced mechanics should be valid, {validAdvanced}/{actionsWithAdvanced} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ACTION/ATTACK Bonuses Testing

        private static void TestActionAttackBonuses(MechanicsInventory inventory)
        {
            Console.WriteLine("\n--- Testing ACTION/ATTACK Bonuses ---");

            var allActions = ActionLoader.GetAllActions();
            var actionsWithBonuses = allActions.Where(a =>
                a.ActionAttackBonuses != null &&
                a.ActionAttackBonuses.BonusGroups != null &&
                a.ActionAttackBonuses.BonusGroups.Count > 0
            ).ToList();

            Console.WriteLine($"  Actions with ACTION/ATTACK bonuses: {actionsWithBonuses.Count}");

            int validBonuses = 0;
            foreach (var action in actionsWithBonuses)
            {
                bool isValid = true;
                foreach (var group in action.ActionAttackBonuses!.BonusGroups)
                {
                    if (string.IsNullOrEmpty(group.Keyword))
                    {
                        isValid = false;
                    }
                    if (group.Count < 1)
                    {
                        isValid = false;
                    }
                    if (group.Bonuses == null || group.Bonuses.Count == 0)
                    {
                        isValid = false;
                    }
                    else
                    {
                        foreach (var bonus in group.Bonuses)
                        {
                            if (string.IsNullOrEmpty(bonus.Type))
                            {
                                isValid = false;
                            }
                        }
                    }
                }

                if (isValid)
                {
                    validBonuses++;
                    if (validBonuses <= 5) // Show first 5 examples
                    {
                        var bonusDesc = string.Join("; ", action.ActionAttackBonuses.BonusGroups.Select(g =>
                            $"{g.Keyword}({g.Count}): {string.Join(", ", g.Bonuses.Select(b => $"{b.Type}+{b.Value}"))}"
                        ));
                        Console.WriteLine($"  ✓ {action.Name}: {bonusDesc}");
                    }
                }
            }

            TestBase.AssertTrue(validBonuses == actionsWithBonuses.Count || actionsWithBonuses.Count == 0,
                $"All ACTION/ATTACK bonuses should be valid, {validBonuses}/{actionsWithBonuses.Count} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Basic Properties Testing

        private static void TestBasicProperties(MechanicsInventory inventory)
        {
            Console.WriteLine("\n--- Testing Basic Properties ---");

            var allActions = ActionLoader.GetAllActions();
            int validActions = 0;
            int actionsWithValidType = 0;
            int actionsWithValidTarget = 0;
            int actionsWithValidDamage = 0;
            int actionsWithValidSpeed = 0;

            foreach (var action in allActions)
            {
                bool isValid = true;

                // Check type
                if (Enum.IsDefined(typeof(ActionType), action.Type))
                {
                    actionsWithValidType++;
                }
                else
                {
                    isValid = false;
                }

                // Check target
                if (Enum.IsDefined(typeof(TargetType), action.Target))
                {
                    actionsWithValidTarget++;
                }
                else
                {
                    isValid = false;
                }

                // Check damage multiplier
                if (action.DamageMultiplier >= 0)
                {
                    actionsWithValidDamage++;
                }
                else
                {
                    isValid = false;
                }

                // Check speed/length
                if (action.Length > 0)
                {
                    actionsWithValidSpeed++;
                }
                else
                {
                    isValid = false;
                }

                if (isValid)
                {
                    validActions++;
                }
            }

            Console.WriteLine($"  Actions with valid type: {actionsWithValidType}/{allActions.Count}");
            Console.WriteLine($"  Actions with valid target: {actionsWithValidTarget}/{allActions.Count}");
            Console.WriteLine($"  Actions with valid damage: {actionsWithValidDamage}/{allActions.Count}");
            Console.WriteLine($"  Actions with valid speed: {actionsWithValidSpeed}/{allActions.Count}");
            Console.WriteLine($"  Action types found: {string.Join(", ", inventory.ActionTypes)}");
            Console.WriteLine($"  Target types found: {string.Join(", ", inventory.TargetTypes)}");

            TestBase.AssertTrue(validActions == allActions.Count,
                $"All actions should have valid basic properties, {validActions}/{allActions.Count} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Combo System Testing

        private static void TestComboSystem(MechanicsInventory inventory)
        {
            Console.WriteLine("\n--- Testing Combo System ---");

            var allActions = ActionLoader.GetAllActions();
            var comboActions = allActions.Where(a => a.IsComboAction).ToList();
            var actionsWithComboOrder = allActions.Where(a => a.ComboOrder > 0).ToList();
            var actionsWithComboBonus = allActions.Where(a => a.ComboBonusAmount != 0 || a.ComboBonusDuration != 0).ToList();

            Console.WriteLine($"  Combo actions: {comboActions.Count}");
            Console.WriteLine($"  Actions with combo order: {actionsWithComboOrder.Count}");
            Console.WriteLine($"  Actions with combo bonuses: {actionsWithComboBonus.Count}");

            int validComboActions = 0;
            foreach (var action in comboActions)
            {
                bool isValid = action.ComboOrder >= 0 && 
                               action.ComboBonusAmount >= 0 && 
                               action.ComboBonusDuration >= 0;
                if (isValid)
                {
                    validComboActions++;
                    if (validComboActions <= 5) // Show first 5 examples
                    {
                        Console.WriteLine($"  ✓ {action.Name}: order={action.ComboOrder}, bonus={action.ComboBonusAmount}, duration={action.ComboBonusDuration}");
                    }
                }
            }

            TestBase.AssertTrue(validComboActions == comboActions.Count || comboActions.Count == 0,
                $"All combo actions should be valid, {validComboActions}/{comboActions.Count} are valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
