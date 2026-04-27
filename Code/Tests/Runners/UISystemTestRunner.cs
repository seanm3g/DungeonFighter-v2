using System;
using RPGGame;
using RPGGame.Tests.Unit.Handlers.Inventory;
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
            CombatActionInfoStateTests.RunAllTests();
            Console.WriteLine();
            ActionStripHoverStateTests.RunAllTests();
            Console.WriteLine();
            RightPanelActionHoverStateTests.RunAllTests();
            Console.WriteLine();
            LeftPanelHoverStateTests.RunAllTests();
            Console.WriteLine();
            LeftPanelTooltipBuilderTests.RunAllTests();
            Console.WriteLine();
            HeroNamePanelColoredTextTests.RunAllTests();
            Console.WriteLine();
            CanvasInteractionManagerHitTestTests.RunAllTests();
            Console.WriteLine();
            CombatActionStripBuilderTests.RunAllTests();
            Console.WriteLine();
            ActionInfoStripLayoutTests.RunAllTests();
            Console.WriteLine();
            EffectiveVisibleWidthRegressionTests.RunAllTests();
            Console.WriteLine();
            DisplayRendererClearBandRegressionTests.RunAllTests();
            Console.WriteLine();
            LayoutConstantsCenterPanelHitTests.RunAllTests();
            Console.WriteLine();
            RightPanelContentTextTests.RunAllTests();
            Console.WriteLine();
            LayoutCharacterResolutionTests.RunAllTests();
            Console.WriteLine();
            ThresholdDisplayFormattingTests.RunAllTests();
            Console.WriteLine();
            InventoryRightPanelLayoutTests.RunAllTests();
            Console.WriteLine();
            InventoryActionPoolEntriesTests.RunAllTests();
            Console.WriteLine();
            StatusEffectDisplayLinesTests.RunAllTests();
            Console.WriteLine();
            HoverTooltipDrawingTests.RunAllTests();
            Console.WriteLine();
            CanvasPrimitiveStackingTests.RunAllTests();
            Console.WriteLine();
            StatsPanelStateManagerTests.RunAllTests();
            Console.WriteLine();
            ComboReordererMoveTests.RunAllTests();
            Console.WriteLine();
            ComboPointerInputTests.RunAllTests();
            Console.WriteLine();
            InventoryMenuStripRightClickTests.RunAllTests();
            Console.WriteLine();
            ActionStripReorderPolicyTests.RunAllTests();
            Console.WriteLine();
            ItemDisplayFormatterTests.RunAllTests();
            Console.WriteLine();
            ItemStatFormatterTests.RunAllTests();
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
            SettingsPanelCatalogTests.RunAllTests();
            Console.WriteLine();
            SettingsApplyServiceTests.RunAllTests();
            Console.WriteLine();
            SettingsSaveRevampTests.RunAllTests();
            Console.WriteLine();
            ActionsTabManagerTests.RunAllTests();
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
