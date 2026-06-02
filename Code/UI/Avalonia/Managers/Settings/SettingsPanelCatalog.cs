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
            "Patches",
            "Travel",
            "Audio",
            "Classes",
            "GameVariables",
            "StatusEffects",
            "TextDelays",
            "Appearance",
            "ItemPrefixes",
            "ItemSuffixes",
            "Items",
            "ItemGeneration",
            "Enemies",
            "EnemyTuning",
            "BalanceTuning",
            "ActionInteractionLab",
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

        /// <summary>Whether this category uses the Text Animation content area (pinned preview).</summary>
        public static bool UsesTextAnimationContentArea(string categoryTag) =>
            string.Equals(categoryTag, "TextAnimation", StringComparison.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Func<UserControl>> PanelFactories =
            new Dictionary<string, Func<UserControl>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Gameplay"] = () => new GameplaySettingsPanel(),
                ["Patches"] = () => new PatchesSettingsPanel(),
                ["Travel"] = () => new TravelSettingsPanel(),
                ["Audio"] = () => new AudioSettingsPanel(),
                ["Classes"] = () => new ClassesSettingsPanel(),
                ["GameVariables"] = () => new GameVariablesSettingsPanel(),
                ["StatusEffects"] = () => new StatusEffectsSettingsPanel(),
                ["TextDelays"] = () => new TextDelaysSettingsPanel(),
                ["TextAnimation"] = () => new TextAnimationPresetsSettingsPanel(),
                ["Appearance"] = () => new AppearanceSettingsPanel(),
                ["ItemPrefixes"] = () => new ItemModifiersSettingsPanel(),
                ["ItemSuffixes"] = () => new ItemSuffixesSettingsPanel(),
                ["Items"] = () => new ItemsSettingsPanel(),
                ["ItemGeneration"] = () => new ItemGenerationSettingsPanel(),
                ["Enemies"] = () => new EnemiesSettingsPanel(),
                ["EnemyTuning"] = () => new EnemyTuningSettingsPanel(),
                ["BalanceTuning"] = () => new BalanceTuningSettingsPanel(),
                ["ActionInteractionLab"] = () => new ActionInteractionLabSettingsPanel(),
                ["About"] = () => new AboutSettingsPanel()
            };
    }
}
