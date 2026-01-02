using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Items
{
    /// <summary>
    /// Comprehensive tests for AttributeRequirement
    /// Tests attribute requirement creation and validation
    /// </summary>
    public static class AttributeRequirementTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all AttributeRequirement tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== AttributeRequirement Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestAttributeRequirementCreation();
            TestAttributeRequirementsCollection();
            TestAddRequirement();
            TestHasRequirements();

            TestBase.PrintSummary("AttributeRequirement Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Requirement Tests

        private static void TestAttributeRequirementCreation()
        {
            Console.WriteLine("--- Testing AttributeRequirement Creation ---");

            var requirement = new AttributeRequirement
            {
                AttributeName = "Strength",
                RequiredValue = 10
            };

            TestBase.AssertEqual("Strength", requirement.AttributeName,
                "Attribute name should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(10, requirement.RequiredValue,
                "Required value should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAttributeRequirementsCollection()
        {
            Console.WriteLine("\n--- Testing AttributeRequirements Collection ---");

            var requirements = new AttributeRequirements();
            TestBase.AssertTrue(requirements.Count == 0,
                "New requirements collection should be empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var dict = new Dictionary<string, int>
            {
                { "Strength", 10 },
                { "Agility", 5 }
            };
            var requirements2 = new AttributeRequirements(dict);
            TestBase.AssertEqual(2, requirements2.Count,
                "Requirements collection should be created from dictionary",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAddRequirement()
        {
            Console.WriteLine("\n--- Testing AddRequirement ---");

            var requirements = new AttributeRequirements();
            requirements.AddRequirement("Strength", 15);

            TestBase.AssertTrue(requirements.ContainsKey("Strength"),
                "Requirement should be added",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(15, requirements["Strength"],
                "Requirement value should be correct",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHasRequirements()
        {
            Console.WriteLine("\n--- Testing HasRequirements ---");

            var requirements1 = new AttributeRequirements();
            TestBase.AssertFalse(requirements1.HasRequirements,
                "Empty requirements should return false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            requirements1.AddRequirement("Strength", 10);
            TestBase.AssertTrue(requirements1.HasRequirements,
                "Requirements with items should return true",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
