using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning.Profiles
{
    public static class TuningCliArgs
    {
        public static string GetProfileId(string[] args, string defaultProfile = BalanceTuningProfileLoader.DefaultProfileId)
        {
            string? fromFlag = GetFlagValue(args, "--profile", "-p");
            if (!string.IsNullOrWhiteSpace(fromFlag))
                return fromFlag;

            // Positional profile id after command name when not numeric and not a flag
            if (args.Length > 1 && !args[1].StartsWith('-') && !int.TryParse(args[1], out _))
                return args[1];

            return defaultProfile;
        }

        public static bool HasFlag(string[] args, string flag) =>
            args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

        public static int? GetIntFlag(string[] args, params string[] flags)
        {
            string? value = GetFlagValue(args, flags);
            return value != null && int.TryParse(value, out int n) ? n : null;
        }

        public static string? GetFlagValue(string[] args, params string[] flags)
        {
            for (int i = 0; i < args.Length; i++)
            {
                foreach (string flag in flags)
                {
                    if (!args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                        return args[i + 1];

                    return "";
                }

                foreach (string flag in flags)
                {
                    if (args[i].StartsWith(flag + "=", StringComparison.OrdinalIgnoreCase))
                        return args[i][(flag.Length + 1)..];
                }
            }

            return null;
        }

        public static SimulationRunOverrides BuildOverrides(string[] args, BalanceTuningProfile profile)
        {
            int? battles = GetIntFlag(args, "--battles", "-b");
            if (battles == null && args.Length > 1 && int.TryParse(args[1], out int positionalBattles))
                battles = positionalBattles;

            string? levelsCsv = GetFlagValue(args, "--levels", "-l");
            if (levelsCsv == null && profile.Simulation.Mode == TuningSimulationModes.MultiLevelWeaponEnemy)
            {
                // Legacy LEVELSIM positional: battles then levels csv
                if (args.Length > 2 && args[2].Contains(','))
                    levelsCsv = args[2];
            }

            IReadOnlyList<int>? levels = null;
            if (!string.IsNullOrWhiteSpace(levelsCsv))
            {
                levels = levelsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse)
                    .ToList();
            }

            return new SimulationRunOverrides
            {
                BattlesPerCombination = battles,
                Levels = levels,
                PlayerLevel = GetIntFlag(args, "--player-level"),
                EnemyLevel = GetIntFlag(args, "--enemy-level"),
                EncounterCount = GetIntFlag(args, "--encounters", "-e"),
                ContinuePastZeroHp = HasFlag(args, "--continue-past-zero-hp") ? true : null,
                NegativeHpFloor = GetIntFlag(args, "--negative-hp-floor"),
                RunsPerClass = GetIntFlag(args, "--runs-per-class", "-r"),
                MaxActionsPerRun = GetIntFlag(args, "--max-actions-per-run"),
                Classes = GetFlagValue(args, "--classes")
            };
        }
    }
}
