using System;
using System.Collections.Generic;
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
        public double LengthReduction { get => _state.LengthReduction; set => _state.LengthReduction = value; }
        public int LengthReductionTurns { get => _state.LengthReductionTurns; set => _state.LengthReductionTurns = value; }
        public double ComboAmplifierMultiplier { get => _state.ComboAmplifierMultiplier; set => _state.ComboAmplifierMultiplier = value; }
        public int ComboAmplifierTurns { get => _state.ComboAmplifierTurns; set => _state.ComboAmplifierTurns = value; }

        // Next attack
        public double NextAttackDamageMultiplier { get => _state.NextAttackDamageMultiplier; set => _state.NextAttackDamageMultiplier = value; }
        public int NextAttackStatBonus { get => _state.NextAttackStatBonus; set => _state.NextAttackStatBonus = value; }
        public string NextAttackStatBonusType { get => _state.NextAttackStatBonusType; set => _state.NextAttackStatBonusType = value; }
        public int NextAttackStatBonusDuration { get => _state.NextAttackStatBonusDuration; set => _state.NextAttackStatBonusDuration = value; }

        // Reroll
        public int RerollCharges { get => _state.RerollCharges; set => _state.RerollCharges = value; }
        public bool UsedRerollThisTurn { get => _state.UsedRerollThisTurn; set => _state.UsedRerollThisTurn = value; }
        public int RerollChargesUsed { get => _state.RerollChargesUsed; set => _state.RerollChargesUsed = value; }

        // Action bonuses
        public List<ActionAttackBonusGroup> AbilityBonuses => _state.AbilityBonuses;
        public List<ActionAttackBonusGroup> ActionBonuses => _state.ActionBonuses;
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
        public double ConsumeNextAttackDamageMultiplier() => _state.ConsumeNextAttackDamageMultiplier();
        public (int bonus, string statType, int duration) ConsumeNextAttackStatBonus() => _state.ConsumeNextAttackStatBonus();
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
            _state.ClearNextAttack();
            _state.ClearReroll();
            _state.ClearActionBonus();
            ExtraDamage = 0;
            ExtraAttacks = 0;
            LastAction = null;
            SkipNextTurn = false;
            GuaranteeNextSuccess = false;
        }

        public void AddActionAttackBonuses(ActionAttackBonuses? bonuses) => _state.AddActionAttackBonuses(bonuses);
        public void ClearConsumedModifierBonuses() => _state.ClearConsumedModifierBonuses();
        public void AccumulateConsumedModifierBonuses(List<ActionAttackBonusItem> bonuses) => _state.AccumulateConsumedModifierBonuses(bonuses);
        public void AddModifierBonusesFromAction(Action? action) => _state.AddModifierBonusesFromAction(action);
        public List<ActionAttackBonusItem> GetAndConsumeAbilityBonuses(bool actionSucceeded) => _state.GetAndConsumeAbilityBonuses(actionSucceeded);
        public List<ActionAttackBonusItem> GetAndConsumeActionBonuses() => _state.GetAndConsumeActionBonuses();
        public List<ActionAttackBonusItem> PeekAbilityBonuses() => _state.PeekAbilityBonuses();
        public List<ActionAttackBonusItem> PeekActionBonuses() => _state.PeekActionBonuses();
        public bool UseReroll() => _state.UseReroll();
        public void ResetRerollUsage() => _state.ResetRerollUsage();
        public void ResetRerollCharges() => _state.ResetRerollCharges();
        public int GetRemainingRerollCharges(int totalRerollCharges) => _state.GetRemainingRerollCharges(totalRerollCharges);
        public bool UseRerollCharge() => _state.UseRerollCharge();
    }
}
