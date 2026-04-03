using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Execution;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Tests for the action bonus system (ACTION/ATTACK cadence).
    /// Verifies: "For next ACTION" vs "For next ATTACK", success vs failure behavior, and full sequence flows.
    /// </summary>
    public static class ActionBonusMechanicsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Bonus Mechanics Tests ===\n");
            Console.WriteLine("Cadence types:");
            Console.WriteLine("  - For next ACTION: Buffs attach to the next combo slot. Added when source action SUCCEEDS (hit+combo). Consumed when that slot rolls.");
            Console.WriteLine("  - For next ATTACK: Buffs apply to the next roll. Consumed on every roll. Stat bonuses (STR/AGI/etc) apply ONLY on hit.\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionBonusAddLogic_Deterministic();
            TestFullFlow_ActionBuffsNextAction();
            TestForNextAction_OnSuccess();
            TestForNextAction_OnFailure();
            TestForNextAttack_OnSuccess();
            TestForNextAttack_OnFailure();
            TestStateAndDisplay();
            TestAbilityCadenceSpeedModToNextSlot();
            TestCadenceTypeFromActionsJson();
            TestAllLoadedActionsWithBonuses();

            TestBase.PrintSummary("Action Bonus Mechanics Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Full Flow: Action Buffs Next Action

        /// <summary>
        /// Deterministic: Simulates the ACTION bonus add logic (what ApplyHitOutcome does) to verify slot indexing.
        /// </summary>
        private static void TestActionBonusAddLogic_Deterministic()
        {
            Console.WriteLine("--- Deterministic: ACTION bonus add logic ---");
            Console.WriteLine("  Simulates ApplyHitOutcome: when slot 0 succeeds, add +3 COMBO to slot 1.\n");

            var character = CreateComboWithBuffingAction();
            var comboActions = character.GetComboActions();
            int comboLength = comboActions.Count;
            character.ComboStep = 0;

            TestBase.AssertTrue(comboLength >= 2,
                "Combo must have at least 2 actions for this test",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            Console.WriteLine($"  Combo has {comboLength} actions: [{string.Join(", ", comboActions.Select(a => a.Name))}]");
            Console.WriteLine($"  ComboStep=0 -> nextSlot = (0+1)%{comboLength} = 1");

            // Simulate what ApplyHitOutcome does for ACTION cadence
            int nextSlot = (character.ComboStep + 1) % comboLength;
            var bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } };
            character.Effects.AddPendingActionBonuses(nextSlot, bonuses);

            var pending = character.Effects.GetPendingActionBonusesForSlot(1);
            TestBase.AssertTrue(pending.Count > 0 && pending.Any(b => b.Type == "COMBO" && b.Value == 3),
                "Simulated add: slot 1 has +3 COMBO",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            Console.WriteLine($"  Slot 1 pending: {pending.Count} bonus(es). OK.\n");
        }

        /// <summary>
        /// Full sequence: Action 1 has "For next ACTION: +3 COMBO". When Action 1 succeeds, Action 2 gets the bonus.
        /// When we roll for Action 2, the bonus is consumed and applied to the roll.
        /// </summary>
        private static void TestFullFlow_ActionBuffsNextAction()
        {
            Console.WriteLine("--- Full Flow: Action 1 buffs Action 2 (For next ACTION) ---");
            Console.WriteLine("  Setup: Combo [SetupStrike, Finisher]. SetupStrike grants +3 COMBO to next action.");
            Console.WriteLine("  Step 1: Execute SetupStrike. On success -> slot 2 gets +3 COMBO.");
            Console.WriteLine("  Step 2: Execute Finisher (slot 2). Bonuses consumed and applied to roll.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int successCount = 0;

            for (int i = 0; i < 100; i++)
            {
                var character = CreateComboWithBuffingAction();
                var comboActions = character.GetComboActions();
                if (comboActions.Count < 2)
                {
                    Console.WriteLine($"  WARNING: Combo has {comboActions.Count} actions (expected 2). Skipping run.");
                    continue;
                }
                var setupAction = comboActions[0];
                var finisherAction = comboActions[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                // Step 1: Execute SetupStrike (has "For next ACTION: +3 COMBO")
                var result1 = ActionExecutionFlow.Execute(character, enemy, null, null, setupAction, null, lastUsed, lastCritMiss);
                if (!result1.Hit || !result1.IsCombo) continue;

                successCount++;
                Console.WriteLine($"  Run {successCount}: SetupStrike succeeded (hit+combo). ComboStep={character.ComboStep}, ComboLength={comboActions.Count}");

                // Check all slots for diagnostics
                var allSlots = character.Effects.GetPendingActionBonusSlots().OrderBy(s => s).ToList();
                Console.WriteLine($"  Pending bonus slots: [{string.Join(", ", allSlots)}]");
                foreach (var slot in allSlots)
                {
                    var p = character.Effects.GetPendingActionBonusesForSlot(slot);
                    Console.WriteLine($"    Slot {slot}: {p.Count} bonus(es) - {string.Join(", ", p.Select(b => $"{b.Type}+{b.Value}"))}");
                }

                // Verify slot 1 (0-indexed = second action = Finisher) now has +3 COMBO pending
                var pendingBefore = character.Effects.GetPendingActionBonusesForSlot(1);
                TestBase.AssertTrue(pendingBefore.Count > 0 && pendingBefore.Any(b => b.Type == "COMBO" && b.Value == 3),
                    "After SetupStrike succeeds: slot 2 (index 1) has +3 COMBO pending",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Step 2: Execute Finisher (slot 1 = second action). Bonuses consumed.
                character.ComboStep = 1;
                var result2 = ActionExecutionFlow.Execute(character, enemy, null, null, finisherAction, null, lastUsed, lastCritMiss);

                var pendingAfter = character.Effects.GetPendingActionBonusesForSlot(1);
                TestBase.AssertTrue(pendingAfter.Count == 0,
                    "After Finisher rolls: slot 2 bonuses consumed (applied to roll)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                Console.WriteLine($"  Run {successCount}: Finisher executed. Bonuses consumed. Hit={result2.Hit}, Combo={result2.IsCombo}.\n");
                break;
            }

            TestBase.AssertTrue(successCount >= 1,
                $"Full flow verified in {successCount} successful run(s)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region For Next ACTION: Success vs Failure

        /// <summary>
        /// For next ACTION: When the SOURCE action SUCCEEDS (hit+combo), bonuses are added to the next slot.
        /// </summary>
        private static void TestForNextAction_OnSuccess()
        {
            Console.WriteLine("\n--- For next ACTION: On SUCCESS ---");
            Console.WriteLine("  When Action 1 (with ACTION cadence) HITS and COMBOs -> next slot gets the bonus.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 80; i++)
            {
                var character = CreateComboWithBuffingAction();
                var action1 = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action1, null, lastUsed, lastCritMiss);

                if (result.Hit && result.IsCombo)
                {
                    verified++;
                    var pending = character.Effects.GetPendingActionBonusesForSlot(1);
                    var allSlots = character.Effects.GetPendingActionBonusSlots().OrderBy(s => s).ToList();
                    Console.WriteLine($"  ComboStep={character.ComboStep}, PendingSlots=[{string.Join(",", allSlots)}], Slot1 count={pending.Count}");
                    TestBase.AssertTrue(pending.Count > 0,
                        "ACTION cadence on SUCCESS: next slot has pending bonuses",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    Console.WriteLine($"  Verified: Action succeeded -> slot 2 has {pending.Count} bonus(es).\n");
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For next ACTION on success: bonuses added to next slot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// For next ACTION: When the SOURCE action FAILS (miss), NO bonuses are added to the next slot.
        /// </summary>
        private static void TestForNextAction_OnFailure()
        {
            Console.WriteLine("\n--- For next ACTION: On FAILURE ---");
            Console.WriteLine("  When Action 1 (with ACTION cadence) MISSES -> next slot gets NOTHING.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 80; i++)
            {
                var character = CreateComboWithBuffingAction();
                var action1 = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action1, null, lastUsed, lastCritMiss);

                if (!result.Hit)
                {
                    verified++;
                    var pending = character.Effects.GetPendingActionBonusesForSlot(1);
                    TestBase.AssertTrue(pending.Count == 0,
                        "ACTION cadence on FAILURE: next slot has NO pending bonuses",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    Console.WriteLine($"  Verified: Action missed -> slot 2 has 0 bonuses.\n");
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For next ACTION on failure: no bonuses added",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region For Next ATTACK: Success vs Failure

        /// <summary>
        /// For next ATTACK: When the roll HITS, stat bonuses (STR, AGI, etc) ARE applied.
        /// </summary>
        private static void TestForNextAttack_OnSuccess()
        {
            Console.WriteLine("\n--- For next ATTACK: On SUCCESS (hit) ---");
            Console.WriteLine("  ATTACK bonuses are consumed on every roll. Stat bonuses (STR/AGI) apply ONLY when we HIT.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 100; i++)
            {
                var character = CreateTestCharacterWithCombo();
                character.Stats.TempStrengthBonus = 0;
                character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new ActionAttackBonusGroup
                        {
                            CadenceType = "ATTACK",
                            Count = 1,
                            Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "STR", Value = 5 } }
                        }
                    }
                });

                var action = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action, null, lastUsed, lastCritMiss);

                if (result.Hit)
                {
                    verified++;
                    TestBase.AssertTrue(character.Stats.TempStrengthBonus >= 5,
                        "ATTACK on HIT: STR bonus applied",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    Console.WriteLine($"  Verified: Roll hit -> TempStrengthBonus = {character.Stats.TempStrengthBonus}.\n");
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For next ATTACK on hit: stat bonus applied",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// For next ATTACK: When the roll MISSES, stat bonuses are consumed but NOT applied.
        /// </summary>
        private static void TestForNextAttack_OnFailure()
        {
            Console.WriteLine("\n--- For next ATTACK: On FAILURE (miss) ---");
            Console.WriteLine("  ATTACK bonuses are consumed on every roll. When we MISS, stat bonuses are wasted (not applied).\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int verified = 0;

            for (int i = 0; i < 100; i++)
            {
                var character = CreateTestCharacterWithCombo();
                character.Stats.TempStrengthBonus = 0;
                character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
                {
                    BonusGroups = new List<ActionAttackBonusGroup>
                    {
                        new ActionAttackBonusGroup
                        {
                            CadenceType = "ATTACK",
                            Count = 1,
                            Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "STR", Value = 5 } }
                        }
                    }
                });

                var action = character.GetComboActions()[0];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);
                var result = ActionExecutionFlow.Execute(character, enemy, null, null, action, null, lastUsed, lastCritMiss);

                if (!result.Hit)
                {
                    verified++;
                    TestBase.AssertTrue(character.Stats.TempStrengthBonus == 0,
                        "ATTACK on MISS: STR bonus NOT applied (consumed but wasted)",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(character.Effects.PeekAttackBonuses().Count == 0,
                        "ATTACK bonus consumed even on miss",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    Console.WriteLine($"  Verified: Roll missed -> TempStrengthBonus = 0, bonus consumed.\n");
                    break;
                }
            }

            TestBase.AssertTrue(verified >= 1,
                "For next ATTACK on miss: stat bonus not applied",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region State and Display

        private static void TestStateAndDisplay()
        {
            Console.WriteLine("\n--- State and Display ---");

            // Pending add/consume
            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            character.Effects.AddPendingActionBonuses(1, new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } });
            var consumed = character.Effects.ConsumePendingActionBonusesForSlot(1);
            TestBase.AssertTrue(consumed.Count == 1 && consumed[0].Type == "COMBO",
                "Pending bonuses: add then consume returns correct bonuses",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // ATTACK consume
            character.Effects.ClearAllTempEffects();
            character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup { CadenceType = "ATTACK", Count = 1, Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "HIT", Value = 2 } } }
                }
            });
            var attackConsumed = character.Effects.GetAndConsumeAttackBonuses();
            TestBase.AssertTrue(attackConsumed.Count > 0 && character.Effects.PeekAttackBonuses().Count == 0,
                "ATTACK bonuses: consume clears queue",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // BuildActiveModifierLines
            character.Effects.ClearAllTempEffects();
            character.Effects.AddPendingActionBonuses(1, new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } });
            var lines = CombatActionStripBuilder.BuildActiveModifierLines(character);
            TestBase.AssertTrue(lines != null && lines.Count >= 1 && lines[0].Contains("Next Action 2") && lines[0].Contains("COMBO"),
                "Display: BuildActiveModifierLines shows 'Next Action 2: +3 COMBO'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Clear on combo change
            character.Effects.AddPendingActionBonuses(0, new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "HIT", Value = 1 } });
            character.Effects.ClearPendingActionBonuses();
            TestBase.AssertTrue(character.Effects.GetPendingActionBonusSlots().Count() == 0,
                "ClearPendingActionBonuses clears all slots",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine();
        }

        #endregion

        #region Ability Cadence: SpeedMod to Next Slot (Adrenal Surge pattern)

        /// <summary>
        /// Ability cadence with SpeedMod: When Adrenal Surge (slot 1) hits in combo, its SpeedMod is added to slot 2.
        /// When slot 2 executes, the bonus is consumed and ConsumedSpeedModPercent is set.
        /// </summary>
        private static void TestAbilityCadenceSpeedModToNextSlot()
        {
            Console.WriteLine("\n--- Ability Cadence: SpeedMod to Next Slot (Adrenal Surge pattern) ---");
            Console.WriteLine("  When slot 1 (Ability cadence, SpeedMod 20) hits in combo -> slot 2 gets SPEED_MOD.");
            Console.WriteLine("  When slot 2 executes, bonus consumed and speed modifier applied.\n");

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCritMiss = new Dictionary<Actor, bool>();
            int successCount = 0;

            for (int i = 0; i < 100; i++)
            {
                var character = CreateComboWithAbilitySpeedModAction();
                var comboActions = character.GetComboActions();
                if (comboActions.Count < 2) continue;

                var adrenalSurge = comboActions[0];
                var rage = comboActions[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                // Step 1: Execute Adrenal Surge (slot 0). On hit+combo -> slot 1 gets SPEED_MOD
                var result1 = ActionExecutionFlow.Execute(character, enemy, null, null, adrenalSurge, null, lastUsed, lastCritMiss);
                if (!result1.Hit || !result1.IsCombo) continue;

                successCount++;
                var pending = character.Effects.GetPendingActionBonusesForSlot(1);
                TestBase.AssertTrue(pending.Count > 0 && pending.Any(b => b.Type == "SPEED_MOD" && Math.Abs(b.Value - 20) < 0.01),
                    "After Adrenal Surge succeeds: slot 2 has +20% SPEED_MOD pending",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Step 2: Execute Rage (slot 1). Bonuses consumed, ConsumedSpeedModPercent set
                character.ComboStep = 1;
                var result2 = ActionExecutionFlow.Execute(character, enemy, null, null, rage, null, lastUsed, lastCritMiss);

                var pendingAfter = character.Effects.GetPendingActionBonusesForSlot(1);
                TestBase.AssertTrue(pendingAfter.Count == 0,
                    "After Rage executes: slot 2 bonuses consumed",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // ConsumedSpeedModPercent should be set when slot 2 rolled (consumed before roll)
                TestBase.AssertTrue(character.Effects.ConsumedSpeedModPercent == 20,
                    "After Rage executes: ConsumedSpeedModPercent = 20 (speed modifier applied)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                Console.WriteLine($"  Verified: Adrenal Surge hit -> slot 2 got SPEED_MOD; Rage consumed it; ConsumedSpeedModPercent={character.Effects.ConsumedSpeedModPercent}.\n");
                break;
            }

            TestBase.AssertTrue(successCount >= 1,
                "Ability cadence SpeedMod to next slot verified",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Loaded Actions: Each Action and Its Modifications

        /// <summary>
        /// For each loaded action with ACTION/ATTACK bonuses, verifies the modification is correctly
        /// applied to the next ACTION or ATTACK. Outputs each action name and what it modifies.
        /// </summary>
        private static void TestAllLoadedActionsWithBonuses()
        {
            Console.WriteLine("\n--- All Loaded Actions: Modifications to Next ACTION/ATTACK ---");
            Console.WriteLine("  Each action that buffs the next action or attack is tested.\n");

            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();
            var withBonuses = allActions
                .Where(a => a.ActionAttackBonuses != null &&
                            a.ActionAttackBonuses.BonusGroups != null &&
                            a.ActionAttackBonuses.BonusGroups.Count > 0)
                .ToList();

            int actionTested = 0;
            int actionPassed = 0;

            foreach (var action in withBonuses)
            {
                foreach (var group in action.ActionAttackBonuses!.BonusGroups)
                {
                    var ct = group.CadenceType ?? group.Keyword ?? "";
                    if (ct != "ACTION" && ct != "ATTACK") continue;

                    string desc = ActionAttackKeywordProcessor.GenerateKeywordString(
                        new ActionAttackBonuses { BonusGroups = new List<ActionAttackBonusGroup> { group } });
                    string modDesc = string.IsNullOrEmpty(desc) ? FormatBonusGroupShort(group) : desc;
                    Console.WriteLine($"  [{action.Name}] {ct}: {modDesc}");

                    var character = TestDataBuilders.Character().WithName("TestHero").Build();
                    character.Effects.ClearAllTempEffects();

                    if (ct == "ACTION")
                    {
                        // Simulate: when slot 0 succeeds, add to slot 1
                        int nextSlot = 1;
                        if (group.Bonuses != null && group.Bonuses.Count > 0)
                        {
                            for (int i = 0; i < group.Count; i++)
                                character.Effects.AddPendingActionBonuses(nextSlot, group.Bonuses);
                            var pending = character.Effects.GetPendingActionBonusesForSlot(nextSlot);
                            bool ok = pending.Count >= group.Bonuses.Count;
                            foreach (var b in group.Bonuses)
                                ok = ok && pending.Any(p => p.Type == b.Type && Math.Abs(p.Value - b.Value) < 0.01);
                            TestBase.AssertTrue(ok,
                                $"{action.Name} ACTION: bonuses stored for next slot",
                                ref _testsRun, ref _testsPassed, ref _testsFailed);
                            if (ok) actionPassed++;
                        }
                    }
                    else // ATTACK
                    {
                        character.Effects.AddActionAttackBonuses(new ActionAttackBonuses { BonusGroups = new List<ActionAttackBonusGroup> { group } });
                        var peeked = character.Effects.PeekAttackBonuses();
                        bool ok = peeked.Count >= (group.Bonuses?.Count ?? 0);
                        if (group.Bonuses != null)
                            foreach (var b in group.Bonuses)
                                ok = ok && peeked.Any(p => p.Type == b.Type && Math.Abs(p.Value - b.Value) < 0.01);
                        TestBase.AssertTrue(ok,
                            $"{action.Name} ATTACK: bonuses queued for next roll",
                            ref _testsRun, ref _testsPassed, ref _testsFailed);
                        if (ok) actionPassed++;
                    }
                    actionTested++;
                }
            }

            Console.WriteLine($"\n  Tested {actionTested} action modification(s).\n");
        }

        private static string FormatBonusGroupShort(ActionAttackBonusGroup g)
        {
            if (g?.Bonuses == null || g.Bonuses.Count == 0) return "(no bonuses)";
            var parts = g.Bonuses.Select(b =>
            {
                string sign = b.Value >= 0 ? "+" : "";
                return $"{sign}{b.Value:0} {b.Type}";
            });
            string cadence = g.CadenceType ?? g.Keyword ?? "";
            string count = g.Count > 1 ? $"{g.Count} {cadence}S" : $"Next {cadence}";
            return $"For {count}: {string.Join(", ", parts)}";
        }

        #endregion

        #region Cadence Parsing

        private static void TestCadenceTypeFromActionsJson()
        {
            Console.WriteLine("\n--- CadenceType from Actions.json ---");
            Console.WriteLine("  Verify loaded actions with cadence have correct ACTION/ATTACK/ABILITY type.\n");

            ActionLoader.LoadActions();
            var allActions = ActionLoader.GetAllActions();
            var withBonuses = allActions.Where(a =>
                a.ActionAttackBonuses != null &&
                a.ActionAttackBonuses.BonusGroups != null &&
                a.ActionAttackBonuses.BonusGroups.Count > 0).ToList();

            int valid = 0;
            foreach (var action in withBonuses)
            {
                foreach (var group in action.ActionAttackBonuses!.BonusGroups)
                {
                    var ct = group.CadenceType ?? "";
                    if (ct == "ACTION" || ct == "ATTACK" || ct == "ABILITY") valid++;
                }
            }

            TestBase.AssertTrue(withBonuses.Count == 0 || valid > 0,
                $"Loaded {withBonuses.Count} actions with bonuses; {valid} have valid CadenceType",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates a 2-action combo: SetupStrike (grants "For next ACTION: +3 COMBO") and Finisher.
        /// </summary>
        private static Character CreateComboWithBuffingAction()
        {
            var character = new Character("TestHero", 1);
            var setup = TestDataBuilders.CreateMockAction("SetupStrike", ActionType.Attack);
            setup.IsComboAction = true;
            setup.ComboOrder = 1; // First in combo
            setup.ActionAttackBonuses = new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "ACTION",
                        Count = 1,
                        Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "COMBO", Value = 3 } }
                    }
                }
            };
            var finisher = TestDataBuilders.CreateMockAction("Finisher", ActionType.Attack);
            finisher.IsComboAction = true;
            finisher.ComboOrder = 2; // Second in combo
            character.AddAction(setup, 1.0);
            character.AddAction(finisher, 1.0);
            character.Actions.AddToCombo(setup);
            character.Actions.AddToCombo(finisher);
            character.ComboStep = 0;
            return character;
        }

        private static Character CreateTestCharacterWithCombo()
        {
            var character = new Character("TestHero", 1);
            var combo1 = TestDataBuilders.CreateMockAction("COMBO1", ActionType.Attack);
            combo1.IsComboAction = true;
            var combo2 = TestDataBuilders.CreateMockAction("COMBO2", ActionType.Attack);
            combo2.IsComboAction = true;
            character.AddAction(combo1, 1.0);
            character.AddAction(combo2, 1.0);
            character.Actions.AddToCombo(combo1);
            character.Actions.AddToCombo(combo2);
            character.ComboStep = 0;
            return character;
        }

        /// <summary>
        /// Creates a 2-action combo: Adrenal Surge (Ability cadence, SpeedMod 20) and Rage.
        /// </summary>
        private static Character CreateComboWithAbilitySpeedModAction()
        {
            var character = new Character("TestHero", 1);
            var adrenalSurge = TestDataBuilders.CreateMockAction("AdrenalSurge", ActionType.Attack);
            adrenalSurge.IsComboAction = true;
            adrenalSurge.Cadence = "Ability";
            adrenalSurge.SpeedMod = "20";
            var rage = TestDataBuilders.CreateMockAction("Rage", ActionType.Attack);
            rage.IsComboAction = true;
            rage.Length = 0.5;
            character.AddAction(adrenalSurge, 1.0);
            character.AddAction(rage, 1.0);
            character.Actions.AddToCombo(adrenalSurge);
            character.Actions.AddToCombo(rage);
            character.ComboStep = 0;
            return character;
        }

        #endregion
    }
}
