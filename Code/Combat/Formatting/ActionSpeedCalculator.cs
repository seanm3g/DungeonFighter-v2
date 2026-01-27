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
        public static double CalculateActualActionSpeed(Actor actor, Action action, bool isCriticalMiss = false)
        {
            if (action == null || action.Length <= 0) return 0;
            
            double actionLength = action.Length;
            
            // Apply critical miss penalty (doubles action length)
            if (isCriticalMiss || actor.HasCriticalMissPenalty)
            {
                actionLength *= 2.0;
            }
            
            if (actor is Character character)
            {
                return character.GetTotalAttackSpeed() * actionLength;
            }
            else if (actor is Enemy enemy)
            {
                return enemy.GetTotalAttackSpeed() * actionLength;
            }
            
            return 0;
        }
    }
}

