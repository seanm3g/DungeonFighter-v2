using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.Data.ActionBonusMechanics
{
    public static class ActionBonusAbilityCadenceTests
    {
        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            TestAbilityCadenceSpeedModToNextSlot();

            testsRun += _testsRun;
            testsPassed += _testsPassed;
            testsFailed += _testsFailed;
        }

        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;



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
                var character = ActionBonusMechanicsTestHelpers.CreateComboWithAbilitySpeedModAction();
                var comboActions = character.GetComboActions();
                if (comboActions.Count < 2) continue;

                var adrenalSurge = comboActions[0];
                var rage = comboActions[1];
                var enemy = new Enemy("TestEnemy", 1, 100, 5, 5, 5, 5);

                ActionSelector.ClearStoredRolls();
                ActionSelector.SetStoredActionRoll(character, 18);
                ActionSelector.SetStoredActionRoll(enemy, 10);

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
                ActionSelector.RemoveStoredRoll(character);
                ActionSelector.RemoveStoredRoll(enemy);
                break;
            }

            TestBase.AssertTrue(successCount >= 1,
                "Ability cadence SpeedMod to next slot verified",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
