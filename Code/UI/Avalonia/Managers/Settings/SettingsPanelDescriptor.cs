using Avalonia.Controls;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
  public sealed record SettingsPanelDescriptor(
    string Tag,
    string DisplayName,
    Func<UserControl> Factory,
    SettingsContentArea ContentArea = SettingsContentArea.MainScroll,
    Type? PanelType = null,
    bool UsesHandler = false,
    bool UsesTabManager = false,
    bool SavesViaHandler = false,
    string? SidebarGroup = null,
    int Order = 0);
}
