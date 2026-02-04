namespace RPGGame.Entity.Services
{
    /// <summary>
    /// Reports save/load errors to the user (console or UI).
    /// Centralizes the "console vs custom UI" messaging logic.
    /// </summary>
    public interface ICharacterSaveErrorReporter
    {
        void ReportSaveError(string title, string message, string? filename = null);
        void ReportLoadError(string message);
        void ReportDeleteError(string message);
    }
}
