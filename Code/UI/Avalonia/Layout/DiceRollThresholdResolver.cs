using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Resolves stored and effective d20 ladder thresholds for HUD display (bar, ladder rows, CHANCES).
    /// </summary>
    public readonly struct DiceRollThresholdSnapshot
    {
        public const int DefaultCritMiss = 1;

        public DiceRollThresholdSnapshot(
            int crit,
            int combo,
            int hit,
            int critMiss,
            int defaultCrit,
            int defaultCombo,
            int defaultHit,
            int effectiveCrit,
            int effectiveCombo,
            int effectiveHit,
            int effectiveCritMiss)
        {
            Crit = crit;
            Combo = combo;
            Hit = hit;
            CritMiss = critMiss;
            DefaultCrit = defaultCrit;
            DefaultCombo = defaultCombo;
            DefaultHit = defaultHit;
            EffectiveCrit = effectiveCrit;
            EffectiveCombo = effectiveCombo;
            EffectiveHit = effectiveHit;
            EffectiveCritMiss = effectiveCritMiss;
            DefaultMinRollToHit = defaultHit + 1;
        }

        public int Crit { get; }
        public int Combo { get; }
        /// <summary>Stored miss-band max (not minimum roll to hit).</summary>
        public int Hit { get; }
        public int CritMiss { get; }
        public int DefaultCrit { get; }
        public int DefaultCombo { get; }
        public int DefaultHit { get; }
        public int EffectiveCrit { get; }
        public int EffectiveCombo { get; }
        /// <summary>Minimum d20 roll to hit (stored hit + 1 after shifts).</summary>
        public int EffectiveHit { get; }
        public int EffectiveCritMiss { get; }
        public int DefaultMinRollToHit { get; }
    }

    public static class DiceRollThresholdResolver
    {
        public static DiceRollThresholdSnapshot Resolve(Actor actor)
        {
            var tm = RollModificationManager.GetThresholdManager();
            var config = GameConfiguration.Instance;
            int hit = tm.GetHitThreshold(actor);
            int combo = tm.GetComboThreshold(actor);
            int crit = tm.GetCriticalHitThreshold(actor);
            int critMiss = tm.GetCriticalMissThreshold(actor);
            int defaultHit = config.RollSystem.MissThreshold.Max > 0 ? config.RollSystem.MissThreshold.Max : 5;
            int defaultCombo = config.RollSystem.ComboThreshold.Min > 0 ? config.RollSystem.ComboThreshold.Min : 14;
            int defaultCrit = config.Combat.CriticalHitThreshold > 0 ? config.Combat.CriticalHitThreshold : 20;

            var pendingHud = actor is Character chHud ? ActionSelector.PeekPendingThresholdHudShifts(chHud) : default;
            var intSteps = actor is Character chInt
                ? IntelligenceMilestoneThresholdBonuses.GetSteps(chInt.GetEffectiveIntelligence())
                : default;

            int eqHit = 0, eqCombo = 0, eqCrit = 0;
            if (actor is Character chEq && chEq is not Enemy)
            {
                eqHit = chEq.Equipment.GetEquipmentStatBonus("HIT", chEq);
                eqCombo = chEq.Equipment.GetEquipmentStatBonus("COMBO", chEq);
                eqCrit = chEq.Equipment.GetEquipmentStatBonus("CRIT", chEq);
            }

            int comboRowShift = pendingHud.SharedAccuracy + pendingHud.ComboDelta + intSteps.ComboSteps + eqCombo;
            int hitRowShift = pendingHud.SharedAccuracy + pendingHud.HitDelta + intSteps.HitSteps + eqHit;
            int critRowShift = pendingHud.CritDelta + intSteps.CritSteps + eqCrit;
            int critMissRowShift = -pendingHud.CritMissDelta;

            int effectiveCrit = ResolveEffectiveThreshold(crit, critRowShift);
            int effectiveCombo = ResolveEffectiveThreshold(combo, comboRowShift);
            int effectiveHit = ResolveEffectiveThreshold(hit + 1, hitRowShift);
            int effectiveCritMiss = ResolveEffectiveThreshold(critMiss, critMissRowShift);

            return new DiceRollThresholdSnapshot(
                crit,
                combo,
                hit,
                critMiss,
                defaultCrit,
                defaultCombo,
                defaultHit,
                effectiveCrit,
                effectiveCombo,
                effectiveHit,
                effectiveCritMiss);
        }

        private static int ResolveEffectiveThreshold(int baseThreshold, int rowAccuracy)
        {
            ThresholdDisplayFormatting.GetThresholdValueWithAccuracyParts(baseThreshold, rowAccuracy, out int effective, out _, out _);
            return ThresholdDisplayFormatting.ClampDiceLadderDisplayValue(effective);
        }
    }
}
