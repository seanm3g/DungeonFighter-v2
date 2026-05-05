using Avalonia.Controls;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Context passed to panel initializers so they can wire up tab managers and handlers without the panel knowing all dependencies.
    /// Used by the initializer registry in SettingsPanel to replace the long switch in InitializePanelHandlers.
    /// </summary>
    public class PanelInitializerContext
    {
        public SettingsInitialization? Initialization { get; set; }
        public Managers.StatusEffectsTabManager? StatusEffectsTabManager { get; set; }
        public ItemModifiersTabManager? ItemModifiersTabManager { get; set; }
        public ItemSuffixesTabManager? ItemSuffixesTabManager { get; set; }
        public ItemsTabManager? ItemsTabManager { get; set; }
        public EnemiesTabManager? EnemiesTabManager { get; set; }
        public PanelHandlerRegistry? PanelHandlerRegistry { get; set; }
        public object? CanvasUI { get; set; }
        public Action<string, bool>? ShowStatusMessage { get; set; }
        public Action<UserControl>? RegisterTestingHandlerAndWireUp { get; set; }
    }
}
