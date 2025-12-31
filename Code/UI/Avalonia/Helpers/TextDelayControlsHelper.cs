using Avalonia.Controls;
using System;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Helper class for finding and managing text delay settings controls
    /// Extracted from SettingsPanel to reduce complexity
    /// </summary>
    public class TextDelayControlsHelper
    {
        /// <summary>
        /// Container for all text delay controls
        /// </summary>
        public class TextDelayControls
        {
            public CheckBox? EnableGuiDelaysCheckBox { get; set; }
            public CheckBox? EnableConsoleDelaysCheckBox { get; set; }
            // Deprecated: ActionDelay and MessageDelay sliders removed - combat timing is now controlled by
            // MessageTypeDelays.Combat and ChunkedTextReveal.Combat presets
            public Slider? ActionDelaySlider { get; set; }
            public TextBox? ActionDelayTextBox { get; set; }
            public Slider? MessageDelaySlider { get; set; }
            public TextBox? MessageDelayTextBox { get; set; }
            public TextBox? CombatDelayTextBox { get; set; }
            public TextBox? SystemDelayTextBox { get; set; }
            public TextBox? MenuDelayTextBox { get; set; }
            public TextBox? TitleDelayTextBox { get; set; }
            public TextBox? MainTitleDelayTextBox { get; set; }
            public TextBox? EnvironmentalDelayTextBox { get; set; }
            public TextBox? EffectMessageDelayTextBox { get; set; }
            public TextBox? DamageOverTimeDelayTextBox { get; set; }
            public TextBox? EncounterDelayTextBox { get; set; }
            public TextBox? RollInfoDelayTextBox { get; set; }
            public TextBox? BaseMenuDelayTextBox { get; set; }
            public TextBox? ProgressiveReductionRateTextBox { get; set; }
            public TextBox? ProgressiveThresholdTextBox { get; set; }
            public TextBox? CombatPresetBaseDelayTextBox { get; set; }
            public TextBox? CombatPresetMinDelayTextBox { get; set; }
            public TextBox? CombatPresetMaxDelayTextBox { get; set; }
            public TextBox? DungeonPresetBaseDelayTextBox { get; set; }
            public TextBox? DungeonPresetMinDelayTextBox { get; set; }
            public TextBox? DungeonPresetMaxDelayTextBox { get; set; }
            public TextBox? RoomPresetBaseDelayTextBox { get; set; }
            public TextBox? RoomPresetMinDelayTextBox { get; set; }
            public TextBox? RoomPresetMaxDelayTextBox { get; set; }
            public TextBox? NarrativePresetBaseDelayTextBox { get; set; }
            public TextBox? NarrativePresetMinDelayTextBox { get; set; }
            public TextBox? NarrativePresetMaxDelayTextBox { get; set; }
            public TextBox? DefaultPresetBaseDelayTextBox { get; set; }
            public TextBox? DefaultPresetMinDelayTextBox { get; set; }
            public TextBox? DefaultPresetMaxDelayTextBox { get; set; }
        }

        /// <summary>
        /// Finds all text delay controls from a UserControl
        /// </summary>
        public static TextDelayControls FindControls(UserControl parent)
        {
            return new TextDelayControls
            {
                EnableGuiDelaysCheckBox = parent.FindControl<CheckBox>("EnableGuiDelaysCheckBox"),
                EnableConsoleDelaysCheckBox = parent.FindControl<CheckBox>("EnableConsoleDelaysCheckBox"),
                // Deprecated: These controls no longer exist in the UI - will return null
                ActionDelaySlider = parent.FindControl<Slider>("ActionDelaySlider"),
                ActionDelayTextBox = parent.FindControl<TextBox>("ActionDelayTextBox"),
                MessageDelaySlider = parent.FindControl<Slider>("MessageDelaySlider"),
                MessageDelayTextBox = parent.FindControl<TextBox>("MessageDelayTextBox"),
                CombatDelayTextBox = parent.FindControl<TextBox>("CombatDelayTextBox"),
                SystemDelayTextBox = parent.FindControl<TextBox>("SystemDelayTextBox"),
                MenuDelayTextBox = parent.FindControl<TextBox>("MenuDelayTextBox"),
                TitleDelayTextBox = parent.FindControl<TextBox>("TitleDelayTextBox"),
                MainTitleDelayTextBox = parent.FindControl<TextBox>("MainTitleDelayTextBox"),
                EnvironmentalDelayTextBox = parent.FindControl<TextBox>("EnvironmentalDelayTextBox"),
                EffectMessageDelayTextBox = parent.FindControl<TextBox>("EffectMessageDelayTextBox"),
                DamageOverTimeDelayTextBox = parent.FindControl<TextBox>("DamageOverTimeDelayTextBox"),
                EncounterDelayTextBox = parent.FindControl<TextBox>("EncounterDelayTextBox"),
                RollInfoDelayTextBox = parent.FindControl<TextBox>("RollInfoDelayTextBox"),
                BaseMenuDelayTextBox = parent.FindControl<TextBox>("BaseMenuDelayTextBox"),
                ProgressiveReductionRateTextBox = parent.FindControl<TextBox>("ProgressiveReductionRateTextBox"),
                ProgressiveThresholdTextBox = parent.FindControl<TextBox>("ProgressiveThresholdTextBox"),
                CombatPresetBaseDelayTextBox = parent.FindControl<TextBox>("CombatPresetBaseDelayTextBox"),
                CombatPresetMinDelayTextBox = parent.FindControl<TextBox>("CombatPresetMinDelayTextBox"),
                CombatPresetMaxDelayTextBox = parent.FindControl<TextBox>("CombatPresetMaxDelayTextBox"),
                DungeonPresetBaseDelayTextBox = parent.FindControl<TextBox>("DungeonPresetBaseDelayTextBox"),
                DungeonPresetMinDelayTextBox = parent.FindControl<TextBox>("DungeonPresetMinDelayTextBox"),
                DungeonPresetMaxDelayTextBox = parent.FindControl<TextBox>("DungeonPresetMaxDelayTextBox"),
                RoomPresetBaseDelayTextBox = parent.FindControl<TextBox>("RoomPresetBaseDelayTextBox"),
                RoomPresetMinDelayTextBox = parent.FindControl<TextBox>("RoomPresetMinDelayTextBox"),
                RoomPresetMaxDelayTextBox = parent.FindControl<TextBox>("RoomPresetMaxDelayTextBox"),
                NarrativePresetBaseDelayTextBox = parent.FindControl<TextBox>("NarrativePresetBaseDelayTextBox"),
                NarrativePresetMinDelayTextBox = parent.FindControl<TextBox>("NarrativePresetMinDelayTextBox"),
                NarrativePresetMaxDelayTextBox = parent.FindControl<TextBox>("NarrativePresetMaxDelayTextBox"),
                DefaultPresetBaseDelayTextBox = parent.FindControl<TextBox>("DefaultPresetBaseDelayTextBox"),
                DefaultPresetMinDelayTextBox = parent.FindControl<TextBox>("DefaultPresetMinDelayTextBox"),
                DefaultPresetMaxDelayTextBox = parent.FindControl<TextBox>("DefaultPresetMaxDelayTextBox")
            };
        }
    }
}

