using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Generates and displays action test blocks for the settings panel
    /// Extracted from SettingsPanel.axaml.cs to reduce file size and improve separation of concerns
    /// </summary>
    public class SettingsActionTestGenerator
    {
        private readonly CanvasUICoordinator? canvasUI;
        private readonly Action<string, bool>? showStatusMessage;

        public SettingsActionTestGenerator(CanvasUICoordinator? canvasUI, Action<string, bool>? showStatusMessage)
        {
            this.canvasUI = canvasUI;
            this.showStatusMessage = showStatusMessage;
        }

    }
}

