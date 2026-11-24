using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RPGGame.Data;


namespace RPGGame.Combat.Events
{
    /// <summary>
    /// Fires the hub events (Documentation/BUILD_SYSTEM_CONDENSED.md §2): ONSTATUSAPPLY, ONCASH, ONSTOREFULL,
    /// ONSELFCOST, ONENEMYACT. They are not on the CombatEvent pipeline; the hub hands them to every rule owner: the
    /// hero's rule book (items, material sets, Listener affixes, skill nodes) and the card being swung when the event
    /// was raised (<see cref="Rules.ActionRules.OnHub"/>), then <see cref="Raised"/>.
    /// </summary>
    public static class HeroEventHub
    {
        /// <summary>Subscribers (skill cross-wires): hero, canonical token, argument (status / store id / beat count).</summary>
        public static event Action<Character, string, string?>? Raised;

        private static readonly ConditionalWeakTable<Character, StrongBox<int>> _fightTurns = new();
        private static readonly ConditionalWeakTable<Character, WeakReference<Actor>> _targets = new();

        /// <summary>The foe the hero is fighting (last attacked or attacked by); cross-wire APPLY / IF TARGET read it.</summary>
        public static void SetTarget(Character hero, Actor? target)
        {
            if (target == null || ReferenceEquals(hero, target))
                return;
            _targets.AddOrUpdate(hero, new WeakReference<Actor>(target));
        }

        public static Actor? CurrentTarget(Character hero) =>
            _targets.TryGetValue(hero, out var weak) && weak.TryGetTarget(out var actor) ? actor : null;
        /// <summary>
        /// Raises <paramref name="token"/> for <paramref name="hero"/>. <paramref name="arg"/> is the status / store id
        /// (ONSTATUSAPPLY, ONCASH, ONSTOREFULL). Resolution goes through
        /// <see cref="Rules.RuleEngine"/>: raised while another event is resolving (a mint filling a store, a rule's spend),
        /// it queues and resolves after that event, up to two generations deep (D6.13–D6.14).
        /// </summary>
        public static void Raise(Character? hero, string token, string? arg = null, int amount = 0)
        {
            if (hero == null || hero is Enemy)
                return;
            // The card swinging when the event is raised hears it too, even when it resolves later in the queue.
            var card = Rules.ActionRules.Swinging(hero);
            Rules.RuleEngine.Raise(hero, arg == null ? token : token + ":" + arg, () => Resolve(hero, token, arg, amount, card));
        }

        private static void Resolve(Character hero, string token, string? arg, int amount, RPGGame.Action? card)
        {
            CrossWires.OnHub(hero, token, arg, amount);
            if (card != null)
                Rules.ActionRules.OnHub(hero, card, token, arg);
            Raised?.Invoke(hero, token, arg);
        }

        /// <summary>
        /// True when a WHEN token (with optional <c>:arg</c>) matches the raised event: the arg must equal the raised
        /// argument (case-insensitive); no arg matches any.
        /// </summary>
        public static bool Matches(string? whenToken, string token, string? raisedArg)
        {
            if (!TriggerEvents.TryGet(whenToken, out var def, out string? wantArg) || def.Token != token)
                return false;
            return string.IsNullOrEmpty(wantArg)
                || string.Equals(Norm(wantArg), Norm(raisedArg), StringComparison.OrdinalIgnoreCase);
        }

        private static string Norm(string? s) => (s ?? "").Replace("_", "").Replace(" ", "").Trim();

        // ------------------------------------------------------------------ call-site helpers

        /// <summary>Hero turn start: counts turns this fight (the TURN self key; rules use ONTURN EVERY n).</summary>
        public static void OnHeroTurnStart(Character hero)
        {
            var box = _fightTurns.GetOrCreateValue(hero);
            box.Value++;
        }

        /// <summary>Hero turn count this fight.</summary>
        public static int FightTurns(Character hero) => _fightTurns.TryGetValue(hero, out var box) ? box.Value : 0;

        /// <summary>New fight: restart the beat clock.</summary>
        public static void ResetFight(Character? hero)
        {
            if (hero != null)
                _fightTurns.Remove(hero);
        }

        /// <summary>A hero spent <paramref name="units"/> of <paramref name="store"/> (converts, verb presets).</summary>
        public static void OnCash(Character hero, string store, int units)
        {
            if (units > 0)
                Raise(hero, "ONCASH", store, units);
        }

        /// <summary>Statuses the hero applied to an enemy this resolution.</summary>
        public static void OnStatusesApplied(Actor source, Actor target, IEnumerable<string> statuses)
        {
            if (source is not Character hero || hero is Enemy || ReferenceEquals(source, target))
                return;
            foreach (string status in statuses)
            {
                Raise(hero, "ONSTATUSAPPLY", status);
                if (status == "expose" && (target.ExposeStacks ?? 0) >= GameConfiguration.Instance.Combat.StatusMaxStacks)
                    Raise(hero, "ONSTOREFULL", "expose");
            }
        }

        /// <summary>An enemy is about to act against the hero.</summary>
        public static void OnEnemyAct(Actor source, Actor target)
        {
            if (source is Enemy && target is Character hero && hero is not Enemy)
            {
                SetTarget(hero, source);
                Raise(hero, "ONENEMYACT");
            }
        }

        /// <summary>The hero paid a cost (HP, condition, a status on themselves).</summary>
        public static void OnSelfCost(Character hero, string kind) => Raise(hero, "ONSELFCOST", kind);

        /// <summary>
        /// A mint went from below to at/above <see cref="CombatConfig.MintStoreFullThreshold"/> (0 = off): raises
        /// ONSTOREFULL with the keyword.
        /// </summary>
        public static void OnMintChanged(Character hero, string keyword, int before, int after)
        {
            int full = GameConfiguration.Instance.Combat.MintStoreFullThreshold;
            if (full > 0 && before < full && after >= full)
                Raise(hero, "ONSTOREFULL", keyword);
        }
    }
}
