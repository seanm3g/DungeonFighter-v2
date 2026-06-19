using Avalonia.Controls;
using RPGGame.UI.Avalonia.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Central registry for settings panels: sidebar labels, factories, content routing, and save metadata.
    /// </summary>
    public static class SettingsPanelCatalog
    {
        public static readonly IReadOnlyList<SettingsPanelDescriptor> AllPanels = new SettingsPanelDescriptor[]
        {
            // Player Settings
            new("Gameplay", "Gameplay", () => new GameplaySettingsPanel(), SettingsContentArea.MainScroll, typeof(GameplaySettingsPanel), UsesHandler: true, SidebarGroup: SettingsSidebarGroups.Player, Order: 1),
            new("Travel", "Travel", () => new TravelSettingsPanel(), SettingsContentArea.MainScroll, typeof(TravelSettingsPanel), UsesHandler: true, SavesViaHandler: true, SidebarGroup: SettingsSidebarGroups.Player, Order: 2),
            new("Audio", "Audio", () => new AudioSettingsPanel(), SettingsContentArea.MainScroll, typeof(AudioSettingsPanel), UsesHandler: true, SavesViaHandler: true, SidebarGroup: SettingsSidebarGroups.Player, Order: 3),
            new("TextAndAnimation", "Text & Animation", () => new TextAndAnimationSettingsPanel(), SettingsContentArea.MainScroll, typeof(TextAndAnimationSettingsPanel), UsesHandler: true, SavesViaHandler: true, SidebarGroup: SettingsSidebarGroups.Player, Order: 4),
            new("Appearance", "Appearance", () => new AppearanceSettingsPanel(), SettingsContentArea.MainScroll, typeof(AppearanceSettingsPanel), UsesHandler: true, SavesViaHandler: true, SidebarGroup: SettingsSidebarGroups.Player, Order: 5),

            // Developer Settings
            new("GameVariables", "Game Variables", () => new GameVariablesSettingsPanel(), SettingsContentArea.MainScroll, typeof(GameVariablesSettingsPanel), UsesTabManager: true, SidebarGroup: SettingsSidebarGroups.Developer, Order: 1),
            new("Actions", "Actions", () => new ActionsSettingsPanel(), SettingsContentArea.Actions, typeof(ActionsSettingsPanel), UsesTabManager: true, SidebarGroup: SettingsSidebarGroups.Developer, Order: 2),
            new("StatusEffects", "Status Effects", () => new StatusEffectsSettingsPanel(), SettingsContentArea.MainScroll, typeof(StatusEffectsSettingsPanel), UsesTabManager: true, SidebarGroup: SettingsSidebarGroups.Developer, Order: 3),
            new("Enemies", "Enemies", () => new EnemiesSettingsPanel(), SettingsContentArea.MainScroll, typeof(EnemiesSettingsPanel), UsesTabManager: true, SidebarGroup: SettingsSidebarGroups.Developer, Order: 4),
            new("Items", "Items", () => new ItemsSettingsPanel(), SettingsContentArea.MainScroll, typeof(ItemsSettingsPanel), UsesTabManager: true, SidebarGroup: SettingsSidebarGroups.Developer, Order: 5),
            new("ItemAffixes", "Item Affixes", () => new ItemAffixesSettingsPanel(), SettingsContentArea.MainScroll, typeof(ItemAffixesSettingsPanel), UsesTabManager: true, SidebarGroup: SettingsSidebarGroups.Developer, Order: 6),
            new("Patches", "Patches", () => new PatchesSettingsPanel(), SettingsContentArea.MainScroll, typeof(PatchesSettingsPanel), UsesHandler: true, SidebarGroup: SettingsSidebarGroups.Developer, Order: 7),
            new("BalanceTuning", "Spreadsheet Import", () => new BalanceTuningSettingsPanel(), SettingsContentArea.MainScroll, typeof(BalanceTuningSettingsPanel), UsesHandler: true, SavesViaHandler: true, SidebarGroup: SettingsSidebarGroups.Developer, Order: 8),

            // Balance & Tuning
            new("CombatTuning", "Combat Tuning", () => new CombatAndEnemyTuningSettingsPanel(), SettingsContentArea.MainScroll, typeof(CombatAndEnemyTuningSettingsPanel), UsesHandler: true, SavesViaHandler: true, SidebarGroup: SettingsSidebarGroups.Balance, Order: 1),
            new("Classes", "Classes", () => new ClassesSettingsPanel(), SettingsContentArea.MainScroll, typeof(ClassesSettingsPanel), UsesHandler: true, SavesViaHandler: true, SidebarGroup: SettingsSidebarGroups.Balance, Order: 2),
            new("ItemGeneration", "Item Generation", () => new ItemGenerationSettingsPanel(), SettingsContentArea.ItemGeneration, typeof(ItemGenerationSettingsPanel), UsesHandler: true, SavesViaHandler: true, SidebarGroup: SettingsSidebarGroups.Balance, Order: 3),

            // Testing
            new("Testing", "Testing", () => new TestingSettingsPanel(), SettingsContentArea.Testing, typeof(TestingSettingsPanel), UsesHandler: true, SidebarGroup: SettingsSidebarGroups.Testing, Order: 1),

            // About (no group header)
            new("About", "About", () => new AboutSettingsPanel(), SettingsContentArea.MainScroll, typeof(AboutSettingsPanel), SidebarGroup: SettingsSidebarGroups.About, Order: 1)
        };

        /// <summary>Handler save order — ItemGeneration before Classes for balance patch consistency.</summary>
        public static readonly IReadOnlyList<string> HandlerSaveCategoryTags = new[]
        {
            "Travel", "TextAndAnimation", "Appearance",
            "BalanceTuning", "ItemGeneration", "CombatTuning", "Classes", "Audio"
        };

        private static readonly Dictionary<string, SettingsPanelDescriptor> ByTag =
            AllPanels.ToDictionary(p => p.Tag, p => p, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<Type, string> TagByPanelType =
            AllPanels.Where(p => p.PanelType != null).ToDictionary(p => p.PanelType!, p => p.Tag);

        public static SettingsPanelDescriptor? GetDescriptor(string categoryTag)
        {
            if (string.IsNullOrEmpty(categoryTag)) return null;
            return ByTag.TryGetValue(categoryTag, out var d) ? d : null;
        }

        public static UserControl? CreatePanel(string categoryTag)
        {
            var descriptor = GetDescriptor(categoryTag);
            return descriptor != null ? descriptor.Factory() : null;
        }

        public static string? GetTagForPanel(UserControl? panel)
        {
            if (panel == null) return null;
            return TagByPanelType.TryGetValue(panel.GetType(), out var tag) ? tag : null;
        }

        public static SettingsContentArea GetContentArea(string categoryTag)
        {
            var descriptor = GetDescriptor(categoryTag);
            return descriptor?.ContentArea ?? SettingsContentArea.MainScroll;
        }

        public static IEnumerable<SettingsPanelDescriptor> GetPanelsForSidebar()
        {
            return AllPanels
                .OrderBy(p => Array.FindIndex(SettingsSidebarGroups.OrderedGroups, g => g.Id == p.SidebarGroup))
                .ThenBy(p => p.Order);
        }
    }
}

