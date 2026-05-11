using System;
using RPGGame.UI.Avalonia;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>Sanity checks for settings category registration used by SettingsPanel.</summary>
    public static class SettingsPanelCatalogTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SettingsPanelCatalog Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            ActionInteractionLab_Is_Main_Content_And_Creates_Panel();
            ItemPrefixes_And_Suffixes_Are_Main_Content_And_Create_Panels();
            AudioPanel_Is_Main_Content_And_Tag_Resolves();

            TestBase.PrintSummary("SettingsPanelCatalog Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void ActionInteractionLab_Is_Main_Content_And_Creates_Panel()
        {
            Console.WriteLine("--- ActionInteractionLab category ---");

            TestBase.AssertTrue(
                SettingsPanelCatalog.MainContentCategories.Contains("ActionInteractionLab"),
                "ActionInteractionLab should use ScrollViewer main content area",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var panel = SettingsPanelCatalog.CreatePanel("ActionInteractionLab");
            TestBase.AssertTrue(
                panel is ActionInteractionLabSettingsPanel,
                "CreatePanel(ActionInteractionLab) should return ActionInteractionLabSettingsPanel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ItemPrefixes_And_Suffixes_Are_Main_Content_And_Create_Panels()
        {
            Console.WriteLine("--- ItemPrefixes / ItemSuffixes categories ---");

            TestBase.AssertTrue(
                SettingsPanelCatalog.MainContentCategories.Contains("ItemPrefixes"),
                "ItemPrefixes should use main content area",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                SettingsPanelCatalog.MainContentCategories.Contains("ItemSuffixes"),
                "ItemSuffixes should use main content area",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var pre = SettingsPanelCatalog.CreatePanel("ItemPrefixes");
            TestBase.AssertTrue(pre is ItemModifiersSettingsPanel,
                "CreatePanel(ItemPrefixes) should return ItemModifiersSettingsPanel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var suf = SettingsPanelCatalog.CreatePanel("ItemSuffixes");
            TestBase.AssertTrue(suf is ItemSuffixesSettingsPanel,
                "CreatePanel(ItemSuffixes) should return ItemSuffixesSettingsPanel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void AudioPanel_Is_Main_Content_And_Tag_Resolves()
        {
            Console.WriteLine("--- Audio category ---");

            TestBase.AssertTrue(
                SettingsPanelCatalog.MainContentCategories.Contains("Audio"),
                "Audio should use main content area",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var panel = SettingsPanelCatalog.CreatePanel("Audio");
            TestBase.AssertTrue(panel is AudioSettingsPanel,
                "CreatePanel(Audio) should return AudioSettingsPanel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Audio", SettingsPanel.GetCategoryTagForPanel(panel),
                "AudioSettingsPanel should resolve to the Audio category tag",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
