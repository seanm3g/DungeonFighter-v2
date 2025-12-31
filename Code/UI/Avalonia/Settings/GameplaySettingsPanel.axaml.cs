using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class GameplaySettingsPanel : UserControl
    {
        // Expose controls for event wiring - these will be auto-generated from XAML x:Name attributes
        // Access via: this.ShowIndividualActionMessagesCheckBox, this.FastCombatCheckBox, etc.

        public GameplaySettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

