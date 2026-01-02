using System;
using RPGGame;
using RPGGame.Tests.Unit.Combat;

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
            Console.WriteLine();
            BattleEventAnalyzerTests.RunAllTests();
            Console.WriteLine();
            DamageFormatterTests.RunAllTests();
            Console.WriteLine();
            BattleNarrativeFormattersTests.RunAllTests();
            Console.WriteLine();
            CombatResultsColoredTextTests.RunAllTests();
        }
    }
}
