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

        public static bool IsTrainingDummy(Actor? actor) =>
            actor is Enemy enemy &&
            string.Equals(enemy.Name, GameConstants.TrainingDummyEnemyName, StringComparison.OrdinalIgnoreCase);

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

        public static TrainingGroundTutorialScript CreateTrainingGroundTutorialScript() =>
            new TrainingGroundTutorialScript(new[]
            {
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    4,
                    "You lunge before you are ready. The roll is 4, so the swing misses and the lesson is to slow down."),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    10,
                    "You center your stance. A roll of 10 is enough to connect, but not enough to unlock the combo path."),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    14,
                    "You follow through with purpose. A roll of 14 reaches the combo gate and turns the punch into your first clean technique."),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    20,
                    "You recognize the opening. A natural 20 shows how a perfect roll can finish a fight.")
            });
    }
}
