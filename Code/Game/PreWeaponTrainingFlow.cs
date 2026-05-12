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
                    10,
                    "You breathe, plant your feet, and drive PUNCH HARD into the target. A 10 is a clean hit: reliable, plain, and enough to move the fight forward."),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    4,
                    "The dummy swings back on its rope, and you chase it too quickly. The 4 stays in the miss band, teaching you that effort without aim is only noise."),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    14,
                    "The dummy rocks back, and you follow the motion instead of resetting. A 14 opens the combo path, turning one good hit into momentum."),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    1,
                    "You get greedy after the combo and trip over your own stance. The natural 1 is a critical miss: momentum can become a mistake if you lose control."),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    20,
                    "You settle, see the final opening, and drive PUNCH HARD straight through it. The natural 20 is both combo and crit, breaking the dummy apart.")
            });
    }
}
