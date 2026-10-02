using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class TextAnimationPresetsSettingsPanel : UserControl
    {
        public TextAnimationPresetsSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Resolves named controls from the visual tree. Use these instead of relying on
        /// compile-time x:Name fields, which may be null when handlers wire up.
        /// </summary>
        private T? Find<T>(string name) where T : Control
            => this.FindControl<T>(name);

        public StackPanel? PreviewCharacterHostControl => Find<StackPanel>("PreviewCharacterHost");

        public ComboBox? PresetComboBoxControl => Find<ComboBox>("PresetComboBox");

        public ComboBox? PreviewTemplateComboBoxControl => Find<ComboBox>("PreviewTemplateComboBox");

        public TextBox? SampleTextTextBoxControl => Find<TextBox>("SampleTextTextBox");

        public Slider? AccentHueShiftSliderControl => Find<Slider>("AccentHueShiftSlider");

        public TextBox? AccentHueShiftTextBoxControl => Find<TextBox>("AccentHueShiftTextBox");

        public Border? AccentHuePreviewControl => Find<Border>("AccentHuePreview");

        public Slider? AccentSaturationSliderControl => Find<Slider>("AccentSaturationSlider");

        public TextBox? AccentSaturationTextBoxControl => Find<TextBox>("AccentSaturationTextBox");

        public Border? AccentSaturationPreviewControl => Find<Border>("AccentSaturationPreview");

        public TextBox? AccentPhaseDivisorMsTextBoxControl => Find<TextBox>("AccentPhaseDivisorMsTextBox");

        public TextBox? AccentCharacterPhaseOffsetTextBoxControl => Find<TextBox>("AccentCharacterPhaseOffsetTextBox");

        public TextBlock? AccentLayerStatusTextBlockControl => Find<TextBlock>("AccentLayerStatusTextBlock");

        // Wind wake — resolve via Find so values load even when x:Name fields are still null.
        public CheckBox? WindSwayEnabledCheckBoxControl => Find<CheckBox>("WindSwayEnabledCheckBox");
        public CheckBox? WindSwayChromaticEnabledCheckBoxControl => Find<CheckBox>("WindSwayChromaticEnabledCheckBox");
        public Slider? WindSwayRadiusSliderControl => Find<Slider>("WindSwayRadiusSlider");
        public TextBox? WindSwayRadiusTextBoxControl => Find<TextBox>("WindSwayRadiusTextBox");
        public Slider? WindSwayNearInfluenceSliderControl => Find<Slider>("WindSwayNearInfluenceSlider");
        public TextBox? WindSwayNearInfluenceTextBoxControl => Find<TextBox>("WindSwayNearInfluenceTextBox");
        public Slider? WindSwayFarInfluenceSliderControl => Find<Slider>("WindSwayFarInfluenceSlider");
        public TextBox? WindSwayFarInfluenceTextBoxControl => Find<TextBox>("WindSwayFarInfluenceTextBox");
        public Slider? WindSwayRearBiasSliderControl => Find<Slider>("WindSwayRearBiasSlider");
        public TextBox? WindSwayRearBiasTextBoxControl => Find<TextBox>("WindSwayRearBiasTextBox");
        public Slider? WindSwayChromaticSpreadSliderControl => Find<Slider>("WindSwayChromaticSpreadSlider");
        public TextBox? WindSwayChromaticSpreadTextBoxControl => Find<TextBox>("WindSwayChromaticSpreadTextBox");
        public Slider? WindSwayChromaticOpacitySliderControl => Find<Slider>("WindSwayChromaticOpacitySlider");
        public TextBox? WindSwayChromaticOpacityTextBoxControl => Find<TextBox>("WindSwayChromaticOpacityTextBox");
        public Button? WindSwayDebugRadiusButtonControl => Find<Button>("WindSwayDebugRadiusButton");

        // Click-charge explosion — resolve via Find so values load even when x:Name fields are still null.
        public CheckBox? ClickBurstEnabledCheckBoxControl => Find<CheckBox>("ClickBurstEnabledCheckBox");
        public Slider? ClickBurstRadiusSliderControl => Find<Slider>("ClickBurstRadiusSlider");
        public TextBox? ClickBurstRadiusTextBoxControl => Find<TextBox>("ClickBurstRadiusTextBox");
        public Slider? ClickBurstExplodeStrengthSliderControl => Find<Slider>("ClickBurstExplodeStrengthSlider");
        public TextBox? ClickBurstExplodeStrengthTextBoxControl => Find<TextBox>("ClickBurstExplodeStrengthTextBox");
        public Slider? ClickBurstExplodeOutSliderControl => Find<Slider>("ClickBurstExplodeOutSlider");
        public TextBox? ClickBurstExplodeOutTextBoxControl => Find<TextBox>("ClickBurstExplodeOutTextBox");
        public Slider? ClickBurstClicksToExplodeSliderControl => Find<Slider>("ClickBurstClicksToExplodeSlider");
        public TextBox? ClickBurstClicksToExplodeTextBoxControl => Find<TextBox>("ClickBurstClicksToExplodeTextBox");
    }
}
