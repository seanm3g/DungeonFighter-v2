using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions;
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
        public int LastArmorAbsorbed { get; set; }
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
            SlowMultiplier = 1.0; SlowTurns = 0; HasShield = false; LastShieldReduction = 0; LastArmorAbsorbed = 0;
            LengthReduction = 0.0; LengthReductionTurns = 0; ComboAmplifierMultiplier = 1.0; ComboAmplifierTurns = 0;
        }
        #endregion

        #region Next turn (legacy one-shot modifiers)
        public double NextTurnDamageMultiplier { get; set; } = 1.0;
        public int NextTurnStatBonus { get; set; }
        public string NextTurnStatBonusType { get; set; } = "";
        public int NextTurnStatBonusDuration { get; set; }

        public double ConsumeNextTurnDamageMultiplier() { var m = NextTurnDamageMultiplier; NextTurnDamageMultiplier = 1.0; return m; }
        public (int bonus, string statType, int duration) ConsumeNextTurnStatBonus()
        {
            var r = (NextTurnStatBonus, NextTurnStatBonusType, NextTurnStatBonusDuration);
            NextTurnStatBonus = 0; NextTurnStatBonusType = ""; NextTurnStatBonusDuration = 0;
            return r;
        }
        public void ClearNextTurn() { NextTurnDamageMultiplier = 1.0; NextTurnStatBonus = 0; NextTurnStatBonusType = ""; NextTurnStatBonusDuration = 0; }
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

        #region Cadence bonuses (TURN / ACTION)
        /// <summary>TURN cadence: consumed per roll, apply stat portions only on hit.</summary>
        public List<ActionAttackBonusGroup> TurnBonuses { get; set; } = new List<ActionAttackBonusGroup>();
        /// <summary>ACTION cadence: slot-based. Key = combo slot index; value = bonuses to apply when that slot executes.</summary>
        public Dictionary<int, List<ActionAttackBonusItem>> PendingActionBonusesBySlot { get; set; } = new Dictionary<int, List<ActionAttackBonusItem>>();
        /// <summary>
        /// ACTION cadence: additive bank. Multiple deposits sum by bonus type; the full bank redeems on the next hit+combo.
        /// Miss and non-combo hits leave the bank pending (strip stays updated until a combo lands).
        /// Cleared when the room ends (<see cref="ClearActionBonus"/>).
        /// </summary>
        public List<ActionAttackBonusItem> PendingActionCadenceBonusBank { get; set; } = new List<ActionAttackBonusItem>();
        /// <summary>Number of deposit events stacked into <see cref="PendingActionCadenceBonusBank"/> (for HUD).</summary>
        public int PendingActionCadenceDepositCount { get; set; }
        public double ConsumedDamageModPercent { get; set; }
        public double ConsumedSpeedModPercent { get; set; }
        public double ConsumedMultiHitMod { get; set; }
        public double ConsumedAmpModPercent { get; set; }
        /// <summary>TURN bonuses consumed this roll; apply stat bonuses on hit, then clear.</summary>
        public List<ActionAttackBonusItem> ConsumedTurnBonusesThisRoll { get; set; } = new List<ActionAttackBonusItem>();

        public void AddActionAttackBonuses(ActionAttackBonuses? bonuses, Action? sourceAction = null)
        {
            if (bonuses?.BonusGroups == null) return;
            foreach (var group in bonuses.BonusGroups)
            {
                var ct = CadenceKeywords.NormalizeCadenceType(
                    string.IsNullOrEmpty(group.CadenceType) ? group.Keyword : group.CadenceType);
                var cloned = CloneBonusGroup(group, sourceAction);
                cloned.CadenceType = ct;
                cloned.Keyword = ct;
                if (CadenceKeywords.IsTurn(ct))
                    TurnBonuses.Add(cloned);
                // ACTION cadence: deposited in ActionExecutionFlow (PendingActionCadenceBonusBank)
            }
        }

        private static ActionAttackBonusGroup CloneBonusGroup(ActionAttackBonusGroup group, Action? sourceAction)
        {
            int count = group.Count;
            if (sourceAction != null && ActionCadenceDurationResolver.IsKeywordCadenceGroup(group))
            {
                int requested = ActionCadenceDurationResolver.GetRequestedLayerCount(sourceAction, group);
                if (requested > 0)
                    count = requested;
            }
            if (count <= 0)
                count = 1;

            return new ActionAttackBonusGroup
            {
                Keyword = group.Keyword,
                CadenceType = group.CadenceType,
                Count = count,
                DurationType = group.DurationType,
                Bonuses = group.Bonuses == null
                    ? new List<ActionAttackBonusItem>()
                    : group.Bonuses.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value }).ToList()
            };
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
        /// <summary>Builds SPEED_MOD / DAMAGE_MOD / MULTIHIT_MOD / AMP_MOD items from hero (AJ–AM) or enemy (AD–AG) fields on <paramref name="action"/>.</summary>
        internal static List<ActionAttackBonusItem> BuildModifierBonusesFromActionFields(Action? action, bool useEnemySpreadsheetMods)
        {
            var bonuses = new List<ActionAttackBonusItem>();
            if (action == null) return bonuses;
            string speed = useEnemySpreadsheetMods ? action.EnemySpeedMod : action.SpeedMod;
            string damage = useEnemySpreadsheetMods ? action.EnemyDamageMod : action.DamageMod;
            string multi = useEnemySpreadsheetMods ? action.EnemyMultiHitMod : action.MultiHitMod;
            string amp = useEnemySpreadsheetMods ? action.EnemyAmpMod : action.AmpMod;
            bool hasSpeed = !string.IsNullOrWhiteSpace(speed);
            bool hasDamage = !string.IsNullOrWhiteSpace(damage);
            bool hasMultiHit = !string.IsNullOrWhiteSpace(multi);
            bool hasAmp = !string.IsNullOrWhiteSpace(amp);
            if (hasSpeed && ModifierParser.ParsePercent(speed) is { } sv) bonuses.Add(new ActionAttackBonusItem { Type = "SPEED_MOD", Value = sv * 100.0 });
            if (hasDamage && ModifierParser.ParsePercent(damage) is { } dv) bonuses.Add(new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = dv * 100.0 });
            if (hasMultiHit && ModifierParser.ParseValue(multi) is { } mv) bonuses.Add(new ActionAttackBonusItem { Type = "MULTIHIT_MOD", Value = mv });
            if (hasAmp && ModifierParser.ParsePercent(amp) is { } av) bonuses.Add(new ActionAttackBonusItem { Type = "AMP_MOD", Value = av * 100.0 });
            return bonuses;
        }

        /// <summary>
        /// Adds modifier bonuses from an action (hero fields by default; enemy AD–AG when <paramref name="useEnemySpreadsheetMods"/>).
        /// When <paramref name="nextComboSlot"/> is provided, adds to that combo slot pending queue.
        /// </summary>
        public void AddModifierBonusesFromAction(Action? action, int? nextComboSlot = null, bool useEnemySpreadsheetMods = false, Character? owner = null)
        {
            if (action == null) return;
            if (!useEnemySpreadsheetMods && CadenceKeywords.IsAction(action.Cadence) && !nextComboSlot.HasValue)
                return;
            var bonuses = BuildModifierBonusesFromActionFields(action, useEnemySpreadsheetMods);
            if (bonuses.Count == 0) return;

            if (nextComboSlot.HasValue)
            {
                AddPendingActionBonuses(nextComboSlot.Value, bonuses);
                return;
            }

            if (owner != null && (CadenceKeywords.IsFight(action.Cadence) || CadenceKeywords.IsDungeon(action.Cadence)))
            {
                int stackTimes = action.ComboBonusDuration > 0 ? action.ComboBonusDuration : 1;
                CadenceScopedBuffApplicator.DepositToScope(owner, CadenceKeywords.Normalize(action.Cadence), bonuses, stackTimes);
                return;
            }

            int count = action.Advanced?.StatBonusDuration > 0 ? action.Advanced.StatBonusDuration : (action.Advanced?.RollBonusDuration > 0 ? action.Advanced.RollBonusDuration : 1);
            if (count <= 0) count = 1;
            var group = new ActionAttackBonusGroup
            {
                Keyword = CadenceKeywords.Turn,
                CadenceType = CadenceKeywords.Turn,
                Count = count,
                Bonuses = bonuses
            };
            TurnBonuses.Add(group);
        }
        public List<ActionAttackBonusItem> GetAndConsumeTurnBonuses()
        {
            var result = new List<ActionAttackBonusItem>();
            var toRemove = new List<ActionAttackBonusGroup>();
            foreach (var group in TurnBonuses)
            {
                if (group.Count > 0 && group.Bonuses != null) { result.AddRange(group.Bonuses); group.Count--; if (group.Count <= 0) toRemove.Add(group); }
            }
            foreach (var g in toRemove) TurnBonuses.Remove(g);
            return result;
        }
        public List<ActionAttackBonusItem> PeekTurnBonuses() { var r = new List<ActionAttackBonusItem>(); foreach (var g in TurnBonuses) { if (g.Bonuses != null) r.AddRange(g.Bonuses); } return r; }
        public void ClearActionBonus()
        {
            TurnBonuses.Clear();
            PendingActionBonusesBySlot.Clear();
            PendingActionCadenceBonusBank.Clear();
            PendingActionCadenceDepositCount = 0;
            ConsumedDamageModPercent = 0;
            ConsumedSpeedModPercent = 0;
            ConsumedMultiHitMod = 0;
            ConsumedAmpModPercent = 0;
        }
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
            PendingActionCadenceBonusBank.Clear();
            PendingActionCadenceDepositCount = 0;
        }

        /// <summary>Adds ACTION cadence bonuses to the additive bank (one deposit).</summary>
        public void AddPendingActionBonusesNextHeroRoll(List<ActionAttackBonusItem>? bonuses) =>
            AccumulatePendingActionCadenceBank(bonuses, 1);

        /// <summary>Legacy name: one additive deposit.</summary>
        public void EnqueuePendingActionCadenceLayer(List<ActionAttackBonusItem>? bonuses) =>
            AccumulatePendingActionCadenceBank(bonuses, 1);

        /// <summary>
        /// Merges bonuses into the ACTION bank. <paramref name="stackTimes"/> multiplies each value
        /// (spreadsheet duration on a single grant).
        /// </summary>
        public void AccumulatePendingActionCadenceBank(List<ActionAttackBonusItem>? bonuses, int stackTimes = 1)
        {
            if (bonuses == null || bonuses.Count == 0 || stackTimes < 1) return;
            ActionCadenceBonusBank.MergeAdditively(PendingActionCadenceBonusBank, bonuses, stackTimes);
            PendingActionCadenceDepositCount += stackTimes;
        }

        public bool HasPendingActionCadenceBank() => PendingActionCadenceBonusBank.Count > 0;

        /// <summary>Peek the full additive ACTION bank (roll help until hit+combo redeems; miss keeps pending).</summary>
        public List<ActionAttackBonusItem> PeekPendingActionBonusesNextHeroRoll() =>
            ActionCadenceBonusBank.Copy(PendingActionCadenceBonusBank);

        /// <summary>Index 0 returns the bank; higher indices are empty (legacy FIFO API).</summary>
        public List<ActionAttackBonusItem> PeekPendingActionCadenceLayerAt(int layerIndex)
        {
            if (layerIndex != 0) return new List<ActionAttackBonusItem>();
            return PeekPendingActionBonusesNextHeroRoll();
        }

        /// <summary>Deposit count for HUD (how many times the bank was charged).</summary>
        public int GetPendingActionCadenceLayerCount() => PendingActionCadenceDepositCount;

        /// <summary>Redeems the full ACTION bank on hit+combo.</summary>
        public List<ActionAttackBonusItem> ConsumePendingActionBonusesNextHeroRoll()
        {
            var result = ActionCadenceBonusBank.Copy(PendingActionCadenceBonusBank);
            PendingActionCadenceBonusBank.Clear();
            PendingActionCadenceDepositCount = 0;
            return result;
        }
        public void SetConsumedTurnBonusesThisRoll(List<ActionAttackBonusItem> bonuses) { ConsumedTurnBonusesThisRoll = bonuses ?? new List<ActionAttackBonusItem>(); }
        public List<ActionAttackBonusItem> GetAndClearConsumedTurnBonusesThisRoll() { var r = ConsumedTurnBonusesThisRoll; ConsumedTurnBonusesThisRoll = new List<ActionAttackBonusItem>(); return r; }

        private static double SumBonusValuesOfType(IEnumerable<ActionAttackBonusItem>? items, string typeUpper)
        {
            if (items == null)
                return 0;
            double sum = 0;
            foreach (var b in items)
            {
                if (string.Equals(b.Type, typeUpper, StringComparison.OrdinalIgnoreCase))
                    sum += b.Value;
            }
            return sum;
        }

        /// <summary>
        /// HUD / tooltips: AMP_MOD (percent points) that will apply on the hero's next damage roll
        /// (same sources as <see cref="ActionExecutionFlow"/> roll prep: slot pending + FIFO + TURN peeks).
        /// </summary>
        public static double PeekSheetAmpModPercentQueuedForNextHeroDamageRoll(Character character)
        {
            double sum = 0;
            var comboActions = character.GetComboActions();
            if (comboActions != null && comboActions.Count > 0)
            {
                int slot = character.ComboStep % comboActions.Count;
                sum += SumBonusValuesOfType(character.Effects.GetPendingActionBonusesForSlot(slot), "AMP_MOD");
            }
            sum += SumBonusValuesOfType(character.Effects.PeekPendingActionBonusesNextHeroRoll(), "AMP_MOD");
            sum += SumBonusValuesOfType(character.Effects.PeekTurnBonuses(), "AMP_MOD");
            sum += SumBonusValuesOfType(character.FightCadenceBuffs.CopyBonuses(), "AMP_MOD");
            sum += SumBonusValuesOfType(character.DungeonCadenceBuffs.CopyBonuses(), "AMP_MOD");
            return sum;
        }

        /// <summary>HUD: DAMAGE_MOD (percent points) on the enemy's next attack-roll FIFO head.</summary>
        public static double PeekSheetDamageModPercentQueuedForNextEnemyAttack(Enemy enemy) =>
            SumBonusValuesOfType(enemy.Effects.PeekPendingActionBonusesNextHeroRoll(), "DAMAGE_MOD");

        #endregion
    }
}
