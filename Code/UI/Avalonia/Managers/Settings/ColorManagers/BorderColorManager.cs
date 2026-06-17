namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>Panel chrome uses SettingsTheme; Appearance preview borders keep inline colors.</summary>
    public class BorderColorManager
    {
        public BorderColorManager(SettingsPanel? settingsPanel) { }

        public void ApplyColors()
        {
            // Intentionally no-op: walking the visual tree overwrote flat-dark panel content.
        }
    }
}
