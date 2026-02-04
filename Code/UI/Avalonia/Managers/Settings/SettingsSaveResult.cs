namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Result of a settings save run. Used by the panel to show status and to run post-save "apply to game" steps.
    /// </summary>
    public readonly struct SettingsSaveResult
    {
        public bool Success { get; }
        public bool ActionsSaved { get; }
        public bool TextDelaysSaved { get; }

        public SettingsSaveResult(bool success, bool actionsSaved = false, bool textDelaysSaved = false)
        {
            Success = success;
            ActionsSaved = actionsSaved;
            TextDelaysSaved = textDelaysSaved;
        }
    }
}
