namespace RPGGame.Combat
{
    /// <summary>Tracks the active environment during combat for roll/threshold modifiers.</summary>
    public static class CombatEnvironmentContext
    {
        public static Environment? CurrentRoom { get; set; }
    }
}
