using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RPGGame
{
    /// <summary>
    /// Persistence rule family (Documentation/BUILD_SYSTEM_CONDENSED.md §6): a class capstone promotes stores from
    /// fight scope to the next fight. Skill nodes list what they carry in <c>persist</c>:
    /// <c>hero:harden,fortify,focus,temphp,streak</c> (the hero keeps them) or
    /// <c>target:expose,mark,poison,bleed,burn</c> (the dead enemy's statuses jump to the next enemy).
    /// Snapshot at fight end, restored at the next fight's start. Material banks already last the dungeon.
    /// </summary>
    public static class Persistence
    {
        private sealed class Snapshot
        {
            public Dictionary<string, int> Hero { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Target { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly ConditionalWeakTable<Character, Snapshot> _carried = new();

        /// <summary>(scope, store) pairs carried by the hero's learned nodes.</summary>
        public static List<(string Scope, string Store)> Carried(Character hero)
        {
            var list = new List<(string, string)>();
            var trees = SkillTreeService.Trees;
            foreach (var kv in hero.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0)
                    continue;
                string spec = trees?.GetNode(kv.Key)?.Persist ?? "";
                foreach (string part in spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    int colon = part.IndexOf(':');
                    if (colon <= 0)
                        continue;
                    string scope = part[..colon].Trim().ToLowerInvariant();
                    if (scope is not ("hero" or "target"))
                        continue;
                    foreach (string store in part[(colon + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        list.Add((scope, store.ToLowerInvariant()));
                }
            }
            return list;
        }

        /// <summary>Fight over: remember what the capstones carry.</summary>
        public static void OnFightEnd(Character? hero, Actor? enemy)
        {
            if (hero == null || hero is Enemy)
                return;
            var carried = Carried(hero);
            if (carried.Count == 0)
            {
                _carried.Remove(hero);
                return;
            }
            var snap = new Snapshot();
            foreach (var (scope, store) in carried)
            {
                if (scope == "hero")
                    snap.Hero[store] = store == "streak" ? hero.Effects.MaterialConsecutiveConnects : Stores.Get(hero, store);
                else if (enemy != null)
                    snap.Target[store] = Stores.Get(enemy, store);
            }
            _carried.AddOrUpdate(hero, snap);
        }

        /// <summary>New fight (after per-fight resets): restore the carried stores. Returns units restored.</summary>
        public static int OnFightStart(Character? hero, Actor? enemy)
        {
            if (hero == null || hero is Enemy || !_carried.TryGetValue(hero, out var snap))
                return 0;
            _carried.Remove(hero);
            int restored = 0;
            foreach (var (store, units) in snap.Hero)
            {
                if (units <= 0)
                    continue;
                if (store == "streak")
                {
                    hero.Effects.MaterialConsecutiveConnects = Math.Max(hero.Effects.MaterialConsecutiveConnects, units);
                    restored += units;
                    continue;
                }
                int missing = units - Stores.Get(hero, store);
                if (missing > 0)
                    restored += Stores.Add(hero, store, missing);
            }
            if (enemy != null)
            {
                foreach (var (store, units) in snap.Target)
                    if (units > 0)
                        restored += ApplyToEnemy(enemy, store, units);
            }
            return restored;
        }

        private static int ApplyToEnemy(Actor enemy, string store, int units)
        {
            switch (store)
            {
                case "poison":
                    enemy.PoisonPercentOfMaxHealth += units * Stores.PoisonPercentPerUnit;
                    return units;
                case "bleed":
                    enemy.BleedIntensity += units;
                    return units;
                case "burn":
                    enemy.BurnIntensity += units;
                    return units;
                default:
                    return Stores.Add(enemy, store, units);
            }
        }

        /// <summary>Drops a pending carry (run ended).</summary>
        public static void Clear(Character hero) => _carried.Remove(hero);
    }
}
