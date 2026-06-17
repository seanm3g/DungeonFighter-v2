using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    /// <summary>
    /// Appearance settings with tabbed sections: Settings UI chrome, color codes,
    /// templates, keywords, and combat text appearance.
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
