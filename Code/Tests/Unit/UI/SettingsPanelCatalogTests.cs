using System;
using System.Linq;
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

            AllDescriptors_Have_Unique_Tags_And_Valid_Factories();
            SidebarGroups_Assign_Every_Panel();
            TextAndAnimation_Is_Main_Content_And_Creates_Panel();
            ItemAffixes_Is_Main_Content_And_Creates_Panel();
            AudioPanel_Is_Main_Content_And_Tag_Resolves();
            BalanceTuning_DisplayName_Is_SpreadsheetImport();
            HandlerSaveTags_Match_Descriptor_Flags();

            TestBase.PrintSummary("SettingsPanelCatalog Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void AllDescriptors_Have_Unique_Tags_And_Valid_Factories()
        {
            Console.WriteLine("--- Descriptor registry integrity ---");

            var tags = SettingsPanelCatalog.AllPanels.Select(p => p.Tag).ToList();
            TestBase.AssertEqual(tags.Count, tags.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                "Every panel descriptor should have a unique Tag",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var descriptor in SettingsPanelCatalog.AllPanels)
            {
                var panel = descriptor.Factory();
                TestBase.AssertTrue(panel != null,
                    $"Factory for {descriptor.Tag} should create a panel",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(descriptor.Tag, SettingsPanelCatalog.GetTagForPanel(panel),
                    $"Panel type for {descriptor.Tag} should reverse-map to its tag",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void SidebarGroups_Assign_Every_Panel()
        {
            Console.WriteLine("--- Sidebar groups ---");

            foreach (var descriptor in SettingsPanelCatalog.AllPanels)
            {
                TestBase.AssertTrue(!string.IsNullOrEmpty(descriptor.SidebarGroup),
                    $"Panel {descriptor.Tag} should have a SidebarGroup",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            var playerPanels = SettingsPanelCatalog.AllPanels
                .Where(p => p.SidebarGroup == SettingsSidebarGroups.Player)
                .Select(p => p.Tag)
                .ToList();
            TestBase.AssertTrue(playerPanels.Contains("Gameplay"),
                "Player group should include Gameplay",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(playerPanels.Contains("TextAndAnimation"),
                "Player group should include TextAndAnimation",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(
                !SettingsPanelCatalog.AllPanels.Any(p => p.Tag == "ActionInteractionLab"),
                "ActionInteractionLab should not be a top-level sidebar entry",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TextAndAnimation_Is_Main_Content_And_Creates_Panel()
        {
            Console.WriteLine("--- TextAndAnimation category ---");

            var descriptor = SettingsPanelCatalog.GetDescriptor("TextAndAnimation");
            TestBase.AssertTrue(descriptor != null, "TextAndAnimation descriptor should exist",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(descriptor != null && descriptor.ContentArea == SettingsContentArea.MainScroll,
                "TextAndAnimation should use main content area",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var panel = SettingsPanelCatalog.CreatePanel("TextAndAnimation");
            TestBase.AssertTrue(panel is TextAndAnimationSettingsPanel,
                "CreatePanel(TextAndAnimation) should return TextAndAnimationSettingsPanel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("TextAndAnimation", SettingsPanel.GetCategoryTagForPanel(panel),
                "TextAndAnimationSettingsPanel should resolve to TextAndAnimation tag",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ItemAffixes_Is_Main_Content_And_Creates_Panel()
        {
            Console.WriteLine("--- ItemAffixes category ---");

            TestBase.AssertTrue(SettingsPanelCatalog.GetContentArea("ItemAffixes") == SettingsContentArea.MainScroll,
                "ItemAffixes should use main content area",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var panel = SettingsPanelCatalog.CreatePanel("ItemAffixes") as ItemAffixesSettingsPanel;
            TestBase.AssertTrue(panel != null,
                "CreatePanel(ItemAffixes) should return ItemAffixesSettingsPanel",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(panel!.PrefixesPanel is ItemModifiersSettingsPanel,
                "ItemAffixes should embed ItemModifiersSettingsPanel for prefixes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(panel.SuffixesPanel is ItemSuffixesSettingsPanel,
                "ItemAffixes should embed ItemSuffixesSettingsPanel for suffixes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void AudioPanel_Is_Main_Content_And_Tag_Resolves()
        {
            Console.WriteLine("--- Audio category ---");

            TestBase.AssertTrue(SettingsPanelCatalog.GetContentArea("Audio") == SettingsContentArea.MainScroll,
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

        private static void BalanceTuning_DisplayName_Is_SpreadsheetImport()
        {
            Console.WriteLine("--- BalanceTuning display name ---");

            var descriptor = SettingsPanelCatalog.GetDescriptor("BalanceTuning");
            TestBase.AssertTrue(descriptor != null, "BalanceTuning descriptor should exist",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Spreadsheet Import", descriptor!.DisplayName,
                "BalanceTuning sidebar label should be Spreadsheet Import",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void HandlerSaveTags_Match_Descriptor_Flags()
        {
            Console.WriteLine("--- Handler save tags ---");

            foreach (var tag in SettingsPanelCatalog.HandlerSaveCategoryTags)
            {
                var descriptor = SettingsPanelCatalog.GetDescriptor(tag);
                TestBase.AssertTrue(descriptor?.SavesViaHandler == true,
                    $"Handler save list tag {tag} should have SavesViaHandler on its descriptor",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.AssertTrue(
                SettingsPanelCatalog.HandlerSaveCategoryTags.Contains("ItemGeneration") &&
                SettingsPanelCatalog.HandlerSaveCategoryTags.Contains("Classes"),
                "Balance handler save list should include ItemGeneration and Classes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(
                SettingsPanelCatalog.HandlerSaveCategoryTags.Contains("TextAndAnimation") &&
                !SettingsPanelCatalog.HandlerSaveCategoryTags.Contains("TextDelays"),
                "Handler save list should use TextAndAnimation instead of TextDelays",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var itemGenIndex = SettingsPanelCatalog.HandlerSaveCategoryTags
                .ToList().FindIndex(t => t.Equals("ItemGeneration", StringComparison.OrdinalIgnoreCase));
            var classesIndex = SettingsPanelCatalog.HandlerSaveCategoryTags
                .ToList().FindIndex(t => t.Equals("Classes", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(itemGenIndex < classesIndex,
                "ItemGeneration should save before Classes in handler save order",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
