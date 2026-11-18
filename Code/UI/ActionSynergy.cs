using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RPGGame.UI
{
    /// <summary>
    /// What an action has in common with other actions and the hero's worn gear, for the Action Sequence editor's
    /// synergy highlight. An action's keys are its mechanic tags, the statuses it applies or names in its description,
    /// and the attributes it names (STR / AGI / TEC / INT). Two actions synergize when they share a key; a key that is
    /// only a class or weapon type is a weak link (most of a class's actions share it). A worn piece links when its
    /// base modifier rule names one of the keys (<c>IF CLASS barbarian</c>, <c>POTENCY burn</c>, <c>IFTAG bludgeon</c>).
    /// </summary>
    public static class ActionSynergy
    {
        /// <summary>Bookkeeping / rarity / category tags that say nothing about how actions work together.</summary>
        private static readonly HashSet<string> MetaTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "common", "uncommon", "rare", "epic", "legendary", "mythic",
            "class", "hybrid", "action", "verb", "store", "status",
            "startingweapon", "starter", "unique", "required", "weapon_basic",
            "enemy", "opener", "finisher", "neutral"
        };

        private static readonly string[] StatusWords =
        {
            "bleed", "poison", "burn", "acid", "weaken", "slow", "stun", "expose", "harden", "focus", "fortify",
            "mark", "haste", "stagger", "brittle", "rattle", "regen", "pierce", "silence", "confusion", "absorb",
            "frail", "reflect", "shield"
        };

        private static readonly string[] AttributeWords = { "STR", "AGI", "TEC", "INT" };

        private static readonly HashSet<string> WeaponTypeTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "mace", "sword", "dagger", "wand"
        };

        private static readonly Regex WordSplit = new(@"[^A-Za-z_]+", RegexOptions.Compiled);

        /// <summary>One shared key between two actions (or an action and a worn piece).</summary>
        public readonly record struct Link(string Key, bool Strong);

        /// <summary>True when <paramref name="key"/> is a class or weapon type (shared by most of a class's actions).</summary>
        public static bool IsClassKey(string key) =>
            ClassTags.All.Contains(key, StringComparer.OrdinalIgnoreCase) || WeaponTypeTags.Contains(key);

        /// <summary>Lowercase synergy keys for <paramref name="action"/> (attributes stay uppercase).</summary>
        public static HashSet<string> Keys(Action? action)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (action == null)
                return keys;

            if (action.Tags != null)
            {
                foreach (string raw in action.Tags)
                {
                    string t = (raw ?? "").Trim().ToLowerInvariant();
                    if (t.Length > 0 && !MetaTags.Contains(t))
                        keys.Add(t);
                }
            }

            void AddIf(bool on, string key) { if (on) keys.Add(key); }
            AddIf(action.CausesBleed, "bleed");
            AddIf(action.CausesPoison, "poison");
            AddIf(action.CausesBurn, "burn");
            AddIf(action.CausesAcid, "acid");
            AddIf(action.CausesWeaken, "weaken");
            AddIf(action.CausesSlow, "slow");
            AddIf(action.CausesStun, "stun");
            AddIf(action.CausesExpose || action.CausesVulnerability || action.CausesArmorBreak, "expose");
            AddIf(action.CausesHarden, "harden");
            AddIf(action.CausesFocus, "focus");
            AddIf(action.CausesFortify, "fortify");
            AddIf(action.CausesMark, "mark");
            AddIf(action.CausesHPRegen, "regen");
            AddIf(action.CausesPierce, "pierce");
            AddIf(action.CausesSilence, "silence");
            AddIf(action.CausesConfusion, "confusion");
            AddIf(action.CausesAbsorb, "absorb");

            string text = (action.Description ?? "") + " " + (action.Verb ?? "");
            if (text.Trim().Length > 0)
            {
                foreach (string word in WordSplit.Split(text))
                {
                    if (word.Length == 0)
                        continue;
                    string lower = word.ToLowerInvariant();
                    if (StatusWords.Contains(lower))
                        keys.Add(lower);
                    else if (AttributeWords.Contains(word, StringComparer.Ordinal))
                        keys.Add(word);
                }
            }
            return keys;
        }

        /// <summary>
        /// Shared keys between two actions, strongest first: mechanic keys (tags, statuses, attributes) before
        /// class / weapon-type keys. Empty when they share nothing.
        /// </summary>
        public static List<Link> Shared(HashSet<string> a, HashSet<string> b)
        {
            var links = new List<Link>();
            foreach (string k in a)
            {
                if (b.Contains(k))
                    links.Add(new Link(k, !IsClassKey(k)));
            }
            return links.OrderByDescending(l => l.Strong).ThenBy(l => l.Key, StringComparer.Ordinal).ToList();
        }

        /// <summary>Worn pieces whose base modifier rule names one of <paramref name="keys"/>: (item name, key).</summary>
        public static List<(string Item, string Key)> GearLinks(Character? hero, HashSet<string> keys)
        {
            var result = new List<(string, string)>();
            if (hero == null || hero is Enemy || keys.Count == 0)
                return result;
            foreach (var item in MaterialSetController.EnumerateEquipped(hero))
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ImplicitRule))
                    continue;
                var ruleWords = new HashSet<string>(
                    WordSplit.Split(item.ImplicitRule).Where(w => w.Length > 0).Select(w => w.ToLowerInvariant()),
                    StringComparer.Ordinal);
                string? hit = keys
                    .OrderBy(k => IsClassKey(k) ? 1 : 0)
                    .FirstOrDefault(k => ruleWords.Contains(k.ToLowerInvariant()));
                if (hit != null)
                    result.Add((item.Name ?? "", hit));
            }
            return result;
        }
    }
}
