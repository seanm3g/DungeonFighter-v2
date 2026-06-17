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
            if (room == null || hero == null)
                return baseRoll;

            int dungeonLevel = Math.Max(1, hero.Progression?.Level ?? 1);
            var tags = room.Tags;
            int shift = 0;
            if (tags.Any(t => string.Equals(t, "elegant", StringComparison.OrdinalIgnoreCase)))
                shift += dungeonLevel;
            if (tags.Any(t => string.Equals(t, "dilapidated", StringComparison.OrdinalIgnoreCase)))
                shift -= dungeonLevel;

            return Math.Clamp(baseRoll + shift, 1, 20);
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
}
