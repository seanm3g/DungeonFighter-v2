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

    public readonly struct ThresholdShiftParts
    {
        public PendingThresholdHudShifts Pending { get; init; }
        public int TechHitSteps { get; init; }
        public int TechComboSteps { get; init; }
        public int TechCritSteps { get; init; }
        public int NaiveteHitSteps { get; init; }
        public int EqHit { get; init; }
        public int EqCombo { get; init; }
        public int EqCrit { get; init; }
        public int DungeonHit { get; init; }
        public int DungeonCombo { get; init; }
        public int DungeonCrit { get; init; }
        public int DungeonCritMiss { get; init; }
        public int HitRowShift { get; init; }
        public int ComboRowShift { get; init; }
        public int CritRowShift { get; init; }
        public int CritMissRowShift { get; init; }
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

            var parts = ResolveShiftParts(actor, hit, combo, crit);

            int effectiveCrit = ResolveEffectiveThreshold(crit, parts.CritRowShift);
            int effectiveCombo = ResolveEffectiveThreshold(combo, parts.ComboRowShift);
            int effectiveHit = ResolveEffectiveThreshold(hit + 1, parts.HitRowShift);
            int effectiveCritMiss = ResolveEffectiveThreshold(critMiss, parts.CritMissRowShift);

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

        public static ThresholdShiftParts ResolveShiftParts(Actor actor)
        {
            var tm = RollModificationManager.GetThresholdManager();
            return ResolveShiftParts(
                actor,
                tm.GetHitThreshold(actor),
                tm.GetComboThreshold(actor),
                tm.GetCriticalHitThreshold(actor));
        }

        private static ThresholdShiftParts ResolveShiftParts(Actor actor, int hit, int combo, int crit)
        {
            var pendingHud = actor is Character chHud ? ActionSelector.PeekPendingThresholdHudShifts(chHud) : default;
            var techMilestoneSteps = actor is Character chTec
                ? TechniqueMilestoneThresholdBonuses.GetSteps(chTec.GetEffectiveTechnique())
                : default;
            int naiveteHitSteps = actor is Character chNav && chNav is not Enemy
                ? NaiveteBalanceHelper.GetHitSteps(chNav)
                : 0;

            int eqHit = 0, eqCombo = 0, eqCrit = 0;
            int dungeonHit = 0, dungeonCombo = 0, dungeonCrit = 0, dungeonCritMiss = 0;
            if (actor is Character chEq && chEq is not Enemy)
            {
                eqHit = chEq.Equipment.GetEquipmentStatBonus("HIT", chEq);
                eqCombo = chEq.Equipment.GetEquipmentStatBonus("COMBO", chEq);
                eqCrit = chEq.Equipment.GetEquipmentStatBonus("CRIT", chEq);
                var dungeonBuffs = chEq.DungeonSearchBuffs;
                dungeonHit = dungeonBuffs.HitThresholdAdjustment;
                dungeonCombo = dungeonBuffs.ComboThresholdAdjustment;
                dungeonCrit = dungeonBuffs.CritThresholdAdjustment;
                dungeonCritMiss = dungeonBuffs.CritMissThresholdAdjustment;
            }

            int comboRowShift = pendingHud.SharedAccuracy + pendingHud.ComboDelta + techMilestoneSteps.ComboSteps + eqCombo + dungeonCombo;
            int hitRowShift = pendingHud.SharedAccuracy + pendingHud.HitDelta + techMilestoneSteps.HitSteps + naiveteHitSteps + eqHit + dungeonHit;
            int critRowShift = pendingHud.CritDelta + techMilestoneSteps.CritSteps + eqCrit + dungeonCrit;
            // Crit-miss potions store the same delta passed to AdjustCriticalMissThreshold (add, not subtract).
            int critMissRowShift = -pendingHud.CritMissDelta - dungeonCritMiss;

            return new ThresholdShiftParts
            {
                Pending = pendingHud,
                TechHitSteps = techMilestoneSteps.HitSteps,
                TechComboSteps = techMilestoneSteps.ComboSteps,
                TechCritSteps = techMilestoneSteps.CritSteps,
                NaiveteHitSteps = naiveteHitSteps,
                EqHit = eqHit,
                EqCombo = eqCombo,
                EqCrit = eqCrit,
                DungeonHit = dungeonHit,
                DungeonCombo = dungeonCombo,
                DungeonCrit = dungeonCrit,
                DungeonCritMiss = dungeonCritMiss,
                HitRowShift = hitRowShift,
                ComboRowShift = comboRowShift,
                CritRowShift = critRowShift,
                CritMissRowShift = critMissRowShift
            };
        }

        private static int ResolveEffectiveThreshold(int baseThreshold, int rowAccuracy)
        {
            ThresholdDisplayFormatting.GetThresholdValueWithAccuracyParts(baseThreshold, rowAccuracy, out int effective, out _, out _);
            return ThresholdDisplayFormatting.ClampDiceLadderDisplayValue(effective);
        }
    }
}
