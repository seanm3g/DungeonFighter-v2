using System;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Helper class for common validation patterns in CanvasUICoordinator
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates that a player is not null
        /// </summary>
        public static void ValidatePlayer(Character? player, string parameterName = "player")
        {
            if (player == null)
            {
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null");
            }
        }

        /// <summary>
        /// Validates that a dungeons list is not null
        /// </summary>
        public static void ValidateDungeonsList<T>(T? dungeons, string parameterName = "dungeons")
        {
            if (dungeons == null)
            {
                throw new ArgumentNullException(parameterName, $"{parameterName} list cannot be null");
            }
        }
    }
}

