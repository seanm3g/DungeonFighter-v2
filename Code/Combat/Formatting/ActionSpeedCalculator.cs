using RPGGame;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// Calculates actual action speed for display purposes
    /// </summary>
    public static class ActionSpeedCalculator
    {
        /// <summary>
        /// Calculates actual action speed based on actor and action
        /// </summary>
        public static double CalculateActualActionSpeed(Actor actor, Action action)
        {
            if (action == null || action.Length <= 0) return 0;
            
            if (actor is Character character)
            {
                return character.GetTotalAttackSpeed() * action.Length;
            }
            else if (actor is Enemy enemy)
            {
                return enemy.GetTotalAttackSpeed() * action.Length;
            }
            
            return 0;
        }
    }
}

