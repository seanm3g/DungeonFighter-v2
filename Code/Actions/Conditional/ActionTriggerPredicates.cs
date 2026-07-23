using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Combat.Events;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// AND-filter evaluation helpers for <see cref="ActionTriggerGate"/> (HP, status, tags, history, last enemy).
    /// </summary>
    internal static class ActionTriggerPredicates
    {
        public static bool IsFilterToken(string normalizedToken) =>
            normalizedToken is "ONWIELD"
                or "IFSOURCEHEALTHBELOW" or "IFSOURCEHEALTHABOVE"
                or "IFTARGETHEALTHBELOW" or "IFTARGETHEALTHABOVE"
                or "IFCLUTCH"
                or "IFSAMESACTION" or "IFDIFFERENTACTION"
                or "IFMIRROR" or "IFSWITCHUP"
                or "IFACTIONHASTAG" or "IFGEARHASTAG" or "IFTARGETHASTAG"
                or "IFSOURCESTATUS" or "IFTARGETSTATUS"
                or "IFSOURCEUNDERDOT" or "IFTARGETUNDERDOT"
                or "IFLASTENEMY" or "IFLASTSTAND"
                or "IFSLOT" or "IFUNARMED" or "IFCLASSTAG";

        public static bool TryClassifyFilter(string raw, out string family, out string? arg)
        {
            family = "";
            arg = null;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string upper = raw.Trim().ToUpperInvariant();
            int colon = upper.IndexOf(':');
            string token = colon >= 0 ? upper.Substring(0, colon).Trim() : upper;
            arg = colon >= 0 && colon < upper.Length - 1 ? upper.Substring(colon + 1).Trim() : null;
            token = ActionTriggerGate.NormalizeToken(token);

            if (token is "IFMIRROR")
                token = "IFSAMESACTION";
            if (token is "IFSWITCHUP")
                token = "IFDIFFERENTACTION";
            if (token is "IFLASTSTAND")
                token = "IFLASTENEMY";
            if (token is "IFCLUTCH")
            {
                family = "IFCLUTCH";
                arg ??= "0.25";
                return true;
            }

            if (!IsFilterToken(token))
                return false;

            family = token;
            return true;
        }

        /// <summary>
        /// All present filter families must pass. Within a multi-value family (wield / tags / status) values OR.
        /// </summary>
        public static bool AllFiltersPass(
            List<(string Family, string? Arg)> filters,
            CombatEvent? combatEvent,
            Action action)
        {
            if (filters.Count == 0)
                return true;

            var byFamily = new Dictionary<string, List<string?>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (family, arg) in filters)
            {
                if (!byFamily.TryGetValue(family, out var list))
                {
                    list = new List<string?>();
                    byFamily[family] = list;
                }
                list.Add(arg);
            }

            Actor? source = combatEvent?.Source;
            Actor? target = combatEvent?.Target;
            // Item procs pass a synthetic carrier (ItemProc:…); swing identity lives on the event.
            Action subject = ResolveSwingSubject(action, combatEvent);

            foreach (var kv in byFamily)
            {
                if (!FamilyPasses(kv.Key, kv.Value, source, target, subject, combatEvent))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Action used for name/tag/mirror filters: combat swing when present, else the evaluated action.
        /// </summary>
        public static Action ResolveSwingSubject(Action action, CombatEvent? combatEvent) =>
            combatEvent?.Action ?? action;

        private static bool FamilyPasses(
            string family,
            List<string?> args,
            Actor? source,
            Actor? target,
            Action action,
            CombatEvent? combatEvent)
        {
            return family switch
            {
                "ONWIELD" => MatchesAnyWield(source, args),
                "IFSOURCEHEALTHBELOW" => args.All(a => HealthBelow(source, a)),
                "IFSOURCEHEALTHABOVE" => args.All(a => HealthAbove(source, a)),
                "IFTARGETHEALTHBELOW" => args.All(a => HealthBelow(target, a)),
                "IFTARGETHEALTHABOVE" => args.All(a => HealthAbove(target, a)),
                "IFCLUTCH" => HealthBelow(source, args.FirstOrDefault() ?? "0.25"),
                "IFSAMESACTION" => MatchesSameAction(source, action),
                "IFDIFFERENTACTION" => MatchesDifferentAction(source, action),
                "IFACTIONHASTAG" => args.Any(a => ActionHasTag(source, action, a)),
                "IFGEARHASTAG" => args.Any(a => GearHasTag(source, a)),
                "IFTARGETHASTAG" => args.Any(a => TargetHasTag(target, a)),
                "IFSOURCESTATUS" => args.Any(a => HasStatus(source, a)),
                "IFTARGETSTATUS" => args.Any(a => HasStatus(target, a)),
                "IFSOURCEUNDERDOT" => HasAnyDot(source),
                "IFTARGETUNDERDOT" => HasAnyDot(target),
                "IFLASTENEMY" => CombatTriggerContext.LivingEnemyCountAtFightStart == 1,
                "IFSLOT" => args.Any(a => MatchesComboSlot(source, a)),
                "IFUNARMED" => source is Character c && c.Weapon == null,
                "IFCLASSTAG" => args.Any(a => ClassTagMatches(source, a)),
                _ => false
            };
        }

        private static bool MatchesAnyWield(Actor? source, List<string?> args)
        {
            var types = new List<WeaponType>();
            foreach (var a in args)
            {
                if (ActionTriggerGate.TryParseWeaponTypeName(a, out var wt) && !types.Contains(wt))
                    types.Add(wt);
            }
            return ActionTriggerGate.MatchesWieldFilter(source, types);
        }

        private static bool MatchesSameAction(Actor? source, Action action)
        {
            if (source == null || string.IsNullOrWhiteSpace(action.Name))
                return false;
            if (!CombatTriggerContext.TryGetPreviousActionName(source, out var prev) || prev == null)
                return false;
            return string.Equals(prev, action.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesDifferentAction(Actor? source, Action action)
        {
            if (source == null || string.IsNullOrWhiteSpace(action.Name))
                return false;
            if (!CombatTriggerContext.TryGetPreviousActionName(source, out var prev) || prev == null)
                return false;
            return !string.Equals(prev, action.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ActionHasTag(Actor? source, Action action, string? tag) =>
            SubjectHasTag(source, action, tag);

        /// <summary>
        /// True when the subject action has <paramref name="tag"/> on its sheet tags
        /// or via character <see cref="Character.EquipmentGrantedActionTags"/> overlay.
        /// </summary>
        public static bool SubjectHasTag(Actor? source, Action action, string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || action == null)
                return false;
            if (action.Tags != null
                && action.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (source is Character character
                && character.EquipmentGrantedActionTags != null
                && character.EquipmentGrantedActionTags.Any(t =>
                    string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                return true;
            return false;
        }

        private static bool MatchesComboSlot(Actor? source, string? arg)
        {
            if (source is not Character character || string.IsNullOrWhiteSpace(arg))
                return false;
            if (!int.TryParse(arg.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int oneBased)
                || oneBased < 1)
                return false;
            var combo = character.GetComboActions();
            if (combo == null || combo.Count == 0)
                return false;
            int currentOneBased = (character.ComboStep % combo.Count) + 1;
            return currentOneBased == oneBased;
        }

        private static bool ClassTagMatches(Actor? source, string? arg)
        {
            if (source is not Character || string.IsNullOrWhiteSpace(arg))
                return false;
            // Class tag on filter matches if any equipped item carries that tag (item class identity),
            // or the hero's equipped weapon path class display name matches.
            string want = arg.Trim();
            if (GearHasTag(source, want))
                return true;
            if (source is Character hero && hero.Weapon is WeaponItem w)
            {
                var cp = GameConfiguration.Instance?.ClassPresentation ?? new ClassPresentationConfig();
                string className = cp.GetDisplayName(w.WeaponType);
                return string.Equals(className, want, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private static bool GearHasTag(Actor? source, string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || source is not Character character)
                return false;

            // Optional count quantifier: gold:count>=2
            string tagOnly = tag;
            int? minCount = null;
            int countSep = tag.IndexOf(":count>=", StringComparison.OrdinalIgnoreCase);
            if (countSep < 0)
                countSep = tag.IndexOf(":COUNT>=", StringComparison.OrdinalIgnoreCase);
            if (countSep > 0)
            {
                tagOnly = tag.Substring(0, countSep);
                string rest = tag.Substring(countSep);
                int ge = rest.IndexOf(">=", StringComparison.Ordinal);
                if (ge >= 0
                    && int.TryParse(rest.Substring(ge + 2).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                    && n > 0)
                    minCount = n;
            }

            int count = 0;
            if (ItemHasTag(character.Weapon, tagOnly)) count++;
            if (ItemHasTag(character.Body, tagOnly)) count++;
            if (ItemHasTag(character.Head, tagOnly)) count++;
            if (ItemHasTag(character.Feet, tagOnly)) count++;
            if (ItemHasTag(character.Legs, tagOnly)) count++;
            if (minCount.HasValue)
                return count >= minCount.Value;
            return count > 0;
        }

        private static bool ItemHasTag(Item? item, string tag) =>
            item?.Tags != null && item.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));

        private static bool TargetHasTag(Actor? target, string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || target is not Enemy enemy)
                return false;
            return enemy.Tags != null
                && enemy.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasAnyDot(Actor? actor) =>
            actor != null
            && (actor.PoisonPercentOfMaxHealth > 0
                || actor.BurnIntensity > 0 || actor.PendingBurnFromHits > 0
                || actor.BleedIntensity > 0 || actor.PendingBleedFromHits > 0
                || actor.AcidIntensity > 0 || actor.PendingAcidFromHits > 0);

        public static bool HasStatus(Actor? actor, string? statusName)
        {
            if (actor == null || string.IsNullOrWhiteSpace(statusName))
                return false;

            string s = statusName.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return s switch
            {
                "POISON" or "POISONED" => actor.PoisonPercentOfMaxHealth > 0,
                "BURN" or "BURNING" or "FIRE" => actor.BurnIntensity > 0 || actor.PendingBurnFromHits > 0,
                "BLEED" or "BLEEDING" => actor.BleedIntensity > 0 || actor.PendingBleedFromHits > 0 || actor.IsBleeding,
                "ACID" or "ACIDIC" => actor.AcidIntensity > 0 || actor.PendingAcidFromHits > 0,
                "DOT" or "ANY" or "UNDERDOT" => HasAnyDot(actor),
                "STUN" or "STUNNED" => actor.IsStunned,
                "SILENCE" or "SILENCED" => actor.IsSilenced,
                "EXPOSE" or "EXPOSED" => (actor.ExposeStacks ?? 0) > 0,
                "MARK" or "MARKED" => actor.IsMarked,
                "SLOW" or "SLOWED" => actor is Character c && c.Effects.SlowTurns > 0,
                _ => false
            };
        }

        private static bool HealthBelow(Actor? actor, string? arg)
        {
            if (!TryParseHealthFraction(arg, out double frac))
                return false;
            return GetHealthRatio(actor) <= frac;
        }

        private static bool HealthAbove(Actor? actor, string? arg)
        {
            if (!TryParseHealthFraction(arg, out double frac))
                return false;
            return GetHealthRatio(actor) >= frac;
        }

        public static bool TryParseHealthFraction(string? raw, out double fraction)
        {
            fraction = 0;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            if (!double.TryParse(raw.Trim().TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return false;
            if (v > 1.0)
                v /= 100.0;
            if (v < 0 || v > 1.0)
                return false;
            fraction = v;
            return true;
        }

        private static double GetHealthRatio(Actor? actor)
        {
            if (actor is Character character)
            {
                int max = character.MaxHealth;
                if (max <= 0) return 0;
                return (double)character.CurrentHealth / max;
            }
            return 0;
        }

        public static string FormatFilterToken(string family, string? arg)
        {
            family = ActionTriggerGate.NormalizeToken(family);
            if (family is "IFMIRROR") family = "IFSAMESACTION";
            if (family is "IFSWITCHUP") family = "IFDIFFERENTACTION";
            if (family is "IFLASTSTAND") family = "IFLASTENEMY";
            if (family is "IFCLUTCH")
                return "IFCLUTCH";

            if (string.IsNullOrWhiteSpace(arg))
                return family;

            if (family is "IFSOURCEHEALTHBELOW" or "IFSOURCEHEALTHABOVE"
                or "IFTARGETHEALTHBELOW" or "IFTARGETHEALTHABOVE")
            {
                if (TryParseHealthFraction(arg, out double frac))
                    return $"{family}:{frac.ToString("0.##", CultureInfo.InvariantCulture)}";
            }

            if (family is "IFSOURCESTATUS" or "IFTARGETSTATUS")
                return $"{family}:{NormalizeStatusArg(arg)}";

            if (family is "IFACTIONHASTAG" or "IFGEARHASTAG" or "IFTARGETHASTAG")
                return $"{family}:{arg.Trim().ToLowerInvariant()}";

            if (family == "ONWIELD" && ActionTriggerGate.TryParseWeaponTypeName(arg, out var wt))
                return ActionTriggerGate.FormatWieldToken(wt);

            return $"{family}:{arg.Trim()}";
        }

        private static string NormalizeStatusArg(string arg)
        {
            string s = arg.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return s switch
            {
                "POISONED" => "poison",
                "BURNING" or "FIRE" => "burn",
                "BLEEDING" => "bleed",
                "ACIDIC" => "acid",
                "STUNNED" => "stun",
                "SILENCED" => "silence",
                "EXPOSED" => "expose",
                "MARKED" => "mark",
                "SLOWED" => "slow",
                "DOT" or "ANY" or "UNDERDOT" => "dot",
                _ => arg.Trim().ToLowerInvariant()
            };
        }
    }
}
