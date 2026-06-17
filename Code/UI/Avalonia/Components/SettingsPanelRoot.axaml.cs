using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Components
{
    /// <summary>
    /// Scrollable settings panel shell: ScrollViewer + spaced StackPanel (Margin 20, Spacing 24).
    /// </summary>
    public partial class SettingsPanelRoot : UserControl
    {
        public SettingsPanelRoot()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
