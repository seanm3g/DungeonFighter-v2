using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RPGGame.Data;

namespace RPGGame
{
    public class ActionData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        [JsonPropertyName("targetType")]
        public string TargetType { get; set; } = "";
        [JsonPropertyName("cooldown")]
        public int Cooldown { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        [JsonPropertyName("damageMultiplier")]
        public double DamageMultiplier { get; set; }
        [JsonPropertyName("length")]
        public double Length { get; set; }
        [JsonPropertyName("causesBleed")]
        public bool CausesBleed { get; set; }
        [JsonPropertyName("causesWeaken")]
        public bool CausesWeaken { get; set; }
        [JsonPropertyName("causesSlow")]
        public bool CausesSlow { get; set; }
        [JsonPropertyName("causesPoison")]
        public bool CausesPoison { get; set; }
        [JsonPropertyName("causesBurn")]
        public bool CausesBurn { get; set; }
        [JsonPropertyName("poisonPercentToAdd")]
        public double PoisonPercentToAdd { get; set; }
        [JsonPropertyName("burnAmountToAdd")]
        public int BurnAmountToAdd { get; set; }
        [JsonPropertyName("bleedAmountToAdd")]
        public int BleedAmountToAdd { get; set; }
        [JsonPropertyName("causesStun")]
        public bool CausesStun { get; set; }
        [JsonPropertyName("causesVulnerability")]
        public bool CausesVulnerability { get; set; }
        [JsonPropertyName("causesHarden")]
        public bool CausesHarden { get; set; }
        [JsonPropertyName("causesExpose")]
        public bool CausesExpose { get; set; }
        [JsonPropertyName("causesSilence")]
        public bool CausesSilence { get; set; }
        [JsonPropertyName("causesPierce")]
        public bool CausesPierce { get; set; }
        [JsonPropertyName("causesStatDrain")]
        public bool CausesStatDrain { get; set; }
        [JsonPropertyName("causesFortify")]
        public bool CausesFortify { get; set; }
        [JsonPropertyName("causesFocus")]
        public bool CausesFocus { get; set; }
        [JsonPropertyName("causesCleanse")]
        public bool CausesCleanse { get; set; }
        [JsonPropertyName("causesReflect")]
        public bool CausesReflect { get; set; }
        [JsonPropertyName("comboBonusAmount")]
        public int ComboBonusAmount { get; set; }
        [JsonPropertyName("comboBonusDuration")]
        public int ComboBonusDuration { get; set; }
        [JsonPropertyName("comboOrder")]
        public int ComboOrder { get; set; }
        [JsonPropertyName("isComboAction")]
        public bool IsComboAction { get; set; } = true;
        [JsonPropertyName("rollBonus")]
        public int RollBonus { get; set; }
        /// <summary>Accuracy (roll bonus) when an enemy uses this action; maps to spreadsheet enemy accuracy column.</summary>
        [JsonPropertyName("enemyRollBonus")]
        public int EnemyRollBonus { get; set; }
        [JsonPropertyName("rollBonusDuration")]
        public int RollBonusDuration { get; set; }
        [JsonPropertyName("statBonus")]
        public int StatBonus { get; set; }
        [JsonPropertyName("statBonusType")]
        public string StatBonusType { get; set; } = "";
        [JsonPropertyName("statBonusDuration")]
        public int StatBonusDuration { get; set; }
        [JsonPropertyName("statBonuses")]
        public List<StatBonusEntry> StatBonuses { get; set; } = new List<StatBonusEntry>();
        [JsonPropertyName("multiHitCount")]
        public int MultiHitCount { get; set; }
        [JsonPropertyName("selfDamagePercent")]
        public int SelfDamagePercent { get; set; }
        [JsonPropertyName("skipNextTurn")]
        public bool SkipNextTurn { get; set; }
        [JsonPropertyName("repeatLastAction")]
        public bool RepeatLastAction { get; set; }
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();
        [JsonPropertyName("enemyRollPenalty")]
        public int EnemyRollPenalty { get; set; }
        [JsonPropertyName("healthThreshold")]
        public double HealthThreshold { get; set; }
        [JsonPropertyName("healthThresholds")]
        [Obsolete("Use Thresholds (attribute-based) instead. Migrated in NormalizeThresholds().")]
        public List<double> HealthThresholds { get; set; } = new List<double>();
        [JsonPropertyName("thresholds")]
        public List<ThresholdEntry> Thresholds { get; set; } = new List<ThresholdEntry>();
        [JsonPropertyName("accumulations")]
        public List<AccumulationEntry> Accumulations { get; set; } = new List<AccumulationEntry>();
        [JsonPropertyName("conditionalDamageMultiplier")]
        public double ConditionalDamageMultiplier { get; set; } = 1.0;

        // Threshold overrides (absolute values)
        [JsonPropertyName("criticalMissThresholdOverride")]
        public int CriticalMissThresholdOverride { get; set; }
        [JsonPropertyName("criticalHitThresholdOverride")]
        public int CriticalHitThresholdOverride { get; set; }
        [JsonPropertyName("comboThresholdOverride")]
        public int ComboThresholdOverride { get; set; }
        [JsonPropertyName("hitThresholdOverride")]
        public int HitThresholdOverride { get; set; }

        // Threshold adjustments (adds to current/default)
        [JsonPropertyName("criticalMissThresholdAdjustment")]
        public int CriticalMissThresholdAdjustment { get; set; }
        [JsonPropertyName("criticalHitThresholdAdjustment")]
        public int CriticalHitThresholdAdjustment { get; set; }
        [JsonPropertyName("comboThresholdAdjustment")]
        public int ComboThresholdAdjustment { get; set; }
        [JsonPropertyName("hitThresholdAdjustment")]
        public int HitThresholdAdjustment { get; set; }

        /// <summary>Enemy attacker: crit miss threshold adjustment (spreadsheet enemy crit miss).</summary>
        [JsonPropertyName("enemyCriticalMissThresholdAdjustment")]
        public int EnemyCriticalMissThresholdAdjustment { get; set; }
        [JsonPropertyName("enemyCriticalHitThresholdAdjustment")]
        public int EnemyCriticalHitThresholdAdjustment { get; set; }
        [JsonPropertyName("enemyComboThresholdAdjustment")]
        public int EnemyComboThresholdAdjustment { get; set; }
        [JsonPropertyName("enemyHitThresholdAdjustment")]
        public int EnemyHitThresholdAdjustment { get; set; }

        // Whether to apply threshold adjustments to both source and target
        [JsonPropertyName("applyThresholdAdjustmentsToBoth")]
        public bool ApplyThresholdAdjustmentsToBoth { get; set; }

        // Roll modification properties
        [JsonPropertyName("multipleDiceCount")]
        public int MultipleDiceCount { get; set; } = 1;
        [JsonPropertyName("multipleDiceMode")]
        public string MultipleDiceMode { get; set; } = "Sum";

        // Starting action flag
        [JsonPropertyName("isStartingAction")]
        public bool IsStartingAction { get; set; }

        // Default action flag - action is always available regardless of weapon
        [JsonPropertyName("isDefaultAction")]
        public bool IsDefaultAction { get; set; }

        // Weapon types this action is assigned to (e.g., ["Sword", "Dagger"])
        [JsonPropertyName("weaponTypes")]
        public List<string> WeaponTypes { get; set; } = new List<string>();

        // ACTION/ATTACK keyword bonuses
        [JsonPropertyName("actionAttackBonuses")]
        public Data.ActionAttackBonuses? ActionAttackBonuses { get; set; }

        // Spreadsheet-origin fields (round-trip with Actions.json spreadsheet format)
        [JsonPropertyName("rarity")]
        public string Rarity { get; set; } = "";
        [JsonPropertyName("category")]
        public string Category { get; set; } = "";
        [JsonPropertyName("cadence")]
        public string Cadence { get; set; } = "";
        /// <summary>Hero next-action speed modifier as a percentage (spreadsheet AJ; e.g. "10" = 10%).</summary>
        [JsonPropertyName("speedMod")]
        public string SpeedMod { get; set; } = "";
        /// <summary>Hero next-action damage modifier as a percentage (spreadsheet AK).</summary>
        [JsonPropertyName("damageMod")]
        public string DamageMod { get; set; } = "";
        /// <summary>Hero next-action multi-hit modifier as a raw value (spreadsheet AL).</summary>
        [JsonPropertyName("multiHitMod")]
        public string MultiHitMod { get; set; } = "";
        /// <summary>Hero next-action amp modifier as a percentage (spreadsheet AM).</summary>
        [JsonPropertyName("ampMod")]
        public string AmpMod { get; set; } = "";

        /// <summary>Enemy next-action speed modifier (spreadsheet AD).</summary>
        [JsonPropertyName("enemySpeedMod")]
        public string EnemySpeedMod { get; set; } = "";
        /// <summary>Enemy next-action damage modifier (spreadsheet AE).</summary>
        [JsonPropertyName("enemyDamageMod")]
        public string EnemyDamageMod { get; set; } = "";
        /// <summary>Enemy next-action multi-hit modifier (spreadsheet AF).</summary>
        [JsonPropertyName("enemyMultiHitMod")]
        public string EnemyMultiHitMod { get; set; } = "";
        /// <summary>Enemy next-action amp modifier (spreadsheet AG).</summary>
        [JsonPropertyName("enemyAmpMod")]
        public string EnemyAmpMod { get; set; } = "";

        // Combo & position (round-trip with spreadsheet / Combo & Position section)
        [JsonPropertyName("chainPosition")]
        public string ChainPosition { get; set; } = "";
        [JsonPropertyName("modifyBasedOnChainPosition")]
        public string ModifyBasedOnChainPosition { get; set; } = "";
        [JsonPropertyName("chainPositionBonuses")]
        public List<ChainPositionBonusEntry> ChainPositionBonuses { get; set; } = new List<ChainPositionBonusEntry>();
        [JsonPropertyName("jump")]
        public string Jump { get; set; } = "";
        /// <summary>Extra combo slots after the normal next step (e.g. 1 at "position" 2 → fourth slot). Ignored when absolute <see cref="Jump"/> is set.</summary>
        [JsonPropertyName("jumpRelative")]
        public string JumpRelative { get; set; } = "";
        [JsonPropertyName("chainLength")]
        public string ChainLength { get; set; } = "";
        [JsonPropertyName("reset")]
        public string Reset { get; set; } = "";
        [JsonPropertyName("resetBlockerBuffer")]
        public string ResetBlockerBuffer { get; set; } = "";
        [JsonPropertyName("opener")]
        public bool IsOpener { get; set; }
        [JsonPropertyName("finisher")]
        public bool IsFinisher { get; set; }

        /// <summary>
        /// When non-empty, this action's effects apply only when the attack result matches one of these (ONHIT, ONMISS, ONCOMBO, ONCRITICAL).
        /// Empty = apply on any result.
        /// </summary>
        [JsonPropertyName("triggerConditions")]
        public List<string> TriggerConditions { get; set; } = new List<string>();

        /// <summary>
        /// Ensures StatBonuses is populated from legacy statBonus/statBonusType when list is empty.
        /// Call after loading so UI and execution can use the list.
        /// </summary>
        /// <summary>Ensures ChainPositionBonuses list is non-null.</summary>
        public void NormalizeChainPositionBonuses()
        {
            if (ChainPositionBonuses == null)
                ChainPositionBonuses = new List<ChainPositionBonusEntry>();
        }

        public void NormalizeStatBonuses()
        {
            if (StatBonuses == null)
                StatBonuses = new List<StatBonusEntry>();
            if (StatBonuses.Count == 0 && (StatBonus != 0 || !string.IsNullOrEmpty(StatBonusType)))
            {
                StatBonuses.Add(new StatBonusEntry { Value = StatBonus, Type = StatBonusType ?? "" });
            }
        }

        /// <summary>
        /// Syncs legacy StatBonus/StatBonusType from first StatBonuses entry for JSON backward compatibility when saving.
        /// </summary>
        public void EnsureLegacyStatBonusFromList()
        {
            if (StatBonuses != null && StatBonuses.Count > 0)
            {
                StatBonus = StatBonuses[0].Value;
                StatBonusType = StatBonuses[0].Type ?? "";
            }
        }

        /// <summary>
        /// Ensures Thresholds is populated from legacy HealthThreshold/HealthThresholds when list is empty.
        /// Call after loading so UI and execution can use the list. Migrates old healthThresholds array to attribute "Health".
        /// </summary>
        public void NormalizeThresholds()
        {
            if (Thresholds == null)
                Thresholds = new List<ThresholdEntry>();
            if (Thresholds.Count == 0)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (HealthThresholds != null && HealthThresholds.Count > 0)
                {
                    foreach (double v in HealthThresholds)
                        Thresholds.Add(new ThresholdEntry { Value = v, Type = "Health" });
                }
                else if (HealthThreshold > 0.0)
                    Thresholds.Add(new ThresholdEntry { Value = HealthThreshold, Type = "Health" });
#pragma warning restore CS0618
            }
        }

