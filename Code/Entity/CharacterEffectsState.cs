using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>ATTACK cadence: consumed per roll, apply only on hit.</summary>
        public List<ActionAttackBonusGroup> AttackBonuses { get; set; } = new List<ActionAttackBonusGroup>();
        /// <summary>ACTION cadence: slot-based. Key = combo slot index; value = bonuses to apply when that slot executes.</summary>
        public Dictionary<int, List<ActionAttackBonusItem>> PendingActionBonusesBySlot { get; set; } = new Dictionary<int, List<ActionAttackBonusItem>>();
        /// <summary>
        /// ACTION cadence: FIFO layers. Each hero (or enemy, when fumble-routed) attack roll consumes exactly one layer.
        /// Duration N is stored as N consecutive identical layers (spreadsheet Count / combo bonus duration).
        /// </summary>
        public List<List<ActionAttackBonusItem>> PendingActionCadenceBonusLayers { get; set; } = new List<List<ActionAttackBonusItem>>();
        public double ConsumedDamageModPercent { get; set; }
        public double ConsumedSpeedModPercent { get; set; }
        public double ConsumedMultiHitMod { get; set; }
        public double ConsumedAmpModPercent { get; set; }
        /// <summary>ATTACK bonuses consumed this roll; apply stat bonuses on hit, then clear.</summary>
        public List<ActionAttackBonusItem> ConsumedAttackBonusesThisRoll { get; set; } = new List<ActionAttackBonusItem>();

        public void AddActionAttackBonuses(ActionAttackBonuses? bonuses)
        {
            if (bonuses?.BonusGroups == null) return;
            foreach (var group in bonuses.BonusGroups)
            {
                var ct = string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType;
                if (ct == "ABILITY") AbilityBonuses.Add(group);
                else if (ct == "ATTACK") AttackBonuses.Add(group);
                // ACTION cadence: queued in ActionExecutionFlow (PendingActionCadenceBonusLayers)
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
        /// <summary>
        /// Adds modifier bonuses (SpeedMod, DamageMod, etc.) from an action.
        /// When nextSlotForAbilityCadence is provided (Ability cadence in combo), adds to that slot instead of AbilityBonuses.
        /// </summary>
        public void AddModifierBonusesFromAction(Action? action, int? nextSlotForAbilityCadence = null)
        {
            if (action == null) return;
            bool hasSpeed = !string.IsNullOrWhiteSpace(action.SpeedMod);
            bool hasDamage = !string.IsNullOrWhiteSpace(action.DamageMod);
            bool hasMultiHit = !string.IsNullOrWhiteSpace(action.MultiHitMod);
            bool hasAmp = !string.IsNullOrWhiteSpace(action.AmpMod);
            if (!hasSpeed && !hasDamage && !hasMultiHit && !hasAmp) return;
            var bonuses = new List<ActionAttackBonusItem>();
            if (hasSpeed && ModifierParser.ParsePercent(action.SpeedMod) is { } sv) bonuses.Add(new ActionAttackBonusItem { Type = "SPEED_MOD", Value = sv * 100.0 });
            if (hasDamage && ModifierParser.ParsePercent(action.DamageMod) is { } dv) bonuses.Add(new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = dv * 100.0 });
            if (hasMultiHit && ModifierParser.ParseValue(action.MultiHitMod) is { } mv) bonuses.Add(new ActionAttackBonusItem { Type = "MULTIHIT_MOD", Value = mv });
            if (hasAmp && ModifierParser.ParsePercent(action.AmpMod) is { } av) bonuses.Add(new ActionAttackBonusItem { Type = "AMP_MOD", Value = av * 100.0 });
            if (bonuses.Count == 0) return;

            if (nextSlotForAbilityCadence.HasValue)
            {
                AddPendingActionBonuses(nextSlotForAbilityCadence.Value, bonuses);
                return;
            }

            string keyword = string.Equals((action.Cadence ?? "").Trim(), "Ability", StringComparison.OrdinalIgnoreCase) ? "ABILITY" : "ATTACK";
            int count = action.Advanced?.StatBonusDuration > 0 ? action.Advanced.StatBonusDuration : (action.Advanced?.RollBonusDuration > 0 ? action.Advanced.RollBonusDuration : 1);
            if (count <= 0) count = 1;
            var group = new ActionAttackBonusGroup { Keyword = keyword, CadenceType = keyword, Count = count, Bonuses = bonuses };
            if (keyword == "ABILITY") AbilityBonuses.Add(group); else AttackBonuses.Add(group);
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
        public List<ActionAttackBonusItem> GetAndConsumeActionBonuses() => GetAndConsumeAttackBonuses();
        public List<ActionAttackBonusItem> GetAndConsumeAttackBonuses()
        {
            var result = new List<ActionAttackBonusItem>();
            var toRemove = new List<ActionAttackBonusGroup>();
            foreach (var group in AttackBonuses)
            {
                if (group.Count > 0 && group.Bonuses != null) { result.AddRange(group.Bonuses); group.Count--; if (group.Count <= 0) toRemove.Add(group); }
            }
            foreach (var g in toRemove) AttackBonuses.Remove(g);
            return result;
        }
        public List<ActionAttackBonusItem> PeekAbilityBonuses() { var r = new List<ActionAttackBonusItem>(); foreach (var g in AbilityBonuses) { if (g.Bonuses != null) r.AddRange(g.Bonuses); } return r; }
        public List<ActionAttackBonusItem> PeekActionBonuses() => PeekAttackBonuses();
        public List<ActionAttackBonusItem> PeekAttackBonuses() { var r = new List<ActionAttackBonusItem>(); foreach (var g in AttackBonuses) { if (g.Bonuses != null) r.AddRange(g.Bonuses); } return r; }
        public void ClearActionBonus() { AbilityBonuses.Clear(); AttackBonuses.Clear(); PendingActionBonusesBySlot.Clear(); PendingActionCadenceBonusLayers.Clear(); ConsumedDamageModPercent = 0; ConsumedSpeedModPercent = 0; ConsumedMultiHitMod = 0; ConsumedAmpModPercent = 0; }
        public void AddPendingActionBonuses(int slot, List<ActionAttackBonusItem> bonuses)
        {
            if (bonuses == null || bonuses.Count == 0) return;
            if (!PendingActionBonusesBySlot.ContainsKey(slot)) PendingActionBonusesBySlot[slot] = new List<ActionAttackBonusItem>();
            PendingActionBonusesBySlot[slot].AddRange(bonuses);
        }
        public List<ActionAttackBonusItem> ConsumePendingActionBonusesForSlot(int slot)
        {
            if (!PendingActionBonusesBySlot.TryGetValue(slot, out var list) || list == null) return new List<ActionAttackBonusItem>();
            var result = new List<ActionAttackBonusItem>(list);
            PendingActionBonusesBySlot.Remove(slot);
            return result;
        }
        public List<ActionAttackBonusItem> GetPendingActionBonusesForSlot(int slot)
        {
            return PendingActionBonusesBySlot.TryGetValue(slot, out var list) ? new List<ActionAttackBonusItem>(list) : new List<ActionAttackBonusItem>();
        }
        public IEnumerable<int> GetPendingActionBonusSlots() => PendingActionBonusesBySlot.Keys;
        public void ClearPendingActionBonuses()
        {
            PendingActionBonusesBySlot.Clear();
            PendingActionCadenceBonusLayers.Clear();
        }

        /// <summary>Enqueues one application (one hero or enemy attack roll) of ACTION cadence bonuses.</summary>
        public void AddPendingActionBonusesNextHeroRoll(List<ActionAttackBonusItem>? bonuses) => EnqueuePendingActionCadenceLayer(bonuses);

        public void EnqueuePendingActionCadenceLayer(List<ActionAttackBonusItem>? bonuses)
        {
            if (bonuses == null || bonuses.Count == 0) return;
            PendingActionCadenceBonusLayers.Add(bonuses.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value }).ToList());
        }

        /// <summary>Peek the next roll's bonuses only (first FIFO layer).</summary>
        public List<ActionAttackBonusItem> PeekPendingActionBonusesNextHeroRoll()
        {
            if (PendingActionCadenceBonusLayers.Count == 0)
                return new List<ActionAttackBonusItem>();
            return new List<ActionAttackBonusItem>(PendingActionCadenceBonusLayers[0]);
        }

        public int GetPendingActionCadenceLayerCount() => PendingActionCadenceBonusLayers.Count;

        public List<ActionAttackBonusItem> ConsumePendingActionBonusesNextHeroRoll()
        {
            if (PendingActionCadenceBonusLayers.Count == 0)
                return new List<ActionAttackBonusItem>();
            var r = PendingActionCadenceBonusLayers[0];
            PendingActionCadenceBonusLayers.RemoveAt(0);
            return r;
        }
        public void SetConsumedAttackBonusesThisRoll(List<ActionAttackBonusItem> bonuses) { ConsumedAttackBonusesThisRoll = bonuses ?? new List<ActionAttackBonusItem>(); }
        public List<ActionAttackBonusItem> GetAndClearConsumedAttackBonusesThisRoll() { var r = ConsumedAttackBonusesThisRoll; ConsumedAttackBonusesThisRoll = new List<ActionAttackBonusItem>(); return r; }
        #endregion
    }
}
