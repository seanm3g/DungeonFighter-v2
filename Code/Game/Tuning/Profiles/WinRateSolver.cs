using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// WRSOLVE: finds the enemy damage multiplier at one level that gives a target even-match win rate
    /// (typical gear, weapon × enemy matrix, seeded). Bisects in log space; win rate falls as the multiplier rises.
    /// Run one process per anchor level, then write the results into <c>enemySystem.damageCurve.anchors</c>.
    /// </summary>
    /// <example>DF.exe WRSOLVE --patch overnight-v1 --level 50 --target 53 --gear typical</example>
    public static class WinRateSolver
    {
        public static async Task<int> RunAsync(string[] args)
        {
            FundamentalsMatrixRunner.ApplySetFlags(args);
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? 10;
            double target = double.Parse(TuningCliArgs.GetFlagValue(args, "--target") ?? "95", CultureInfo.InvariantCulture) / 100.0;
            int encounters = TuningCliArgs.GetIntFlag(args, "--encounters", "-e") ?? 60;
            int seed = TuningCliArgs.GetIntFlag(args, "--seed") ?? 1;
            int steps = TuningCliArgs.GetIntFlag(args, "--steps") ?? 8;
            string gear = TuningCliArgs.GetFlagValue(args, "--gear") ?? "typical";
            double lo = double.Parse(TuningCliArgs.GetFlagValue(args, "--lo") ?? "0.25", CultureInfo.InvariantCulture);
            double hi = double.Parse(TuningCliArgs.GetFlagValue(args, "--hi") ?? "30", CultureInfo.InvariantCulture);
            var weapons = (TuningCliArgs.GetFlagValue(args, "--weapons") ?? "Mace,Sword,Dagger,Wand").Split(',', StringSplitOptions.TrimEntries);
            var enemies = (TuningCliArgs.GetFlagValue(args, "--enemies") ?? "Wolf,Bear,Goblin,Rats,Lich,Wraith").Split(',', StringSplitOptions.TrimEntries);

            var curve = GameConfiguration.Instance.EnemySystem.DamageCurve;
            curve.Enabled = true;
            curve.Anchors = new List<RPGGame.EnemyDamageCurveAnchor>();
            curve.GrowthPerLevel = 0;

            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== WRSOLVE L{level} target {target * 100:F1}% | {weapons.Length}x{enemies.Length} cells x {encounters} | gear {gear} ===");

            double best = 1, bestErr = double.MaxValue, bestWr = 0, bestTurns = 0;
            for (int step = 0; step < steps; step++)
            {
                double m = Math.Sqrt(lo * hi);
                curve.BaseMultiplier = m;
                var (wr, turns) = await EvaluateAsync(level, weapons, enemies, encounters, seed, gear).ConfigureAwait(false);
                Console.WriteLine($"  step {step + 1}: multiplier {m:F3} -> WR {wr * 100:F1}%  hero turns {turns:F2}");
                if (Math.Abs(wr - target) < bestErr)
                {
                    bestErr = Math.Abs(wr - target);
                    best = m;
                    bestWr = wr;
                    bestTurns = turns;
                }
                if (wr > target)
                    lo = m;
                else
                    hi = m;
            }

            Console.WriteLine($"RESULT level {level} multiplier {best.ToString("F3", CultureInfo.InvariantCulture)} WR {bestWr * 100:F1}% turns {bestTurns:F2}");
            return 0;
        }

        public static async Task<(double winRate, double heroTurns)> EvaluateAsync(
            int level, IReadOnlyList<string> weapons, IReadOnlyList<string> enemies, int encounters, int seed, string gear)
        {
            var wrs = new List<double>();
            var turns = new List<double>();
            foreach (string enemy in enemies)
            {
                foreach (string weapon in weapons)
                {
                    var profile = new BalanceTuningProfile
                    {
                        Id = "wrsolve",
                        Simulation = new SimulationProfileConfig
                        {
                            Mode = TuningSimulationModes.FundamentalsEncounter,
                            EncounterCount = encounters,
                            Levels = new List<int> { level },
                            PlayerLevel = level,
                            EnemyLevel = level,
                            WeaponType = weapon,
                            EnemyType = enemy,
                            Gear = gear
                        }
                    };
                    var r = await FundamentalsSimulationRunner.RunAsync(profile, new SimulationRunOverrides { Seed = seed }).ConfigureAwait(false);
                    wrs.Add(r.WinRate);
                    turns.Add(r.AveragePlayerTurnsPerEncounter);
                }
            }
            return (wrs.Average(), turns.Average());
        }
    }
}
