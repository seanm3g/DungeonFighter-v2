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
        public double ConsumedMultiHitMod { get => _state.ConsumedMultiHitMod; set => _state.ConsumedMultiHitMod = value; }
        public double ConsumedAmpModPercent { get => _state.ConsumedAmpModPercent; set => _state.ConsumedAmpModPercent = value; }

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
        public void ApplySlow(double slowMultiplier, int duration) => _state.ApplySlow(slowMultiplier, duration);
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
            foreach (var group in bonuses.BonusGroups)
            {
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
        public List<ActionAttackBonusItem> GetAndConsumeTurnBonuses() => _state.GetAndConsumeTurnBonuses();
        public List<ActionAttackBonusItem> PeekTurnBonuses() => _state.PeekTurnBonuses();
        public void AddPendingActionBonuses(int slot, List<ActionAttackBonusItem> bonuses) => _state.AddPendingActionBonuses(slot, bonuses);
        public List<ActionAttackBonusItem> ConsumePendingActionBonusesForSlot(int slot) => _state.ConsumePendingActionBonusesForSlot(slot);
        public List<ActionAttackBonusItem> GetPendingActionBonusesForSlot(int slot) => _state.GetPendingActionBonusesForSlot(slot);
        public IEnumerable<int> GetPendingActionBonusSlots() => _state.GetPendingActionBonusSlots();
        public void ClearPendingActionBonuses() => _state.ClearPendingActionBonuses();
        public void AddPendingActionBonusesNextHeroRoll(List<ActionAttackBonusItem>? bonuses) => _state.AddPendingActionBonusesNextHeroRoll(bonuses);
        public void AccumulatePendingActionCadenceBank(List<ActionAttackBonusItem>? bonuses, int stackTimes = 1) => _state.AccumulatePendingActionCadenceBank(bonuses, stackTimes);
        public void EnqueuePendingActionCadenceLayer(List<ActionAttackBonusItem>? bonuses) => _state.EnqueuePendingActionCadenceLayer(bonuses);
        public bool HasPendingActionCadenceBank() => _state.HasPendingActionCadenceBank();
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
