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
    }
}
