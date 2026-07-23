using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    public enum MechanicStatus
    {
        Good,
        NeedsImprovement,
        Cut,
        Redundant,
    }

    /// <summary>Cadence eligibility from the mechanic matrix (TURN, ACTION, etc.).</summary>
    public readonly record struct MechanicCadenceProfile(
        bool TurnBonus,
        bool ActionBonus,
        bool Fight,
        bool Dungeon,
        bool OnActions,
        MechanicStatus Status)
    {
        public IEnumerable<string> GetAllowedCadenceKeywords()
        {
            if (TurnBonus) yield return CadenceKeywords.Turn;
            if (ActionBonus) yield return CadenceKeywords.Action;
            if (Fight) yield return "FIGHT";
            if (Dungeon) yield return "DUNGEON";
        }

        public bool RequiresCadenceTiming =>
            TurnBonus || ActionBonus || Fight || Dungeon;
    }

    /// <summary>
    /// Declarative mechanic IDs for the ACTIONS sheet MECHANICS column. Detection mirrors populated detail columns;
    /// cadence-gated mechanics default to TURN or ACTION per the mechanic matrix when timing cells are empty.
    /// Stun, poison, burn, and bleed are applied via items only — not action-sheet mechanics.
    /// </summary>
    public static class ActionMechanicsRegistry
    {
        /// <summary>DoT/control IDs granted by item mods, not authored on the ACTIONS MECHANICS column.</summary>
        public static IReadOnlyList<string> ItemAppliedStatusEffectIds { get; } = new[]
        {
            "stun", "poison", "burn", "acid", "bleed",
        };
        private static readonly Dictionary<string, MechanicCadenceProfile> Profiles = BuildProfiles();

        private static readonly HashSet<string> NextActionModIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "hero_next_action_speed", "hero_next_action_damage", "hero_next_action_multihit", "hero_next_action_amp",
            "enemy_next_action_speed", "enemy_next_action_damage", "enemy_next_action_multihit", "enemy_next_action_amp",
        };

        private static readonly HashSet<string> TurnScopedModIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "hero_accuracy", "hero_hit_threshold", "hero_combo_threshold", "hero_crit_threshold", "hero_crit_miss_threshold",
            "enemy_accuracy", "enemy_hit_threshold", "enemy_combo_threshold", "enemy_crit_threshold", "enemy_crit_miss_threshold",
            "hero_stat_bonus", "enemy_stat_bonus", "advantage", "disadvantage",
            "hero_weapon_speed", "hero_weapon_damage", "enemy_weapon_speed", "enemy_weapon_damage",
        };

        /// <summary>Maps legacy unprefixed IDs to current hero-prefixed IDs.</summary>
        private static readonly Dictionary<string, string> LegacyAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["accuracy"] = "hero_accuracy",
            ["hit_threshold"] = "hero_hit_threshold",
            ["combo_threshold"] = "hero_combo_threshold",
            ["crit_threshold"] = "hero_crit_threshold",
            ["crit_miss_threshold"] = "hero_crit_miss_threshold",
            ["next_action_speed"] = "hero_next_action_speed",
            ["next_action_damage"] = "hero_next_action_damage",
            ["next_action_multihit"] = "hero_next_action_multihit",
            ["next_action_amp"] = "hero_next_action_amp",
            ["stat_bonus"] = "hero_stat_bonus",
        };

        /// <summary>Mechanic IDs for MECHANIC_LIST dropdown (ON ACTIONS=TRUE, STATUS=GOOD).</summary>
        public static IReadOnlyList<string> AllMechanicIds { get; } =
            Profiles
                .Where(p => p.Value.OnActions && p.Value.Status == MechanicStatus.Good)
                .Select(p => p.Key)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public static bool TryGetProfile(string? mechanicId, out MechanicCadenceProfile profile)
        {
            profile = default;
            if (string.IsNullOrWhiteSpace(mechanicId))
                return false;
            return Profiles.TryGetValue(NormalizeMechanicId(mechanicId), out profile);
        }

        public static HashSet<string> GetAllowedCadencesForMechanic(string mechanicId)
        {
            if (!TryGetProfile(mechanicId, out var profile))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return profile.GetAllowedCadenceKeywords()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public static HashSet<string> GetAllowedCadencesForMechanics(IEnumerable<string> mechanicIds)
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in mechanicIds)
            {
                if (!TryGetProfile(id, out var profile))
                    continue;
                foreach (string cadence in profile.GetAllowedCadenceKeywords())
                    allowed.Add(cadence);
            }
            return allowed;
        }

        public static bool RequiresCadenceDuration(string? mechanicId)
        {
            if (string.IsNullOrWhiteSpace(mechanicId))
                return false;
            string normalized = NormalizeMechanicId(mechanicId);
            if (NextActionModIds.Contains(normalized) || TurnScopedModIds.Contains(normalized))
                return true;
            if (TryGetProfile(normalized, out var profile) && profile.RequiresCadenceTiming)
            {
                // Status/roll mechanics with turn timing do not use CADENCE/DURATION columns.
                return normalized is "advantage" or "disadvantage" or "self_damage";
            }
            return false;
        }

        public static bool RowHasCadenceGatedMechanic(SpreadsheetActionData? row)
        {
            if (row == null)
                return false;
            return DetectFromSpreadsheetRow(row).Any(RequiresCadenceDuration)
                || ParseMechanicsCell(row.Mechanics).Any(RequiresCadenceDuration);
        }

        /// <summary>Default CADENCE when timing cells are blank, from detected cadence-gated mechanics.</summary>
        public static string? ResolveDefaultCadence(IEnumerable<string> detectedMechanicIds)
        {
            var gated = detectedMechanicIds
                .Select(NormalizeMechanicId)
                .Where(id => !string.IsNullOrEmpty(id) && RequiresCadenceDuration(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (gated.Count == 0)
                return null;

            bool hasNextAction = gated.Any(id => NextActionModIds.Contains(id));
            bool hasTurnScoped = gated.Any(id => TurnScopedModIds.Contains(id) || id == "self_damage");

            if (hasNextAction && !hasTurnScoped)
                return CadenceKeywords.Action;
            if (hasTurnScoped && !hasNextAction)
                return CadenceKeywords.Turn;
            return CadenceKeywords.Turn;
        }

        /// <summary>IDs eligible for the MECHANICS column on push (GOOD + ON ACTIONS).</summary>
        public static List<string> FilterForMechanicsColumn(IEnumerable<string> detectedIds)
        {
            return detectedIds
                .Select(NormalizeMechanicId)
                .Where(id => !string.IsNullOrEmpty(id)
                    && TryGetProfile(id, out var p)
                    && p.OnActions
                    && p.Status == MechanicStatus.Good)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Non-fatal warning when explicit CADENCE is not allowed for detected mechanics.</summary>
        public static string? ValidateCadenceForRow(SpreadsheetActionData row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Cadence))
                return null;

            var detected = DetectFromSpreadsheetRow(row);
            var allowed = GetAllowedCadencesForMechanics(detected.Where(RequiresCadenceDuration));
            if (allowed.Count == 0)
                return null;

            string cadence = row.Cadence.Trim().ToUpperInvariant();
            if (cadence.EndsWith('S'))
                cadence = cadence[..^1];

            if (allowed.Contains(cadence))
                return null;

            return $"CADENCE '{row.Cadence}' not in allowed set [{string.Join(", ", allowed.OrderBy(c => c))}] for mechanics: {string.Join(", ", detected.Where(RequiresCadenceDuration))}";
        }

        /// <summary>Comma/semicolon/pipe list → normalized lowercase mechanic IDs (legacy aliases upgraded).</summary>
        public static List<string> ParseMechanicsCell(string? cell)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return new List<string>();

            return cell
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => NormalizeMechanicId(p.Trim()))
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string NormalizeMechanicId(string? mechanicId)
        {
            if (string.IsNullOrWhiteSpace(mechanicId))
                return "";
            string id = mechanicId.Trim().ToLowerInvariant();
            return LegacyAliases.TryGetValue(id, out string? canonical) ? canonical : id;
        }

        /// <summary>Sorted mechanic IDs inferred from populated spreadsheet columns (full internal audit set).</summary>
        public static List<string> DetectFromSpreadsheetRow(SpreadsheetActionData row)
        {
            if (row == null)
                return new List<string>();

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Nz(row.HeroAccuracy)) ids.Add("hero_accuracy");
            if (Nz(row.HeroHit)) ids.Add("hero_hit_threshold");
            if (Nz(row.HeroCombo)) ids.Add("hero_combo_threshold");
            if (Nz(row.HeroCrit)) ids.Add("hero_crit_threshold");
            if (Nz(row.HeroCritMiss)) ids.Add("hero_crit_miss_threshold");

            if (Nz(row.EnemyAccuracy)) ids.Add("enemy_accuracy");
            if (Nz(row.EnemyHit)) ids.Add("enemy_hit_threshold");
            if (Nz(row.EnemyCombo)) ids.Add("enemy_combo_threshold");
            if (Nz(row.EnemyCrit)) ids.Add("enemy_crit_threshold");
            if (Nz(row.EnemyCritMiss)) ids.Add("enemy_crit_miss_threshold");

            if (Nz(row.SpeedMod)) ids.Add("hero_next_action_speed");
            if (Nz(row.DamageMod)) ids.Add("hero_next_action_damage");
            if (Nz(row.MultiHitMod)) ids.Add("hero_next_action_multihit");
            if (Nz(row.AmpMod)) ids.Add("hero_next_action_amp");

            if (Nz(row.EnemySpeedMod)) ids.Add("enemy_next_action_speed");
            if (Nz(row.EnemyDamageMod)) ids.Add("enemy_next_action_damage");
            if (Nz(row.EnemyMultiHitMod)) ids.Add("enemy_next_action_multihit");
            if (Nz(row.EnemyAmpMod)) ids.Add("enemy_next_action_amp");

            if (Nz(row.WeaponSpeedMod)) ids.Add("hero_weapon_speed");
            if (Nz(row.WeaponDamageMod)) ids.Add("hero_weapon_damage");
            if (Nz(row.EnemyWeaponSpeedMod)) ids.Add("enemy_weapon_speed");
            if (Nz(row.EnemyWeaponDamageMod)) ids.Add("enemy_weapon_damage");

            if (Nz(row.HeroSTR) || Nz(row.HeroAGI) || Nz(row.HeroTECH) || Nz(row.HeroINT))
                ids.Add("hero_stat_bonus");
            if (Nz(row.EnemySTR) || Nz(row.EnemyAGI) || Nz(row.EnemyTECH) || Nz(row.EnemyINT))
                ids.Add("enemy_stat_bonus");

            if (Nz(row.Weaken)) ids.Add("weaken");
            if (Nz(row.Slow)) ids.Add("slow");
            if (Nz(row.Vulnerability)) ids.Add("vulnerability");
            if (Nz(row.Harden)) ids.Add("harden");
            if (Nz(row.Focus)) ids.Add("focus");
            if (Nz(row.Confuse)) ids.Add("confuse");
            if (Nz(row.Expose)) ids.Add("expose");
            if (Nz(row.Silence)) ids.Add("silence");
            if (Nz(row.Pierce)) ids.Add("pierce");
            if (Nz(row.StatDrain)) ids.Add("stat_drain");
            if (Nz(row.Fortify)) ids.Add("fortify");
            if (Nz(row.Reflect)) ids.Add("reflect");
            if (Nz(row.Lifesteal)) ids.Add("lifesteal");
            if (Nz(row.Disrupt)) ids.Add("disrupt");
            if (Nz(row.Consume)) ids.Add("consume");
            if (Nz(row.Cleanse)) ids.Add("cleanse");
            if (Nz(row.SelfDamage)) ids.Add("self_damage");

            if (Nz(row.Jump) || Nz(row.JumpRelative)) ids.Add("combo_jump");
            if (Nz(row.Opener)) ids.Add("opener");
            if (Nz(row.Finisher)) ids.Add("finisher");
            if (Nz(row.ChainLength)) ids.Add("chain_length");
            if (Nz(row.ChainPosition)) ids.Add("chain_position");
            if (Nz(row.ModifyBasedOnChainPosition)) ids.Add("modify_chain_position");
            if (Nz(row.LoopChain)) ids.Add("loop_chain");
            if (Nz(row.Shuffle)) ids.Add("shuffle");
            if (Nz(row.ReplaceAction)) ids.Add("replace_action");
            if (Nz(row.Skip)) ids.Add("skip");
            if (Nz(row.Grace)) ids.Add("grace");

            if (Nz(row.DiceRolls)) ids.Add("multi_dice");
            if (Nz(row.ExplodingDiceThreshold)) ids.Add("exploding_dice");
            if (HasAdvantage(row.HighestLowestRoll)) ids.Add("advantage");
            if (HasDisadvantage(row.HighestLowestRoll)) ids.Add("disadvantage");
            if (Nz(row.ReplaceNextRoll)) ids.Add("replace_next_roll");
            if (Nz(row.Curse)) ids.Add("curse");

            if (Nz(row.OnHit) || TriggerContains(row.TriggerConditions, "ONHIT") || BundleWhen(row, "ONHIT"))
                ids.Add("on_hit");
            if (Nz(row.OnMiss) || TriggerContains(row.TriggerConditions, "ONMISS") || BundleWhen(row, "ONMISS"))
                ids.Add("on_miss");
            if (Nz(row.OnCrit) || TriggerContains(row.TriggerConditions, "ONCRITICAL") || TriggerContains(row.TriggerConditions, "ONCRIT")
                || BundleWhen(row, "ONCRITICAL"))
                ids.Add("on_crit");
            if (TriggerContains(row.TriggerConditions, "ONCOMBO") || BundleWhen(row, "ONCOMBO"))
                ids.Add("on_combo");
            if (TriggerContains(row.TriggerConditions, "ONCOMBOEND") || TriggerContains(row.TriggerConditions, "ONCOMBOENDED")
                || BundleWhen(row, "ONCOMBOEND"))
                ids.Add("on_combo_end");
            if (TriggerContains(row.TriggerConditions, "ONCONNECT") || TriggerContains(row.TriggerConditions, "ONANYHIT")
                || BundleWhen(row, "ONCONNECT"))
                ids.Add("on_connect");
            if (TriggerContains(row.TriggerConditions, "ONCRITICALMISS") || TriggerContains(row.TriggerConditions, "ONCRITMISS")
                || BundleWhen(row, "ONCRITICALMISS"))
                ids.Add("on_crit_miss");
            if (Nz(row.OnKill) || TriggerContains(row.TriggerConditions, "ONKILL") || BundleWhen(row, "ONKILL"))
                ids.Add("on_kill");
            if (Nz(row.OnRoomsCleared) || BundleWhen(row, "ONROOMSCLEARED"))
                ids.Add("on_rooms_cleared");
            if (Nz(row.OnRollValue) || TriggerContains(row.TriggerConditions, "ONROLLVALUE") || BundleWhen(row, "ONROLLVALUE"))
                ids.Add("on_roll_value");
            if (TriggerContains(row.TriggerConditions, "ONHEALTHTHRESHOLD")) ids.Add("on_health_threshold");
            if (BundleWhen(row, "ONFIRSTHIT")) ids.Add("on_first_hit");
            if (BundleWhen(row, "ONAFTERMISS")) ids.Add("on_after_miss");

            if (Nz(row.HeroHeal)) ids.Add("heal");
            if (Nz(row.HeroHealMaxHealth)) ids.Add("max_health");
            if (Nz(row.ModifyRoom)) ids.Add("modify_room");

            return ids.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Declared mechanic IDs not detected from detail columns (for pull warnings).</summary>
        public static List<string> FindUndetectedDeclaredMechanics(SpreadsheetActionData row)
        {
            var declared = ParseMechanicsCell(row.Mechanics);
            if (declared.Count == 0)
                return new List<string>();

            var detected = new HashSet<string>(DetectFromSpreadsheetRow(row), StringComparer.OrdinalIgnoreCase);
            return declared.Where(d => !detected.Contains(d)).ToList();
        }

        public static string JoinMechanics(IEnumerable<string> ids)
        {
            return string.Join(", ", ids.Where(id => !string.IsNullOrWhiteSpace(id)));
        }

        /// <summary>Cadence dropdown options for the actions settings editor.</summary>
        public static readonly string[] EditorCadenceOptions = { "Turn", "Action", "Fight", "Dungeon" };

        /// <summary>Mechanic IDs eligible for a cadence in the editor (GOOD + ON ACTIONS). Hero mods list before enemy mods.</summary>
        /// <remarks>
        /// Timing-agnostic mechanics (empty allowed-cadence set, e.g. heal / disrupt) stay available for every cadence
        /// so the Actions-settings mechanic dropdown can always add them.
        /// </remarks>
        public static IReadOnlyList<string> GetMechanicIdsForCadence(string? cadence)
        {
            string normalized = CadenceKeywords.Normalize(cadence ?? "");
            if (string.IsNullOrEmpty(normalized))
                normalized = CadenceKeywords.Turn;
            return AllMechanicIds
                .Where(id =>
                {
                    var allowed = GetAllowedCadencesForMechanic(id);
                    return allowed.Count == 0 || allowed.Contains(normalized);
                })
                .OrderBy(id => id, Comparer<string>.Create(CompareMechanicIdsForEditor))
                .ToList();
        }

        /// <summary>Short Action-set-style label for the mechanics dropdown (e.g. "Hero ACC", "WEAKEN").</summary>
        public static string GetEditorDropdownLabel(string? mechanicId, string? statSubType = null)
        {
            string id = NormalizeMechanicId(mechanicId ?? "");
            if (string.IsNullOrEmpty(id))
                return "";

            string core = GetDisplayLabel(id, statSubType);
            if (id.StartsWith("hero_", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrEmpty(core) ? id : $"Hero {core}";
            if (id.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrEmpty(core) ? id : $"Enemy {core}";
            return string.IsNullOrEmpty(core) ? id : core;
        }

        /// <summary>Editor sort: hero_* first, then neutral mechanics, then enemy_*; alphabetical within each group.</summary>
        public static int CompareMechanicIdsForEditor(string? a, string? b)
        {
            int groupCmp = GetEditorMechanicSortGroup(a).CompareTo(GetEditorMechanicSortGroup(b));
            if (groupCmp != 0)
                return groupCmp;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetEditorMechanicSortGroup(string? mechanicId)
        {
            if (string.IsNullOrWhiteSpace(mechanicId))
                return 1;
            if (mechanicId.StartsWith("hero_", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (mechanicId.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase))
                return 2;
            return 1;
        }

        /// <summary>Player-facing label for card/editor lines (e.g. COMBO, DAMAGE).</summary>
        public static string GetDisplayLabel(string? mechanicId, string? statSubType = null)
        {
            string id = NormalizeMechanicId(mechanicId ?? "");
            if (string.IsNullOrEmpty(id))
                return "";
            if ((id is "hero_stat_bonus" or "enemy_stat_bonus") && !string.IsNullOrWhiteSpace(statSubType))
            {
                return statSubType.Trim().ToUpperInvariant() switch
                {
                    "STRENGTH" => "STR",
                    "AGILITY" => "AGI",
                    "TECHNIQUE" => "TECH",
                    "INTELLIGENCE" => "INT",
                    _ => statSubType.Trim().ToUpperInvariant()
                };
            }
            return id switch
            {
                "hero_accuracy" or "enemy_accuracy" => "ACC",
                "hero_hit_threshold" or "enemy_hit_threshold" => "HIT",
                "hero_combo_threshold" or "enemy_combo_threshold" => "COMBO",
                "hero_crit_threshold" or "enemy_crit_threshold" => "CRIT",
                "hero_crit_miss_threshold" or "enemy_crit_miss_threshold" => "CRIT MISS",
                "hero_next_action_speed" or "enemy_next_action_speed" => "SPEED",
                "hero_next_action_damage" or "enemy_next_action_damage" => "DAMAGE",
                "hero_next_action_multihit" or "enemy_next_action_multihit" => "MULTIHIT",
                "hero_next_action_amp" or "enemy_next_action_amp" => "AMP",
                "hero_weapon_speed" or "enemy_weapon_speed" => "WPN SPD",
                "hero_weapon_damage" or "enemy_weapon_damage" => "WPN DMG",
                "hero_stat_bonus" => "STAT",
                "enemy_stat_bonus" => "ENEMY STAT",
                "weaken" => "WEAKEN",
                "slow" => "SLOW",
                "vulnerability" => "VULNERABILITY",
                "harden" => "HARDEN",
                "focus" => "FOCUS",
                "confuse" => "CONFUSE",
                "stat_drain" => "STAT DRAIN",
                "fortify" => "FORTIFY",
                "disrupt" => "DISRUPT",
                "heal" => "HEAL",
                "max_health" => "MAX HEALTH",
                "advantage" => "ADVANTAGE",
                "disadvantage" => "DISADVANTAGE",
                "pierce" => "PIERCE",
                "self_damage" => "SELF DAMAGE",
                _ => id.Replace('_', ' ').ToUpperInvariant()
            };
        }

        public static bool IsPercentQuantityMechanic(string? mechanicId)
        {
            string id = NormalizeMechanicId(mechanicId ?? "");
            return id is "hero_next_action_speed" or "hero_next_action_damage" or "hero_next_action_amp"
                or "enemy_next_action_speed" or "enemy_next_action_damage" or "enemy_next_action_amp";
        }

        public static bool RequiresStatSubType(string? mechanicId)
        {
            string id = NormalizeMechanicId(mechanicId ?? "");
            return id is "hero_stat_bonus" or "enemy_stat_bonus";
        }

        public static bool TryGetMechanicIdFromBonusType(string? bonusType, out string mechanicId, out string? statSubType)
        {
            mechanicId = "";
            statSubType = null;
            if (string.IsNullOrWhiteSpace(bonusType))
                return false;
            string t = bonusType.Trim().ToUpperInvariant();
            switch (t)
            {
                case "ACCURACY": mechanicId = "hero_accuracy"; return true;
                case "HIT": mechanicId = "hero_hit_threshold"; return true;
                case "COMBO": mechanicId = "hero_combo_threshold"; return true;
                case "CRIT": mechanicId = "hero_crit_threshold"; return true;
                case "CRIT_MISS": mechanicId = "hero_crit_miss_threshold"; return true;
                case "SPEED_MOD": mechanicId = "hero_next_action_speed"; return true;
                case "DAMAGE_MOD": mechanicId = "hero_next_action_damage"; return true;
                case "MULTIHIT_MOD": mechanicId = "hero_next_action_multihit"; return true;
                case "AMP_MOD": mechanicId = "hero_next_action_amp"; return true;
                case "WEAPON_SPEED": mechanicId = "hero_weapon_speed"; return true;
                case "WEAPON_DAMAGE": mechanicId = "hero_weapon_damage"; return true;
                case "STR" or "STRENGTH":
                    mechanicId = "hero_stat_bonus"; statSubType = "STR"; return true;
                case "AGI" or "AGILITY":
                    mechanicId = "hero_stat_bonus"; statSubType = "AGI"; return true;
                case "TECH" or "TECHNIQUE":
                    mechanicId = "hero_stat_bonus"; statSubType = "TECH"; return true;
                case "INT" or "INTELLIGENCE":
                    mechanicId = "hero_stat_bonus"; statSubType = "INT"; return true;
                case "ADVANTAGE":
                    mechanicId = "advantage"; return true;
                case "DISADVANTAGE":
                    mechanicId = "disadvantage"; return true;
                default: return false;
            }
        }

        public static bool TryGetBonusTypeForMechanic(string mechanicId, string? statSubType, out string bonusType)
        {
            bonusType = "";
            string id = NormalizeMechanicId(mechanicId);
            switch (id)
            {
                case "hero_accuracy": bonusType = "ACCURACY"; return true;
                case "enemy_accuracy": bonusType = "ACCURACY"; return true;
                case "hero_hit_threshold": bonusType = "HIT"; return true;
                case "enemy_hit_threshold": bonusType = "HIT"; return true;
                case "hero_combo_threshold": bonusType = "COMBO"; return true;
                case "enemy_combo_threshold": bonusType = "COMBO"; return true;
                case "hero_crit_threshold": bonusType = "CRIT"; return true;
                case "enemy_crit_threshold": bonusType = "CRIT"; return true;
                case "hero_crit_miss_threshold": bonusType = "CRIT_MISS"; return true;
                case "enemy_crit_miss_threshold": bonusType = "CRIT_MISS"; return true;
                case "hero_next_action_speed": bonusType = "SPEED_MOD"; return true;
                case "enemy_next_action_speed": bonusType = "SPEED_MOD"; return true;
                case "hero_next_action_damage": bonusType = "DAMAGE_MOD"; return true;
                case "enemy_next_action_damage": bonusType = "DAMAGE_MOD"; return true;
                case "hero_next_action_multihit": bonusType = "MULTIHIT_MOD"; return true;
                case "enemy_next_action_multihit": bonusType = "MULTIHIT_MOD"; return true;
                case "hero_next_action_amp": bonusType = "AMP_MOD"; return true;
                case "enemy_next_action_amp": bonusType = "AMP_MOD"; return true;
                case "hero_weapon_speed":
                case "enemy_weapon_speed": bonusType = "WEAPON_SPEED"; return true;
                case "hero_weapon_damage":
                case "enemy_weapon_damage": bonusType = "WEAPON_DAMAGE"; return true;
                case "hero_stat_bonus":
                    bonusType = NormalizeStatBonusType(statSubType);
                    return !string.IsNullOrEmpty(bonusType);
                case "enemy_stat_bonus":
                    bonusType = NormalizeStatBonusType(statSubType);
                    return !string.IsNullOrEmpty(bonusType);
                case "advantage":
                    bonusType = MultiDiceRollMapper.AdvantageBonusType;
                    return true;
                case "disadvantage":
                    bonusType = MultiDiceRollMapper.DisadvantageBonusType;
                    return true;
                default: return false;
            }
        }

        private static string NormalizeStatBonusType(string? statSubType)
        {
            if (string.IsNullOrWhiteSpace(statSubType))
                return "";
            return statSubType.Trim().ToUpperInvariant() switch
            {
                "STRENGTH" => "STR",
                "AGILITY" => "AGI",
                "TECHNIQUE" => "TECH",
                "INTELLIGENCE" => "INT",
                _ => statSubType.Trim().ToUpperInvariant()
            };
        }

        private static bool Nz(string? v) => !string.IsNullOrWhiteSpace(v) && v != "0";

        private static bool HasAdvantage(string? highestLowest)
        {
            if (string.IsNullOrWhiteSpace(highestLowest))
                return false;
            return highestLowest.IndexOf("high", StringComparison.OrdinalIgnoreCase) >= 0
                || highestLowest.IndexOf("advantage", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasDisadvantage(string? highestLowest)
        {
            if (string.IsNullOrWhiteSpace(highestLowest))
                return false;
            return highestLowest.IndexOf("low", StringComparison.OrdinalIgnoreCase) >= 0
                || highestLowest.IndexOf("disadvantage", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TriggerContains(string? triggerConditions, string token)
        {
            if (string.IsNullOrWhiteSpace(triggerConditions))
                return false;
            return triggerConditions
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(p =>
                {
                    string part = p.Trim();
                    if (string.Equals(part, token, StringComparison.OrdinalIgnoreCase))
                        return true;
                    // ONROLLVALUE:15 etc.
                    int colon = part.IndexOf(':');
                    if (colon > 0)
                        part = part.Substring(0, colon).Trim();
                    return string.Equals(part, token, StringComparison.OrdinalIgnoreCase);
                });
        }

        private static bool BundleWhen(SpreadsheetActionData row, string whenToken)
        {
            foreach (var b in ActionTriggerSheetColumns.LoadBundles(row))
            {
                if (b.IsEnabled
                    && string.Equals(
                        ActionTriggerSheetColumns.CanonicalWhen(b.When),
                        ActionTriggerSheetColumns.CanonicalWhen(whenToken),
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static Dictionary<string, MechanicCadenceProfile> BuildProfiles()
        {
            var d = new Dictionary<string, MechanicCadenceProfile>(StringComparer.OrdinalIgnoreCase);
            void P(string id, bool turn, bool action, bool fight, bool dungeon, bool onActions, MechanicStatus status) =>
                d[id] = new MechanicCadenceProfile(turn, action, fight, dungeon, onActions, status);

            // Hero / enemy dice & mods (GOOD, ON ACTIONS)
            foreach (string id in new[]
            {
                "hero_accuracy", "hero_hit_threshold", "hero_combo_threshold", "hero_crit_threshold", "hero_crit_miss_threshold",
                "enemy_accuracy", "enemy_hit_threshold", "enemy_combo_threshold", "enemy_crit_threshold", "enemy_crit_miss_threshold",
                "hero_stat_bonus", "enemy_stat_bonus",
            })
                P(id, turn: true, action: false, fight: true, dungeon: true, onActions: true, MechanicStatus.Good);

            P("hero_next_action_speed", false, true, false, false, true, MechanicStatus.Good);
            P("hero_next_action_damage", false, true, false, true, true, MechanicStatus.Good);
            P("hero_action_damage", false, false, false, false, true, MechanicStatus.Good); // same-swing %
            P("hero_next_action_multihit", false, true, false, false, true, MechanicStatus.Good);
            P("hero_next_action_amp", false, true, false, false, true, MechanicStatus.Good);
            P("enemy_next_action_speed", false, true, false, false, true, MechanicStatus.Good);
            P("enemy_next_action_damage", false, true, false, false, true, MechanicStatus.Good);
            P("enemy_next_action_multihit", false, true, false, false, true, MechanicStatus.Good);
            P("enemy_next_action_amp", false, true, false, false, true, MechanicStatus.Good);

            // Flat weapon bonuses (HERO/ENEMY BASE → WEAPON SPEED / WEAPON DAMAGE); all cadences.
            P("hero_weapon_speed", true, true, true, true, true, MechanicStatus.Good);
            P("hero_weapon_damage", true, true, true, true, true, MechanicStatus.Good);
            P("enemy_weapon_speed", true, true, true, true, true, MechanicStatus.Good);
            P("enemy_weapon_damage", true, true, true, true, true, MechanicStatus.Good);

            // stun / poison / burn / bleed — item-applied only; not ACTIONS MECHANICS (see ItemAppliedStatusEffectIds)

            // Status — ON ACTIONS
            P("weaken", true, false, false, false, true, MechanicStatus.Good);
            P("slow", true, false, false, false, true, MechanicStatus.Good);
            P("vulnerability", true, false, false, false, true, MechanicStatus.Good);
            P("harden", true, false, false, false, true, MechanicStatus.Good);
            P("focus", true, false, false, false, true, MechanicStatus.Good);
            P("confuse", true, false, false, false, true, MechanicStatus.Good);
            P("stat_drain", true, false, true, true, true, MechanicStatus.Good);
            P("fortify", true, false, true, true, true, MechanicStatus.Good);
            P("disrupt", false, false, false, false, true, MechanicStatus.Good);
            P("heal", false, false, false, false, true, MechanicStatus.Good);
            P("max_health", true, false, true, true, true, MechanicStatus.Good);
            P("advantage", true, false, true, true, true, MechanicStatus.Good);
            P("disadvantage", true, false, true, true, true, MechanicStatus.Good);

            P("pierce", true, false, false, false, true, MechanicStatus.Good);
            P("opener", false, false, false, false, false, MechanicStatus.Good);
            P("finisher", false, false, false, false, false, MechanicStatus.Good);

            // Strip / deck verbs (fight-scoped via StripMutationApplier)
            foreach (string id in new[]
            {
                "strip_jump", "strip_skip", "strip_repeat", "strip_loop", "strip_stop", "strip_random",
                "strip_disable", "strip_shuffle", "strip_replace_next",
                "combo_jump", "loop_chain", "shuffle", "replace_action", "skip"
            })
                P(id, false, false, false, false, true, MechanicStatus.Good);

            // Retrigger (nested resolve — not Multihit)
            foreach (string id in new[] { "retrigger_next", "retrigger_opener", "retrigger_finisher", "retrigger_slot" })
                P(id, false, false, false, false, true, MechanicStatus.Good);

            // Probability-as-content
            P("salvage_miss", false, false, true, false, true, MechanicStatus.Good);
            P("crit_face_min", false, false, true, true, true, MechanicStatus.Good);
            P("replace_next_roll", false, false, false, false, true, MechanicStatus.Good);
            P("exploding_dice", false, false, false, false, true, MechanicStatus.Good);

            // NEEDS IMPROVEMENT
            P("expose", false, false, false, false, false, MechanicStatus.NeedsImprovement);
            P("reflect", true, false, false, false, false, MechanicStatus.NeedsImprovement);
            P("cleanse", false, false, false, false, true, MechanicStatus.NeedsImprovement);
            P("self_damage", true, true, false, false, true, MechanicStatus.NeedsImprovement);
            foreach (string id in new[]
            {
                "chain_length", "chain_position", "chain_position_bonus", "modify_chain_position",
                "grace", "curse",
                "on_hit", "on_miss", "on_crit", "on_combo", "on_combo_end", "on_connect", "on_crit_miss", "on_kill",
                "on_rooms_cleared", "on_roll_value", "on_natural_roll", "on_health_threshold", "modify_room",
            })
                P(id, false, false, false, false, false, MechanicStatus.NeedsImprovement);

            // CUT (runtime kept; excluded from MECHANIC_LIST)
            foreach (string id in new[]
            {
                "silence", "lifesteal", "consume", "multi_dice",
            })
                P(id, false, false, false, false, false, MechanicStatus.Cut);

            // REDUNDANT (removed as mechanic IDs)
            foreach (string id in new[]
            {
                "damage", "multi_hit", "threshold_bonus", "stat_bonuses", "accumulations",
            })
                P(id, false, false, false, false, false, MechanicStatus.Redundant);

            return d;
        }
    }
}
