using Avalonia.Media;

namespace RPGGame.UI.Avalonia.Resources
{
    /// <summary>Static brushes matching SettingsTheme.axaml tokens for programmatic UI builders.</summary>
    public static class SettingsThemeBrushes
    {
        public static readonly IBrush Background = new SolidColorBrush(Color.Parse("#1A1A1A"));
        public static readonly IBrush TextPrimary = new SolidColorBrush(Color.Parse("#E8E8E8"));
        public static readonly IBrush TextMuted = new SolidColorBrush(Color.Parse("#B0B0B0"));
        public static readonly IBrush TextTitle = new SolidColorBrush(Color.Parse("#FFD700"));
        public static readonly IBrush InputBackground = new SolidColorBrush(Color.Parse("#2A2A2A"));
        public static readonly IBrush InputHoverBackground = new SolidColorBrush(Color.Parse("#353535"));
        public static readonly IBrush InputBorder = new SolidColorBrush(Color.Parse("#555555"));
        public static readonly IBrush InputFocusBorder = new SolidColorBrush(Color.Parse("#0078D4"));
        public static readonly IBrush InputErrorBackground = new SolidColorBrush(Color.Parse("#4A2828"));
        public static readonly IBrush InputErrorBorder = new SolidColorBrush(Color.Parse("#8B4545"));
        public static readonly IBrush ButtonPrimary = new SolidColorBrush(Color.Parse("#0078D4"));
        public static readonly IBrush ButtonSecondary = new SolidColorBrush(Color.Parse("#555555"));
        public static readonly IBrush ButtonNeutral = new SolidColorBrush(Color.Parse("#404040"));
        public static readonly IBrush SidebarBackground = new SolidColorBrush(Color.Parse("#404040"));
        public static readonly IBrush SidebarItem = new SolidColorBrush(Color.Parse("#484848"));
        public static readonly IBrush SidebarSelected = new SolidColorBrush(Color.Parse("#2F65B2"));
        public static readonly IBrush ExpanderHeader = new SolidColorBrush(Color.Parse("#383838"));
    }
}
