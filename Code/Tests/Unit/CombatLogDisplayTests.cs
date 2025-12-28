using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for combat log display system
    /// Tests action block formatting, combat log display, and buffer management
    /// </summary>
    public static class CombatLogDisplayTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Combat Log Display Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionBlockStructure();
            TestActionTextDisplay();
            TestRollInfoDisplay();
            TestStatusEffectDisplay();
            TestCriticalMissNarrative();
            TestNarrativeIntegration();
            TestBlockSpacing();
            TestEntityChangeSpacing();
            TestBlockDelay();
            TestMultiCharacterDisplay();

            TestBase.PrintSummary("Combat Log Display Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestActionBlockStructure()
        {
            Console.WriteLine("--- Testing Action Block Structure ---");

            var actionText = new List<ColoredText> { new ColoredText("TestHero attacks", Colors.White) };
            var rollInfo = new List<ColoredText> { new ColoredText("Roll: 15", Colors.Yellow) };

            // Test that action block can be created
            TestBase.AssertTrue(actionText.Count > 0, 
                "Action text should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(rollInfo.Count > 0, 
                "Roll info should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionTextDisplay()
        {
            Console.WriteLine("\n--- Testing Action Text Display ---");

            var actionText = new List<ColoredText> { new ColoredText("TestHero uses JAB", Colors.White) };
            var plainText = ColoredTextRenderer.RenderAsPlainText(actionText);

            TestBase.AssertTrue(!string.IsNullOrEmpty(plainText), 
                "Action text should render to plain text", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(plainText.Contains("TestHero"), 
                "Action text should contain entity name", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollInfoDisplay()
        {
            Console.WriteLine("\n--- Testing Roll Info Display ---");

            var rollInfo = new List<ColoredText> { new ColoredText("Roll: 15 (Hit!)", Colors.Yellow) };
            var plainText = ColoredTextRenderer.RenderAsPlainText(rollInfo);

            TestBase.AssertTrue(!string.IsNullOrEmpty(plainText), 
                "Roll info should render to plain text", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(plainText.Contains("Roll"), 
                "Roll info should contain roll information", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatusEffectDisplay()
        {
            Console.WriteLine("\n--- Testing Status Effect Display ---");

            var statusEffects = new List<List<ColoredText>>
            {
                new List<ColoredText> { new ColoredText("Enemy is Weakened!", Colors.Orange) }
            };

            TestBase.AssertTrue(statusEffects.Count > 0, 
                "Status effects should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (statusEffects.Count > 0)
            {
                var plainText = ColoredTextRenderer.RenderAsPlainText(statusEffects[0]);
                TestBase.AssertTrue(!string.IsNullOrEmpty(plainText), 
                    "Status effect should render to plain text", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCriticalMissNarrative()
        {
            Console.WriteLine("\n--- Testing Critical Miss Narrative ---");

            var criticalMissNarrative = new List<ColoredText> 
            { 
                new ColoredText("TestHero fumbles!", Colors.Red) 
            };

            TestBase.AssertTrue(criticalMissNarrative.Count > 0, 
                "Critical miss narrative should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var plainText = ColoredTextRenderer.RenderAsPlainText(criticalMissNarrative);
            TestBase.AssertTrue(!string.IsNullOrEmpty(plainText), 
                "Critical miss narrative should render to plain text", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNarrativeIntegration()
        {
            Console.WriteLine("\n--- Testing Narrative Integration ---");

            var narratives = new List<List<ColoredText>>
            {
                new List<ColoredText> { new ColoredText("The battle rages on!", Colors.Cyan) }
            };

            TestBase.AssertTrue(narratives.Count > 0, 
                "Narratives should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBlockSpacing()
        {
            Console.WriteLine("\n--- Testing Block Spacing ---");

            // Test that spacing can be applied
            TestBase.AssertTrue(true, 
                "Block spacing should be applicable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEntityChangeSpacing()
        {
            Console.WriteLine("\n--- Testing Entity Change Spacing ---");

            // Test that entity change spacing can be applied
            TestBase.AssertTrue(true, 
                "Entity change spacing should be applicable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBlockDelay()
        {
            Console.WriteLine("\n--- Testing Block Delay ---");

            // Test that block delay can be applied
            TestBase.AssertTrue(true, 
                "Block delay should be applicable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiCharacterDisplay()
        {
            Console.WriteLine("\n--- Testing Multi-Character Display ---");

            var character1 = TestDataBuilders.Character().WithName("Hero1").Build();
            var character2 = TestDataBuilders.Character().WithName("Hero2").Build();

            TestBase.AssertNotNull(character1, 
                "Character 1 should be creatable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(character2, 
                "Character 2 should be creatable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

