using System;
using RPGGame.Tests; // For TestBase

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for dice rolls, combo sequences, and action triggering
    /// Tests dice roll mechanics, action selection, combo sequence information, and conditional triggers
    /// NOTE: This class now delegates to split test classes for better organization.
    /// The original test methods have been moved to:
    /// - ComboDiceRollTestsDiceMechanics (Dice Roll Mechanics)
    /// - ComboDiceRollTestsActionSelection (Action Selection Based on Dice Rolls)
    /// - ComboDiceRollTestsComboSequences (Combo Sequence Information and Integration)
    /// - ComboDiceRollTestsIsComboFlag (IsCombo Flag Tests - Bug Detection)
    /// - ComboDiceRollTestsConditionalTriggers (Conditional Trigger Tests)
    /// </summary>
    public static class ComboDiceRollTests
    {
        /// <summary>
        /// Runs all combo and dice roll tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Combo and Dice Roll Tests ===\n");
            
            // Run split test classes
            ComboDiceRollTestsDiceMechanics.RunAllTests();
            Console.WriteLine();
            ComboDiceRollTestsActionSelection.RunAllTests();
            Console.WriteLine();
            ComboDiceRollTestsComboSequences.RunAllTests();
            Console.WriteLine();
            ComboDiceRollTestsIsComboFlag.RunAllTests();
            Console.WriteLine();
            ComboDiceRollTestsConditionalTriggers.RunAllTests();
        }
    }
}
