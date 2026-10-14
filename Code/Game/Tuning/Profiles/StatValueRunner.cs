using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// STATVALUE: prices each item stat by adding +k of it (as a stat line on the hero's weapon) and measuring the change in
    /// combat value (offense × defense, as in ACTIONVALUE) against the same hero without it. Rates are reported per unit and
    /// in "weapon damage points" (relative to +1 flat Damage) — the exchange rates <see cref="ItemValueModel"/> should use.
    /// </summary>
    /// <example>DF.exe STATVALUE --patch overnight-v1 --level 10 --gear typical</example>
    public static class StatValueRunner
    {
        public static readonly (string Stat, double Amount)[] DefaultStats =
        {
            ("Damage", 3), ("STR", 10), ("AGI", 10), ("TEC", 10), ("INT", 10), ("HIT", 2), ("COMBO", 2), ("CRIT", 2),
            ("Armor", 5), ("Health", 30), ("AttackSpeed", 1), ("HealthRegen", 5),
        };

        public static async Task<int> RunAsync(string[] args)
        {
            FundamentalsMatrixRunner.ApplySetFlags(args);
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? 10;
            int encounters = TuningCliArgs.GetIntFlag(args, "--encounters", "-e") ?? 150;
            int seed = TuningCliArgs.GetIntFlag(args, "--seed") ?? 1;
            string enemy = TuningCliArgs.GetFlagValue(args, "--enemy") ?? "Wolf";
            SequenceLabRunner.Gear = TuningCliArgs.GetFlagValue(args, "--gear") ?? "typical";
            var weapons = (TuningCliArgs.GetFlagValue(args, "--weapons") ?? "Mace,Sword,Dagger,Wand").Split(',', StringSplitOptions.TrimEntries);
            string? only = TuningCliArgs.GetFlagValue(args, "--stats");
            // --attr-amount N: price STR/AGI/TEC/INT with a bigger step (small steps sit inside the fight-to-fight noise).
            double? attrAmount = TuningCliArgs.GetIntFlag(args, "--attr-amount");
            var stats = DefaultStats
                .Where(s => only == null || only.Split(',').Contains(s.Stat, StringComparer.OrdinalIgnoreCase))
                .Select(s => attrAmount.HasValue && s.Stat is "STR" or "AGI" or "TEC" or "INT" ? (s.Stat, attrAmount.Value) : s)
                .ToList();

            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== STATVALUE L{level} vs {enemy} | gear {SequenceLabRunner.Gear} | {encounters} fights | seed {seed} ===");

            async Task<(double turns, double netHpPerTurn)> Measure()
            {
                double turns = 0, net = 0;
                foreach (string w in weapons)
                {
                    var r = await SequenceLabRunner.RunSequenceAsync(w, enemy, level, new[] { SequenceLabRunner.WeaponBasics[w] }, encounters, seed).ConfigureAwait(false);
                    turns += r.HeroTurns;
                    net += r.EnemyDamagePerHeroTurn;
                }
                return (turns / weapons.Length, net / weapons.Length);
            }

            FundamentalsCombatSetup.ExtraWeaponStatBonuses.Clear();
            var baseline = await Measure().ConfigureAwait(false);
            Console.WriteLine($"baseline: hero turns {baseline.turns:F2}  net HP lost/turn {baseline.netHpPerTurn:F2}");

            var rows = new List<(string stat, double amount, double value, double perUnit)>();
            foreach (var (stat, amount) in stats)
            {
                FundamentalsCombatSetup.ExtraWeaponStatBonuses.Clear();
                FundamentalsCombatSetup.ExtraWeaponStatBonuses.Add(new StatBonus
                {
                    Name = $"STATVALUE {stat}",
                    StatType = stat,
                    Value = amount,
                    Mechanics = new List<StatBonusMechanic> { new() { StatType = stat, Value = amount } }
                });
                var m = await Measure().ConfigureAwait(false);
                double offense = m.turns > 0 ? baseline.turns / m.turns : 0;
                double defense = m.netHpPerTurn > 0 ? baseline.netHpPerTurn / m.netHpPerTurn : 2;
                double value = offense * defense * 100;
                rows.Add((stat, amount, value, (value - 100) / amount));
                Console.WriteLine($"  +{amount} {stat,-12} value {value,6:F1}  (offense {offense:F3}, defense {defense:F3})");
            }
            FundamentalsCombatSetup.ExtraWeaponStatBonuses.Clear();

            double damageRate = rows.FirstOrDefault(r => r.stat == "Damage").perUnit;
            Console.WriteLine();
            Console.WriteLine($"{"Stat",-12}{"Value%/unit",12}{"Points/unit",12}   (points = weapon-damage equivalents)");
            foreach (var r in rows)
                Console.WriteLine($"{r.stat,-12}{r.perUnit,12:F3}{(damageRate > 0 ? r.perUnit / damageRate : 0),12:F3}");

            string outPath = GameConstants.ResolveGameDataPath("TuningRuns", $"statvalue-L{level}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(outPath, JsonSerializer.Serialize(rows.Select(r => new
            {
                r.stat, r.amount, r.value, r.perUnit, points = damageRate > 0 ? r.perUnit / damageRate : 0
            }), new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Run record: {Path.GetFullPath(outPath)}");
            return 0;
        }
    }
}
