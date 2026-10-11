namespace RPGGame.World.DungeonMap
{
    /// <summary>
    /// INT-biased d20 gate: Smart when (roll + Intelligence) &gt;= 15.
    /// </summary>
    public static class TraversalStrategyResolver
    {
        public const int SmartThreshold = 15;

        public static TraversalStrategyResolution Resolve(Character player)
        {
            if (player == null)
                throw new System.ArgumentNullException(nameof(player));

            int natural = Dice.Roll(20);
            int intelligence = player.Intelligence;
            int total = natural + intelligence;
            var strategy = total >= SmartThreshold ? TraversalStrategy.Smart : TraversalStrategy.Random;
            return new TraversalStrategyResolution(strategy, natural, intelligence, total);
        }
    }
}
