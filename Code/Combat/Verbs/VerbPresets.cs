using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Verb shapes of the <c>verb</c> column (Documentation/BUILD_SYSTEM_CONDENSED.md §4). Since 2026-11-19 (D6.4) the
    /// generic verbs are CASH IN and DETONATE; LIQUIDATE, RECKONING, INDEX, MARK, BARRIER, DISTILL, TRANSMUTE, INVEST and
    /// PALM are retired (their actions load as CASH IN / BULWARK / DETONATE, ActionLoader aliases).
    /// </summary>
    public enum VerbKind
    {
        None,
        /// <summary>Spend a store for its cash effect (CASH IN = your fullest store; a store-named cash).</summary>
        Cash,
        /// <summary>Spend the target's largest spendable status (DETONATE).</summary>
        Detonate,
        /// <summary>Add stacks to a store (<c>APPLY burn 2</c>): runs as an action-owned rule
        /// (<see cref="RPGGame.Combat.Rules.ActionRules"/>), parsed here for the bot's pricing.</summary>
        Apply,
        /// <summary>Pull every queued buff (TURN grants and later strip-slot grants) onto this swing at +rate% (RELEASE).</summary>
        Release
    }

    /// <summary>
    /// Parsed <c>verb</c> column: <c>VERB [store] [rate%]</c>, e.g. <c>CASH FULLEST 75</c>, <c>CASH stagger 100</c>,
    /// <c>DETONATE 100</c>, <c>APPLY burn 2</c>, <c>RELEASE 25</c>. Store defaults to FULLEST (ATTUNED reads the same);
    /// rate defaults to 100.
    /// </summary>
    public sealed class VerbSpec
    {
        public VerbKind Kind { get; init; }
        public string Store { get; init; } = FullestStore.StoreToken;
        /// <summary>Percent: share of the store's cash (CASH / DETONATE); APPLY's stack count; RELEASE's bonus.</summary>
        public double Rate { get; init; } = 100;
        /// <summary>
        /// CASH payout (<c>CASH ATTUNED 100 INTO harden</c>): <c>DAMAGE</c> (default, % of the hit), <c>HEAL</c> (% max HP),
        /// an attribute (<c>STR</c> / <c>AGI</c> / <c>TEC</c> / <c>INT</c>, or <c>PRIMARY</c> = the cashed material's class stat)
        /// for the rest of the fight, or any store id (<c>temphp</c>, <c>harden</c>, <c>haste</c>, <c>bleed</c>, ...): hero
        /// resources land on the hero, statuses on the target. Per-unit rates: <see cref="CombatConfig.CashIntoPerUnit"/>.
        /// </summary>
        public string Into { get; init; } = CashPayout.Damage;

        public static VerbSpec? Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var parts = raw.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            VerbKind kind = parts[0].ToUpperInvariant() switch
            {
                "CASH" => VerbKind.Cash,
                "DETONATE" => VerbKind.Detonate,
                "APPLY" => VerbKind.Apply,
                "RELEASE" => VerbKind.Release,
                _ => VerbKind.None
            };
            if (kind == VerbKind.None)
                return null;

            string store = FullestStore.StoreToken;
            double rate = kind == VerbKind.Apply ? 1 : kind == VerbKind.Release ? 0 : 100; // APPLY's number is a stack count
            int numbers = 0;
            string into = CashPayout.Damage;
            var rest = parts.Skip(1).ToList();
            int intoAt = rest.FindIndex(p => p.Equals("INTO", StringComparison.OrdinalIgnoreCase));
            if (intoAt >= 0)
            {
                if (kind != VerbKind.Cash || intoAt != rest.Count - 2 || !CashPayout.IsValid(rest[^1]))
                    return null;
                into = CashPayout.Normalize(rest[^1]);
                rest = rest.Take(intoAt).ToList();
            }
            foreach (string part in rest)
            {
                if (double.TryParse(part.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
                {
                    if (numbers++ == 0) rate = Math.Max(0, r);
                }
                else
                {
                    store = part;
                }
            }
            return new VerbSpec { Kind = kind, Store = FullestStore.IsToken(store) ? FullestStore.StoreToken : store, Rate = rate, Into = into };
        }
    }

    /// <summary>
    /// Store-reading verb presets: CASH IN and DETONATE. Damage is added in
    /// <see cref="RPGGame.Combat.Calculators.DamageCalculator.CalculateDamage"/> before armor (reading stores as they stand);
    /// the spend happens on connect (<see cref="OnConnect"/>), after the hit, like material converts.
    /// <para>Cash per unit: a mint pays its convert (primary stat, plus convert damage when the patch pays damage);
    /// a status pays <see cref="CombatConfig.VerbStatusCashPercentPerUnit"/> % of the hit per unit.</para>
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

        /// <summary>Resolves a spec's store id for this hero (FULLEST → the fullest material store). Null when nothing resolves.</summary>
        public static string? ResolveStore(Character? hero, string? store)
        {
            if (string.IsNullOrWhiteSpace(store))
                return null;
            if (FullestStore.IsToken(store))
                return FullestStore.GetKeyword(hero);
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
                    if (spec.Into != CashPayout.Damage)
                        return 0;
                    string? id = ResolveStore(hero, spec.Store);
                    return id == null ? 0 : CashDamagePercent(hero, target, id) * share;
                }
                case VerbKind.Detonate:
                {
                    string? id = LargestTargetStatus(target);
                    return id == null ? 0 : CashDamagePercent(hero, target, id) * share;
                }
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
        /// <see cref="CombatConfig.VerbStatusCashPercentPerUnit"/>.
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
                    if (id == null)
                        return 0;
                    if (spec.Into == CashPayout.Damage)
                        return Cash(hero, target, id, share);
                    int spent = Stores.SpendAll(Holder(hero, target, id), id);
                    CashPayout.Pay(hero, target, id, spent, share, spec.Into);
                    RPGGame.Combat.Events.HeroEventHub.OnCash(hero, id, spent);
                    return spent;
                }
                case VerbKind.Detonate:
                {
                    string? id = LargestTargetStatus(target);
                    return id == null ? 0 : Cash(hero, target, id, share);
                }
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
            RPGGame.Combat.Events.HeroEventHub.OnCash(hero, storeId, spent);
            return spent;
        }

        // ------------------------------------------------------------------ helpers

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
            string store = FullestStore.IsToken(spec.Store)
                ? (hero != null ? FullestStore.GetKeyword(hero) : null) ?? "your fullest store"
                : spec.Store;
            string rate = spec.Rate.ToString("0.#", CultureInfo.InvariantCulture);
            return spec.Kind switch
            {
                VerbKind.Cash => spec.Into == CashPayout.Damage
                    ? $"Cashes {store} at {rate}%"
                    : $"Cashes {store} into {CashPayout.Describe(hero, store, spec.Into, spec.Rate / 100.0)}",
                VerbKind.Detonate => $"Detonates the target's largest status at {rate}%",
                VerbKind.Apply => $"Applies {store} {rate}",
                VerbKind.Release => $"Every queued buff lands on this action at +{rate}%",
                _ => null
            };
        }
    }
}
