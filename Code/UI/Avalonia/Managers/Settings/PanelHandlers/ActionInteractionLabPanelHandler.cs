using Avalonia.Controls;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>Wires the Action Lab launch control on its dedicated settings tab.</summary>
    public sealed class ActionInteractionLabPanelHandler : ISettingsPanelHandler
    {
        private readonly CanvasUICoordinator? canvasUI;

        public ActionInteractionLabPanelHandler(CanvasUICoordinator? canvasUI)
        {
            this.canvasUI = canvasUI;
        }

        public string PanelType => "ActionInteractionLab";

        public void WireUp(UserControl panel)
        {
            if (panel is not ActionInteractionLabSettingsPanel || canvasUI == null)
                return;

            var btn = panel.FindControl<Button>("ActionInteractionLabButton");
            if (btn == null)
                return;

            btn.Click += async (_, _) =>
            {
                var game = canvasUI.GetGame();
                if (game == null)
                    return;
                canvasUI.GetMainWindow()?.Activate();
                await game.StartActionInteractionLabAsync(canvasUI).ConfigureAwait(true);
            };
        }

        public void LoadSettings(UserControl panel)
        {
        }

        public void SaveSettings(UserControl panel)
        {
        }
    }
}
