using System;
using RPGGame;
using RPGGame.Tests.Unit;
using RPGGame.Tests.Unit.Combat;
using RPGGame.Tests.Unit.Diagnostics;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for Combat system tests
    /// </summary>
    public static class CombatSystemTestRunner
    {
        /// <summary>
        /// Runs all Combat system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  COMBAT SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            CombatManagerTests.RunAllTests();
            Console.WriteLine();
            CombatDelayManagerTests.RunAllTests();
            Console.WriteLine();
            CombatUiMuteScopeTests.RunAllTests();
            Console.WriteLine();
            CombatStateManagerTests.RunAllTests();
            Console.WriteLine();
            CombatTurnHandlerTests.RunAllTests();
            Console.WriteLine();
            CombatEffectsTests.RunAllTests();
            Console.WriteLine();
            CombatResultsTests.RunAllTests();
            Console.WriteLine();
            TurnManagerTests.RunAllTests();
            Console.WriteLine();
            DamageCalculatorTests.RunAllTests();
            Console.WriteLine();
            WeaponBaseModCadenceTests.RunAllTests();
            Console.WriteLine();
            CombatHotPathMetricsTests.RunAllTests();
            Console.WriteLine();
            HitCalculatorTests.RunAllTests();
            Console.WriteLine();
            SpeedCalculatorTests.RunAllTests();
            Console.WriteLine();
            StatusEffectCalculatorTests.RunAllTests();
            Console.WriteLine();
            ThresholdManagerTests.RunAllTests();
            Console.WriteLine();
            BattleNarrativeTests.RunAllTests();
            Console.WriteLine();
            CombatEffectsSimplifiedTests.RunAllTests();
            ActionTriggerGateTests.RunAllTests();
            ActionTriggerBundleApplicatorTests.RunAllTests();
            StripMutationTests.RunAllTests();
            RetriggerTests.RunAllTests();
            RollProbabilityContentTests.RunAllTests();
            WeaponModTriggerBridgeTests.RunAllTests();
            EquippedItemTriggerTests.RunAllTests();
            ItemTriggerCombatIntegrationTests.RunAllTests();
            Console.WriteLine();
            StunProcessorTests.RunAllTests();
            Console.WriteLine();
            ActionEffectTargetTests.RunAllTests();
            Console.WriteLine();
            HealthBarDeltaDamageHintTests.RunAllTests();
            Console.WriteLine();
            BattleEventAnalyzerTests.RunAllTests();
            Console.WriteLine();
            DamageFormatterTests.RunAllTests();
            Console.WriteLine();
            BattleNarrativeFormattersTests.RunAllTests();
            Console.WriteLine();
            CombatResultsColoredTextTests.RunAllTests();
            Console.WriteLine();
            int run = 0, pass = 0, fail = 0;
            ActionMechanicTagProcessorTests.RunAll(ref run, ref pass, ref fail);
            Console.WriteLine($"ActionMechanicTagProcessor: {pass}/{run} passed\n");
            ActionRollTagProcessorTests.RunAll(ref run, ref pass, ref fail);
            Console.WriteLine($"ActionRollTagProcessor: {pass}/{run} passed\n");
            EnvironmentRollModifierTests.RunAll(ref run, ref pass, ref fail);
            Console.WriteLine($"EnvironmentRollModifier: {pass}/{run} passed\n");
        }
    }
}
