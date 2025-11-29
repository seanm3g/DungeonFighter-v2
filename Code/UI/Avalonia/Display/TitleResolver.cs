using RPGGame;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Utility class for determining display titles based on game state
    /// </summary>
    public static class TitleResolver
    {
        /// <summary>
        /// Determines the title based on current game state
        /// </summary>
        public static string DetermineTitle(Character? character, Enemy? enemy)
        {
            if (enemy != null)
                return "COMBAT";
            if (character != null)
                return "DUNGEON FIGHTER";
            return "DUNGEON FIGHTER";
        }
    }
}

