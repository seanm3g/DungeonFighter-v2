using System;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for Advanced Action Mechanics (All Phases)
    /// This class delegates to phase-specific test classes for better organization.
    /// </summary>
    public static class AdvancedMechanicsTest
    {
        /// <summary>
        /// Runs all Advanced Mechanics tests
        /// NOTE: This class now delegates to phase-specific test classes for better organization.
        /// The original test methods have been moved to:
        /// - AdvancedMechanicsTest_Phase1 (Roll Modification & Conditional Triggers)
        /// - AdvancedMechanicsTest_Phase2 (Advanced Status Effects)
        /// - AdvancedMechanicsTest_Phase3 (Tag System & Combo Routing)
        /// - AdvancedMechanicsTest_Phase4 (Outcome-Based Actions & Meta-Progression)
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Advanced Action Mechanics Tests ===\n");

            // Phase 1: Roll Modification & Conditional Triggers
            AdvancedMechanicsTest_Phase1.RunAllTests();
            Console.WriteLine();

            // Phase 2: Advanced Status Effects
            AdvancedMechanicsTest_Phase2.RunAllTests();
            Console.WriteLine();

            // Phase 3: Tag System & Combo Routing
            AdvancedMechanicsTest_Phase3.RunAllTests();
            Console.WriteLine();

            // Phase 4: Outcome-Based Actions & Meta-Progression
            AdvancedMechanicsTest_Phase4.RunAllTests();
            Console.WriteLine();

            Console.WriteLine("=== All Advanced Mechanics Tests Complete ===");
        }
    }
}
