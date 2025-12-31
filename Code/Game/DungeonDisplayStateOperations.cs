namespace RPGGame
{
    /// <summary>
    /// Helper class for dungeon display state accessor operations.
    /// Extracted from DungeonDisplayManager to provide a clean API for state queries.
    /// </summary>
    public static class DungeonDisplayStateOperations
    {
        /// <summary>
        /// Gets the dungeon name from the state
        /// </summary>
        public static string? GetDungeonName(DungeonDisplayState state)
        {
            return state?.GetDungeonName();
        }

        /// <summary>
        /// Gets the room name from the state
        /// </summary>
        public static string? GetRoomName(DungeonDisplayState state)
        {
            return state?.GetRoomName();
        }

        /// <summary>
        /// Gets the current enemy from the state
        /// </summary>
        public static Enemy? GetCurrentEnemy(DungeonDisplayState state)
        {
            return state?.CurrentEnemy;
        }

        /// <summary>
        /// Gets the current player from the state
        /// </summary>
        public static Character? GetCurrentPlayer(DungeonDisplayState state)
        {
            return state?.CurrentPlayer;
        }
    }
}
