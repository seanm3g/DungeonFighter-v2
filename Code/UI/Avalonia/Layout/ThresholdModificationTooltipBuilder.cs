using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Builds threshold hover tooltip lines listing each source that shifts the d20 ladder vs defaults.
    /// Uses the same components as <see cref="DiceRollThresholdResolver"/>.
    /// </summary>
    public static class ThresholdModificationTooltipBuilder
    {
        public enum Kind
        {
            Crit,
            Combo,
            Hit,
            CritMiss
        }

        public static void AppendLines(Actor actor, Kind kind, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(actor);
            var parts = DiceRollThresholdResolver.ResolveShiftParts(actor);
            var lines = new List<string>();

            switch (kind)
            {
                case Kind.Crit:
                    AppendStoredLine(lines, snapshot.Crit, snapshot.DefaultCrit, "Crit threshold tuning");
                    AppendShiftLine(lines, parts.Pending.CritDelta, "Queued CRIT");
                    AppendShiftLine(lines, parts.TechCritSteps, "TECH milestones");
                    AppendShiftLine(lines, parts.EqCrit, "Gear CRIT");
                    AppendShiftLine(lines, parts.DungeonCrit, "Dungeon buff (crit)");
                    AppendFloorNoteIfNeeded(lines, snapshot.EffectiveCrit, snapshot.Crit, parts.CritRowShift);
                    break;
                case Kind.Combo:
                    AppendStoredLine(lines, snapshot.Combo, snapshot.DefaultCombo, "Combo threshold tuning");
                    AppendShiftLine(lines, parts.Pending.SharedAccuracy, "Queued accuracy");
                    AppendShiftLine(lines, parts.Pending.ComboDelta, "Queued COMBO");
                    AppendShiftLine(lines, parts.TechComboSteps, "TECH milestones");
                    AppendShiftLine(lines, parts.EqCombo, "Gear COMBO");
                    AppendShiftLine(lines, parts.DungeonCombo, "Dungeon buff (combo)");
                    AppendFloorNoteIfNeeded(lines, snapshot.EffectiveCombo, snapshot.Combo, parts.ComboRowShift);
                    break;
                case Kind.Hit:
                    AppendStoredLine(lines, snapshot.Hit + 1, snapshot.DefaultMinRollToHit, "Miss-band tuning");
                    AppendShiftLine(lines, parts.Pending.SharedAccuracy, "Queued accuracy");
                    AppendShiftLine(lines, parts.Pending.HitDelta, "Queued HIT");
                    AppendShiftLine(lines, parts.TechHitSteps, "TECH milestones");
                    AppendShiftLine(lines, parts.NaiveteHitSteps, "Naiveté");
                    AppendShiftLine(lines, parts.EqHit, "Gear HIT");
                    AppendShiftLine(lines, parts.DungeonHit, "Dungeon buff (hit)");
                    AppendFloorNoteIfNeeded(lines, snapshot.EffectiveHit, snapshot.Hit + 1, parts.HitRowShift);
                    break;
                case Kind.CritMiss:
                    AppendStoredLine(lines, snapshot.CritMiss, DiceRollThresholdSnapshot.DefaultCritMiss, "Crit-miss tuning");
                    AppendCritMissShiftLine(lines, parts.Pending.CritMissDelta, "Queued crit miss");
                    AppendCritMissShiftLine(lines, parts.DungeonCritMiss, "Dungeon buff (crit miss)");
                    AppendFloorNoteIfNeeded(lines, snapshot.EffectiveCritMiss, snapshot.CritMiss, parts.CritMissRowShift);
                    break;
            }

            if (lines.Count == 0)
                return;

            addWrapped("Modifications:");
            foreach (string line in lines)
                addWrapped(line);
        }

        private static void AppendStoredLine(List<string> lines, int currentDisplayed, int defaultDisplayed, string label)
        {
            int delta = currentDisplayed - defaultDisplayed;
            if (delta == 0)
                return;

            lines.Add($"  {label}: {FormatDisplayDelta(delta)} (current {currentDisplayed}, default {defaultDisplayed})");
        }

        /// <summary>Positive shift lowers crit/combo/hit ladder numbers (easier).</summary>
        private static void AppendShiftLine(List<string> lines, int shift, string label)
        {
            if (shift == 0)
                return;

            lines.Add($"  {label} (+{shift}): {FormatDisplayDelta(-shift)}");
        }

        /// <summary>Positive queued crit-miss bonus raises the crit-miss ladder (harder).</summary>
        private static void AppendCritMissShiftLine(List<string> lines, int delta, string label)
        {
            if (delta == 0)
                return;

            lines.Add($"  {label} ({delta:+0;-0;0}): {FormatDisplayDelta(delta)}");
        }

        private static void AppendFloorNoteIfNeeded(List<string> lines, int effective, int baseBeforeShift, int rowShift)
        {
            int unclamped = baseBeforeShift - rowShift;
            if (unclamped < 1 && effective == 1)
                lines.Add("  Display floored at 1.");
        }

        private static string FormatDisplayDelta(int delta) =>
            delta == 0 ? "0" : ThresholdDisplayFormatting.FormatSignedDeltaSuffix(delta).Trim();
    }
}
