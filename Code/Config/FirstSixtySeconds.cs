namespace RPGGame
{
    /// <summary>
    /// Rebuild 1.8 (D1.1, Phase 1 "the first 60 seconds"): New → Training Ground offer → 4 class cards → the first room
    /// of the first dungeon. Each cut piece of the old flow sits behind a switch until the step 12 cleanup.
    /// </summary>
    public static class FirstSixtySeconds
    {
        /// <summary>False (default): the path intro screen is skipped; the offer goes straight to weapon choice.</summary>
        public static bool ShowPathIntro { get; set; }

        /// <summary>False (default): no random extra armor piece in the starting bag (items arrive on the first drop).</summary>
        public static bool RandomStartingArmor { get; set; }

        /// <summary>True (default): picking a weapon starts the first dungeon instead of opening the inventory.</summary>
        public static bool StraightIntoFirstDungeon { get; set; } = true;
    }
}
