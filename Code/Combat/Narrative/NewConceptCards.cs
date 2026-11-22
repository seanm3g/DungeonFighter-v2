using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.Combat.Narrative
{
    /// <summary>One NEW card: the status, who holds it and the one sentence it shows.</summary>
    public sealed record NewConceptCard(string StatusId, string Label, string Text, Actor? Holder, Actor? Source, DateTimeOffset ShownUtc);

    /// <summary>
    /// NEW cards (D9.3, Phase 9 proposal 6): the first time a status appears for this hero, a small card shows on the board
    /// beside the enemy that brought it ("NEW · Rattle: your next rolls are lower."), and the next loot reveal marks an
    /// item that applies that status ("own it"). One sentence, no help page. First-seen comes from the hero's unlock
    /// record (<see cref="SystemUnlocks.HasSeen"/>), detected by <see cref="FernisFightSession"/>.
    /// </summary>
    public static class NewConceptCards
    {
        /// <summary>How long the board shows a NEW card (it fades over the last second).</summary>
        public const double ShowSeconds = 9.0;

        private static readonly object _lock = new();
        private static readonly List<string> _metSinceLoot = new();

        /// <summary>The card on the board now (null when none, or after <see cref="ShowSeconds"/>).</summary>
        public static NewConceptCard? Current { get; private set; }

        /// <summary>Statuses introduced since the last loot reveal (resolved ids, oldest first).</summary>
        public static IReadOnlyList<string> MetSinceLoot { get { lock (_lock) return _metSinceLoot.ToList(); } }

        /// <summary>The one sentence for a status, worded for who holds it ("your next rolls are lower" / "its ...").</summary>
        public static string Sentence(string statusId, bool onHero)
        {
            string id = CoreStatuses.Resolve(statusId) ?? statusId ?? "";
            (string you, string it) = id.ToLowerInvariant() switch
            {
                "stagger" => ("your next swing comes later.", "its next swing comes later."),
                CoreStatuses.Guard => ("you take less damage.", "it takes less damage."),
                "bleed" => ("you lose HP each time you act.", "it loses HP each time it acts."),
                "burn" => ("you lose HP over time.", "it loses HP over time."),
                "focus" => ("you deal more damage.", "it deals more damage."),
                "rattle" => ("your next rolls are lower.", "its next rolls are lower."),
                "expose" => ("you take more damage.", "it takes more damage."),
                "mark" => ("the next hit on you crits.", "the next hit on it crits."),
                "haste" => ("your actions come faster.", "its actions come faster."),
                _ => ("", "")
            };
            return onHero ? you : it;
        }

        /// <summary>"NEW · Rattle: your next rolls are lower." (just "NEW · X" for a status with no sentence).</summary>
        public static string CardText(string statusId, bool onHero)
        {
            string label = CoreStatuses.Label(statusId);
            string sentence = Sentence(statusId, onHero);
            return sentence.Length > 0 ? $"NEW · {label}: {sentence}" : $"NEW · {label}";
        }

        /// <summary>Puts a NEW card on the board for a status the hero meets for the first time.</summary>
        public static NewConceptCard Show(string statusId, Actor? holder, Actor? source, Character hero, DateTimeOffset? now = null)
        {
            string id = CoreStatuses.Resolve(statusId) ?? statusId;
            bool onHero = holder != null ? ReferenceEquals(holder, hero) : !ReferenceEquals(source, hero);
            var card = new NewConceptCard(id, CoreStatuses.Label(id), CardText(id, onHero), holder, source, now ?? DateTimeOffset.UtcNow);
            lock (_lock)
            {
                Current = card;
                if (!_metSinceLoot.Contains(id, StringComparer.OrdinalIgnoreCase))
                    _metSinceLoot.Add(id);
            }
            return card;
        }

        /// <summary>The card to draw at <paramref name="now"/> and its opacity (1, fading to 0 over the last second); null when none.</summary>
        public static (NewConceptCard Card, double Alpha)? Visible(DateTimeOffset now)
        {
            var card = Current;
            if (card == null)
                return null;
            double age = (now - card.ShownUtc).TotalSeconds;
            if (age < 0 || age >= ShowSeconds)
                return null;
            return (card, Math.Clamp(ShowSeconds - age, 0.0, 1.0));
        }

        /// <summary>Takes the card off the board (a new fight).</summary>
        public static void Dismiss() => Current = null;

        /// <summary>Clears the statuses met (after the loot reveal has shown them, and in tests).</summary>
        public static void ClearMet()
        {
            lock (_lock)
                _metSinceLoot.Clear();
        }

        /// <summary>
        /// The loot reveal's NEW line for an item that applies a status met since the last reveal ("NEW · applies Rattle:
        /// its next rolls are lower."); null when the item answers none.
        /// </summary>
        public static string? LootLine(Item? item, IEnumerable<string>? met = null)
        {
            if (item == null)
                return null;
            var statuses = met?.ToList() ?? MetSinceLoot.ToList();
            if (statuses.Count == 0)
                return null;
            var applied = StatusesApplied(item);
            string? hit = statuses.FirstOrDefault(s => applied.Contains(s));
            if (hit == null)
                return null;
            string sentence = Sentence(hit, onHero: false);
            return sentence.Length > 0 ? $"NEW · applies {CoreStatuses.Label(hit)}: {sentence}" : $"NEW · applies {CoreStatuses.Label(hit)}";
        }

        /// <summary>
        /// Statuses (resolved ids) an item applies or adds: its base modifier and rule lines (APPLY / ADD), and its
        /// actions (an <c>APPLY</c> verb or a Causes flag).
        /// </summary>
        public static HashSet<string> StatusesApplied(Item item)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string? raw)
            {
                string? id = CoreStatuses.Resolve(raw?.Trim());
                if (!string.IsNullOrWhiteSpace(id))
                    set.Add(id.ToLowerInvariant());
            }
            void FromRule(CrossWireRule? rule)
            {
                if (rule != null && (rule.Verb == "APPLY" || rule.Verb == "ADD") && Stores.TryGet(rule.Store, out var def)
                    && def.Kind == StoreKind.Status)
                    Add(def.Id);
            }
            foreach (string part in (item.ImplicitRule ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                FromRule(CrossWireRule.Parse(part, item.Name ?? ""));
            foreach (var mod in item.Modifications ?? new List<Modification>())
            {
                IEnumerable<CrossWireRule> rules;
                try { rules = CrossWires.RulesOf(mod, item.Name ?? "").ToList(); }
                catch { continue; }
                foreach (var rule in rules)
                    FromRule(rule);
            }
            IEnumerable<string> actions;
            try { actions = GearActionNames.Resolve(item).ToList(); }
            catch { actions = Array.Empty<string>(); }
            foreach (string name in actions)
            {
                var data = ActionLoader.GetActionData(name);
                if (data == null)
                    continue;
                if (VerbSpec.Parse(data.Verb) is { Kind: VerbKind.Apply } spec)
                    Add(spec.Store);
                if (data.CausesBleed || data.CausesPoison) Add("bleed");
                if (data.CausesBurn) Add("burn");
                if (data.CausesStun || data.CausesSlow) Add("stagger");
                if (data.CausesHarden || data.CausesFortify) Add(CoreStatuses.Guard);
                if (data.CausesExpose || data.CausesVulnerability || data.CausesPierce || data.CausesAcid) Add("expose");
                if (data.CausesFocus) Add("focus");
                if (data.CausesWeaken) Add("rattle");
            }
            return set;
        }
    }
}
