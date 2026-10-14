using System;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.Combat
{
    /// <summary>Applies environment structure tags and unstable threshold mods to hero rolls.</summary>
    public static class EnvironmentRollModifier
    {
        public static int ApplyStructureRollShift(Environment? room, Character? hero, int baseRoll)
        {
            return ResolveStructureRollShift(room, hero, baseRoll).Roll;
        }

        /// <summary>
        /// Applies the elegant / dilapidated shift and reports the delta that actually changed the face.
        /// A clamped or cancelled shift has <see cref="StructureRollAdjustment.AppliedDelta"/> 0.
        /// </summary>
        public static StructureRollAdjustment ResolveStructureRollShift(Environment? room, Character? hero, int baseRoll)
        {
            if (room == null || hero == null)
                return new StructureRollAdjustment(baseRoll, 0, null);

            int dungeonLevel = Math.Max(1, hero.Progression?.Level ?? 1);
            int cap = GameConfiguration.Instance.Combat.RoomStructureRollShiftCap;
            if (cap > 0)
                dungeonLevel = Math.Min(dungeonLevel, cap);
            var tags = room.Tags;
            bool elegant = tags.Any(t => string.Equals(t, "elegant", StringComparison.OrdinalIgnoreCase));
            bool dilapidated = tags.Any(t => string.Equals(t, "dilapidated", StringComparison.OrdinalIgnoreCase));
            int shift = 0;
            if (elegant)
                shift += dungeonLevel;
            if (dilapidated)
                shift -= dungeonLevel;

            int shifted = Math.Clamp(baseRoll + shift, 1, 20);
            int applied = shifted - baseRoll;
            string? label = applied == 0
                ? null
                : applied > 0 ? "elegant" : "dilapidated";
            return new StructureRollAdjustment(shifted, applied, label);
        }

        public static void ApplyUnstableThresholdShift(Environment? room, Actor source)
        {
            if (room == null || source == null || room.UnstableThresholdMod == 0)
                return;

            var tm = RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager();
            int mod = room.UnstableThresholdMod;
            tm.AdjustHitThreshold(source, mod);
            tm.AdjustComboThreshold(source, mod);
            tm.AdjustCriticalHitThreshold(source, mod);
            tm.AdjustCriticalMissThreshold(source, mod);
        }
    }

    /// <summary>Hero d20 after a room structure tag, and the delta that actually landed on the face.</summary>
    public readonly struct StructureRollAdjustment
    {
        public StructureRollAdjustment(int roll, int appliedDelta, string? label)
        {
            Roll = roll;
            AppliedDelta = appliedDelta;
            Label = label;
        }

        public int Roll { get; }
        public int AppliedDelta { get; }
        /// <summary><c>elegant</c> or <c>dilapidated</c> when <see cref="AppliedDelta"/> is non-zero.</summary>
        public string? Label { get; }
        public bool AffectsRoll => AppliedDelta != 0 && !string.IsNullOrEmpty(Label);
    }
}
