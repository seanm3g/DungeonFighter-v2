namespace RPGGame.Combat.Sequence
{
    /// <summary>Which d20 band a swing landed in, as the combat board draws its branches.</summary>
    public enum CombatSequenceSwingOutcome
    {
        Miss,
        Hit,
        Combo
    }

    /// <summary>d20 bands a swing was resolved against, in the bar's terms (MinHit = lowest face that hits).</summary>
    public readonly record struct CombatSequenceThresholds(int Crit, int Combo, int MinHit, int CritMiss);

    /// <summary>
    /// Structured facts about one resolved swing, attached to each of its <see cref="CombatSequenceStep"/>s so the
    /// combat board can animate the pre-swing state (the card that fired, the die, the branch taken) while
    /// the actor's live combo step has already advanced.
    /// </summary>
    public sealed class CombatSequenceSwingInfo
    {
        public CombatSequenceSwingInfo(Actor source, Action action, int roll, CombatSequenceSwingOutcome outcome, bool isCritical, int damage,
            CombatSequenceThresholds? thresholds = null)
        {
            Thresholds = thresholds;
            Source = source;
            Action = action;
            Roll = roll;
            Outcome = outcome;
            IsCritical = isCritical;
            Damage = damage;
        }

        public Actor Source { get; }
        public Action Action { get; }

        /// <summary>Final attack roll (die + bonus) compared against the d20 bands.</summary>
        public int Roll { get; }

        public CombatSequenceSwingOutcome Outcome { get; }
        public bool IsCritical { get; }
        public int Damage { get; }

        /// <summary>Thresholds in force for this roll (null when execution did not record them).</summary>
        public CombatSequenceThresholds? Thresholds { get; }
    }
}
