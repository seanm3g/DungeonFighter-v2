using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    /// <summary>
    /// Main appearance settings panel that composes three specialized panels:
    /// - ColorCodesSettingsPanel: Manages color codes
    /// - ColorTemplatesSettingsPanel: Manages color templates
    /// - KeywordGroupsSettingsPanel: Manages keyword groups
    /// </summary>
    public partial class AppearanceSettingsPanel : UserControl
    {
        public AppearanceSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
