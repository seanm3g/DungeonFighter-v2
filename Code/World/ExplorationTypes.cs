namespace RPGGame
{
    /// <summary>
    /// Data structures for exploration system results
    /// </summary>
    public enum ExplorationOutcome
    {
        FindNothing,           // Roll 1-5: Nothing happens (may result in surprise)
        DiscoverEnvironment,   // Roll 6-12: Learn about environment
        EnvironmentalHazard,   // Roll 13-15: Trap/collapse affects player
        SpotEnemyEarly         // Roll 16-20: Player spots enemy, gets first attack
    }

    /// <summary>
    /// Result of pre-combat exploration
    /// </summary>
    public class ExplorationResult
    {
        public ExplorationOutcome Outcome { get; set; }
        public int Roll { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? EnvironmentInfo { get; set; }
        public bool PlayerGetsFirstAttack { get; set; } = false;
        public bool IsSurprised { get; set; } = false;
        public EnvironmentalHazard? Hazard { get; set; }
    }

    /// <summary>
    /// Environmental hazard that can occur during exploration
    /// </summary>
    public class EnvironmentalHazard
    {
        public string Message { get; set; } = string.Empty;
        public bool SkipToCombat { get; set; } = false; // If true, apply damage and proceed to combat
        public bool SkipToSearch { get; set; } = false; // If true, skip combat entirely, go to search
        public int Damage { get; set; } = 0; // Damage if SkipToCombat
    }

    /// <summary>
    /// Result of post-combat search
    /// </summary>
    public class SearchResult
    {
        public bool FoundLoot { get; set; }
        public Item? LootItem { get; set; }
        public int Roll { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

