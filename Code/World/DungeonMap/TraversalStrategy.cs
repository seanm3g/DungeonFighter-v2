namespace RPGGame.World.DungeonMap
{
    /// <summary>
    /// Pathing mode for automated dungeon traversal between content rooms.
    /// </summary>
    public enum TraversalStrategy
    {
        Smart,
        Random
    }

    /// <summary>
    /// Result of the INT-biased d20 that picks Smart vs Random pathing.
    /// </summary>
    public readonly struct TraversalStrategyResolution
    {
        public TraversalStrategyResolution(TraversalStrategy strategy, int naturalRoll, int intelligence, int total)
        {
            Strategy = strategy;
            NaturalRoll = naturalRoll;
            Intelligence = intelligence;
            Total = total;
        }

        public TraversalStrategy Strategy { get; }
        public int NaturalRoll { get; }
        public int Intelligence { get; }
        public int Total { get; }

        public bool IsSmart => Strategy == TraversalStrategy.Smart;
    }
}
