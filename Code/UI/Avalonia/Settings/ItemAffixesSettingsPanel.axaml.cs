using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ItemAffixesSettingsPanel : UserControl
    {
        public ItemModifiersSettingsPanel PrefixesPanel { get; }
        public ItemSuffixesSettingsPanel SuffixesPanel { get; }

        public ItemAffixesSettingsPanel()
        {
            InitializeComponent();
            PrefixesPanel = new ItemModifiersSettingsPanel();
            SuffixesPanel = new ItemSuffixesSettingsPanel();

            var prefixesHost = this.FindControl<ContentControl>("PrefixesHost");
            var suffixesHost = this.FindControl<ContentControl>("SuffixesHost");
            if (prefixesHost != null)
                prefixesHost.Content = PrefixesPanel;
            if (suffixesHost != null)
                suffixesHost.Content = SuffixesPanel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
