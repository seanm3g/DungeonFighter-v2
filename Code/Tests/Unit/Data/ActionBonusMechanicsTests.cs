using System;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Tests;
using RPGGame.Utils;
using ActionBonusMechanics = RPGGame.Tests.Unit.Data.ActionBonusMechanics;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Tests for the action bonus system (ACTION/ATTACK cadence).
    /// Verifies: "For next ACTION" vs "For Next turn", success vs failure behavior, and full sequence flows.
    /// </summary>
    public static class ActionBonusMechanicsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Bonus Mechanics Tests ===\n");
            Console.WriteLine("Cadence types:");
            Console.WriteLine("  - For next ACTION: Buffs bank when source action hit+combos. Duration stacks additively. Full bank redeems on the next hit+combo. Miss / non-combo hit keep the bank pending. Cleared when the room ends.");
            Console.WriteLine("  - For Next turn: Buffs apply to the next roll. Consumed on every roll. Stat bonuses (STR/AGI/etc) apply ONLY on hit.\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            RollModificationManager.GetThresholdManager().Clear();
            Dice.ClearTestRoll();
            Dice.ClearAsyncLabEncounterTestRoll();
            ActionSelector.ClearStoredRolls();

            ActionBonusMechanics.ActionBonusMechanicsFlowTests.RunAll(ref testsRun, ref testsPassed, ref testsFailed);
            ActionBonusMechanics.ActionBonusCadenceActionTests.RunAll(ref testsRun, ref testsPassed, ref testsFailed);
            ActionBonusMechanics.ActionBonusCadenceTurnTests.RunAll(ref testsRun, ref testsPassed, ref testsFailed);
            ActionBonusMechanics.ActionBonusDisplayTests.RunAll(ref testsRun, ref testsPassed, ref testsFailed);
            ActionBonusMechanics.ActionBonusAbilityCadenceTests.RunAll(ref testsRun, ref testsPassed, ref testsFailed);
            ActionBonusMechanics.ActionBonusLoadedActionsTests.RunAll(ref testsRun, ref testsPassed, ref testsFailed);
            ActionBonusMechanics.ActionBonusScopedCadenceTests.RunAll(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("Action Bonus Mechanics Tests", testsRun, testsPassed, testsFailed);
        }
    }
}