        /// <summary>
        /// Syncs legacy HealthThreshold from first Health-type threshold for JSON backward compatibility when saving.
        /// </summary>
        public void EnsureLegacyHealthThresholdFromList()
        {
            if (Thresholds != null && Thresholds.Count > 0)
            {
                var firstHealth = Thresholds.FirstOrDefault(t => string.Equals(t.Type, "Health", StringComparison.OrdinalIgnoreCase));
                HealthThreshold = firstHealth != null ? firstHealth.Value : Thresholds[0].Value;
            }
        }

        private static readonly string[] CanonicalCadencesForAccumulation = { "Action", "Ability", "Chain", "Fight", "Dungeon" };

        /// <summary>
        /// Ensures Accumulations list is non-null. Migrates legacy CadencePassed+Param to Cadence{Param} (e.g. CadenceAction).
        /// </summary>
        public void NormalizeAccumulations()
        {
            if (Accumulations == null)
                Accumulations = new List<AccumulationEntry>();
            var toAdd = new List<AccumulationEntry>();
            var toRemove = new List<int>();
            for (int i = 0; i < Accumulations.Count; i++)
            {
                var e = Accumulations[i];
                if (string.Equals(e.Type, "CadencePassed", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Param))
                {
                    var param = e.Param.Trim();
                    var canonical = CanonicalCadencesForAccumulation.FirstOrDefault(c => string.Equals(c, param, StringComparison.OrdinalIgnoreCase));
                    if (canonical != null)
                    {
                        toRemove.Add(i);
                        if (!Accumulations.Any(a => string.Equals(a.Type, "Cadence" + canonical, StringComparison.OrdinalIgnoreCase)))
                            toAdd.Add(new AccumulationEntry { Type = "Cadence" + canonical, ModifiesParam = "Damage", ValueKind = "#", Operator = "", Value = e.Value, Param = "" });
                    }
                }
            }
            for (int r = toRemove.Count - 1; r >= 0; r--)
                Accumulations.RemoveAt(toRemove[r]);
            foreach (var entry in toAdd)
                Accumulations.Add(entry);
            // Legacy: ensure every entry has ModifiesParam and ValueKind for row-based UI
            foreach (var e in Accumulations)
            {
                if (string.IsNullOrWhiteSpace(e.ModifiesParam))
                    e.ModifiesParam = "Damage";
                if (string.IsNullOrWhiteSpace(e.ValueKind))
                    e.ValueKind = "#";
            }
        }

        /// <summary>
        /// Ensures Tags list has no duplicates (case-insensitive). Duplicate tags are removed so each tag appears at most once.
        /// </summary>
        public void NormalizeTags()
        {
            if (Tags == null)
                Tags = new List<string>();
            Tags = Tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
