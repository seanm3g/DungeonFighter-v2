using System;
using System.Collections.Generic;
using RPGGame.UI;

namespace RPGGame.Tests
{
    /// <summary>
    /// Base class for test runners providing shared utilities and common functionality
    /// </summary>
    public abstract class TestHarnessBase
    {
        /// <summary>
        /// Helper method to create template syntax strings without quadruple braces
        /// Uses string.Format to avoid escaping issues in string interpolation
        /// </summary>
        public static string ApplyTemplate(string templateName, string text)
        {
            return string.Format("{{{{0}|{1}}}}", templateName, text);
        }

        /// <summary>
        /// Gets the order for rarity sorting
        /// </summary>
        public static int GetRarityOrder(string rarity)
        {
            return rarity.ToLower() switch
            {
                "common" => 1,
                "uncommon" => 2,
                "rare" => 3,
                "epic" => 4,
                "legendary" => 5,
                _ => 6
            };
        }

        /// <summary>
        /// Prompts user to continue or quit
        /// </summary>
        public static bool PromptContinue(string message = "Press any key to continue or 'q' to quit...")
        {
            TextDisplayIntegration.DisplaySystem(message);
            var key = Console.ReadKey();
            if (key.KeyChar == 'q' || key.KeyChar == 'Q')
            {
                TextDisplayIntegration.DisplaySystem("Test cancelled.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Displays a test header
        /// </summary>
        public static void DisplayTestHeader(string testName)
        {
            TextDisplayIntegration.DisplaySystem("\n" + new string('=', 80));
            TextDisplayIntegration.DisplaySystem(testName);
            TextDisplayIntegration.DisplaySystem(new string('=', 80));
        }

        /// <summary>
        /// Displays a test section header
        /// </summary>
        public static void DisplaySectionHeader(string sectionName)
        {
            TextDisplayIntegration.DisplaySystem("\n" + new string('-', 50));
            TextDisplayIntegration.DisplaySystem(sectionName);
            TextDisplayIntegration.DisplaySystem(new string('-', 50));
        }

        /// <summary>
        /// Waits for user input to continue
        /// </summary>
        public static void WaitForContinue()
        {
            TextDisplayIntegration.DisplaySystem("\nPress any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Asserts a condition and throws if false
        /// </summary>
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }

        /// <summary>
        /// Asserts two values are equal
        /// </summary>
        public static void AssertEqual<T>(T expected, T actual, string message = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception($"Assertion failed: Expected {expected}, got {actual}. {message}");
            }
        }

        /// <summary>
        /// Asserts a value is not null
        /// </summary>
        public static void AssertNotNull(object? value, string message = "")
        {
            if (value == null)
            {
                throw new Exception($"Assertion failed: Value is null. {message}");
            }
        }

        /// <summary>
        /// Asserts a value is null
        /// </summary>
        public static void AssertNull(object? value, string message = "")
        {
            if (value != null)
            {
                throw new Exception($"Assertion failed: Value is not null. {message}");
            }
        }

        /// <summary>
        /// Asserts a condition is true (alias for Assert)
        /// </summary>
        public static void AssertTrue(bool condition, string message = "")
        {
            Assert(condition, message);
        }

        /// <summary>
        /// Asserts a condition is false
        /// </summary>
        public static void AssertFalse(bool condition, string message = "")
        {
            Assert(!condition, message);
        }
    }
}

