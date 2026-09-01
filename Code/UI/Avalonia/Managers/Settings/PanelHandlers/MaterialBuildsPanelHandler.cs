using System.Linq;
using Avalonia.Controls;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public sealed class MaterialBuildsPanelHandler : ISettingsPanelHandler
    {
        public string PanelType => "MaterialBuilds";

        public void WireUp(UserControl panel)
        {
            LoadSettings(panel);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not MaterialBuildsSettingsPanel buildsPanel)
                return;
            MaterialBuildsLoader.Reload();
            buildsPanel.LoadFromCatalog();
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not MaterialBuildsSettingsPanel buildsPanel)
                return;
            var rows = buildsPanel.Rows.Select(r => r.ToData()).ToList();
            MaterialBuildsLoader.Save(rows);
        }
    }
}
