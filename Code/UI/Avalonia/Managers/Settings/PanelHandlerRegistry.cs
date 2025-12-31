using System.Collections.Generic;
using RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Registry that maps panel category tags to their handlers
    /// </summary>
    public class PanelHandlerRegistry
    {
        private readonly Dictionary<string, ISettingsPanelHandler> handlers = new Dictionary<string, ISettingsPanelHandler>();

        /// <summary>
        /// Registers a panel handler for a specific category tag
        /// </summary>
        public void Register(ISettingsPanelHandler handler)
        {
            if (handler == null) return;
            handlers[handler.PanelType] = handler;
        }

        /// <summary>
        /// Gets the handler for a specific category tag
        /// </summary>
        public ISettingsPanelHandler? GetHandler(string categoryTag)
        {
            handlers.TryGetValue(categoryTag, out var handler);
            return handler;
        }

        /// <summary>
        /// Checks if a handler exists for the given category tag
        /// </summary>
        public bool HasHandler(string categoryTag)
        {
            return handlers.ContainsKey(categoryTag);
        }
    }
}

