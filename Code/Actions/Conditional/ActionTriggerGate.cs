using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Live combat gate for action status effects based on <see cref="ConditionalTriggerProperties"/>.
    /// Empty conditions → apply on successful <see cref="CombatEventType.ActionHit"/> only (not miss/kill).
    /// Non-empty → OR of matching outcome tokens, ANDed with optional filter tokens
    /// (<c>ONWIELD</c>, HP/status/tag/history/last-enemy). Standalone filters imply connect.
    /// </summary>
    public static class ActionTriggerGate
    {
        /// <summary>
        /// Canonical tokens designers can put in <c>triggerConditions</c>.
        /// </summary>
        public static readonly string[] KnownTokens =
        {
            "ONHIT", "ONNORMALHIT", "ONCONNECT", "ONANYHIT",
            "ONMISS", "ONCRITICALMISS", "ONCRITMISS",
            "ONCOMBO", "ONCOMBOHIT", "ONCOMBOEND", "ONCOMBOENDED",
            "ONCRITICAL", "ONCRITICALHIT", "ONCRIT",
            "ONKILL", "ONROLLVALUE", "ONNATURALROLL", "ONEVEN", "ONODD",
            "ONHEALTHTHRESHOLD", "ONENEMYHEALTHTHRESHOLD",
            "ONROOMSCLEARED", "ONROOMCLEARED",
            "ONFIRSTHIT", "ONFIRSTBLOOD", "ONAFTERMISS",
            "ONTAKEHIT", "ONHEROHURT",
            "ONWIELD",
            "IFCLUTCH", "IFSOURCEHEALTHBELOW", "IFSOURCEHEALTHABOVE",
            "IFTARGETHEALTHBELOW", "IFTARGETHEALTHABOVE",
            "IFSAMESACTION", "IFDIFFERENTACTION", "IFMIRROR", "IFSWITCHUP",
            "IFACTIONHASTAG", "IFGEARHASTAG", "IFTARGETHASTAG",
            "IFSOURCESTATUS", "IFTARGETSTATUS", "IFSOURCEUNDERDOT", "IFTARGETUNDERDOT",
            "IFLASTENEMY", "IFLASTSTAND",
            "IFSLOT", "IFUNARMED", "IFCLASSTAG",
            "IFATTR"
        };

        /// <summary>
        /// Non-outcome filters on the row (ONWIELD, HP/status/tag/history, required tag) must still pass
        /// for <see cref="ActionTriggerBundleApplicator"/> grants. Empty filters ⇒ true.
        /// </summary>
        public static bool PassesNonOutcomeFilters(Action action, CombatEvent? combatEvent)
        {
            if (action?.Triggers == null)
                return true;

            var filters = new List<(string Family, string? Arg)>();
            var conditions = action.Triggers.TriggerConditions;
            if (conditions != null)
            {
                foreach (var raw in conditions)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;
                    if (ActionTriggerPredicates.TryClassifyFilter(raw, out string family, out string? arg))
                        filters.Add((family, arg));
                }
            }

            if (filters.Count > 0 && !ActionTriggerPredicates.AllFiltersPass(filters, combatEvent, action))
                return false;

            if (!string.IsNullOrWhiteSpace(action.Triggers.RequiredTag))
            {
                string tag = action.Triggers.RequiredTag!.Trim();
                Action subject = ActionTriggerPredicates.ResolveSwingSubject(action, combatEvent);
                if (!ActionTriggerPredicates.SubjectHasTag(combatEvent?.Source, subject, tag))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Whether this action's status effects should apply for the given combat event.
        /// </summary>
        public static bool ShouldApplyStatusEffects(Action action, CombatEvent? combatEvent)
        {
            if (action?.Triggers == null)
                return combatEvent == null || combatEvent.Type == CombatEventType.ActionHit;

            var conditions = action.Triggers.TriggerConditions;
            bool hasStringConditions = conditions != null && conditions.Count > 0;
            bool hasExactRoll = action.Triggers.ExactRollTriggerValue > 0;
            bool hasRoomsClearedN = action.Triggers.RoomsClearedTriggerValue > 0;
            bool hasRequiredTag = !string.IsNullOrWhiteSpace(action.Triggers.RequiredTag);

            if (!hasStringConditions && !hasExactRoll && !hasRoomsClearedN && !hasRequiredTag)
            {
                if (combatEvent == null)
                    return true;
                return combatEvent.Type == CombatEventType.ActionHit;
            }

            var filters = new List<(string Family, string? Arg)>();
            bool hasOutcomeTokens = false;
            bool outcomeMatched = false;

            if (hasStringConditions)
            {
                foreach (var raw in conditions!)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;

                    if (ActionTriggerPredicates.TryClassifyFilter(raw, out string family, out string? arg))
                    {
                        filters.Add((family, arg));
                        continue;
                    }

                    hasOutcomeTokens = true;
                    if (MatchesConditionToken(raw, action, combatEvent))
                        outcomeMatched = true;
                }
            }

            if (!outcomeMatched && hasExactRoll && combatEvent != null)
            {
                hasOutcomeTokens = true;
                outcomeMatched = combatEvent.RollValue == action.Triggers.ExactRollTriggerValue;
            }

            if (!outcomeMatched && hasRoomsClearedN && combatEvent != null
                && combatEvent.Type == CombatEventType.RoomCleared)
            {
                hasOutcomeTokens = true;
                outcomeMatched = combatEvent.RoomsClearedCount == action.Triggers.RoomsClearedTriggerValue;
            }

            // Standalone filters (no outcome tokens) ⇒ implicit connect
            if (!outcomeMatched && !hasOutcomeTokens && filters.Count > 0)
            {
                outcomeMatched = combatEvent != null
                    && combatEvent.Type == CombatEventType.ActionHit
                    && !combatEvent.IsMiss;
            }

            if (!outcomeMatched)
                return false;

            if (!ActionTriggerPredicates.AllFiltersPass(filters, combatEvent, action))
                return false;

            if (hasRequiredTag)
            {
                string tag = action.Triggers.RequiredTag!.Trim();
                Action subject = ActionTriggerPredicates.ResolveSwingSubject(action, combatEvent);
                if (!ActionTriggerPredicates.SubjectHasTag(combatEvent?.Source, subject, tag))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// True when <paramref name="rawCondition"/> matches the event (supports <c>ONROLLVALUE:15</c>).
        /// Filter tokens are not outcomes — use <see cref="ShouldApplyStatusEffects"/>.
        /// </summary>
        public static bool MatchesConditionToken(string rawCondition, Action action, CombatEvent? combatEvent)
        {
            if (combatEvent == null || string.IsNullOrWhiteSpace(rawCondition))
                return false;

            if (ActionTriggerPredicates.TryClassifyFilter(rawCondition, out _, out _))
                return false;

            string upper = rawCondition.Trim().ToUpperInvariant();
            int colon = upper.IndexOf(':');
            string token = colon >= 0 ? upper.Substring(0, colon).Trim() : upper;
            string? arg = colon >= 0 && colon < upper.Length - 1 ? upper.Substring(colon + 1).Trim() : null;

            token = NormalizeToken(token);

            return token switch
            {
                "ONMISS" => combatEvent.IsMiss || combatEvent.Type == CombatEventType.ActionMiss,
                "ONCRITICALMISS" => combatEvent.IsCriticalMiss
                    || (combatEvent.Type == CombatEventType.ActionMiss && combatEvent.IsCriticalMiss),
                "ONHIT" or "ONNORMALHIT" => combatEvent.Type == CombatEventType.ActionHit
                    && !combatEvent.IsCombo
                    && !combatEvent.IsCritical
                    && !combatEvent.IsMiss,
                "ONCONNECT" or "ONANYHIT" => combatEvent.Type == CombatEventType.ActionHit
                    && !combatEvent.IsMiss,
                "ONCOMBO" or "ONCOMBOHIT" => combatEvent.IsCombo && !combatEvent.IsMiss,
                "ONCRITICAL" or "ONCRITICALHIT" => combatEvent.IsCritical && !combatEvent.IsMiss,
                "ONKILL" => combatEvent.Type == CombatEventType.EnemyDied,
                "ONROLLVALUE" => MatchesExactRoll(action, combatEvent, arg),
                "ONNATURALROLL" => MatchesNaturalRoll(action, combatEvent, arg),
                "ONEVEN" => MatchesRollParity(combatEvent, even: true),
                "ONODD" => MatchesRollParity(combatEvent, even: false),
                "ONHEALTHTHRESHOLD" or "ONENEMYHEALTHTHRESHOLD" =>
                    combatEvent.Type == CombatEventType.EnemyHealthThreshold,
                "ONCOMBOEND" or "ONCOMBOENDED" => combatEvent.Type == CombatEventType.ComboEnded,
                "ONROOMSCLEARED" or "ONROOMCLEARED" => MatchesRoomsCleared(action, combatEvent, arg),
                "ONFIRSTHIT" or "ONFIRSTBLOOD" => MatchesFirstBlood(combatEvent),
                "ONAFTERMISS" => MatchesAfterMiss(combatEvent),
                "ONTAKEHIT" or "ONHEROHURT" => MatchesTakeHit(combatEvent),
                _ => false
            };
        }

        public static string NormalizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return "";
            string t = token.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return t switch
            {
                "ONCRIT" => "ONCRITICAL",
                "ONCRITMISS" => "ONCRITICALMISS",
                "ONANYHIT" => "ONCONNECT",
                "ONNORMALHIT" => "ONHIT",
                "ONCOMBOHIT" => "ONCOMBO",
                "ONCOMBOENDED" => "ONCOMBOEND",
                "ONCRITICALHIT" => "ONCRITICAL",
                "ONENEMYHEALTHTHRESHOLD" => "ONHEALTHTHRESHOLD",
                "ONNATURALROLL" => "ONNATURALROLL",
                "ONROOMCLEARED" => "ONROOMSCLEARED",
                "ONFIRSTBLOOD" => "ONFIRSTHIT",
                "ONHEROHURT" => "ONTAKEHIT",
                "IFMIRROR" => "IFSAMESACTION",
                "IFSWITCHUP" => "IFDIFFERENTACTION",
                "IFLASTSTAND" => "IFLASTENEMY",
                _ => t
            };
        }

        /// <summary>
        /// Parses spreadsheet / form trigger strings into canonical condition list entries.
        /// </summary>
        public static List<string> ParseTriggerConditionList(string? raw)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                string trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                string upper = trimmed.ToUpperInvariant();
                int colon = upper.IndexOf(':');
                if (colon > 0)
                {
                    string token = NormalizeToken(upper.Substring(0, colon));
                    string arg = upper.Substring(colon + 1).Trim();
                    if (token == "ONROLLVALUE" && int.TryParse(arg, out _))
                    {
                        AddUnique(result, $"ONROLLVALUE:{arg}");
                        continue;
                    }
                    if (token == "ONNATURALROLL" && int.TryParse(arg, out _))
                    {
                        AddUnique(result, $"ONNATURALROLL:{arg}");
                        continue;
                    }
                    if (token == "ONROOMSCLEARED" && int.TryParse(arg, out _))
                    {
                        AddUnique(result, $"ONROOMSCLEARED:{arg}");
                        continue;
                    }
                    if (ActionTriggerPredicates.TryClassifyFilter($"{token}:{arg}", out string family, out string? fArg))
                    {
                        AddUnique(result, ActionTriggerPredicates.FormatFilterToken(family, fArg ?? arg));
                        continue;
                    }
                }

                string normalized = NormalizeToken(upper);
                if (ActionTriggerPredicates.TryClassifyFilter(normalized, out string fam, out string? famArg))
                {
                    AddUnique(result, ActionTriggerPredicates.FormatFilterToken(fam, famArg));
                    continue;
                }

                if (IsKnownCanonical(normalized))
                {
                    string stored = normalized switch
                    {
                        "ONCRITICALMISS" => "ONCRITICALMISS",
                        "ONCONNECT" => "ONCONNECT",
                        "ONKILL" => "ONKILL",
                        "ONROLLVALUE" => "ONROLLVALUE",
                        "ONNATURALROLL" => "ONNATURALROLL",
                        "ONHEALTHTHRESHOLD" => "ONHEALTHTHRESHOLD",
                        "ONCOMBOEND" => "ONCOMBOEND",
                        "ONROOMSCLEARED" => "ONROOMSCLEARED",
                        "ONFIRSTHIT" => "ONFIRSTHIT",
                        "ONAFTERMISS" => "ONAFTERMISS",
                        "ONHIT" => "ONHIT",
                        "ONMISS" => "ONMISS",
                        "ONCOMBO" => "ONCOMBO",
                        "ONCRITICAL" => "ONCRITICAL",
                        _ => normalized
                    };
                    AddUnique(result, stored);
                }
            }

            return result;
        }

        /// <summary>Canonical stored form, e.g. <c>ONWIELD:Sword</c>.</summary>
        public static string FormatWieldToken(WeaponType weaponType) => $"ONWIELD:{weaponType}";

        public static bool TryParseWieldToken(string? raw, out WeaponType weaponType)
        {
            weaponType = default;
            if (!ActionTriggerPredicates.TryClassifyFilter(raw ?? "", out string family, out string? arg))
                return false;
            if (family != "ONWIELD")
                return false;
            return TryParseWeaponTypeName(arg, out weaponType);
        }

        public static bool TryParseWeaponTypeName(string? name, out WeaponType weaponType)
        {
            weaponType = default;
            if (string.IsNullOrWhiteSpace(name))
                return false;
            return Enum.TryParse(name.Trim(), ignoreCase: true, out weaponType)
                && Enum.IsDefined(typeof(WeaponType), weaponType);
        }

        public static bool MatchesWieldFilter(Actor? source, IReadOnlyList<WeaponType> allowedTypes)
        {
            if (source == null || allowedTypes == null || allowedTypes.Count == 0)
                return false;

            if (!TryGetWieldedWeaponType(source, out WeaponType held))
                return false;

            for (int i = 0; i < allowedTypes.Count; i++)
            {
                if (allowedTypes[i] == held)
                    return true;
            }

            return false;
        }

        public static bool TryGetWieldedWeaponType(Actor source, out WeaponType weaponType)
        {
            weaponType = default;
            Item? weapon = null;
            if (source is Character character)
                weapon = character.Weapon;

            if (weapon == null)
                return false;

            return GearActionNames.TryResolveWeaponType(weapon, out weaponType);
        }

        private static bool MatchesFirstBlood(CombatEvent combatEvent) =>
            combatEvent.Type == CombatEventType.ActionHit
            && !combatEvent.IsMiss
            && !CombatTriggerContext.SourceHasConnectedThisFight(combatEvent.Source);

        private static bool MatchesAfterMiss(CombatEvent combatEvent) =>
            combatEvent.Type == CombatEventType.ActionHit
            && !combatEvent.IsMiss
            && CombatTriggerContext.SourceLastSwingWasMiss(combatEvent.Source);

        /// <summary>Hero took a successful hit (defender is a non-enemy character, attacker is someone else).</summary>
        private static bool MatchesTakeHit(CombatEvent combatEvent) =>
            combatEvent.Type == CombatEventType.ActionHit
            && !combatEvent.IsMiss
            && combatEvent.Target is Character hero
            && hero is not Enemy
            && !ReferenceEquals(combatEvent.Source, combatEvent.Target);

        private static bool IsKnownCanonical(string normalized) =>
            normalized is "ONHIT" or "ONMISS" or "ONCOMBO" or "ONCRITICAL"
                or "ONCONNECT" or "ONCRITICALMISS" or "ONKILL" or "ONROLLVALUE" or "ONNATURALROLL"
                or "ONEVEN" or "ONODD"
                or "ONHEALTHTHRESHOLD" or "ONCOMBOEND" or "ONROOMSCLEARED"
                or "ONFIRSTHIT" or "ONAFTERMISS" or "ONTAKEHIT";

        private static bool MatchesRollParity(CombatEvent combatEvent, bool even)
        {
            int face = combatEvent.NaturalRollValue > 0
                ? combatEvent.NaturalRollValue
                : combatEvent.RollValue;
            if (face <= 0)
                return false;
            bool isEven = (face % 2) == 0;
            return even ? isEven : !isEven;
        }

        private static void AddUnique(List<string> result, string stored)
        {
            if (!result.Exists(c => string.Equals(c, stored, StringComparison.OrdinalIgnoreCase)))
                result.Add(stored);
        }

        private static bool MatchesExactRoll(Action action, CombatEvent combatEvent, string? argFromToken)
        {
            int expected = action.Triggers.ExactRollTriggerValue;
            if (!string.IsNullOrWhiteSpace(argFromToken) && int.TryParse(argFromToken, out int fromToken))
                expected = fromToken;
            if (expected <= 0)
                return false;
            return combatEvent.RollValue == expected;
        }

        /// <summary>Compares <see cref="CombatEvent.NaturalRollValue"/> (die face), not attack total.</summary>
        private static bool MatchesNaturalRoll(Action action, CombatEvent combatEvent, string? argFromToken)
        {
            int expected = 0;
            if (!string.IsNullOrWhiteSpace(argFromToken) && int.TryParse(argFromToken, out int fromToken))
                expected = fromToken;
            else if (action.Triggers?.ExactRollTriggerValue > 0)
                expected = action.Triggers.ExactRollTriggerValue;
            if (expected <= 0)
                return false;
            int natural = combatEvent.NaturalRollValue > 0 ? combatEvent.NaturalRollValue : combatEvent.RollValue;
            return natural == expected;
        }

        private static bool MatchesRoomsCleared(Action action, CombatEvent combatEvent, string? argFromToken)
        {
            if (combatEvent.Type != CombatEventType.RoomCleared)
                return false;

            int required = action.Triggers.RoomsClearedTriggerValue;
            if (!string.IsNullOrWhiteSpace(argFromToken) && int.TryParse(argFromToken, out int fromToken))
                required = fromToken;

            if (required <= 0)
                return true;

            return combatEvent.RoomsClearedCount == required;
        }
    }
}
