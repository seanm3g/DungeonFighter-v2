using System;
using System.Collections.Generic;
using RPGGame.Actions;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Manages character status effects, debuffs, and temporary conditions.
    /// Delegates to a single consolidated state (combo, roll/shield, reroll, action bonuses, next-attack).
    /// </summary>
    public class CharacterEffects
    {
        private readonly CharacterEffectsState _state = new CharacterEffectsState();

        /// <summary>The character these effects belong to (set by <see cref="Character"/>), so cadence tag gates see its tag aliases.</summary>
        public Character? Owner { get => _state.Owner; set => _state.Owner = value; }

        // Combo
        public int ComboStep { get => _state.ComboStep; set => _state.ComboStep = value; }
        public double ComboAmplifier { get => _state.ComboAmplifier; set => _state.ComboAmplifier = value; }
        public int ComboBonus { get => _state.ComboBonus; set => _state.ComboBonus = value; }
        public int TempComboBonus { get => _state.TempComboBonus; set => _state.TempComboBonus = value; }
        public int TempComboBonusTurns { get => _state.TempComboBonusTurns; set => _state.TempComboBonusTurns = value; }
        public bool ComboModeActive { get => _state.ComboModeActive; set => _state.ComboModeActive = value; }
        public int LastComboActionIdx { get => _state.LastComboActionIdx; set => _state.LastComboActionIdx = value; }

        // Roll and shield
        public int TempRollBonus { get => _state.TempRollBonus; set => _state.TempRollBonus = value; }
        public int TempRollBonusTurns { get => _state.TempRollBonusTurns; set => _state.TempRollBonusTurns = value; }
        public int EnemyRollPenalty { get => _state.EnemyRollPenalty; set => _state.EnemyRollPenalty = value; }
        public int EnemyRollPenaltyTurns { get => _state.EnemyRollPenaltyTurns; set => _state.EnemyRollPenaltyTurns = value; }
        public double SlowMultiplier { get => _state.SlowMultiplier; set => _state.SlowMultiplier = value; }
        public int SlowTurns { get => _state.SlowTurns; set => _state.SlowTurns = value; }
        public bool HasShield { get => _state.HasShield; set => _state.HasShield = value; }
        public int LastShieldReduction { get => _state.LastShieldReduction; set => _state.LastShieldReduction = value; }
        public int LastArmorAbsorbed { get => _state.LastArmorAbsorbed; set => _state.LastArmorAbsorbed = value; }
        public double LengthReduction { get => _state.LengthReduction; set => _state.LengthReduction = value; }
        public int LengthReductionTurns { get => _state.LengthReductionTurns; set => _state.LengthReductionTurns = value; }
        public double ComboAmplifierMultiplier { get => _state.ComboAmplifierMultiplier; set => _state.ComboAmplifierMultiplier = value; }
        public int ComboAmplifierTurns { get => _state.ComboAmplifierTurns; set => _state.ComboAmplifierTurns = value; }

        // Next turn (legacy one-shot)
        public double NextTurnDamageMultiplier { get => _state.NextTurnDamageMultiplier; set => _state.NextTurnDamageMultiplier = value; }
        public int NextTurnStatBonus { get => _state.NextTurnStatBonus; set => _state.NextTurnStatBonus = value; }
        public string NextTurnStatBonusType { get => _state.NextTurnStatBonusType; set => _state.NextTurnStatBonusType = value; }
        public int NextTurnStatBonusDuration { get => _state.NextTurnStatBonusDuration; set => _state.NextTurnStatBonusDuration = value; }

        // Reroll
        public int RerollCharges { get => _state.RerollCharges; set => _state.RerollCharges = value; }
        public bool UsedRerollThisTurn { get => _state.UsedRerollThisTurn; set => _state.UsedRerollThisTurn = value; }
        public int RerollChargesUsed { get => _state.RerollChargesUsed; set => _state.RerollChargesUsed = value; }

        // Cadence bonuses (TURN / ACTION)
        public List<ActionAttackBonusGroup> TurnBonuses => _state.TurnBonuses;
        public double ConsumedDamageModPercent { get => _state.ConsumedDamageModPercent; set => _state.ConsumedDamageModPercent = value; }
        public double ConsumedSpeedModPercent { get => _state.ConsumedSpeedModPercent; set => _state.ConsumedSpeedModPercent = value; }
        public double LastSwingSpeedModPercent { get => _state.LastSwingSpeedModPercent; set => _state.LastSwingSpeedModPercent = value; }

        /// <summary>Returns and clears <see cref="LastSwingSpeedModPercent"/> (read once when the swing's duration is set).</summary>
        public double TakeLastSwingSpeedModPercent()
        {
            double v = _state.LastSwingSpeedModPercent;
            _state.LastSwingSpeedModPercent = 0;
            return v;
        }
        public double LastSwingWeaponSpeedFlat { get => _state.LastSwingWeaponSpeedFlat; set => _state.LastSwingWeaponSpeedFlat = value; }

        /// <summary>Returns and clears <see cref="LastSwingWeaponSpeedFlat"/> (read once when the swing's duration is set).</summary>
        public double TakeLastSwingWeaponSpeedFlat()
        {
            double v = _state.LastSwingWeaponSpeedFlat;
            _state.LastSwingWeaponSpeedFlat = 0;
            return v;
        }
        public double ConsumedMultiHitMod { get => _state.ConsumedMultiHitMod; set => _state.ConsumedMultiHitMod = value; }
        public double ConsumedAmpModPercent { get => _state.ConsumedAmpModPercent; set => _state.ConsumedAmpModPercent = value; }
        public double ConsumedSkillAmpPercent { get => _state.ConsumedSkillAmpPercent; set => _state.ConsumedSkillAmpPercent = value; }
        public double ConsumedWeaponDamageFlat { get => _state.ConsumedWeaponDamageFlat; set => _state.ConsumedWeaponDamageFlat = value; }
        public double ConsumedWeaponSpeedFlat { get => _state.ConsumedWeaponSpeedFlat; set => _state.ConsumedWeaponSpeedFlat = value; }

        // Advanced action mechanics (on facade)
        public Action? LastAction { get; set; }
        public bool SkipNextTurn { get; set; }
        public bool GuaranteeNextSuccess { get; set; }
        public int ExtraAttacks { get; set; }
        public int ExtraDamage { get; set; }

        public void UpdateTempEffects(double actionLength = 1.0)
        {
            double turnsPassed = actionLength / Character.DEFAULT_ACTION_LENGTH;
            _state.TickRollAndShield(turnsPassed);
            if (ExtraDamage > 0) ExtraDamage = Math.Max(0, ExtraDamage - (int)Math.Ceiling(turnsPassed));
            _state.ResetRerollUsage();
        }

        public void SetTempComboBonus(int bonus, int turns) => _state.SetTempComboBonus(bonus, turns);
        public int ConsumeTempComboBonus() => _state.ConsumeTempComboBonus();
        public void SetTempRollBonus(int bonus, int turns) => _state.SetTempRollBonus(bonus, turns);
        public int GetTempRollBonus() => _state.GetTempRollBonus();
        public int ConsumeTempRollBonus() => _state.ConsumeTempRollBonus();
        public double ConsumeNextTurnDamageMultiplier() => _state.ConsumeNextTurnDamageMultiplier();
        public (int bonus, string statType, int duration) ConsumeNextTurnStatBonus() => _state.ConsumeNextTurnStatBonus();
        public void ActivateComboMode() => _state.ActivateComboMode();
        public void DeactivateComboMode() => _state.DeactivateComboMode();
        public void ResetCombo() => _state.ResetCombo();
        public void ClearCombo() => _state.ClearCombo();

        public int GetMaterialKeyword(string keyword) => _state.GetMaterialKeyword(keyword);
        public void AddMaterialKeyword(string keyword, int amount) => _state.AddMaterialKeyword(keyword, amount);
        public void SetMaterialKeywordSource(string keyword, string source) => _state.SetMaterialKeywordSource(keyword, source);
        public string GetMaterialKeywordSource(string keyword) => _state.GetMaterialKeywordSource(keyword);
        public void ClearMaterialKeywordBank() => _state.ClearMaterialKeywordBank();
        public List<(string Keyword, int Count)> GetMaterialKeywordCounts() => _state.GetMaterialKeywordCounts();
        public int MaterialConsecutiveConnects
        {
            get => _state.MaterialConsecutiveConnects;
            set => _state.MaterialConsecutiveConnects = value;
        }
        public void ApplySlow(double speedReduction, int duration) => _state.ApplySlow(speedReduction, duration);
        public void ApplyShield() => _state.ApplyShield();
        public bool ConsumeShield() => _state.ConsumeShield();

        public void ClearAllTempEffects()
        {
            _state.ClearRollAndShield();
            _state.ClearCombo();
            _state.ClearNextTurn();
            _state.ClearReroll();
            _state.ClearActionBonus();
            ExtraDamage = 0;
            ExtraAttacks = 0;
            LastAction = null;
            SkipNextTurn = false;
            GuaranteeNextSuccess = false;
        }

        public void AddActionAttackBonuses(ActionAttackBonuses? bonuses, Action? sourceAction = null, Character? owner = null)
        {
            if (bonuses?.BonusGroups == null) return;

            var turnOnly = new ActionAttackBonuses();
            foreach (var rawGroup in bonuses.BonusGroups)
            {
                var group = ApplySourceSlotTiming(rawGroup, sourceAction, owner);
                if (group == null)
                    continue;
                string scope = CadenceKeywords.ResolveScope(
                    string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType,
                    group.DurationType);
                if (owner != null && (CadenceKeywords.IsFight(scope) || CadenceKeywords.IsDungeon(scope)))
                {
                    int stackTimes = ResolveGrantStackTimes(group, sourceAction);
                    if (group.Bonuses != null && group.Bonuses.Count > 0)
                        CadenceScopedBuffApplicator.DepositToScope(owner, scope, group.Bonuses, stackTimes);
                    continue;
                }

                var ct = CadenceKeywords.NormalizeCadenceType(
                    string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType);
                if (CadenceKeywords.IsTurn(ct))
                    turnOnly.BonusGroups.Add(group);
            }

            if (turnOnly.BonusGroups.Count > 0)
                _state.AddActionAttackBonuses(turnOnly, sourceAction);
        }

        /// <summary>
        /// Source-slot timing (<see cref="CadenceTiming.SlotIf"/>, <see cref="CadenceTiming.PerSlot"/>) for a TURN / FIGHT /
        /// DUNGEON grant: null when the action's strip slot fails <c>slotIf</c>; a scaled copy for <c>perSlot</c>.
        /// </summary>
        internal static ActionAttackBonusGroup? ApplySourceSlotTiming(ActionAttackBonusGroup? group, Action? sourceAction, Character? owner)
        {
            var timing = group?.Timing;
            if (group == null || timing == null
                || (string.IsNullOrWhiteSpace(timing.SlotIf) && !timing.PerSlot && string.IsNullOrWhiteSpace(timing.PerTag)))
                return group;
            var (slot0, count) = SourceSlot(sourceAction, owner);
            if (!timing.SlotIfHolds(slot0, count))
                return null;
            // perTag: x strip actions with the tag (no owner = no strip known, unscaled).
            double tagFactor = owner != null ? timing.PerTagFactor(ActionUtilities.GetComboActions(owner), owner) : 1;
            if (tagFactor <= 0)
                return null;
            double slotFactor = timing.PerSlot && slot0 >= 0 ? slot0 + 1 : 1;
            if (slotFactor * tagFactor == 1)
                return group;
            return new ActionAttackBonusGroup
            {
                Keyword = group.Keyword,
                CadenceType = group.CadenceType,
                Count = group.Count,
                DurationType = group.DurationType,
                Timing = group.Timing,
                Bonuses = CadenceTiming.Scale(group.Bonuses, slotFactor * tagFactor)
            };
        }

        /// <summary>0-based strip slot of <paramref name="sourceAction"/> on <paramref name="owner"/>'s combo strip, and the strip length.</summary>
        internal static (int Slot0, int Count) SourceSlot(Action? sourceAction, Character? owner)
        {
            if (owner == null || sourceAction == null)
                return (-1, 0);
            var combo = ActionUtilities.GetComboActions(owner);
            if (combo.Count == 0)
                return (-1, 0);
            int idx = ActionUtilities.TryGetComboActionSlotIndex(sourceAction, combo);
            return (idx, combo.Count);
        }

        private static int ResolveGrantStackTimes(ActionAttackBonusGroup group, Action? sourceAction)
        {
            int count = group.Count;
            if (sourceAction != null && ActionCadenceDurationResolver.IsKeywordCadenceGroup(group))
            {
                int requested = ActionCadenceDurationResolver.GetRequestedLayerCount(sourceAction, group);
                if (requested > 0)
                    count = requested;
            }
            return count > 0 ? count : 1;
        }
        public void ClearConsumedModifierBonuses() => _state.ClearConsumedModifierBonuses();
        public void AccumulateConsumedModifierBonuses(List<ActionAttackBonusItem> bonuses) => _state.AccumulateConsumedModifierBonuses(bonuses);
        public void AddModifierBonusesFromAction(Action? action, int? nextComboSlot = null, bool useEnemySpreadsheetMods = false, Character? owner = null) => _state.AddModifierBonusesFromAction(action, nextComboSlot, useEnemySpreadsheetMods, owner);
        public List<ActionAttackBonusItem> GetAndConsumeTurnBonuses(Action? action = null) => _state.GetAndConsumeTurnBonuses(action);
        public List<ActionAttackBonusItem> PeekTurnBonuses(Action? action = null) => _state.PeekTurnBonuses(action);
        public void BreakTurnBonusesOnMiss() => _state.BreakTurnBonusesOnMiss();
        public List<ActionAttackBonusItem> ReleaseQueuedBonuses(double multiplier, int currentSlot) =>
            _state.ReleaseQueuedBonuses(multiplier, currentSlot);
        public ActionAttackBonusGroup[] SnapshotTurnBonuses() => _state.SnapshotTurnBonuses();
        public void AddPendingActionBonuses(int slot, List<ActionAttackBonusItem> bonuses) => _state.AddPendingActionBonuses(slot, bonuses);
        public List<ActionAttackBonusItem> ConsumePendingActionBonusesForSlot(int slot) => _state.ConsumePendingActionBonusesForSlot(slot);
        public List<ActionAttackBonusItem> GetPendingActionBonusesForSlot(int slot) => _state.GetPendingActionBonusesForSlot(slot);
        public IEnumerable<int> GetPendingActionBonusSlots() => _state.GetPendingActionBonusSlots();
        public void ClearPendingActionBonuses() => _state.ClearPendingActionBonuses();
        public int PendingActionCadencePreviewSlot
        {
            get => _state.PendingActionCadencePreviewSlot;
            set => _state.PendingActionCadencePreviewSlot = value;
        }
        public void AddPendingActionBonusesNextHeroRoll(List<ActionAttackBonusItem>? bonuses, int? previewSlot = null) =>
            _state.AddPendingActionBonusesNextHeroRoll(bonuses, previewSlot);
        public void AccumulatePendingActionCadenceBank(List<ActionAttackBonusItem>? bonuses, int stackTimes = 1, int? previewSlot = null) =>
            _state.AccumulatePendingActionCadenceBank(bonuses, stackTimes, previewSlot);
        public void EnqueuePendingActionCadenceLayer(List<ActionAttackBonusItem>? bonuses, int? previewSlot = null) =>
            _state.EnqueuePendingActionCadenceLayer(bonuses, previewSlot);
        public bool HasPendingActionCadenceBank() => _state.HasPendingActionCadenceBank();
        public int GetActionCadenceBankPaintSlot(int comboStepFallback, int actionCount) =>
            _state.GetActionCadenceBankPaintSlot(comboStepFallback, actionCount);
        public bool SlotShowsActionCadenceBank(int comboSlot, int comboStepFallback, int actionCount) =>
            _state.SlotShowsActionCadenceBank(comboSlot, comboStepFallback, actionCount);
        public bool ShouldPeekActionCadenceBankForExecutedSlot(int comboSlot) =>
            _state.ShouldPeekActionCadenceBankForExecutedSlot(comboSlot);
        public int GetPendingActionCadenceLayerCount() => _state.GetPendingActionCadenceLayerCount();
        public List<ActionAttackBonusItem> PeekPendingActionBonusesNextHeroRoll() => _state.PeekPendingActionBonusesNextHeroRoll();
        public List<ActionAttackBonusItem> PeekPendingActionCadenceLayerAt(int layerIndex) => _state.PeekPendingActionCadenceLayerAt(layerIndex);
        public List<ActionAttackBonusItem> ConsumePendingActionBonusesNextHeroRoll() => _state.ConsumePendingActionBonusesNextHeroRoll();
        public void SetConsumedTurnBonusesThisRoll(List<ActionAttackBonusItem> bonuses) => _state.SetConsumedTurnBonusesThisRoll(bonuses);
        public List<ActionAttackBonusItem> GetAndClearConsumedTurnBonusesThisRoll() => _state.GetAndClearConsumedTurnBonusesThisRoll();
        public bool UseReroll() => _state.UseReroll();
        public void ResetRerollUsage() => _state.ResetRerollUsage();
        public void ResetRerollCharges() => _state.ResetRerollCharges();
        public int GetRemainingRerollCharges(int totalRerollCharges) => _state.GetRemainingRerollCharges(totalRerollCharges);
        public bool UseRerollCharge() => _state.UseRerollCharge();
    }
}
