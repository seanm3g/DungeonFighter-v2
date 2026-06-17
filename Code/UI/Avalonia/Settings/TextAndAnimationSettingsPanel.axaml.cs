using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class TextAndAnimationSettingsPanel : UserControl
    {
        public TextDelaysSettingsPanel DelaysPanel { get; }
        public TextAnimationPresetsSettingsPanel AnimationPanel { get; }

        public TextAndAnimationSettingsPanel()
        {
            InitializeComponent();
            DelaysPanel = new TextDelaysSettingsPanel();
            AnimationPanel = new TextAnimationPresetsSettingsPanel();

            var delaysHost = this.FindControl<ContentControl>("DelaysHost");
            var animationHost = this.FindControl<ContentControl>("AnimationHost");
            if (delaysHost != null)
                delaysHost.Content = DelaysPanel;
            if (animationHost != null)
                animationHost.Content = AnimationPanel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
