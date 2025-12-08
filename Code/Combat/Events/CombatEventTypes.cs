using RPGGame;

namespace RPGGame.Combat.Events
{
    /// <summary>
    /// Types of combat events that can be triggered
    /// </summary>
    public enum CombatEventType
    {
        ActionExecuted,
        ActionHit,
        ActionMiss,
        ActionCombo,
        ActionCritical,
        ExactRollValue,
        EnemyDied,
        EnemyHealthThreshold,
        ComboEnded,
        StatusEffectApplied,
        StatChanged,
        TurnStarted,
        TurnEnded
    }

    /// <summary>
    /// Base class for combat events
    /// </summary>
    public class CombatEvent
    {
        public CombatEventType Type { get; set; }
        public Actor Source { get; set; }
        public Actor? Target { get; set; }
        public Action? Action { get; set; }
        public int RollValue { get; set; }
        public int Damage { get; set; }
        public bool IsCombo { get; set; }
        public bool IsCritical { get; set; }
        public bool IsMiss { get; set; }
        public double HealthPercentage { get; set; }
        public string? StatusEffectType { get; set; }
        public string? StatType { get; set; }
        public int StatValue { get; set; }

        public CombatEvent(CombatEventType type, Actor source)
        {
            Type = type;
            Source = source;
        }
    }
}

