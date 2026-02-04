using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Consolidated state for character effects: combo, roll/shield, reroll, action bonuses, next-attack.
    /// Merged from CharacterEffectsComboState, RollAndShieldState, RerollState, ActionBonusState, NextAttackState.
    /// </summary>
    internal class CharacterEffectsState
    {
        #region Combo
        public int ComboStep { get; set; }
        public double ComboAmplifier { get; set; } = 1.0;
        public int ComboBonus { get; set; }
        public int TempComboBonus { get; set; }
        public int TempComboBonusTurns { get; set; }
        public bool ComboModeActive { get; set; }
        public int LastComboActionIdx { get; set; } = -1;

        public void SetTempComboBonus(int bonus, int turns) { TempComboBonus = bonus; TempComboBonusTurns = turns; }
        public int ConsumeTempComboBonus()
        {
            int bonus = TempComboBonus;
            if (TempComboBonusTurns > 0) { TempComboBonusTurns--; if (TempComboBonusTurns == 0) TempComboBonus = 0; }
            return bonus;
        }
        public void ActivateComboMode() => ComboModeActive = true;
        public void DeactivateComboMode() => ComboModeActive = false;
        public void ResetCombo() { ComboStep = 0; ComboAmplifier = 1.0; LastComboActionIdx = -1; ComboModeActive = false; }
        public void ClearCombo() { ComboStep = 0; ComboAmplifier = 1.0; LastComboActionIdx = -1; ComboModeActive = false; TempComboBonus = 0; TempComboBonusTurns = 0; }
        #endregion

        #region Roll and shield
        public int TempRollBonus { get; set; }
        public int TempRollBonusTurns { get; set; }
        public int EnemyRollPenalty { get; set; }
        public int EnemyRollPenaltyTurns { get; set; }
        public double SlowMultiplier { get; set; } = 1.0;
        public int SlowTurns { get; set; }
        public bool HasShield { get; set; }
        public int LastShieldReduction { get; set; }
        public double LengthReduction { get; set; }
        public int LengthReductionTurns { get; set; }
        public double ComboAmplifierMultiplier { get; set; } = 1.0;
        public int ComboAmplifierTurns { get; set; }

        public void SetTempRollBonus(int bonus, int turns) { TempRollBonus = bonus; TempRollBonusTurns = turns; }
        public int GetTempRollBonus() => TempRollBonus;
        public int ConsumeTempRollBonus()
        {
            int bonus = TempRollBonus;
            if (TempRollBonusTurns > 0) { TempRollBonusTurns--; if (TempRollBonusTurns == 0) TempRollBonus = 0; }
            return bonus;
        }
        public void ApplySlow(double slowMultiplier, int duration) { SlowMultiplier = slowMultiplier; SlowTurns = duration; }
        public void ApplyShield() => HasShield = true;
        public bool ConsumeShield() { if (!HasShield) return false; HasShield = false; return true; }
        public void TickRollAndShield(double turnsPassed)
        {
            if (LengthReductionTurns > 0) { LengthReductionTurns = Math.Max(0, LengthReductionTurns - (int)Math.Ceiling(turnsPassed)); if (LengthReductionTurns == 0) LengthReduction = 0.0; }
            if (EnemyRollPenaltyTurns > 0) { EnemyRollPenaltyTurns = Math.Max(0, EnemyRollPenaltyTurns - (int)Math.Ceiling(turnsPassed)); if (EnemyRollPenaltyTurns == 0) EnemyRollPenalty = 0; }
            if (ComboAmplifierTurns > 0) { ComboAmplifierTurns = Math.Max(0, ComboAmplifierTurns - (int)Math.Ceiling(turnsPassed)); if (ComboAmplifierTurns == 0) ComboAmplifierMultiplier = 1.0; }
            if (SlowTurns > 0) { SlowTurns = Math.Max(0, SlowTurns - (int)Math.Ceiling(turnsPassed)); if (SlowTurns == 0) SlowMultiplier = 1.0; }
        }
        public void ClearRollAndShield()
        {
            TempRollBonus = 0; TempRollBonusTurns = 0; EnemyRollPenalty = 0; EnemyRollPenaltyTurns = 0;
            SlowMultiplier = 1.0; SlowTurns = 0; HasShield = false; LastShieldReduction = 0;
            LengthReduction = 0.0; LengthReductionTurns = 0; ComboAmplifierMultiplier = 1.0; ComboAmplifierTurns = 0;
        }
        #endregion

        #region Next attack
        public double NextAttackDamageMultiplier { get; set; } = 1.0;
        public int NextAttackStatBonus { get; set; }
        public string NextAttackStatBonusType { get; set; } = "";
        public int NextAttackStatBonusDuration { get; set; }

        public double ConsumeNextAttackDamageMultiplier() { var m = NextAttackDamageMultiplier; NextAttackDamageMultiplier = 1.0; return m; }
        public (int bonus, string statType, int duration) ConsumeNextAttackStatBonus()
        {
            var r = (NextAttackStatBonus, NextAttackStatBonusType, NextAttackStatBonusDuration);
            NextAttackStatBonus = 0; NextAttackStatBonusType = ""; NextAttackStatBonusDuration = 0;
            return r;
        }
        public void ClearNextAttack() { NextAttackDamageMultiplier = 1.0; NextAttackStatBonus = 0; NextAttackStatBonusType = ""; NextAttackStatBonusDuration = 0; }
        #endregion

        #region Reroll
        public int RerollCharges { get; set; }
        public bool UsedRerollThisTurn { get; set; }
        public int RerollChargesUsed { get; set; }

        public bool UseReroll() { if (RerollCharges <= 0 || UsedRerollThisTurn) return false; RerollCharges--; UsedRerollThisTurn = true; return true; }
        public void ResetRerollUsage() => UsedRerollThisTurn = false;
        public void ResetRerollCharges() => RerollChargesUsed = 0;
        public int GetRemainingRerollCharges(int totalRerollCharges) => Math.Max(0, totalRerollCharges - RerollChargesUsed);
        public bool UseRerollCharge() { if (RerollCharges <= 0) return false; RerollChargesUsed++; return true; }
        public void ClearReroll() { RerollCharges = 0; UsedRerollThisTurn = false; RerollChargesUsed = 0; }
        #endregion

        #region Action bonus
        public List<ActionAttackBonusGroup> AbilityBonuses { get; set; } = new List<ActionAttackBonusGroup>();
        public List<ActionAttackBonusGroup> ActionBonuses { get; set; } = new List<ActionAttackBonusGroup>();
        public double ConsumedDamageModPercent { get; set; }
        public double ConsumedSpeedModPercent { get; set; }
        public double ConsumedMultiHitMod { get; set; }
        public double ConsumedAmpModPercent { get; set; }

        public void AddActionAttackBonuses(ActionAttackBonuses? bonuses)
        {
            if (bonuses?.BonusGroups == null) return;
            foreach (var group in bonuses.BonusGroups)
            {
                if (group.Keyword == "ABILITY") AbilityBonuses.Add(group);
                else if (group.Keyword == "ACTION") ActionBonuses.Add(group);
            }
        }
        public void ClearConsumedModifierBonuses() { ConsumedDamageModPercent = 0; ConsumedSpeedModPercent = 0; ConsumedMultiHitMod = 0; ConsumedAmpModPercent = 0; }
        public void AccumulateConsumedModifierBonuses(List<ActionAttackBonusItem>? bonuses)
        {
            if (bonuses == null) return;
            foreach (var b in bonuses)
            {
                switch ((b.Type ?? "").ToUpper())
                {
                    case "SPEED_MOD": ConsumedSpeedModPercent += b.Value; break;
                    case "DAMAGE_MOD": ConsumedDamageModPercent += b.Value; break;
                    case "MULTIHIT_MOD": ConsumedMultiHitMod += b.Value; break;
                    case "AMP_MOD": ConsumedAmpModPercent += b.Value; break;
                }
            }
        }
        public void AddModifierBonusesFromAction(Action? action)
        {
            if (action == null) return;
            bool hasSpeed = !string.IsNullOrWhiteSpace(action.SpeedMod);
            bool hasDamage = !string.IsNullOrWhiteSpace(action.DamageMod);
            bool hasMultiHit = !string.IsNullOrWhiteSpace(action.MultiHitMod);
            bool hasAmp = !string.IsNullOrWhiteSpace(action.AmpMod);
            if (!hasSpeed && !hasDamage && !hasMultiHit && !hasAmp) return;
            string keyword = string.Equals((action.Cadence ?? "").Trim(), "Ability", StringComparison.OrdinalIgnoreCase) ? "ABILITY" : "ACTION";
            int count = action.Advanced?.StatBonusDuration > 0 ? action.Advanced.StatBonusDuration : (action.Advanced?.RollBonusDuration > 0 ? action.Advanced.RollBonusDuration : 1);
            if (count <= 0) count = 1;
            var bonuses = new List<ActionAttackBonusItem>();
            if (hasSpeed && ModifierParser.ParsePercent(action.SpeedMod) is { } sv) bonuses.Add(new ActionAttackBonusItem { Type = "SPEED_MOD", Value = sv * 100.0 });
            if (hasDamage && ModifierParser.ParsePercent(action.DamageMod) is { } dv) bonuses.Add(new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = dv * 100.0 });
            if (hasMultiHit && ModifierParser.ParseValue(action.MultiHitMod) is { } mv) bonuses.Add(new ActionAttackBonusItem { Type = "MULTIHIT_MOD", Value = mv });
            if (hasAmp && ModifierParser.ParsePercent(action.AmpMod) is { } av) bonuses.Add(new ActionAttackBonusItem { Type = "AMP_MOD", Value = av * 100.0 });
            if (bonuses.Count == 0) return;
            var group = new ActionAttackBonusGroup { Keyword = keyword, Count = count, Bonuses = bonuses };
            if (keyword == "ABILITY") AbilityBonuses.Add(group); else ActionBonuses.Add(group);
        }
        public List<ActionAttackBonusItem> GetAndConsumeAbilityBonuses(bool actionSucceeded)
        {
            var result = new List<ActionAttackBonusItem>();
            if (!actionSucceeded) return result;
            var toRemove = new List<ActionAttackBonusGroup>();
            foreach (var group in AbilityBonuses)
            {
                if (group.Count > 0 && group.Bonuses != null) { result.AddRange(group.Bonuses); group.Count--; if (group.Count <= 0) toRemove.Add(group); }
            }
            foreach (var g in toRemove) AbilityBonuses.Remove(g);
            return result;
        }
        public List<ActionAttackBonusItem> GetAndConsumeActionBonuses()
        {
            var result = new List<ActionAttackBonusItem>();
            var toRemove = new List<ActionAttackBonusGroup>();
            foreach (var group in ActionBonuses)
            {
                if (group.Count > 0 && group.Bonuses != null) { result.AddRange(group.Bonuses); group.Count--; if (group.Count <= 0) toRemove.Add(group); }
            }
            foreach (var g in toRemove) ActionBonuses.Remove(g);
            return result;
        }
        public List<ActionAttackBonusItem> PeekAbilityBonuses() { var r = new List<ActionAttackBonusItem>(); foreach (var g in AbilityBonuses) { if (g.Bonuses != null) r.AddRange(g.Bonuses); } return r; }
        public List<ActionAttackBonusItem> PeekActionBonuses() { var r = new List<ActionAttackBonusItem>(); foreach (var g in ActionBonuses) { if (g.Bonuses != null) r.AddRange(g.Bonuses); } return r; }
        public void ClearActionBonus() { AbilityBonuses.Clear(); ActionBonuses.Clear(); ConsumedDamageModPercent = 0; ConsumedSpeedModPercent = 0; ConsumedMultiHitMod = 0; ConsumedAmpModPercent = 0; }
        #endregion
    }
}
