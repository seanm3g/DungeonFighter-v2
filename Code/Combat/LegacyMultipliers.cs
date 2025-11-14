namespace RPGGame
{
    /// <summary>
    /// Legacy multipliers for backward compatibility with older configurations
    /// These are alternative timing values that can be loaded from JSON
    /// </summary>
    public class LegacyMultipliers
    {
        public double Title { get; set; } = 15.0;
        public double MainTitle { get; set; } = 20.0;
        public double System { get; set; } = 1.0;
        public double Combat { get; set; } = 0.5;
        public double Environmental { get; set; } = 1.5;
        public double EffectMessage { get; set; } = 0.4;
        public double DamageOverTime { get; set; } = 0.4;
        public double RollInfo { get; set; } = 1.0;
        public double Encounter { get; set; } = 2.0;
    }
}
