using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests
{
    /// <summary>
    /// Comprehensive visual tests for colored text rendering
    /// These tests display text on screen so you can visually evaluate correctness
    /// </summary>
    public static class ColoredTextVisualTests
    {
        /// <summary>
        /// Runs all visual colored text tests and displays them on screen
        /// </summary>
        public static async Task RunAllVisualTests(CanvasUICoordinator uiCoordinator)
        {
            uiCoordinator.Clear();
            
            // Set up a basic character context to ensure layout renders
            // This ensures the center panel is properly initialized
            var testCharacter = new Character("Test", 1);
            uiCoordinator.SetCharacter(testCharacter);
            
            // Force a full layout render to initialize the display
            uiCoordinator.ForceFullLayoutRender();
            await Task.Delay(200); // Wait for layout to initialize
            
            // Ensure auto-scroll is enabled
            uiCoordinator.ResetScroll();
            
            // Write header to center panel
            uiCoordinator.WriteLine("=== COLORED TEXT VISUAL TESTS ===");
            uiCoordinator.WriteBlankLine();
            uiCoordinator.WriteLine("These tests display various colored text scenarios.");
            uiCoordinator.WriteLine("Please evaluate each test visually for correctness.");
            uiCoordinator.WriteBlankLine();
            
            // Force immediate render to show header
            await Task.Delay(100); // Small delay to ensure messages are added
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 1: Basic Single-Color Text
            TestBasicSingleColor(uiCoordinator, 0);
            await Task.Delay(100); // Small delay to ensure messages are added
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 2: Multi-Color Segments
            TestMultiColorSegments(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 3: Template-Based Coloring (Dungeon Names)
            TestTemplateColoring(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 4: Multi-Word Names with Spaces
            TestMultiWordNames(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 5: Whitespace Preservation
            TestWhitespacePreservation(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 6: Long Text with Wrapping
            TestLongTextWrapping(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 7: Special Characters and Punctuation
            TestSpecialCharacters(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 8: Color Transitions
            TestColorTransitions(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 9: Edge Cases
            TestEdgeCases(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // Test 10: Real-World Scenarios
            TestRealWorldScenarios(uiCoordinator, 0);
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // CRITICAL MARKER: If you see this, execution continued past TEST 10
            uiCoordinator.WriteBlankLine();
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.WriteLine("*** MARKER: CONTINUING TO TEST 11 ***");
            uiCoordinator.WriteLine("*** IF YOU SEE THIS, CODE IS RUNNING ***");
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(1000); // Longer delay to ensure visibility
            
            // Test 11: Combat Log Spacing (for/with)
            uiCoordinator.WriteBlankLine();
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.WriteLine("TEST 11: Combat Log Spacing (for/with)");
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            try
            {
                TestCombatLogSpacing(uiCoordinator, 0);
                uiCoordinator.WriteLine("*** TEST 11 COMPLETED SUCCESSFULLY ***");
            }
            catch (Exception ex)
            {
                uiCoordinator.WriteLine($"*** TEST 11 FAILED: {ex.Message} ***");
                uiCoordinator.WriteLine($"Stack: {ex.StackTrace?.Substring(0, Math.Min(300, ex.StackTrace.Length)) ?? "No stack"}");
            }
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            // CRITICAL MARKER: If you see this, execution continued past TEST 11
            uiCoordinator.WriteBlankLine();
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.WriteLine("*** MARKER: CONTINUING TO TEST 12 ***");
            uiCoordinator.WriteLine("*** IF YOU SEE THIS, CODE IS RUNNING ***");
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(1000); // Longer delay to ensure visibility
            
            // Test 12: Roll Info Spacing
            uiCoordinator.WriteBlankLine();
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.WriteLine("TEST 12: Roll Info Spacing");
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            try
            {
                TestRollInfoSpacing(uiCoordinator, 0);
                uiCoordinator.WriteLine("*** TEST 12 COMPLETED SUCCESSFULLY ***");
            }
            catch (Exception ex)
            {
                uiCoordinator.WriteLine($"*** TEST 12 FAILED: {ex.Message} ***");
                uiCoordinator.WriteLine($"Stack: {ex.StackTrace?.Substring(0, Math.Min(300, ex.StackTrace.Length)) ?? "No stack"}");
            }
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
            await Task.Delay(500);
            
            uiCoordinator.WriteBlankLine();
            uiCoordinator.WriteLine("========================================");
            uiCoordinator.WriteLine("=== ALL VISUAL TESTS COMPLETE ===");
            uiCoordinator.WriteLine("Total tests run: 12 (Tests 1-12)");
            uiCoordinator.WriteLine("Please review the displayed text above for correctness.");
            uiCoordinator.WriteLine("Check especially TEST 11 and TEST 12 for spacing issues.");
            uiCoordinator.WriteLine("========================================");
            await Task.Delay(100);
            uiCoordinator.ForceFullLayoutRender();
            uiCoordinator.Refresh();
        }
        
        private static int TestBasicSingleColor(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 1: Basic Single-Color Text");
            
            // White text
            var whiteText = new List<ColoredText> { new ColoredText("White text", Colors.White) };
            uiCoordinator.WriteLineColoredSegments(whiteText);
            
            // Red text
            var redText = new List<ColoredText> { new ColoredText("Red text", Colors.Red) };
            uiCoordinator.WriteLineColoredSegments(redText);
            
            // Green text
            var greenText = new List<ColoredText> { new ColoredText("Green text", Colors.Green) };
            uiCoordinator.WriteLineColoredSegments(greenText);
            
            // Blue text
            var blueText = new List<ColoredText> { new ColoredText("Blue text", Colors.Blue) };
            uiCoordinator.WriteLineColoredSegments(blueText);
            
            // Yellow text
            var yellowText = new List<ColoredText> { new ColoredText("Yellow text", Colors.Yellow) };
            uiCoordinator.WriteLineColoredSegments(yellowText);
            
            uiCoordinator.WriteBlankLine();
            return 0; // y is not used anymore
        }
        
        private static int TestMultiColorSegments(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 2: Multi-Color Segments");
            
            // Multiple colors in one line
            var segments1 = new List<ColoredText>
            {
                new ColoredText("Red ", Colors.Red),
                new ColoredText("Green ", Colors.Green),
                new ColoredText("Blue ", Colors.Blue),
                new ColoredText("text", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(segments1);
            
            // Alternating colors
            var segments2 = new List<ColoredText>
            {
                new ColoredText("A", Colors.Red),
                new ColoredText("B", Colors.Green),
                new ColoredText("C", Colors.Blue),
                new ColoredText("D", Colors.Yellow),
                new ColoredText("E", Colors.Cyan),
                new ColoredText("F", Colors.Magenta)
            };
            uiCoordinator.WriteLineColoredSegments(segments2);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestTemplateColoring(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 3: Template-Based Coloring (Dungeon Names)");
            
            // Test dungeon name templates
            string[] dungeonNames = {
                "Celestial Observatory",
                "Ancient Forest",
                "Lava Caves",
                "Haunted Crypt",
                "Frozen Tundra"
            };
            
            foreach (var name in dungeonNames)
            {
                // Use template system to create colored segments
                var coloredSegments = ColoredText.FromTemplate("astral", name);
                
                if (coloredSegments.Count == 0)
                {
                    // Fallback to simple coloring if template fails
                    coloredSegments = new List<ColoredText> { new ColoredText(name, Colors.White) };
                }
                
                uiCoordinator.WriteLineColoredSegments(coloredSegments);
            }
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestMultiWordNames(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 4: Multi-Word Names with Spaces");
            
            // Test names with spaces that should be preserved
            string[] multiWordNames = {
                "Captain Pae",  // Changed from "Cpt Pae" for clarity
                "Room Name",
                "Celestial Observatory",
                "Ancient Forest",
                "Test Name Here"
            };
            
            foreach (var name in multiWordNames)
            {
                // Create segments with template coloring
                var coloredSegments = ColoredText.FromTemplate("astral", name);
                
                if (coloredSegments.Count == 0)
                {
                    coloredSegments = new List<ColoredText> { new ColoredText(name, Colors.White) };
                }
                
                uiCoordinator.WriteLineColoredSegments(coloredSegments);
            }
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestWhitespacePreservation(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 5: Whitespace Preservation");
            
            // Test explicit whitespace segments
            var segments1 = new List<ColoredText>
            {
                new ColoredText("Word1", Colors.Red),
                new ColoredText(" ", Colors.White),
                new ColoredText("Word2", Colors.Green)
            };
            uiCoordinator.WriteLineColoredSegments(segments1);
            
            // Test multiple spaces
            var segments2 = new List<ColoredText>
            {
                new ColoredText("Start", Colors.White),
                new ColoredText("  ", Colors.White), // Two spaces
                new ColoredText("End", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(segments2);
            
            // Test space at beginning (should be 4 spaces for indentation)
            var segments3 = new List<ColoredText>
            {
                new ColoredText("    ", Colors.White), // 4 spaces for indentation
                new ColoredText("Indented", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(segments3);
            
            // Test space at end
            var segments4 = new List<ColoredText>
            {
                new ColoredText("Trailing", Colors.White),
                new ColoredText(" ", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(segments4);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestLongTextWrapping(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 6: Long Text with Wrapping");
            
            // Long text with colors - convert to markup for wrapping
            var longSegments = new List<ColoredText>
            {
                new ColoredText("This is a very long piece of text that should wrap properly when displayed. ", Colors.White),
                new ColoredText("It contains ", Colors.White),
                new ColoredText("colored segments", Colors.Yellow),
                new ColoredText(" that need to be preserved correctly.", Colors.White)
            };
            
            // Add as segments - the display system will handle wrapping
            uiCoordinator.WriteLineColoredSegments(longSegments);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestSpecialCharacters(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 7: Special Characters and Punctuation");
            
            // Test with punctuation
            var segments1 = new List<ColoredText>
            {
                new ColoredText("Room:", Colors.White),
                new ColoredText(" CptPae", Colors.Yellow)
            };
            uiCoordinator.WriteLineColoredSegments(segments1);
            
            // Test with numbers
            var segments2 = new List<ColoredText>
            {
                new ColoredText("Total Rooms: ", Colors.White),
                new ColoredText("3", Colors.Green)
            };
            uiCoordinator.WriteLineColoredSegments(segments2);
            
            // Test with brackets
            var segments3 = new List<ColoredText>
            {
                new ColoredText("[", Colors.Gray),
                new ColoredText("1", Colors.White),
                new ColoredText("] ", Colors.Gray),
                new ColoredText("Dungeon Name", Colors.Yellow)
            };
            uiCoordinator.WriteLineColoredSegments(segments3);
            
            // Test with special characters
            var segments4 = new List<ColoredText>
            {
                new ColoredText("═══ ", Colors.Gold),
                new ColoredText("ENTERING DUNGEON", Colors.White),
                new ColoredText(" ═══", Colors.Gold)
            };
            uiCoordinator.WriteLineColoredSegments(segments4);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestColorTransitions(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 8: Color Transitions");
            
            // Rapid color changes
            var segments1 = new List<ColoredText>
            {
                new ColoredText("R", Colors.Red),
                new ColoredText("G", Colors.Green),
                new ColoredText("B", Colors.Blue),
                new ColoredText(" ", Colors.White),
                new ColoredText("R", Colors.Red),
                new ColoredText("G", Colors.Green),
                new ColoredText("B", Colors.Blue)
            };
            uiCoordinator.WriteLineColoredSegments(segments1);
            
            // Same color adjacent segments (should merge)
            var segments2 = new List<ColoredText>
            {
                new ColoredText("Same", Colors.Red),
                new ColoredText("Color", Colors.Red),
                new ColoredText("Segments", Colors.Red)
            };
            uiCoordinator.WriteLineColoredSegments(segments2);
            
            // Color with whitespace
            var segments3 = new List<ColoredText>
            {
                new ColoredText("Red", Colors.Red),
                new ColoredText(" ", Colors.White),
                new ColoredText("White", Colors.White),
                new ColoredText(" ", Colors.White),
                new ColoredText("Red", Colors.Red)
            };
            uiCoordinator.WriteLineColoredSegments(segments3);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestEdgeCases(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 9: Edge Cases");
            
            // Empty segments (should be skipped)
            var segments1 = new List<ColoredText>
            {
                new ColoredText("Start", Colors.White),
                new ColoredText("", Colors.White),
                new ColoredText("End", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(segments1);
            
            // Single character segments
            var segments2 = new List<ColoredText>
            {
                new ColoredText("A", Colors.Red),
                new ColoredText("B", Colors.Green),
                new ColoredText("C", Colors.Blue)
            };
            uiCoordinator.WriteLineColoredSegments(segments2);
            
            // Very long single segment
            var longSegment = new List<ColoredText> { new ColoredText("This is a very long single segment that should display correctly without corruption.", Colors.White) };
            uiCoordinator.WriteLineColoredSegments(longSegment);
            
            // Mixed case
            var segments3 = new List<ColoredText>
            {
                new ColoredText("UPPERCASE", Colors.White),
                new ColoredText(" ", Colors.White),
                new ColoredText("lowercase", Colors.White),
                new ColoredText(" ", Colors.White),
                new ColoredText("MixedCase", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(segments3);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestRealWorldScenarios(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 10: Real-World Scenarios");
            
            // Dungeon selection menu item
            var dungeonItem = new List<ColoredText>
            {
                new ColoredText("[", Colors.Gray),
                new ColoredText("1", Colors.White),
                new ColoredText("] ", Colors.Gray)
            };
            var dungeonName = ColoredText.FromTemplate("astral", "Celestial Observatory");
            if (dungeonName.Count == 0)
            {
                dungeonName = new List<ColoredText> { new ColoredText("Celestial Observatory", Colors.White) };
            }
            dungeonItem.AddRange(dungeonName);
            dungeonItem.Add(new ColoredText(" (lvl 5)", Colors.White));
            uiCoordinator.WriteLineColoredSegments(dungeonItem);
            
            // Room header (matching actual format: entire header is yellow)
            var roomHeaderText = "═══ ENTERING ROOM ═══";
            var roomHeader = new List<ColoredText>
            {
                new ColoredText(roomHeaderText, Colors.Yellow)
            };
            uiCoordinator.WriteLineColoredSegments(roomHeader);
            
            // Room name with colon
            var roomName = new List<ColoredText>
            {
                new ColoredText("Room:", Colors.White),
                new ColoredText(" ", Colors.White),
                new ColoredText("Cpt Pae", Colors.Yellow)
            };
            uiCoordinator.WriteLineColoredSegments(roomName);
            
            // Combat message (matching actual combat format)
            // Format: [Attacker] hits [Target] for [damage] damage
            // NOTE: This should use the actual DamageFormatter, but for visual test we'll simulate it
            var combatMsg = new List<ColoredText>
            {
                new ColoredText("You", ColorPalette.Gold.GetColor()),
                new ColoredText(" hits ", Colors.White),
                new ColoredText("the enemy", ColorPalette.Enemy.GetColor()),
                new ColoredText("for ", Colors.White), // Space included in "for "
                new ColoredText("15", ColorPalette.Damage.GetColor()),
                new ColoredText(" damage", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(combatMsg);
            
            // Item display
            var itemDisplay = new List<ColoredText>
            {
                new ColoredText("Item: ", Colors.White),
                new ColoredText("Sword of Fire", Colors.Orange),
                new ColoredText(" (Rare)", Colors.Purple)
            };
            uiCoordinator.WriteLineColoredSegments(itemDisplay);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestCombatLogSpacing(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 11: Combat Log Spacing (for/with)");
            
            // Test basic "for" spacing
            uiCoordinator.WriteLine("Basic 'for' spacing:");
            var forTest1 = new List<ColoredText>
            {
                new ColoredText("Attacker", ColorPalette.Gold.GetColor()),
                new ColoredText(" hits ", Colors.White),
                new ColoredText("Target", ColorPalette.Enemy.GetColor()),
                new ColoredText("for ", Colors.White), // Space included
                new ColoredText("42", ColorPalette.Damage.GetColor()),
                new ColoredText(" damage", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(forTest1);
            
            // Test "for" with different numbers
            var forTest2 = new List<ColoredText>
            {
                new ColoredText("Player", ColorPalette.Gold.GetColor()),
                new ColoredText(" hits ", Colors.White),
                new ColoredText("Enemy", ColorPalette.Enemy.GetColor()),
                new ColoredText("for ", Colors.White),
                new ColoredText("7", ColorPalette.Damage.GetColor()),
                new ColoredText(" damage", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(forTest2);
            
            var forTest3 = new List<ColoredText>
            {
                new ColoredText("Player", ColorPalette.Gold.GetColor()),
                new ColoredText(" hits ", Colors.White),
                new ColoredText("Enemy", ColorPalette.Enemy.GetColor()),
                new ColoredText("for ", Colors.White),
                new ColoredText("10", ColorPalette.Damage.GetColor()),
                new ColoredText(" damage", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(forTest3);
            
            // Test "with" spacing
            uiCoordinator.WriteLine("'with' spacing:");
            var withTest1 = new List<ColoredText>
            {
                new ColoredText("Shadowbane", ColorPalette.Gold.GetColor()),
                new ColoredText(" hits ", Colors.White),
                new ColoredText("Enemy", ColorPalette.Enemy.GetColor()),
                new ColoredText("with ", Colors.White), // Space included
                new ColoredText("CRITICAL THUNDER CLAP", ColorPalette.Critical.GetColor()),
                new ColoredText("for ", Colors.White),
                new ColoredText("7", ColorPalette.Damage.GetColor()),
                new ColoredText(" damage", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(withTest1);
            
            var withTest2 = new List<ColoredText>
            {
                new ColoredText("Player", ColorPalette.Gold.GetColor()),
                new ColoredText(" hits ", Colors.White),
                new ColoredText("Enemy", ColorPalette.Enemy.GetColor()),
                new ColoredText("with ", Colors.White),
                new ColoredText("CRUSHING BLOW", ColorPalette.Warning.GetColor()),
                new ColoredText("for ", Colors.White),
                new ColoredText("6", ColorPalette.Damage.GetColor()),
                new ColoredText(" damage", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(withTest2);
            
            var withTest3 = new List<ColoredText>
            {
                new ColoredText("Player", ColorPalette.Gold.GetColor()),
                new ColoredText(" hits ", Colors.White),
                new ColoredText("Enemy", ColorPalette.Enemy.GetColor()),
                new ColoredText("with ", Colors.White),
                new ColoredText("SHIELD BREAK", ColorPalette.Warning.GetColor()),
                new ColoredText("for ", Colors.White),
                new ColoredText("6", ColorPalette.Damage.GetColor()),
                new ColoredText(" damage", Colors.White)
            };
            uiCoordinator.WriteLineColoredSegments(withTest3);
            
            // Test using ColoredTextBuilder (like actual code)
            uiCoordinator.WriteLine("Using ColoredTextBuilder (actual code pattern):");
            var builder = new ColoredTextBuilder();
            builder.Add("TestPlayer", ColorPalette.Gold);
            builder.AddSpace();
            builder.Add("hits", Colors.White);
            builder.AddSpace();
            builder.Add("TestEnemy", ColorPalette.Enemy);
            builder.Add("for ", Colors.White);
            builder.Add("42", ColorPalette.Damage);
            builder.Add("damage", Colors.White);
            var builderResult = builder.Build();
            uiCoordinator.WriteLineColoredSegments(builderResult);
            
            var builder2 = new ColoredTextBuilder();
            builder2.Add("TestPlayer", ColorPalette.Gold);
            builder2.AddSpace();
            builder2.Add("hits", Colors.White);
            builder2.AddSpace();
            builder2.Add("TestEnemy", ColorPalette.Enemy);
            builder2.Add("with ", Colors.White);
            builder2.Add("PARRY", ColorPalette.Warning);
            builder2.Add("for ", Colors.White);
            builder2.Add("15", ColorPalette.Damage);
            builder2.Add("damage", Colors.White);
            var builderResult2 = builder2.Build();
            uiCoordinator.WriteLineColoredSegments(builderResult2);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
        
        private static int TestRollInfoSpacing(CanvasUICoordinator uiCoordinator, int y)
        {
            uiCoordinator.WriteLine("TEST 12: Roll Info Spacing");
            
            // Test roll info with armor and speed
            var rollInfo1 = new List<ColoredText>
            {
                new ColoredText("     (", Colors.Gray),
                new ColoredText("roll:", ColorPalette.Info.GetColor()),
                new ColoredText(" ", Colors.White),
                new ColoredText("9", Colors.White),
                new ColoredText(" | ", Colors.Gray),
                new ColoredText("attack", ColorPalette.Info.GetColor()),
                new ColoredText(" ", Colors.White),
                new ColoredText("4", Colors.White),
                new ColoredText(" - ", Colors.White),
                new ColoredText("2", Colors.White),
                new ColoredText(" armor", Colors.White),
                new ColoredText(" ", Colors.White), // Space after armor
                new ColoredText("| ", Colors.Gray),
                new ColoredText("speed:", ColorPalette.Info.GetColor()),
                new ColoredText(" ", Colors.White),
                new ColoredText("8.5s", Colors.White),
                new ColoredText(")", Colors.Gray)
            };
            uiCoordinator.WriteLineColoredSegments(rollInfo1);
            
            // Test roll info with 0 armor
            var rollInfo2 = new List<ColoredText>
            {
                new ColoredText("     (", Colors.Gray),
                new ColoredText("roll:", ColorPalette.Info.GetColor()),
                new ColoredText(" ", Colors.White),
                new ColoredText("11", Colors.White),
                new ColoredText(" | ", Colors.Gray),
                new ColoredText("attack", ColorPalette.Info.GetColor()),
                new ColoredText(" ", Colors.White),
                new ColoredText("6", Colors.White),
                new ColoredText(" - ", Colors.White),
                new ColoredText("0", Colors.White),
                new ColoredText(" armor", Colors.White),
                new ColoredText(" ", Colors.White), // Space after armor
                new ColoredText("| ", Colors.Gray),
                new ColoredText("speed:", ColorPalette.Info.GetColor()),
                new ColoredText(" ", Colors.White),
                new ColoredText("8.4s", Colors.White),
                new ColoredText(" | ", Colors.Gray),
                new ColoredText("amp:", ColorPalette.Info.GetColor()),
                new ColoredText(" ", Colors.White),
                new ColoredText("1.0x", Colors.White),
                new ColoredText(")", Colors.Gray)
            };
            uiCoordinator.WriteLineColoredSegments(rollInfo2);
            
            // Test using ColoredTextBuilder (like actual RollInfoFormatter)
            uiCoordinator.WriteLine("Using ColoredTextBuilder (actual RollInfoFormatter pattern):");
            var builder = new ColoredTextBuilder();
            builder.Add("     (", Colors.Gray);
            builder.Add("roll:", ColorPalette.Info);
            builder.AddSpace();
            builder.Add("9", Colors.White);
            builder.Add(" | ", Colors.Gray);
            builder.Add("attack", ColorPalette.Info);
            builder.AddSpace();
            builder.Add("4", Colors.White);
            builder.Add(" - ", Colors.White);
            builder.Add("2", Colors.White);
            builder.Add(" armor", Colors.White);
            builder.AddSpace(); // Space after armor
            builder.Add("| ", Colors.Gray);
            builder.Add("speed:", ColorPalette.Info);
            builder.AddSpace();
            builder.Add("8.5s", Colors.White);
            builder.Add(")", Colors.Gray);
            var builderResult = builder.Build();
            uiCoordinator.WriteLineColoredSegments(builderResult);
            
            uiCoordinator.WriteBlankLine();
            return 0;
        }
    }
}

