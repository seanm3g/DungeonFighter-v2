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
        HeroLowHealth,
        EnemyLowHealth,
        ComboEnded,
        RoomCleared,
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
        /// <summary>Natural die face (after adv/replace, before flat roll bonus). Used by ONNATURALROLL.</summary>
        public int NaturalRollValue { get; set; }
        public int Damage { get; set; }
        public bool IsCombo { get; set; }
        public bool IsCritical { get; set; }
        public bool IsCriticalMiss { get; set; }
        public bool IsMiss { get; set; }
        public double HealthPercentage { get; set; }
        public string? StatusEffectType { get; set; }
        public string? StatType { get; set; }
        public int StatValue { get; set; }
        /// <summary>Dungeon rooms cleared count for <see cref="CombatEventType.RoomCleared"/> (1-based after increment).</summary>
        public int RoomsClearedCount { get; set; }

        public CombatEvent(CombatEventType type, Actor source)
        {
            Type = type;
            Source = source;
        }
    }
}

