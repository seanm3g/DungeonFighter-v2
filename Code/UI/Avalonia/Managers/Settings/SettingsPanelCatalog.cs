using Avalonia.Controls;
using RPGGame.UI.Avalonia.Settings;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Central mapping from settings category tag to panel type. Used by SettingsPanel to create
    /// panels and to determine which content area (ScrollViewer, Testing, Actions) to use.
    /// </summary>
    public static class SettingsPanelCatalog
    {
        /// <summary>Categories that use the main ContentScrollViewer (created by the catalog).</summary>
        public static readonly IReadOnlySet<string> MainContentCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Gameplay",
            "Classes",
            "GameVariables",
            "StatusEffects",
            "TextDelays",
            "Appearance",
            "ItemModifiers",
            "Items",
            "BalanceTuning",
            "About"
        };

        /// <summary>Creates the panel for the given category tag, or null if tag is Testing/Actions (different content area) or unknown.</summary>
        public static UserControl? CreatePanel(string categoryTag)
        {
            if (string.IsNullOrEmpty(categoryTag)) return null;
            return PanelFactories.TryGetValue(categoryTag, out var factory) ? factory() : null;
        }

        /// <summary>Whether this category uses the Testing content area (no ScrollViewer).</summary>
        public static bool UsesTestingContentArea(string categoryTag) =>
            string.Equals(categoryTag, "Testing", StringComparison.OrdinalIgnoreCase);

        /// <summary>Whether this category uses the Actions content area.</summary>
        public static bool UsesActionsContentArea(string categoryTag) =>
            string.Equals(categoryTag, "Actions", StringComparison.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Func<UserControl>> PanelFactories =
            new Dictionary<string, Func<UserControl>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Gameplay"] = () => new GameplaySettingsPanel(),
                ["Classes"] = () => new ClassesSettingsPanel(),
                ["GameVariables"] = () => new GameVariablesSettingsPanel(),
                ["StatusEffects"] = () => new StatusEffectsSettingsPanel(),
                ["TextDelays"] = () => new TextDelaysSettingsPanel(),
                ["Appearance"] = () => new AppearanceSettingsPanel(),
                ["ItemModifiers"] = () => new ItemModifiersSettingsPanel(),
                ["Items"] = () => new ItemsSettingsPanel(),
                ["BalanceTuning"] = () => new BalanceTuningSettingsPanel(),
                ["About"] = () => new AboutSettingsPanel()
            };
    }
}
