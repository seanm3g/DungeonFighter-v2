using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Picks a window safe to use as <c>Show(owner)</c> / <c>ShowDialog(owner)</c> parent.
    /// Prevents Avalonia <see cref="System.InvalidOperationException"/> when a stale reference
    /// (for example from <see cref="CanvasWindowManager"/>) points at a closed or non-visible window.
    /// </summary>
    public static class WindowOwnerResolver
    {
        public static Window? ResolveUsableOwnerWindow(Window? preferred)
        {
            if (IsUsableOwner(preferred))
                return preferred;

            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime life)
                return null;

            if (IsUsableOwner(life.MainWindow))
                return life.MainWindow;

            foreach (var top in life.Windows)
            {
                if (top is Window win && IsUsableOwner(win))
                    return win;
            }

            return null;
        }

        private static bool IsUsableOwner(Window? w)
        {
            if (w == null)
                return false;
            try
            {
                return w.IsVisible;
            }
            catch
            {
                return false;
            }
        }
    }
}
