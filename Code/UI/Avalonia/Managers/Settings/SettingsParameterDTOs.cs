using Avalonia.Controls;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Data Transfer Objects for settings parameters to reduce method parameter counts.
    /// </summary>
    
    /// <summary>
    /// DTO for main game settings controls
    /// </summary>
    public class MainSettingsControls
    {
        public Slider? NarrativeBalanceSlider { get; set; }
        public TextBox? NarrativeBalanceTextBox { get; set; }
        public CheckBox? EnableNarrativeEventsCheckBox { get; set; }
        public CheckBox? EnableInformationalSummariesCheckBox { get; set; }
        public Slider? CombatSpeedSlider { get; set; }
        public TextBox? CombatSpeedTextBox { get; set; }
        public CheckBox? ShowIndividualActionMessagesCheckBox { get; set; }
        public CheckBox? EnableComboSystemCheckBox { get; set; }
        public CheckBox? EnableTextDisplayDelaysCheckBox { get; set; }
        public CheckBox? FastCombatCheckBox { get; set; }
        public CheckBox? EnableAutoSaveCheckBox { get; set; }
        public TextBox? AutoSaveIntervalTextBox { get; set; }
        public CheckBox? ShowDetailedStatsCheckBox { get; set; }
        public CheckBox? EnableSoundEffectsCheckBox { get; set; }
        public Slider? EnemyHealthMultiplierSlider { get; set; }
        public TextBox? EnemyHealthMultiplierTextBox { get; set; }
        public Slider? EnemyDamageMultiplierSlider { get; set; }
        public TextBox? EnemyDamageMultiplierTextBox { get; set; }
        public Slider? PlayerHealthMultiplierSlider { get; set; }
        public TextBox? PlayerHealthMultiplierTextBox { get; set; }
        public Slider? PlayerDamageMultiplierSlider { get; set; }
        public TextBox? PlayerDamageMultiplierTextBox { get; set; }
        public CheckBox? ShowHealthBarsCheckBox { get; set; }
        public CheckBox? ShowDamageNumbersCheckBox { get; set; }
        public CheckBox? ShowComboProgressCheckBox { get; set; }
    }

    /// <summary>
    /// DTO for text delay settings controls
    /// </summary>
    public class TextDelaySettingsControls
    {
        public CheckBox? EnableGuiDelaysCheckBox { get; set; }
        public CheckBox? EnableConsoleDelaysCheckBox { get; set; }
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
    /// DTO for animation settings controls
    /// </summary>
    public class AnimationSettingsControls
    {
        public CheckBox? BrightnessMaskEnabledCheckBox { get; set; }
        public Slider? BrightnessMaskIntensitySlider { get; set; }
        public TextBox? BrightnessMaskIntensityTextBox { get; set; }
        public Slider? BrightnessMaskWaveLengthSlider { get; set; }
        public TextBox? BrightnessMaskWaveLengthTextBox { get; set; }
        public TextBox? BrightnessMaskUpdateIntervalTextBox { get; set; }
        public Slider? UndulationSpeedSlider { get; set; }
        public TextBox? UndulationSpeedTextBox { get; set; }
        public Slider? UndulationWaveLengthSlider { get; set; }
        public TextBox? UndulationWaveLengthTextBox { get; set; }
        public TextBox? UndulationIntervalTextBox { get; set; }
    }
}

