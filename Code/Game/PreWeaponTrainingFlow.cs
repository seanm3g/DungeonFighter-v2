using System;

namespace RPGGame
{
    /// <summary>
    /// Shared checks for the pre-weapon Training Ground tutorial flow.
    /// </summary>
    public static class PreWeaponTrainingFlow
    {
        public static bool IsTrainingGroundDungeonName(string? dungeonName) =>
            string.Equals(dungeonName, GameConstants.TrainingGroundDungeonName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True when the current player is in the pre-weapon tutorial gate and the named dungeon is Training Ground.
        /// </summary>
        public static bool IsPreWeaponTrainingDungeon(GameStateManager? stateManager, string? dungeonName)
        {
            if (stateManager?.CurrentPlayer == null)
                return false;
            return stateManager.CurrentPlayer.PendingPreWeaponTrainingGround && IsTrainingGroundDungeonName(dungeonName);
        }

        public static Dungeon CreateTrainingGroundDungeon() =>
            new Dungeon(
                GameConstants.TrainingGroundDungeonName,
                1,
                1,
                "Generic",
                new System.Collections.Generic.List<string>(),
                null);
    }
}
