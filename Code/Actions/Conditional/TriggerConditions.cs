using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Events;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Defines conditions that can trigger actions
    /// </summary>
    public enum TriggerConditionType
    {
        OnMiss,
        OnNormalHit,
        OnComboHit,
        OnCriticalHit,
        OnExactRollValue,
        IfSameActionUsedPreviously,
        IfDifferentActionUsedPreviously,
        IfActionHasTag,
        IfGearHasTag,
        IfTargetHealthBelow,
        IfTargetHealthAbove,
        IfSourceHealthBelow,
        IfSourceHealthAbove,
        IfComboPosition,
        IfComboLength
    }

    /// <summary>
    /// Represents a trigger condition
    /// </summary>
    public class TriggerCondition
    {
        public TriggerConditionType Type { get; set; }
        public object? Value { get; set; } // Can be int, string, double, etc. depending on condition type
        public string? Tag { get; set; } // For tag-based conditions
        public int? ComboPosition { get; set; } // For combo position conditions

        public TriggerCondition(TriggerConditionType type, object? value = null)
        {
            Type = type;
            Value = value;
        }
    }

    /// <summary>
    /// Helper class for creating common trigger conditions
    /// </summary>
    public static class TriggerConditionFactory
    {
        public static TriggerCondition OnMiss() => new TriggerCondition(TriggerConditionType.OnMiss);
        public static TriggerCondition OnNormalHit() => new TriggerCondition(TriggerConditionType.OnNormalHit);
        public static TriggerCondition OnComboHit() => new TriggerCondition(TriggerConditionType.OnComboHit);
        public static TriggerCondition OnCriticalHit() => new TriggerCondition(TriggerConditionType.OnCriticalHit);
        public static TriggerCondition OnExactRollValue(int value) => new TriggerCondition(TriggerConditionType.OnExactRollValue, value);
        public static TriggerCondition IfSameActionUsedPreviously() => new TriggerCondition(TriggerConditionType.IfSameActionUsedPreviously);
        public static TriggerCondition IfDifferentActionUsedPreviously() => new TriggerCondition(TriggerConditionType.IfDifferentActionUsedPreviously);
        public static TriggerCondition IfActionHasTag(string tag) => new TriggerCondition(TriggerConditionType.IfActionHasTag) { Tag = tag };
        public static TriggerCondition IfGearHasTag(string tag) => new TriggerCondition(TriggerConditionType.IfGearHasTag) { Tag = tag };
        public static TriggerCondition IfTargetHealthBelow(double percentage) => new TriggerCondition(TriggerConditionType.IfTargetHealthBelow, percentage);
        public static TriggerCondition IfTargetHealthAbove(double percentage) => new TriggerCondition(TriggerConditionType.IfTargetHealthAbove, percentage);
        public static TriggerCondition IfSourceHealthBelow(double percentage) => new TriggerCondition(TriggerConditionType.IfSourceHealthBelow, percentage);
        public static TriggerCondition IfSourceHealthAbove(double percentage) => new TriggerCondition(TriggerConditionType.IfSourceHealthAbove, percentage);
        public static TriggerCondition IfComboPosition(int position) => new TriggerCondition(TriggerConditionType.IfComboPosition) { ComboPosition = position };
        public static TriggerCondition IfComboLength(int length) => new TriggerCondition(TriggerConditionType.IfComboLength, length);
    }
}

