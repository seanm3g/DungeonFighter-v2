using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>One consume / produce entry: a store and units (<c>4</c>, <c>all</c>, <c>half:bleed</c>).</summary>
    public sealed class ConversionAmount
    {
        [JsonPropertyName("store")] public string Store { get; set; } = "";
        [JsonPropertyName("units")] public string Units { get; set; } = "1";
    }

    /// <summary>One row of <c>GameData/Conversions.json</c>.</summary>
    public sealed class ConversionRow
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("owner")] public string Owner { get; set; } = "";
        [JsonPropertyName("phase")] public string Phase { get; set; } = "early";
        [JsonPropertyName("label")] public string Label { get; set; } = "";
        [JsonPropertyName("requires")] public List<string> Requires { get; set; } = new();
        [JsonPropertyName("chance")] public string Chance { get; set; } = "";
        [JsonPropertyName("repeatOnCrit")] public int RepeatOnCrit { get; set; } = 1;
        [JsonPropertyName("consume")] public List<ConversionAmount> Consume { get; set; } = new();
        [JsonPropertyName("produce")] public List<ConversionAmount> Produce { get; set; } = new();
        [JsonPropertyName("produceOnCrit")] public List<ConversionAmount> ProduceOnCrit { get; set; } = new();
    }

    /// <summary>
    /// The conversion table (Documentation/BUILD_SYSTEM_CONDENSED.md §8): data rows instead of one bespoke method per
    /// conversion skill. Rows are owned by skill nodes (<see cref="ConversionRow.Owner"/> = customEffectId) and run on
    /// the owner's connects, in file order within a phase. Store semantics match the methods they replaced: requires
    /// read live intensity (not queued), DoTs produce as queued-from-hit, poison units are 0.25% max HP.
    /// </summary>
    public static class ConversionTable
    {
        private sealed class FileShape
        {
            [JsonPropertyName("rows")] public List<ConversionRow> Rows { get; set; } = new();
        }

        private static List<ConversionRow>? _rows;
        private static readonly object _lock = new();

        public static IReadOnlyList<ConversionRow> Rows
        {
            get
            {
                lock (_lock)
                    return _rows ??= Load();
            }
        }

        public static void Reload()
        {
            lock (_lock)
                _rows = null;
        }

        private static List<ConversionRow> Load()
        {
            try
            {
                string path = GameConstants.ResolveGameDataPath("Conversions.json");
                if (!File.Exists(path))
                    return new List<ConversionRow>();
                var shape = JsonSerializer.Deserialize<FileShape>(File.ReadAllText(path));
                return shape?.Rows ?? new List<ConversionRow>();
            }
            catch (Exception)
            {
                return new List<ConversionRow>();
            }
        }

        /// <summary>Runs every owned row of <paramref name="phase"/> for a hero connect on <paramref name="target"/>.</summary>
        public static void Apply(Character hero, Actor? target, bool isCritical, string phase)
        {
            if (target == null)
                return;
            foreach (var row in Rows)
            {
                if (!string.Equals(row.Phase, phase, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (SkillRanks.GetScaledRank(hero, row.Owner) <= 0)
                    continue;
                ApplyRow(hero, target, row, isCritical);
            }
        }

        /// <summary>Runs one row (ownership already checked). Returns passes that converted.</summary>
        public static int ApplyRow(Character hero, Actor target, ConversionRow row, bool isCritical)
        {
            if (!row.Requires.All(s => IsUp(target, s)))
                return 0;
            if (row.Chance.Equals("power", StringComparison.OrdinalIgnoreCase)
                && !(RandomUtility.NextDouble() < Math.Min(1.0, SkillRanks.GetPower(hero, row.Owner))))
                return 0;

            int passes = isCritical ? Math.Max(1, row.RepeatOnCrit) : 1;
            int done = 0;
            for (int i = 0; i < passes; i++)
            {
                if (i > 0 && !row.Requires.All(s => IsUp(target, s)))
                    break;
                var produce = row.Produce.Select(p => (p.Store, Units: Resolve(target, p.Units))).ToList();
                foreach (var c in row.Consume)
                    Consume(target, c.Store, c.Units);
                foreach (var (store, units) in produce)
                    Produce(target, store, units);
                done++;
            }
            if (isCritical && done > 0)
                foreach (var p in row.ProduceOnCrit)
                    Produce(target, p.Store, Resolve(target, p.Units));
            return done;
        }

        /// <summary>Live status check (queued DoTs don't count, as in the original skills).</summary>
        public static bool IsUp(Actor target, string store) => store.ToLowerInvariant() switch
        {
            "poison" => target.PoisonPercentOfMaxHealth > 0,
            "bleed" => target.BleedIntensity > 0,
            "burn" => target.BurnIntensity > 0,
            "acid" => target.AcidIntensity > 0,
            "slow" => target is Character c && c.Effects.SlowTurns > 0,
            _ => Stores.Get(target, store) > 0
        };

        private static int Live(Actor target, string store) => store.ToLowerInvariant() switch
        {
            "bleed" => target.BleedIntensity,
            "burn" => target.BurnIntensity,
            "acid" => target.AcidIntensity,
            _ => Stores.Get(target, store)
        };

        private static int Resolve(Actor target, string units)
        {
            units = (units ?? "").Trim();
            if (units.StartsWith("half:", StringComparison.OrdinalIgnoreCase))
                return Math.Max(1, Live(target, units[5..]) / 2);
            return int.TryParse(units, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
        }

        private static void Consume(Actor target, string store, string units)
        {
            bool all = units.Trim().Equals("all", StringComparison.OrdinalIgnoreCase);
            int n = all ? int.MaxValue : Resolve(target, units);
            switch (store.ToLowerInvariant())
            {
                case "poison":
                    if (all) target.PoisonPercentOfMaxHealth = 0;
                    else Stores.Spend(target, "poison", n);
                    break;
                case "burn":
                    target.BurnIntensity = all ? 0 : Math.Max(0, target.BurnIntensity - n);
                    break;
                case "bleed":
                    target.BleedIntensity = all ? 0 : Math.Max(0, target.BleedIntensity - n);
                    break;
                case "slow":
                    if (target is Character slowed)
                    {
                        slowed.Effects.SlowTurns = 0;
                        slowed.Effects.SlowMultiplier = 1.0;
                    }
                    break;
                default:
                    Stores.Spend(target, store, n);
                    break;
            }
        }

        private static void Produce(Actor target, string store, int units)
        {
            if (units <= 0)
                return;
            switch (store.ToLowerInvariant())
            {
                case "acid": target.QueueAcidFromHit(units); break;
                case "bleed": target.QueueBleedFromHit(units); break;
                case "burn": target.QueueBurnFromHit(units); break;
                case "poison": target.ApplyPoisonPercent(units * Stores.PoisonPercentPerUnit); break;
                case "stun":
                    target.IsStunned = true;
                    target.StunTurnsRemaining = Math.Max(target.StunTurnsRemaining, units);
                    break;
                case "expose": ExposeStatus.Apply(target, units, 2); break;
                default: Stores.Add(target, store, units); break;
            }
        }
    }
}
