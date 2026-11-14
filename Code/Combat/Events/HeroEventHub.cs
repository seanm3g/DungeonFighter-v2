using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RPGGame.Data;


namespace RPGGame.Combat.Events
{
    /// <summary>
    /// Fires the hub events (Documentation/BUILD_SYSTEM_CONDENSED.md §2): ONSTATUSAPPLY, ONCASH, ONSTOREFULL, ONBEAT,
    /// ONSELFCOST, ONENEMYACT. They are not on the CombatEvent pipeline (action trigger bundles don't see them); the
    /// hub hands them to everything that reads events by name: material set WHENs, Listener affixes and skill
    /// cross-wires (<see cref="Raised"/>).
    /// </summary>
    public static class HeroEventHub
    {
        /// <summary>Default ONBEAT period when a token has no <c>:n</c>.</summary>
        public const int DefaultBeat = 4;

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
        [ThreadStatic] private static int _depth;

        /// <summary>
        /// Raises <paramref name="token"/> for <paramref name="hero"/>. <paramref name="arg"/> is the status / store id
        /// (ONSTATUSAPPLY, ONCASH, ONSTOREFULL) or the hero's turn count this fight (ONBEAT). Re-entrant raises (a mint
        /// filling a store that raises ONSTOREFULL…) are allowed two levels deep.
        /// </summary>
        public static void Raise(Character? hero, string token, string? arg = null, int amount = 0)
        {
            if (hero == null || hero is Enemy || _depth > 2)
                return;
            _depth++;
            try
            {
                foreach (var build in MaterialBuildsLoader.GetAll())
                {
                    if (string.IsNullOrWhiteSpace(build.Keyword) || !Matches(build.WhenToken, token, arg))
                        continue;
                    if (MaterialSetController.CountEquipped(hero, build.Material) < MaterialBuildData.StackUnlockCount)
                        continue;
                    int mint = MaterialSetController.ComputeMintPerTrigger(hero, build);
                    if (mint > 0)
                        MaterialSetController.MintKeyword(hero, build.Keyword, mint, MaterialBuildData.FormatWhenLabel(build.Synthesis));
                }
                ListenerAffix.MintFromHub(hero, token, arg);
                CrossWires.OnHub(hero, token, arg, amount);
                Raised?.Invoke(hero, token, arg);
            }
            finally
            {
                _depth--;
            }
        }

        /// <summary>
        /// True when a WHEN token (with optional <c>:arg</c>) matches the raised event. ONBEAT:n matches every n-th
        /// turn; other args must equal the raised argument (case-insensitive); no arg matches any.
        /// </summary>
        public static bool Matches(string? whenToken, string token, string? raisedArg)
        {
            if (!TriggerEvents.TryGet(whenToken, out var def, out string? wantArg) || def.Token != token)
                return false;
            if (token == "ONBEAT")
            {
                int period = int.TryParse(wantArg, out int n) && n > 0 ? n : DefaultBeat;
                return int.TryParse(raisedArg, out int turn) && turn > 0 && turn % period == 0;
            }
            return string.IsNullOrEmpty(wantArg)
                || string.Equals(Norm(wantArg), Norm(raisedArg), StringComparison.OrdinalIgnoreCase);
        }

        private static string Norm(string? s) => (s ?? "").Replace("_", "").Replace(" ", "").Trim();

        // ------------------------------------------------------------------ call-site helpers

        /// <summary>Hero turn start: counts turns this fight and raises ONBEAT with the count.</summary>
        public static void OnHeroTurnStart(Character hero)
        {
            var box = _fightTurns.GetOrCreateValue(hero);
            box.Value++;
            Raise(hero, "ONBEAT", box.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>Hero turn count this fight (ONBEAT's clock).</summary>
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
