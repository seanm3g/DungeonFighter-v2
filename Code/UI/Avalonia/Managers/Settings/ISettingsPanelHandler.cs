using Avalonia.Controls;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Interface for settings panel handlers that wire up controls and load settings
    /// </summary>
    public interface ISettingsPanelHandler
    {
        /// <summary>
        /// Gets the panel type/category tag this handler manages
        /// </summary>
        string PanelType { get; }
        
        /// <summary>
        /// Wires up event handlers for the panel controls
        /// </summary>
        void WireUp(UserControl panel);
        
        /// <summary>
        /// Loads current settings into the panel controls
        /// </summary>
        void LoadSettings(UserControl panel);
    }
}

