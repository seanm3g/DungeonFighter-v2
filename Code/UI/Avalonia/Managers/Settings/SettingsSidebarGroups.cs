namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>Canonical sidebar group IDs and display labels for settings navigation.</summary>
    public static class SettingsSidebarGroups
    {
        public const string HeaderTag = "__sidebar_header__";

        public const string Player = "Player";
        public const string Developer = "Developer";
        public const string Balance = "Balance";
        public const string Testing = "Testing";
        public const string About = "About";

        public sealed record SidebarGroupDefinition(string Id, string? DisplayLabel, int Order);

        public static readonly SidebarGroupDefinition[] OrderedGroups =
        {
            new(Player, "Player Settings", 1),
            new(Developer, "Developer Settings", 2),
            new(Balance, "Balance & Tuning", 3),
            new(Testing, "Testing", 4),
            new(About, null, 5)
        };
    }
}
