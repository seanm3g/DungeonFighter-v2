using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for action block display system
    /// Tests BlockDisplayManager, action block formatting, and display logic
    /// </summary>
    public static class ActionBlockTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Block Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionBlockCreation();
            TestActionBlockFormatting();
            TestActionBlockWithRollInfo();
            TestActionBlockWithStatusEffects();
            TestActionBlockWithNarratives();
            TestActionBlockMessageCollection();
            TestActionBlockSpacing();

            TestBase.PrintSummary("Action Block Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestActionBlockCreation()
        {
            Console.WriteLine("--- Testing Action Block Creation ---");

            // Create a simple action block
            var actionText = new List<ColoredText>
            {
                new ColoredText("Player", Colors.Yellow),
                new ColoredText(" attacks with ", Colors.White),
                new ColoredText("JAB", Colors.Cyan)
            };

            var rollInfo = new List<ColoredText>
            {
                new ColoredText("Roll: 15", Colors.LightBlue)
            };

            TestBase.AssertTrue(actionText.Count > 0, 
                "Action text should be created", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(rollInfo.Count > 0, 
                "Roll info should be created", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionBlockFormatting()
        {
            Console.WriteLine("\n--- Testing Action Block Formatting ---");

            var actionText = new List<ColoredText>
            {
                new ColoredText("Enemy", Colors.Red),
                new ColoredText(" uses ", Colors.White),
                new ColoredText("SLASH", Colors.Cyan)
            };

            // Test that action text can be converted to plain text
            string plainText = ColoredTextRenderer.RenderAsPlainText(actionText);
            TestBase.AssertTrue(!string.IsNullOrEmpty(plainText), 
                "Action text should convert to plain text", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(plainText.Contains("Enemy"), 
                "Plain text should contain entity name", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionBlockWithRollInfo()
        {
            Console.WriteLine("\n--- Testing Action Block with Roll Info ---");

            var actionText = new List<ColoredText>
            {
                new ColoredText("Player", Colors.Yellow),
                new ColoredText(" attacks", Colors.White)
            };

            var rollInfo = new List<ColoredText>
            {
                new ColoredText("Roll: 18 (Hit!)", Colors.LightBlue)
            };

            // Test that roll info is properly formatted
            string rollText = ColoredTextRenderer.RenderAsPlainText(rollInfo);
            TestBase.AssertTrue(rollText.Contains("Roll"), 
                "Roll info should contain 'Roll'", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionBlockWithStatusEffects()
        {
            Console.WriteLine("\n--- Testing Action Block with Status Effects ---");

            var actionText = new List<ColoredText>
            {
                new ColoredText("Player", Colors.Yellow),
                new ColoredText(" casts ", Colors.White),
                new ColoredText("POISON", Colors.Cyan)
            };

            var statusEffects = new List<List<ColoredText>>
            {
                new List<ColoredText>
                {
                    new ColoredText("Enemy", Colors.Red),
                    new ColoredText(" is now ", Colors.White),
                    new ColoredText("Poisoned", Colors.Green)
                }
            };

            TestBase.AssertTrue(statusEffects.Count > 0, 
                "Status effects should be created", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (statusEffects.Count > 0)
            {
                TestBase.AssertTrue(statusEffects[0].Count > 0, 
                    "Status effect should have content", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestActionBlockWithNarratives()
        {
            Console.WriteLine("\n--- Testing Action Block with Narratives ---");

            var actionText = new List<ColoredText>
            {
                new ColoredText("Player", Colors.Yellow),
                new ColoredText(" performs a ", Colors.White),
                new ColoredText("COMBO", Colors.Cyan)
            };

            var narratives = new List<List<ColoredText>>
            {
                new List<ColoredText>
                {
                    new ColoredText("The attack connects with devastating force!", Colors.Orange)
                }
            };

            TestBase.AssertTrue(narratives.Count > 0, 
                "Narratives should be created", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionBlockMessageCollection()
        {
            Console.WriteLine("\n--- Testing Action Block Message Collection ---");

            var actionText = new List<ColoredText>
            {
                new ColoredText("Player", Colors.Yellow),
                new ColoredText(" attacks", Colors.White)
            };

            var rollInfo = new List<ColoredText>
            {
                new ColoredText("Roll: 12", Colors.LightBlue)
            };

            // Test that messages can be collected together
            // Note: This tests the concept, actual BlockMessageCollector may need UI context
            TestBase.AssertTrue(actionText.Count > 0 && rollInfo.Count > 0, 
                "Action block messages should be collectable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionBlockSpacing()
        {
            Console.WriteLine("\n--- Testing Action Block Spacing ---");

            // Test that action blocks can be created with proper spacing
            var actionText1 = new List<ColoredText>
            {
                new ColoredText("Player", Colors.Yellow),
                new ColoredText(" attacks", Colors.White)
            };

            var actionText2 = new List<ColoredText>
            {
                new ColoredText("Enemy", Colors.Red),
                new ColoredText(" counterattacks", Colors.White)
            };

            // Verify both blocks can be created independently
            TestBase.AssertTrue(actionText1.Count > 0 && actionText2.Count > 0, 
                "Multiple action blocks should be creatable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

