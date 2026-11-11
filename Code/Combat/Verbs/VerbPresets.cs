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
        Index
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
                _ => VerbKind.None
            };
            if (kind == VerbKind.None)
                return null;

            string store = Attunement.StoreToken;
            double rate = 100;
            foreach (string part in parts.Skip(1))
            {
                if (double.TryParse(part.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
                    rate = Math.Max(0, r);
                else
                    store = part;
            }
            return new VerbSpec { Kind = kind, Store = store, Rate = rate };
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
        public static int OnConnect(Character hero, Actor? target, Action? action)
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
                default:
                    return 0;
            }
        }

        private static int Cash(Character hero, Actor? target, string storeId, double share)
        {
            var holder = Holder(hero, target, storeId);
            int spent = Stores.SpendAll(holder, storeId);
            if (spent > 0 && Stores.TryGet(storeId, out var def) && def.Kind == StoreKind.Mint)
                MaterialSetController.GrantConvertPrimaryStat(hero, def.Id, spent, share);
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
                _ => null
            };
        }
    }
}
