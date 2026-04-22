using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Maps ActionData to Action. Extracted from ActionLoader for testability and single responsibility.
    /// </summary>
    public static class ActionDataToActionMapper
    {
        private static ActionAttackBonuses? CloneActionAttackBonuses(ActionAttackBonuses? src)
        {
            if (src == null)
                return null;
            var dst = new ActionAttackBonuses();
            foreach (var grp in src.BonusGroups ?? new List<ActionAttackBonusGroup>())
            {
                var g = new ActionAttackBonusGroup
                {
                    Keyword = grp.Keyword,
                    CadenceType = grp.CadenceType,
                    Count = grp.Count,
                    DurationType = grp.DurationType,
                };
                g.Bonuses = grp.Bonuses == null || grp.Bonuses.Count == 0
                    ? new List<ActionAttackBonusItem>()
                    : grp.Bonuses.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value }).ToList();
                dst.BonusGroups.Add(g);
            }
            return dst;
        }

        /// <summary>
        /// Creates an Action instance from ActionData. Caller should ensure data.NormalizeStatBonuses/Thresholds/Accumulations
        /// have been called if required.
        /// </summary>
        public static Action CreateAction(ActionData data)
        {
            var actionType = ParseActionType(data.Type);
            var targetType = ParseTargetType(data.TargetType);

            string enhancedDescription = ActionDescriptionEnhancer.EnhanceActionDescription(data);

            var action = new Action(
                name: data.Name,
                type: actionType,
                targetType: targetType,
                cooldown: data.Cooldown,
                description: enhancedDescription,
                comboOrder: data.ComboOrder,
                damageMultiplier: data.DamageMultiplier,
                length: data.Length,
                causesBleed: data.CausesBleed,
                causesWeaken: data.CausesWeaken,
                isComboAction: data.IsComboAction,
                comboBonusAmount: data.ComboBonusAmount,
                comboBonusDuration: data.ComboBonusDuration
            );

            action.CausesSlow = data.CausesSlow;
            action.CausesPoison = data.CausesPoison;
            action.CausesBurn = data.CausesBurn;
            action.CausesBleed = data.CausesBleed;
            action.PoisonPercentToAdd = data.PoisonPercentToAdd;
            action.BurnAmountToAdd = data.BurnAmountToAdd;
            action.BleedAmountToAdd = data.BleedAmountToAdd;
            action.CausesStun = data.CausesStun;

            action.CausesVulnerability = data.CausesVulnerability;
            action.CausesHarden = data.CausesHarden;
            action.CausesExpose = data.CausesExpose;
            action.CausesSilence = data.CausesSilence;
            action.CausesPierce = data.CausesPierce;
            action.CausesStatDrain = data.CausesStatDrain;
            action.CausesFortify = data.CausesFortify;
            action.CausesFocus = data.CausesFocus;
            action.CausesCleanse = data.CausesCleanse;
            action.CausesReflect = data.CausesReflect;

            data.NormalizeStatBonuses();
            action.Advanced.StatBonuses = data.StatBonuses == null ? new List<StatBonusEntry>() : new List<StatBonusEntry>(data.StatBonuses);
            action.Advanced.StatBonus = action.Advanced.StatBonuses.Count > 0 ? action.Advanced.StatBonuses[0].Value : data.StatBonus;
            action.Advanced.StatBonusType = action.Advanced.StatBonuses.Count > 0 ? action.Advanced.StatBonuses[0].Type : data.StatBonusType;
            action.Advanced.RollBonus = data.RollBonus;
            action.Advanced.EnemyRollBonus = data.EnemyRollBonus;
            action.Advanced.RollBonusDuration = data.RollBonusDuration;
            action.Advanced.StatBonusDuration = data.StatBonusDuration;
            action.Advanced.MultiHitCount = data.MultiHitCount;
            action.Advanced.SelfDamagePercent = data.SelfDamagePercent;
            action.Advanced.SkipNextTurn = data.SkipNextTurn;
            action.Advanced.RepeatLastAction = data.RepeatLastAction;
            action.Tags = data.Tags == null || data.Tags.Count == 0
                ? new List<string>()
                : new List<string>(data.Tags);
            action.Advanced.EnemyRollPenalty = data.EnemyRollPenalty;

            data.NormalizeThresholds();
            action.Advanced.Thresholds = data.Thresholds == null ? new List<ThresholdEntry>() : new List<ThresholdEntry>(data.Thresholds);
            var firstHealth = action.Advanced.Thresholds.FirstOrDefault(t => string.Equals(t.Type, "Health", StringComparison.OrdinalIgnoreCase));
            action.Advanced.HealthThreshold = firstHealth != null ? firstHealth.Value : (action.Advanced.Thresholds.Count > 0 ? action.Advanced.Thresholds[0].Value : data.HealthThreshold);

            data.NormalizeAccumulations();
            action.Advanced.Accumulations = data.Accumulations == null ? new List<AccumulationEntry>() : new List<AccumulationEntry>(data.Accumulations);
            action.Advanced.ConditionalDamageMultiplier = data.ConditionalDamageMultiplier;

            action.RollMods.MultipleDiceCount = data.MultipleDiceCount;
            action.RollMods.MultipleDiceMode = data.MultipleDiceMode;
            action.RollMods.CriticalMissThresholdOverride = data.CriticalMissThresholdOverride;
            action.RollMods.CriticalHitThresholdOverride = data.CriticalHitThresholdOverride;
            action.RollMods.ComboThresholdOverride = data.ComboThresholdOverride;
            action.RollMods.HitThresholdOverride = data.HitThresholdOverride;
            action.RollMods.CriticalMissThresholdAdjustment = data.CriticalMissThresholdAdjustment;
            action.RollMods.CriticalHitThresholdAdjustment = data.CriticalHitThresholdAdjustment;
            action.RollMods.ComboThresholdAdjustment = data.ComboThresholdAdjustment;
            action.RollMods.HitThresholdAdjustment = data.HitThresholdAdjustment;
            action.RollMods.EnemyCriticalMissThresholdAdjustment = data.EnemyCriticalMissThresholdAdjustment;
            action.RollMods.EnemyCriticalHitThresholdAdjustment = data.EnemyCriticalHitThresholdAdjustment;
            action.RollMods.EnemyComboThresholdAdjustment = data.EnemyComboThresholdAdjustment;
            action.RollMods.EnemyHitThresholdAdjustment = data.EnemyHitThresholdAdjustment;
            action.RollMods.ApplyThresholdAdjustmentsToBoth = data.ApplyThresholdAdjustmentsToBoth;

            action.ActionAttackBonuses = CloneActionAttackBonuses(data.ActionAttackBonuses);

            action.SpeedMod = data.SpeedMod ?? "";
            action.DamageMod = data.DamageMod ?? "";
            action.MultiHitMod = data.MultiHitMod ?? "";
            action.AmpMod = data.AmpMod ?? "";
            action.EnemySpeedMod = data.EnemySpeedMod ?? "";
            action.EnemyDamageMod = data.EnemyDamageMod ?? "";
            action.EnemyMultiHitMod = data.EnemyMultiHitMod ?? "";
            action.EnemyAmpMod = data.EnemyAmpMod ?? "";
            action.Cadence = data.Cadence ?? "";

            action.Triggers.TriggerConditions = data.TriggerConditions != null
                ? new List<string>(data.TriggerConditions)
                : new List<string>();

            if (int.TryParse(data.Jump?.Trim(), out int jumpVal) && jumpVal > 0)
                action.ComboRouting.JumpToSlot = jumpVal;
            if (action.ComboRouting.JumpToSlot == 0 &&
                int.TryParse(data.JumpRelative?.Trim(), out int jumpRel) && jumpRel > 0)
                action.ComboRouting.JumpRelativeSlots = jumpRel;
            action.ComboRouting.ChainPosition = data.ChainPosition ?? "";
            action.ComboRouting.ChainLength = data.ChainLength ?? "";
            action.ComboRouting.Reset = data.Reset ?? "";
            action.ComboRouting.ModifyBasedOnChainPosition = data.ModifyBasedOnChainPosition ?? "";
            data.NormalizeChainPositionBonuses();
            action.ComboRouting.ChainPositionBonuses = data.ChainPositionBonuses == null
                ? new List<ChainPositionBonusEntry>()
                : new List<ChainPositionBonusEntry>(data.ChainPositionBonuses);
            action.ComboRouting.IsOpener = data.IsOpener;
            action.ComboRouting.IsFinisher = data.IsFinisher;

            return action;
        }

        internal static ActionType ParseActionType(string type)
        {
            return (type ?? "").ToLower() switch
            {
                "attack" => ActionType.Attack,
                "heal" => ActionType.Heal,
                "buff" => ActionType.Buff,
                "debuff" => ActionType.Debuff,
                "interact" => ActionType.Interact,
                "move" => ActionType.Move,
                "useitem" => ActionType.UseItem,
                "spell" => ActionType.Spell,
                _ => ActionType.Attack
            };
        }

        internal static TargetType ParseTargetType(string targetType)
        {
            return (targetType ?? "").ToLower() switch
            {
                "self" => TargetType.Self,
                "singletarget" => TargetType.SingleTarget,
                "areaofeffect" => TargetType.AreaOfEffect,
                "environment" => TargetType.Environment,
                "selfandtarget" => TargetType.SelfAndTarget,
                _ => TargetType.SingleTarget
            };
        }
    }
}
