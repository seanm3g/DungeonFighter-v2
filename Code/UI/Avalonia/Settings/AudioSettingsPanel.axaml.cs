using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    /// <summary>
    /// Audio settings panel — volume / mute, per-state music mapping, and per-cue file bindings.
    /// Wired up by <see cref="Managers.Settings.PanelHandlers.AudioPanelHandler"/>.
    /// </summary>
    public partial class AudioSettingsPanel : UserControl
    {
        public AudioSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
