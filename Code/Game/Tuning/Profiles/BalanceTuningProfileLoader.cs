using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Utils;

namespace RPGGame.Tuning.Profiles
{
    public static class BalanceTuningProfileLoader
    {
        public const string DefaultProfileId = "level-curve";
        public const string ProfilesRelativeDir = "GameData/TuningProfiles";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static string ResolveProfilesDirectory(string? overrideDir = null)
        {
            if (!string.IsNullOrWhiteSpace(overrideDir))
                return overrideDir;

            foreach (string candidate in GetPossibleProfilesDirectories())
            {
                if (Directory.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(Directory.GetCurrentDirectory(), ProfilesRelativeDir);
        }

        private static IEnumerable<string> GetPossibleProfilesDirectories()
        {
            string cwd = Directory.GetCurrentDirectory();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            foreach (string root in new[] { cwd, baseDir })
            {
                if (string.IsNullOrEmpty(root))
                    continue;

                yield return Path.Combine(root, ProfilesRelativeDir);
                yield return Path.GetFullPath(Path.Combine(root, "..", ProfilesRelativeDir));
                yield return Path.GetFullPath(Path.Combine(root, "..", "..", ProfilesRelativeDir));
                yield return Path.GetFullPath(Path.Combine(root, "..", "..", "..", ProfilesRelativeDir));
            }
        }

        public static BalanceTuningProfile Load(string profileIdOrPath, string? profilesDir = null)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrPath))
                profileIdOrPath = DefaultProfileId;

            string path = ResolveProfilePath(profileIdOrPath, profilesDir);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Tuning profile not found: {profileIdOrPath} (looked at {path})");

            string json = File.ReadAllText(path);
            var profile = JsonSerializer.Deserialize<BalanceTuningProfile>(json, JsonOptions)
                ?? throw new InvalidOperationException($"Failed to parse tuning profile: {path}");

            if (string.IsNullOrWhiteSpace(profile.Id))
                profile.Id = Path.GetFileNameWithoutExtension(path);

            ApplyDefaults(profile);
            return profile;
        }

        public static IReadOnlyList<BalanceTuningProfile> ListProfiles(string? profilesDir = null)
        {
            string dir = ResolveProfilesDirectory(profilesDir);
            if (!Directory.Exists(dir))
                return Array.Empty<BalanceTuningProfile>();

            return Directory.GetFiles(dir, "*.json")
                .OrderBy(Path.GetFileName)
                .Select(f =>
                {
                    try { return Load(f, profilesDir); }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"BalanceTuningProfileLoader: skip {f}: {ex.Message}");
                        return null;
                    }
                })
                .Where(p => p != null)
                .Cast<BalanceTuningProfile>()
                .ToList();
        }

        public static string ResolveProfilePath(string profileIdOrPath, string? profilesDir = null)
        {
            if (File.Exists(profileIdOrPath))
                return Path.GetFullPath(profileIdOrPath);

            string dir = ResolveProfilesDirectory(profilesDir);
            string withJson = Path.Combine(dir, profileIdOrPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? profileIdOrPath
                : $"{profileIdOrPath}.json");
            return withJson;
        }

        public static void ApplyDefaults(BalanceTuningProfile profile)
        {
            if (profile.Analysis.Validators.Count == 0)
            {
                profile.Analysis.Validators = profile.Simulation.Mode switch
                {
                    TuningSimulationModes.ComprehensiveWeaponEnemy => profile.Analysis.OptimizeWinRate
                        ? new List<string> { TuningValidatorIds.Comprehensive }
                        : new List<string>
                        {
                            TuningValidatorIds.CombatDuration,
                            TuningValidatorIds.WeaponVariance,
                            TuningValidatorIds.EnemyDifferentiation
                        },
                    TuningSimulationModes.FundamentalsEncounter => new List<string>
                    {
                        TuningValidatorIds.FundamentalsTempo,
                        TuningValidatorIds.FundamentalsAnchors,
                        TuningValidatorIds.FundamentalsComboStreaks
                    },
                    TuningSimulationModes.ClassPlaythroughBatch => new List<string>
                    {
                        TuningValidatorIds.PlaythroughProgression,
                        TuningValidatorIds.PlaythroughClassParity
                    },
                    _ => profile.Analysis.OptimizeWinRate
                        ? new List<string> { TuningValidatorIds.LevelCurve }
                        : new List<string> { TuningValidatorIds.CombatDuration }
                };
            }

            if (profile.Analysis.Suggesters.Count == 0)
            {
                profile.Analysis.Suggesters = profile.Simulation.Mode switch
                {
                    TuningSimulationModes.ComprehensiveWeaponEnemy => profile.Analysis.OptimizeWinRate
                        ? new List<string>
                        {
                            TuningSuggesterIds.Global,
                            TuningSuggesterIds.Player,
                            TuningSuggesterIds.EnemyBaseline,
                            TuningSuggesterIds.Weapon,
                            TuningSuggesterIds.Enemy,
                            TuningSuggesterIds.Duration
                        }
                        : new List<string>
                        {
                            TuningSuggesterIds.Weapon,
                            TuningSuggesterIds.Duration
                        },
                    TuningSimulationModes.FundamentalsEncounter => new List<string>(),
                    TuningSimulationModes.ClassPlaythroughBatch => new List<string>
                    {
                        TuningSuggesterIds.PlaythroughBalance
                    },
                    _ => profile.Analysis.OptimizeWinRate
                        ? new List<string> { TuningSuggesterIds.LevelCurve }
                        : new List<string>()
                };
            }

            profile.Analysis.FundamentalsTargets ??= new FundamentalsAnalysisTargets();
            profile.Analysis.PlaythroughTargets ??= new PlaythroughAnalysisTargets();

            if (profile.Simulation.Mode == TuningSimulationModes.FundamentalsEncounter
                && (profile.Simulation.Levels == null || profile.Simulation.Levels.Count == 0))
            {
                profile.Simulation.Levels = FundamentalsLevelAnchors.GetDecadeLevels().ToList();
            }

            if (profile.Analysis.MaxSuggestions < 0)
                profile.Analysis.MaxSuggestions = 1;
        }
    }
}
