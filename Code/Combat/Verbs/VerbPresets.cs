using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>Verb shapes the store-reading presets use (Documentation/BUILD_SYSTEM_CONDENSED.md §4).</summary>
    public enum VerbKind
    {
        None,
        /// <summary>Spend a store for its cash effect (CASH IN, a store-named cash).</summary>
        Cash,
        /// <summary>Spend the target's largest spendable status (DETONATE).</summary>
        Detonate,
        /// <summary>Spend every hero mint (LIQUIDATE).</summary>
        Liquidate,
        /// <summary>Damage grows with a store without spending it (RECKONING).</summary>
        Scale,
        /// <summary>Damage grows with the number of distinct materials worn (INDEX).</summary>
        Index,
        /// <summary>Add stacks to a store (MARK: <c>APPLY mark 1</c>).</summary>
        Apply,
        /// <summary>Temporary HP worth rate% of max HP (BARRIER).</summary>
        Barrier,
        /// <summary>Spend the target's largest status into your attuned mint at rate% (DISTILL).</summary>
        Distill,
        /// <summary>Run the conversion table on the target (owned rows always, unowned at half rate), then spend every other mint into your attuned mint at rate% (TRANSMUTE).</summary>
        Transmute,
        /// <summary>Spend your attuned mint now; it comes back at rate% after <see cref="VerbSpec.Delay"/> of your actions (INVEST).</summary>
        Invest,
        /// <summary>Bank this attack's natural roll: your next roll becomes it (PALM).</summary>
        Palm
    }

    /// <summary>
    /// Parsed <c>verb</c> column: <c>VERB [store] [rate%]</c>, e.g. <c>CASH ATTUNED 75</c>, <c>SCALE ATTUNED 50</c>,
    /// <c>DETONATE 100</c>, <c>LIQUIDATE 50</c>, <c>INDEX 8</c>. Store defaults to ATTUNED; rate defaults to 100.
    /// </summary>
    public sealed class VerbSpec
    {
        public VerbKind Kind { get; init; }
        public string Store { get; init; } = Attunement.StoreToken;
        /// <summary>Percent: share of the store's cash (CASH / DETONATE / LIQUIDATE / SCALE) or % damage per material (INDEX).</summary>
        public double Rate { get; init; } = 100;
        /// <summary>Second number in the spec: INVEST's delay in hero actions (default 2).</summary>
        public int Delay { get; init; } = 2;

        public static VerbSpec? Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var parts = raw.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            VerbKind kind = parts[0].ToUpperInvariant() switch
            {
                "CASH" => VerbKind.Cash,
                "DETONATE" => VerbKind.Detonate,
                "LIQUIDATE" => VerbKind.Liquidate,
                "SCALE" => VerbKind.Scale,
                "INDEX" => VerbKind.Index,
                "APPLY" => VerbKind.Apply,
                "BARRIER" => VerbKind.Barrier,
                "DISTILL" => VerbKind.Distill,
                "TRANSMUTE" => VerbKind.Transmute,
                "INVEST" => VerbKind.Invest,
                "PALM" => VerbKind.Palm,
                _ => VerbKind.None
            };
            if (kind == VerbKind.None)
                return null;

            string store = Attunement.StoreToken;
            double rate = kind == VerbKind.Apply ? 1 : 100; // APPLY's number is a stack count
            int delay = 2;
            int numbers = 0;
            foreach (string part in parts.Skip(1))
            {
                if (double.TryParse(part.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
                {
                    if (numbers++ == 0) rate = Math.Max(0, r);
                    else delay = Math.Max(1, (int)Math.Round(r));
                }
                else
                {
                    store = part;
                }
            }
            return new VerbSpec { Kind = kind, Store = store, Rate = rate, Delay = delay };
        }
    }

    /// <summary>
    /// Store-reading verb presets: CASH IN, DETONATE, LIQUIDATE, RECKONING, INDEX. Damage is added in
    /// <see cref="RPGGame.Combat.Calculators.DamageCalculator.CalculateDamage"/> before armor (reading stores as they stand);
    /// the spend happens on connect (<see cref="OnConnect"/>), after the hit, like material converts.
    /// <para>Cash per unit: a mint pays its convert (primary stat, plus convert damage when the patch pays damage);
    /// a status pays <see cref="CombatConfig.VerbStatusCashPercentPerUnit"/> % of the hit per unit.
    /// SCALE reads the same per-unit damage without spending, capped at <see cref="CombatConfig.VerbScaleMaxPercent"/>.</para>
    /// </summary>
    public static class VerbPresets
    {
        private static readonly Dictionary<string, VerbSpec?> _cache = new(StringComparer.Ordinal);
        private static readonly object _lock = new();

        public static VerbSpec? GetSpec(Action? action)
        {
            string raw = action?.Verb ?? "";
            if (raw.Length == 0)
                return null;
            lock (_lock)
            {
                if (!_cache.TryGetValue(raw, out var spec))
                {
                    spec = VerbSpec.Parse(raw);
                    _cache[raw] = spec;
                }
                return spec;
            }
        }

        /// <summary>Resolves a spec's store id for this hero (ATTUNED → the attuned mint). Null when nothing resolves.</summary>
        public static string? ResolveStore(Character? hero, string? store)
        {
            if (string.IsNullOrWhiteSpace(store))
                return null;
            if (string.Equals(store, Attunement.StoreToken, StringComparison.OrdinalIgnoreCase))
                return Attunement.GetKeyword(hero);
            return Stores.TryGet(store, out var def) ? def.Id : null;
        }

        /// <summary>Whose store this is: statuses live on the target, mints and resources on the hero.</summary>
        public static Actor? Holder(Character hero, Actor? target, string storeId) =>
            Stores.TryGet(storeId, out var def) && def.Kind == StoreKind.Status ? target : hero;

        // ------------------------------------------------------------------ damage

        /// <summary>Flat damage the verb adds to this hit (before armor). 0 for non-verb actions and enemies.</summary>
        public static int GetDamageBonus(Actor attacker, Actor? target, Action? action, int hitDamage)
        {
            if (attacker is not Character hero || hero is Enemy || hitDamage <= 0)
                return 0;
            var spec = GetSpec(action);
            if (spec == null)
                return 0;
            double pct = GetDamagePercent(hero, target, spec);
            return pct <= 0 ? 0 : (int)Math.Round(hitDamage * pct / 100.0);
        }

        /// <summary>Damage percent the verb would add right now (also used by card previews).</summary>
        public static double GetDamagePercent(Character hero, Actor? target, VerbSpec spec)
        {
            var combat = GameConfiguration.Instance.Combat;
            double share = spec.Rate / 100.0;
            switch (spec.Kind)
            {
                case VerbKind.Cash:
                {
                    string? id = ResolveStore(hero, spec.Store);
                    return id == null ? 0 : CashDamagePercent(hero, target, id) * share;
                }
                case VerbKind.Detonate:
                {
                    string? id = LargestTargetStatus(target);
                    return id == null ? 0 : CashDamagePercent(hero, target, id) * share;
                }
                case VerbKind.Liquidate:
                    return HeroMints(hero).Sum(id => CashDamagePercent(hero, target, id)) * share;
                case VerbKind.Scale:
                {
                    string? id = ResolveStore(hero, spec.Store);
                    if (id == null)
                        return 0;
                    var holder = Holder(hero, target, id);
                    double pct = Stores.Get(holder, id) * DamagePercentPerUnit(id, forScale: true) * share;
                    double cap = combat.VerbScaleMaxPercent;
                    return cap > 0 ? Math.Min(cap, pct) : pct;
                }
                case VerbKind.Index:
                    return Attunement.CountDistinctMaterials(hero) * spec.Rate;
                default:
                    return 0;
            }
        }

        private static double CashDamagePercent(Character hero, Actor? target, string storeId)
        {
            var holder = Holder(hero, target, storeId);
            int units = Stores.Get(holder, storeId);
            if (units <= 0)
                return 0;
            if (Stores.TryGet(storeId, out var def) && def.Kind == StoreKind.Mint && !MaterialSetController.ConvertPaysDamage)
                return 0;
            int cap = GameConfiguration.Instance.Combat.MaterialConvertMaxUnits;
            if (def?.Kind == StoreKind.Mint && cap > 0)
                units = Math.Min(units, cap);
            return units * DamagePercentPerUnit(storeId, forScale: false);
        }

        /// <summary>
        /// % of the hit per unit: a mint uses its convert rate (<see cref="CombatConfig.MaterialConvertPercentByKeyword"/>,
        /// else 5 × <see cref="CombatConfig.MaterialConvertPercentPerUnit"/>); statuses and resources use
        /// <see cref="CombatConfig.VerbStatusCashPercentPerUnit"/>. SCALE reads mints at their convert rate even when the
        /// patch turns convert damage off (it never spends, so it is its own payout).
        /// </summary>
        public static double DamagePercentPerUnit(string storeId, bool forScale)
        {
            var combat = GameConfiguration.Instance.Combat;
            if (Stores.TryGet(storeId, out var def) && def.Kind == StoreKind.Mint)
            {
                if (combat.MaterialConvertPercentByKeyword != null
                    && combat.MaterialConvertPercentByKeyword.TryGetValue(def.Id, out double byKeyword))
                    return byKeyword;
                double perUnit = combat.MaterialConvertPercentPerUnit > 0 ? combat.MaterialConvertPercentPerUnit : 1.0;
                return MaterialBuildData.ConvertDamagePerKeyword * perUnit;
            }
            return combat.VerbStatusCashPercentPerUnit;
        }

        // ------------------------------------------------------------------ spend

        /// <summary>Spends what the verb cashes, after a connecting hit. Returns units spent.</summary>
        /// <param name="naturalRoll">This attack's natural die face (PALM banks it); 0 when unknown.</param>
        /// <param name="isCritical">This connect was a critical (conversion rows with repeatOnCrit repeat).</param>
        public static int OnConnect(Character hero, Actor? target, Action? action, int naturalRoll = 0, bool isCritical = false)
        {
            if (hero is Enemy)
                return 0;
            var spec = GetSpec(action);
            if (spec == null)
                return 0;
            double share = spec.Rate / 100.0;
            switch (spec.Kind)
            {
                case VerbKind.Cash:
                {
                    string? id = ResolveStore(hero, spec.Store);
                    return id == null ? 0 : Cash(hero, target, id, share);
                }
                case VerbKind.Detonate:
                {
                    string? id = LargestTargetStatus(target);
                    return id == null ? 0 : Cash(hero, target, id, share);
                }
                case VerbKind.Liquidate:
                    return HeroMints(hero).ToList().Sum(id => Cash(hero, target, id, share));
                case VerbKind.Apply:
                {
                    string? id = ResolveStore(hero, spec.Store);
                    if (id == null)
                        return 0;
                    int stacks = Math.Max(1, (int)Math.Round(spec.Rate));
                    return Stores.Add(Holder(hero, target, id), id, stacks);
                }
                case VerbKind.Barrier:
                {
                    int hp = (int)Math.Round(hero.GetEffectiveMaxHealth() * share);
                    return Stores.Add(hero, "temphp", Math.Max(1, hp));
                }
                case VerbKind.Distill:
                {
                    string? from = LargestTargetStatus(target);
                    string? into = Attunement.GetKeyword(hero);
                    if (from == null || into == null)
                        return 0;
                    int spent = Stores.SpendAll(target, from);
                    Stores.Add(hero, into, (int)Math.Floor(spent * share + 1e-9));
                    RPGGame.Combat.Events.HeroEventHub.OnCash(hero, from, spent);
                    return spent;
                }
                case VerbKind.Transmute:
                {
                    int converted = target == null ? 0 : ConversionTable.Transmute(hero, target, isCritical);
                    string? into = Attunement.GetKeyword(hero);
                    if (into == null)
                        return converted;
                    int spent = 0;
                    foreach (string id in HeroMints(hero).Where(id => !string.Equals(id, into, StringComparison.OrdinalIgnoreCase)).ToList())
                    {
                        int n = Stores.SpendAll(hero, id);
                        spent += n;
                        RPGGame.Combat.Events.HeroEventHub.OnCash(hero, id, n);
                    }
                    Stores.Add(hero, into, (int)Math.Floor(spent * share + 1e-9));
                    return spent + converted;
                }
                case VerbKind.Invest:
                {
                    string? id = ResolveStore(hero, spec.Store);
                    if (id == null || !Stores.TryGet(id, out var def) || def.Kind != StoreKind.Mint)
                        return 0;
                    int spent = Stores.SpendAll(hero, id);
                    RPGGame.Combat.Events.HeroEventHub.OnCash(hero, id, spent);
                    if (spent > 0)
                        Investments(hero).Add(new Investment(id, (int)Math.Floor(spent * share + 1e-9), spec.Delay + 1));
                    return spent;
                }
                case VerbKind.Palm:
                {
                    if (naturalRoll < 1 || naturalRoll > 20)
                        return 0;
                    RPGGame.Actions.Conditional.CombatTriggerContext.SetPendingReplaceRollFace(hero, naturalRoll);
                    return 1;
                }
                default:
                    return 0;
            }
        }

        // ------------------------------------------------------------------ investments (INVEST)

        private sealed class Investment
        {
            public Investment(string store, int payout, int actionsLeft)
            {
                Store = store;
                Payout = payout;
                ActionsLeft = actionsLeft;
            }

            public string Store { get; }
            public int Payout { get; }
            public int ActionsLeft { get; set; }
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Character, List<Investment>> _investments = new();

        private static List<Investment> Investments(Character hero) => _investments.GetOrCreateValue(hero);

        /// <summary>Units invested and not yet paid back, per store.</summary>
        public static int GetPendingInvestment(Character hero, string store) =>
            _investments.TryGetValue(hero, out var list)
                ? list.Where(i => string.Equals(i.Store, store, StringComparison.OrdinalIgnoreCase)).Sum(i => i.Payout)
                : 0;

        /// <summary>
        /// Counts down investments on each hero action (hit or miss); pays matured ones back into their store.
        /// The INVEST action itself counts as the first tick, so a delay of 2 pays out after two more actions.
        /// Returns units paid back.
        /// </summary>
        public static int TickInvestments(Character hero)
        {
            if (!_investments.TryGetValue(hero, out var list) || list.Count == 0)
                return 0;
            int paid = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var inv = list[i];
                inv.ActionsLeft--;
                if (inv.ActionsLeft > 0)
                    continue;
                paid += Stores.Add(hero, inv.Store, inv.Payout);
                list.RemoveAt(i);
            }
            return paid;
        }

        /// <summary>Drops unpaid investments (new run / reset).</summary>
        public static void ClearInvestments(Character hero)
        {
            if (_investments.TryGetValue(hero, out var list))
                list.Clear();
        }

        private static int Cash(Character hero, Actor? target, string storeId, double share)
        {
            var holder = Holder(hero, target, storeId);
            int spent = Stores.SpendAll(holder, storeId);
            if (spent > 0 && Stores.TryGet(storeId, out var def) && def.Kind == StoreKind.Mint)
                MaterialSetController.GrantConvertPrimaryStat(hero, def.Id, spent, share);
            RPGGame.Combat.Events.HeroEventHub.OnCash(hero, storeId, spent);
            return spent;
        }

        // ------------------------------------------------------------------ helpers

        private static IEnumerable<string> HeroMints(Character hero) =>
            Stores.All.Where(s => s.Kind == StoreKind.Mint && s.Read(hero) > 0).Select(s => s.Id);

        /// <summary>Target's spendable status with the most units (ties: registry order). Null when none.</summary>
        public static string? LargestTargetStatus(Actor? target)
        {
            if (target == null)
                return null;
            string? best = null;
            int bestUnits = 0;
            foreach (var def in Stores.All)
            {
                if (def.Kind != StoreKind.Status || !def.CanSpend)
                    continue;
                int units = def.Read(target);
                if (units > bestUnits)
                {
                    bestUnits = units;
                    best = def.Id;
                }
            }
            return best;
        }

        /// <summary>Short card line, e.g. <c>Cashes RAGE (Bone) at 75%</c>.</summary>
        public static string? Describe(Character? hero, Action? action)
        {
            var spec = GetSpec(action);
            if (spec == null)
                return null;
            string store = hero != null ? ResolveStore(hero, spec.Store) ?? "attuned store" : spec.Store;
            string rate = spec.Rate.ToString("0.#", CultureInfo.InvariantCulture);
            return spec.Kind switch
            {
                VerbKind.Cash => $"Cashes {store} at {rate}%",
                VerbKind.Detonate => $"Detonates the target's largest status at {rate}%",
                VerbKind.Liquidate => $"Cashes every material store at {rate}%",
                VerbKind.Scale => $"Scales with {store} at {rate}% (not spent)",
                VerbKind.Index => $"+{rate}% damage per distinct material worn",
                VerbKind.Apply => $"Applies {store}",
                VerbKind.Barrier => $"Temporary HP worth {rate}% of max HP",
                VerbKind.Distill => $"Moves the target's largest status into {store} at {rate}%",
                VerbKind.Transmute => $"Runs your status conversions on the target (unowned ones at half rate), then moves your other material stores into {store} at {rate}%",
                VerbKind.Invest => $"Spends {store}; it returns at {rate}% after {spec.Delay} actions",
                VerbKind.Palm => "Banks this roll: your next roll becomes it",
                _ => null
            };
        }
    }
}
