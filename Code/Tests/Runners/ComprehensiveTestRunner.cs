using System;
using RPGGame.Tests.Unit;
using RPGGame.Tests.Integration;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Comprehensive test runner that organizes all test categories and provides execution interfaces
    /// </summary>
    public static class ComprehensiveTestRunner
    {
        /// <summary>
        /// Runs all tests in the comprehensive test suite
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  COMPREHENSIVE GAME TEST SUITE");
            Console.WriteLine("========================================\n");

            // Phase 1: Core Mechanics
            Console.WriteLine("=== PHASE 1: CORE MECHANICS ===\n");
            ActionSystemTests.RunAllTests();
            Console.WriteLine();
            ActionExecutionFlowTests.RunAllTests();
            Console.WriteLine();
            DiceMechanicsTests.RunAllTests();
            Console.WriteLine();
            CombatOutcomeTests.RunAllTests();
            Console.WriteLine();
            RollBonusTests.RunAllTests();
            Console.WriteLine();
            CharacterAttributesTests.RunAllTests();
            Console.WriteLine();

            // Phase 2: Advanced Systems
            Console.WriteLine("\n=== PHASE 2: ADVANCED SYSTEMS ===\n");
            ComboExecutionTests.RunAllTests();
            Console.WriteLine();
            StatusEffectsTests.RunAllTests();
            Console.WriteLine();
            EnvironmentalActionsTests.RunAllTests();
            Console.WriteLine();
            ConditionalTriggersTests.RunAllTests();
            Console.WriteLine();

            // Phase 3: Display and UI
            Console.WriteLine("\n=== PHASE 3: DISPLAY AND UI ===\n");
            ColorSystemCoreTests.RunAllTests();
            Console.WriteLine();
            ColorSystemApplicationTests.RunAllTests();
            Console.WriteLine();
            ColorSystemRenderingTests.RunAllTests();
            Console.WriteLine();
            CombatLogDisplayTests.RunAllTests();
            Console.WriteLine();

            // Phase 4: Integration
            Console.WriteLine("\n=== PHASE 4: INTEGRATION ===\n");
            CombatIntegrationTests.RunAllTests();
            Console.WriteLine();
            SystemInteractionTests.RunAllTests();
            Console.WriteLine();

            Console.WriteLine("\n========================================");
            Console.WriteLine("  ALL TESTS COMPLETE");
            Console.WriteLine("========================================\n");
        }

        /// <summary>
        /// Runs quick tests (fast unit tests)
        /// </summary>
        public static void RunQuickTests()
        {
            Console.WriteLine("=== QUICK TESTS ===\n");
            
            DiceMechanicsTests.RunAllTests();
            Console.WriteLine();
            CombatOutcomeTests.RunAllTests();
            Console.WriteLine();
            RollBonusTests.RunAllTests();
        }

        /// <summary>
        /// Runs action system tests
        /// </summary>
        public static void RunActionSystemTests()
        {
            Console.WriteLine("=== ACTION SYSTEM TESTS ===\n");
            
            ActionSystemTests.RunAllTests();
            Console.WriteLine();
            ActionExecutionFlowTests.RunAllTests();
        }

        /// <summary>
        /// Runs dice mechanics tests
        /// </summary>
        public static void RunDiceMechanicsTests()
        {
            Console.WriteLine("=== DICE MECHANICS TESTS ===\n");
            
            DiceMechanicsTests.RunAllTests();
            Console.WriteLine();
            CombatOutcomeTests.RunAllTests();
            Console.WriteLine();
            RollBonusTests.RunAllTests();
        }

        /// <summary>
        /// Runs combo system tests
        /// </summary>
        public static void RunComboSystemTests()
        {
            Console.WriteLine("=== COMBO SYSTEM TESTS ===\n");
            
            ComboExecutionTests.RunAllTests();
        }

        /// <summary>
        /// Runs color system tests
        /// </summary>
        public static void RunColorSystemTests()
        {
            Console.WriteLine("=== COLOR SYSTEM TESTS ===\n");
            
            ColorSystemCoreTests.RunAllTests();
            Console.WriteLine();
            ColorSystemApplicationTests.RunAllTests();
            Console.WriteLine();
            ColorSystemRenderingTests.RunAllTests();
        }

        /// <summary>
        /// Runs display system tests
        /// </summary>
        public static void RunDisplaySystemTests()
        {
            Console.WriteLine("=== DISPLAY SYSTEM TESTS ===\n");
            
            CombatLogDisplayTests.RunAllTests();
        }

        /// <summary>
        /// Runs character system tests
        /// </summary>
        public static void RunCharacterSystemTests()
        {
            Console.WriteLine("=== CHARACTER SYSTEM TESTS ===\n");
            
            CharacterAttributesTests.RunAllTests();
        }

        /// <summary>
        /// Runs status effects tests
        /// </summary>
        public static void RunStatusEffectsTests()
        {
            Console.WriteLine("=== STATUS EFFECTS TESTS ===\n");
            
            StatusEffectsTests.RunAllTests();
        }

        /// <summary>
        /// Runs integration tests
        /// </summary>
        public static void RunIntegrationTests()
        {
            Console.WriteLine("=== INTEGRATION TESTS ===\n");
            
            CombatIntegrationTests.RunAllTests();
            Console.WriteLine();
            SystemInteractionTests.RunAllTests();
        }
    }
}

