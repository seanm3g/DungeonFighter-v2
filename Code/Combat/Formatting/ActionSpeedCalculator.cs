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

            // Apply consumed SPEED_MOD from ACTION/ABILITY keyword (positive = faster = shorter time)
            if (actor is Character speedModChar && speedModChar.Effects.ConsumedSpeedModPercent != 0)
                actionLength = actionLength / (1.0 + speedModChar.Effects.ConsumedSpeedModPercent / 100.0);
            
            // Apply critical miss penalty (doubles action length)
            if (isCriticalMiss || actor.HasCriticalMissPenalty)
            {
                actionLength *= 2.0;
            }
            
            // Enemy must be checked before Character: Enemy subclasses Character, and Enemy uses `new`
            // GetTotalAttackSpeed() for direct-stat dummies (Action Lab). A Character-typed match would bypass that.
            if (actor is Enemy enemy)
            {
                return enemy.GetTotalAttackSpeed() * actionLength;
            }
            
            if (actor is Character character)
            {
                return character.GetTotalAttackSpeed() * actionLength;
            }
            
            return 0;
        }
    }
}

