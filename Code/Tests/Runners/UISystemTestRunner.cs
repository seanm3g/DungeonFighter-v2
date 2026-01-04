using System;
using RPGGame;
using RPGGame.Tests.Unit.UI;
using RPGGame.Tests.Unit.UI.Spacing;
using RPGGame.Tests.Unit.UI.Chunking;
using RPGGame.Tests.Unit.UI.BlockDisplay;
using RPGGame.Tests.Unit.UI.Services;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Test runner for UI system tests
    /// </summary>
    public static class UISystemTestRunner
    {
        /// <summary>
        /// Runs all UI system tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  UI SYSTEM TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            // Core UI Tests
            UIManagerTests.RunAllTests();
            Console.WriteLine();
            BlockDisplayManagerTests.RunAllTests();
            Console.WriteLine();
            ItemDisplayFormatterTests.RunAllTests();
            Console.WriteLine();
            
            // Spacing System Tests
            TextSpacingSystemTests.RunAllTests();
            Console.WriteLine();
            SpacingFormatterTests.RunAllTests();
            Console.WriteLine();
            SpacingValidatorTests.RunAllTests();
            Console.WriteLine();
            
            // Chunking System Tests
            ChunkStrategyFactoryTests.RunAllTests();
            Console.WriteLine();
            
            // Block Display Tests
            BlockDelayManagerTests.RunAllTests();
            Console.WriteLine();
            BlockMessageCollectorTests.RunAllTests();
            Console.WriteLine();
            EntityNameExtractorTests.RunAllTests();
            Console.WriteLine();
            
            // UI Services Tests
            MessageRouterTests.RunAllTests();
            Console.WriteLine();
            MessageFilterServiceTests.RunAllTests();
            Console.WriteLine();
            
            // Canvas UI Tests
            CanvasUICoordinatorTests.RunAllTests();
            Console.WriteLine();
            CanvasRendererTests.RunAllTests();
            Console.WriteLine();
            SettingsManagerTests.RunAllTests();
            Console.WriteLine();
            DungeonRendererTests.RunAllTests();
            Console.WriteLine();
            ItemModifiersTabManagerTests.RunAllTests();
            Console.WriteLine();
            ItemsTabManagerTests.RunAllTests();
            Console.WriteLine();
            KeywordColorSystemTests.RunAllTests();
            Console.WriteLine();
            DisplayRendererTests.RunAllTests();
            Console.WriteLine();
            GameCanvasControlTests.RunAllTests();
            Console.WriteLine();
            CanvasTextManagerTests.RunAllTests();
            Console.WriteLine();
            MenuRendererTests.RunAllTests();
            Console.WriteLine();
            CombatRendererTests.RunAllTests();
            Console.WriteLine();
            InventoryRendererTests.RunAllTests();
        }
    }
}
