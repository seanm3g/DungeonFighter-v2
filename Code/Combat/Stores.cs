using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>What kind of thing accumulates in a store.</summary>
    public enum StoreKind
    {
        /// <summary>Material currency minted by a worn set (RAGE, GRAZE, BEAT, …). Hero only.</summary>
        Mint,
        /// <summary>A status on whoever holds it (Expose, Poison, Bleed, …).</summary>
        Status,
        /// <summary>A hero-side defensive or offensive resource (Harden, Focus, Temp HP, …).</summary>
        Resource
    }

    /// <summary>How long a store's contents last by default (what survives).</summary>
    public enum StoreScope
    {
        Turn,
        Fight,
        Dungeon
    }

    /// <summary>
    /// One store: a named quantity on an actor with a common read / add / spend interface
    /// (Documentation/BUILD_SYSTEM_CONDENSED.md §3). Add / Spend are null where the store is read-only through this
    /// registry (e.g. DoTs are applied through their effect handlers, which own tick timing).
    /// </summary>
    public sealed class StoreDef
    {
        internal StoreDef(string id, string label, StoreKind kind, StoreScope scope, Func<Actor, int> read,
            Func<Actor, int, int>? add = null, Func<Actor, int, int>? spend = null)
        {
            Id = id;
            Label = label;
            Kind = kind;
            Scope = scope;
            Read = read;
            AddFn = add;
            SpendFn = spend;
        }

        /// <summary>Lowercase id (<c>expose</c>, <c>poison</c>) or the mint keyword (<c>RAGE</c>).</summary>
        public string Id { get; }
        public string Label { get; }
        public StoreKind Kind { get; }
        public StoreScope Scope { get; }
        public Func<Actor, int> Read { get; }
        internal Func<Actor, int, int>? AddFn { get; }
        internal Func<Actor, int, int>? SpendFn { get; }
        public bool CanAdd => AddFn != null;
        public bool CanSpend => SpendFn != null;
    }

    /// <summary>
    /// Registry of every store: the material mints from MaterialBuilds.json, enemy-facing statuses and hero resources.
    /// A thin adapter over the existing fields (no new combat state); the condensed build system's verbs (CASH,
    /// CONVERT, SCALE) and rules read and write through it.
    /// </summary>
    public static class Stores
    {
        /// <summary>Turns a stack status lasts when added through the registry (matches the effect handlers).</summary>
        public const int DefaultStatusTurns = 3;

        /// <summary>Poison store unit: 0.25% of max HP (one ordinary poison proc).</summary>
        public const double PoisonPercentPerUnit = 0.25;

        private static IReadOnlyList<StoreDef>? _all;
        private static Dictionary<string, StoreDef>? _byId;

        public static IReadOnlyList<StoreDef> All
        {
            get
            {
                EnsureBuilt();
                return _all!;
            }
        }

        /// <summary>Rebuild after material builds reload.</summary>
        public static void Reload()
        {
            _all = null;
            _byId = null;
        }

        public static bool TryGet(string? id, out StoreDef def)
        {
            EnsureBuilt();
            def = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _byId!.TryGetValue(id.Trim(), out def!);
        }

        /// <summary>Current amount in the store (0 for unknown stores or a null actor).</summary>
        public static int Get(Actor? actor, string? id) =>
            actor != null && TryGet(id, out var def) ? Math.Max(0, def.Read(actor)) : 0;

        /// <summary>Adds up to <paramref name="amount"/>; returns how much was added (0 when the store is read-only here).</summary>
        public static int Add(Actor? actor, string? id, int amount)
        {
            if (actor == null || amount <= 0 || !TryGet(id, out var def) || def.AddFn == null)
                return 0;
            return Math.Max(0, def.AddFn(actor, amount));
        }

        /// <summary>Spends up to <paramref name="max"/>; returns how much was spent.</summary>
        public static int Spend(Actor? actor, string? id, int max)
        {
            if (actor == null || max <= 0 || !TryGet(id, out var def) || def.SpendFn == null)
                return 0;
            return Math.Max(0, def.SpendFn(actor, max));
        }

        /// <summary>Spends everything in the store; returns how much was spent.</summary>
        public static int SpendAll(Actor? actor, string? id) => Spend(actor, id, int.MaxValue);

        private static void EnsureBuilt()
        {
            if (_all != null && _byId != null)
                return;
            var list = new List<StoreDef>();
            list.AddRange(BuildMints());
            list.AddRange(BuildStatuses());
            list.AddRange(BuildResources());
            var map = new Dictionary<string, StoreDef>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in list)
                map.TryAdd(def.Id, def);
            _all = list;
            _byId = map;
        }

        // ------------------------------------------------------------------ mints

        private static IEnumerable<StoreDef> BuildMints()
        {
            foreach (var build in MaterialBuildsLoader.GetAll())
            {
                string keyword = build.Keyword;
                if (string.IsNullOrWhiteSpace(keyword))
                    continue;
                yield return new StoreDef(keyword, $"{keyword} ({build.Material})", StoreKind.Mint, StoreScope.Dungeon,
                    read: a => Hero(a)?.Effects.GetMaterialKeyword(keyword) ?? 0,
                    add: (a, n) =>
                    {
                        var hero = Hero(a);
                        if (hero == null) return 0;
                        hero.Effects.AddMaterialKeyword(keyword, n);
                        return n;
                    },
                    spend: (a, max) =>
                    {
                        var hero = Hero(a);
                        if (hero == null) return 0;
                        int n = Math.Min(max, hero.Effects.GetMaterialKeyword(keyword));
                        if (n > 0) hero.Effects.AddMaterialKeyword(keyword, -n);
                        return n;
                    });
            }
        }

        private static Character? Hero(Actor a) => a is Character c && c is not Enemy ? c : null;

        // ------------------------------------------------------------------ statuses

        private static IEnumerable<StoreDef> BuildStatuses()
        {
            yield return new StoreDef("expose", "Expose", StoreKind.Status, StoreScope.Turn,
                read: a => ExposeStatus.IsExposed(a) ? a.ExposeStacks ?? 0 : 0,
                add: (a, n) =>
                {
                    ExposeStatus.Apply(a, n, DefaultStatusTurns);
                    return n;
                },
                spend: (a, max) =>
                {
                    int have = ExposeStatus.IsExposed(a) ? a.ExposeStacks ?? 0 : 0;
                    int n = Math.Min(max, have);
                    if (n <= 0) return 0;
                    if (n >= have) ExposeStatus.Clear(a);
                    else a.ExposeStacks = have - n;
                    return n;
                });

            yield return new StoreDef("poison", "Poison", StoreKind.Status, StoreScope.Fight,
                read: a => (int)Math.Round(a.PoisonPercentOfMaxHealth / PoisonPercentPerUnit, MidpointRounding.AwayFromZero),
                spend: (a, max) =>
                {
                    int have = (int)Math.Round(a.PoisonPercentOfMaxHealth / PoisonPercentPerUnit, MidpointRounding.AwayFromZero);
                    int n = Math.Min(max, have);
                    if (n <= 0) return 0;
                    a.PoisonPercentOfMaxHealth = n >= have ? 0 : Math.Max(0, a.PoisonPercentOfMaxHealth - n * PoisonPercentPerUnit);
                    return n;
                });

            yield return new StoreDef("bleed", "Bleed", StoreKind.Status, StoreScope.Fight,
                read: a => a.BleedIntensity + a.PendingBleedFromHits,
                spend: (a, max) =>
                {
                    int fromNow = Math.Min(max, a.BleedIntensity);
                    a.BleedIntensity -= fromNow;
                    int fromPending = Math.Min(max - fromNow, a.PendingBleedFromHits);
                    a.PendingBleedFromHits -= fromPending;
                    return fromNow + fromPending;
                });

            yield return new StoreDef("burn", "Burn", StoreKind.Status, StoreScope.Fight,
                read: a => a.BurnIntensity + a.PendingBurnFromHits,
                spend: (a, max) =>
                {
                    int fromNow = Math.Min(max, a.BurnIntensity);
                    a.BurnIntensity -= fromNow;
                    int fromPending = Math.Min(max - fromNow, a.PendingBurnFromHits);
                    a.PendingBurnFromHits -= fromPending;
                    return fromNow + fromPending;
                });

            yield return new StoreDef("acid", "Acid", StoreKind.Status, StoreScope.Fight,
                read: a => a.AcidIntensity + a.PendingAcidFromHits);

            yield return new StoreDef("weaken", "Weaken", StoreKind.Status, StoreScope.Turn,
                read: a => a.IsWeakened ? a.WeakenTurns : 0);

            yield return new StoreDef("slow", "Slow", StoreKind.Status, StoreScope.Turn,
                read: a => a is Character c ? c.Effects.SlowTurns : 0);

            yield return new StoreDef("stun", "Stun", StoreKind.Status, StoreScope.Turn,
                read: a => a.IsStunned ? a.StunTurnsRemaining : 0);

            yield return new StoreDef("mark", "Mark", StoreKind.Status, StoreScope.Turn,
                read: a => a.IsMarked ? Math.Max(1, a.MarkTurns) : 0);
        }

        // ------------------------------------------------------------------ hero resources

        private static IEnumerable<StoreDef> BuildResources()
        {
            yield return StackResource("harden", "Harden", a => a.HardenStacks, (a, v) => a.HardenStacks = v,
                a => a.HardenTurns, (a, v) => a.HardenTurns = v);
            yield return StackResource("fortify", "Fortify", a => a.FortifyStacks, (a, v) => a.FortifyStacks = v,
                a => a.FortifyTurns, (a, v) => a.FortifyTurns = v);
            yield return StackResource("focus", "Focus", a => a.FocusStacks, (a, v) => a.FocusStacks = v,
                a => a.FocusTurns, (a, v) => a.FocusTurns = v);

            yield return new StoreDef("temphp", "Temporary HP", StoreKind.Resource, StoreScope.Turn,
                read: a => a.TemporaryHP ?? 0,
                add: (a, n) =>
                {
                    a.TemporaryHP = (a.TemporaryHP ?? 0) + n;
                    a.TemporaryHPTurns = Math.Max(a.TemporaryHPTurns, DefaultStatusTurns);
                    return n;
                },
                spend: (a, max) =>
                {
                    int have = a.TemporaryHP ?? 0;
                    int n = Math.Min(max, have);
                    if (n <= 0) return 0;
                    a.TemporaryHP = have - n > 0 ? have - n : null;
                    if (a.TemporaryHP == null) a.TemporaryHPTurns = 0;
                    return n;
                });
        }

        private static StoreDef StackResource(string id, string label,
            Func<Actor, int?> getStacks, Action<Actor, int?> setStacks,
            Func<Actor, int> getTurns, Action<Actor, int> setTurns) =>
            new(id, label, StoreKind.Resource, StoreScope.Turn,
                read: a => getTurns(a) > 0 ? getStacks(a) ?? 0 : 0,
                add: (a, n) =>
                {
                    setStacks(a, (getStacks(a) ?? 0) + n);
                    setTurns(a, Math.Max(getTurns(a), DefaultStatusTurns));
                    return n;
                },
                spend: (a, max) =>
                {
                    int have = getTurns(a) > 0 ? getStacks(a) ?? 0 : 0;
                    int n = Math.Min(max, have);
                    if (n <= 0) return 0;
                    if (n >= have)
                    {
                        setStacks(a, null);
                        setTurns(a, 0);
                    }
                    else
                    {
                        setStacks(a, have - n);
                    }
                    return n;
                });
    }
}
