using System;
using RPGGame.Tests.Unit;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for manager and calculator classes
    /// Executes all tests for ClassActionManager, ComboSequenceManager, GearActionManager,
    /// DefaultActionManager, CombatCalculator, and UIMessageBuilder
    /// </summary>
    public static class ManagerAndCalculatorTestRunner
    {
        /// <summary>
        /// Runs all manager and calculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Manager and Calculator Test Suite");
            Console.WriteLine("========================================\n");

            try
            {
                // Run manager tests
                Console.WriteLine("\n[MANAGER TESTS]");
                Console.WriteLine("===============\n");
                
                ClassActionManagerTests.RunAllTests();
                Console.WriteLine();
                
                ComboSequenceManagerTests.RunAllTests();
                Console.WriteLine();
                
                GearActionManagerTests.RunAllTests();
                Console.WriteLine();
                
                DefaultActionManagerTests.RunAllTests();
                Console.WriteLine();

                // Run calculator tests
                Console.WriteLine("\n[CALCULATOR TESTS]");
                Console.WriteLine("==================\n");
                
                CombatCalculatorTests.RunAllTests();
                Console.WriteLine();

                // Run UI builder tests
                Console.WriteLine("\n[UI BUILDER TESTS]");
                Console.WriteLine("==================\n");
                
                UIMessageBuilderTests.RunAllTests();
                Console.WriteLine();

                Console.WriteLine("\n========================================");
                Console.WriteLine("All Manager and Calculator Tests Complete");
                Console.WriteLine("========================================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERROR: Test suite failed with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Runs only manager tests
        /// </summary>
        public static void RunManagerTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Manager Test Suite");
            Console.WriteLine("========================================\n");

            ClassActionManagerTests.RunAllTests();
            Console.WriteLine();
            ComboSequenceManagerTests.RunAllTests();
            Console.WriteLine();
            GearActionManagerTests.RunAllTests();
            Console.WriteLine();
            DefaultActionManagerTests.RunAllTests();
        }

        /// <summary>
        /// Runs only calculator tests
        /// </summary>
        public static void RunCalculatorTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Calculator Test Suite");
            Console.WriteLine("========================================\n");

            CombatCalculatorTests.RunAllTests();
        }

        /// <summary>
        /// Runs only UI builder tests
        /// </summary>
        public static void RunUIBuilderTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("UI Builder Test Suite");
            Console.WriteLine("========================================\n");

            UIMessageBuilderTests.RunAllTests();
        }
    }
}

