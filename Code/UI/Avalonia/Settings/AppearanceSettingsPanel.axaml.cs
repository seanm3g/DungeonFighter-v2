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
            BindVisualOption("AnimateCombatCheckBox", GameConfiguration.Instance.UICustomization.AnimateCombat,
                value => GameConfiguration.Instance.UICustomization.AnimateCombat = value);
            BindVisualOption("CombatVfxCheckBox", GameConfiguration.Instance.UICustomization.CombatVisualEffects,
                value => GameConfiguration.Instance.UICustomization.CombatVisualEffects = value);
            BindVisualOption("ReducedCombatMotionCheckBox", GameConfiguration.Instance.UICustomization.ReducedCombatMotion,
                value => GameConfiguration.Instance.UICustomization.ReducedCombatMotion = value);
            var illustrated = this.FindControl<CheckBox>("IllustratedCombatCheckBox");
            if (illustrated != null)
            {
                illustrated.IsChecked = GameConfiguration.Instance.UICustomization.IllustratedCombat;
                illustrated.IsCheckedChanged += (_, _) =>
                {
                    GameConfiguration.Instance.UICustomization.IllustratedCombat = illustrated.IsChecked == true;
                    bool saved = GameConfiguration.Instance.SaveToFile();
                    var status = this.FindControl<TextBlock>("BattleVisualsSaveStatus");
                    if (status != null)
                        status.Text = saved ? "Saved. Resume combat to see the change." : "Applied for this session; settings could not be saved.";
                };
            }
        }

        private void BindVisualOption(string name, bool initial, System.Action<bool> update)
        {
            var checkbox = this.FindControl<CheckBox>(name);
            if (checkbox == null) return;
            checkbox.IsChecked = initial;
            checkbox.IsCheckedChanged += (_, _) =>
            {
                update(checkbox.IsChecked == true);
                bool saved = GameConfiguration.Instance.SaveToFile();
                var status = this.FindControl<TextBlock>("BattleVisualsSaveStatus");
                if (status != null)
                    status.Text = saved ? "Saved. Resume combat to see the change." : "Applied for this session; settings could not be saved.";
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
