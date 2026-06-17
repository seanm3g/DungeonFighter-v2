namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Panel content TextBoxes use SettingsTheme.axaml (.settings-ui). Appearance color pickers
    /// keep their own inline styling. Shell Save/Reset/Close buttons use ButtonColorManager.
    /// </summary>
    public class TextBoxColorManager
    {
        public TextBoxColorManager(SettingsPanel? settingsPanel) { }

        public void ApplyColors()
        {
            // Intentionally no-op: runtime GameSettings textbox colors conflict with unified theme.
        }
    }
}
