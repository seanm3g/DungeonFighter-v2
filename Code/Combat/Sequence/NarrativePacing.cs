using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// PROTOTYPE (flavor sandbox, branch joey_sandbox_v2 - not reviewed by Max).
    ///
    /// Decides how much of a swing the F7 prose narrator actually says.
    ///
    /// Before this existed, <see cref="CombatSequenceFlavorTokens.ShouldInclude"/> was the only
    /// gate, and it answers "does this step have a value to report", not "should we say it out
    /// loud". Every step with a value spoke on every swing, so an ordinary hit produced eight
    /// sentences - attacker, outcome, roll, action, defense, damage, stance, tempo - and did so
    /// for the whole fight. That is the wall of text, and no rewrite of the sequence* banks can
    /// fix it: the banks control wording, not how many of them fire.
    ///
    /// Two standing decisions need this knob:
    ///   - decision 3, budget flavor by turn duration (~14 words on a 4s action). Unmeetable
    ///     when eight sentences share that window.
    ///   - decision 4, fire on variable intervals with roughly 40-60% of beats carrying no
    ///     flavor at all. Not expressible in the banks - nothing in the JSON can make a step
    ///     stay quiet.
    ///
    /// Config lives in GameData/NarrativePacing.json so pacing stays data-side (ground rule 2:
    /// adding content must never require a code change). Missing file, unreadable file, unknown
    /// profile or unknown step name all fall back to pre-prototype behaviour.
    /// </summary>
    public static class NarrativePacing
    {
        private const string FileName = "NarrativePacing.json";

        private static readonly object Lock = new();
        private static Profile? _active;
        private static bool _loaded;

        [ThreadStatic] private static Random? _threadRandom;

        private static Random Rng => _threadRandom ??= new Random();

        /// <summary>Resolved pacing profile. "Full" means pre-prototype behaviour.</summary>
        public sealed class Profile
        {
            /// <summary>Chance this swing gets any prose at all. 1.0 = always narrate.</summary>
            public double SwingChance { get; init; } = 1.0;

            /// <summary>Cap on sentences for a narrated swing. 0 = no cap.</summary>
            public int MaxSentencesPerSwing { get; init; }

            /// <summary>Per-step speak chance by <see cref="CombatSequenceStepKind"/> name. Missing = 1.0.</summary>
            public IReadOnlyDictionary<string, double> StepChance { get; init; }
                = new Dictionary<string, double>();

            /// <summary>
            /// Overrides the typewriter's whole-paragraph time budget, in ms. 0 = leave the
            /// existing setting alone.
            ///
            /// This matters more than it looks. The shipping budget is a flat
            /// GameConstants.NarrativeParagraphTargetMs (8000), and
            /// CharacterRevealRhythmCalculator.BuildParagraphSchedule divides it by the
            /// *sentence count* - so a paragraph takes 8 seconds whether it holds eight
            /// sentences or two. Cutting the prose down does not make the log faster; it
            /// just types the surviving words more slowly. Pacing cannot be felt without
            /// this knob.
            /// </summary>
            public int ParagraphTargetMs { get; init; }

            /// <summary>True when this profile cannot change anything (the shipping default).</summary>
            public bool IsPassthrough =>
                SwingChance >= 1.0
                && MaxSentencesPerSwing <= 0
                && StepChance.Count == 0
                && ParagraphTargetMs <= 0;

            public static Profile Full { get; } = new();
        }

        /// <summary>Pre-prototype behaviour: narrate every eligible step on every swing.</summary>
        public static Profile Active
        {
            get
            {
                lock (Lock)
                {
                    if (!_loaded)
                    {
                        _active = Load();
                        _loaded = true;
                    }

                    return _active ?? Profile.Full;
                }
            }
        }

        /// <summary>Drops the cached profile so the next read re-reads the file.</summary>
        public static void Invalidate()
        {
            lock (Lock)
            {
                _loaded = false;
                _active = null;
            }
        }

        /// <summary>Overrides the profile in process. Used by tests; pass null to return to the file.</summary>
        public static void OverrideForTesting(Profile? profile, Random? random = null)
        {
            lock (Lock)
            {
                _active = profile;
                _loaded = true;
            }

            _threadRandom = random;
        }

        /// <summary>True when the whole swing should stay silent.</summary>
        public static bool SuppressSwing(Profile profile)
        {
            if (profile.SwingChance >= 1.0)
            {
                return false;
            }

            return Rng.NextDouble() >= profile.SwingChance;
        }

        /// <summary>True when this step should stay quiet on a swing that is otherwise narrated.</summary>
        public static bool SuppressStep(Profile profile, CombatSequenceStepKind kind)
        {
            if (profile.StepChance.Count == 0)
            {
                return false;
            }

            // Unknown or unlisted step kinds keep speaking, so a new kind never goes silent
            // just because nobody has added it to the file yet.
            if (!profile.StepChance.TryGetValue(kind.ToString(), out double chance))
            {
                return false;
            }

            if (chance >= 1.0)
            {
                return false;
            }

            return Rng.NextDouble() >= chance;
        }

        private static Profile? Load()
        {
            try
            {
                string? path = ResolvePath();
                if (path == null)
                {
                    return Profile.Full;
                }

                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;

                if (!root.TryGetProperty("enabled", out var enabled)
                    || enabled.ValueKind != JsonValueKind.True)
                {
                    return Profile.Full;
                }

                if (!root.TryGetProperty("profiles", out var profiles)
                    || profiles.ValueKind != JsonValueKind.Object)
                {
                    return Profile.Full;
                }

                string profileName = root.TryGetProperty("activeProfile", out var active)
                    && active.ValueKind == JsonValueKind.String
                        ? active.GetString() ?? "full"
                        : "full";

                if (!profiles.TryGetProperty(profileName, out var profile)
                    || profile.ValueKind != JsonValueKind.Object)
                {
                    return Profile.Full;
                }

                var stepChance = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                if (profile.TryGetProperty("stepChance", out var steps)
                    && steps.ValueKind == JsonValueKind.Object)
                {
                    foreach (var step in steps.EnumerateObject())
                    {
                        if (step.Value.ValueKind == JsonValueKind.Number
                            && step.Value.TryGetDouble(out double value))
                        {
                            stepChance[step.Name] = Math.Clamp(value, 0.0, 1.0);
                        }
                    }
                }

                double swingChance = 1.0;
                if (profile.TryGetProperty("swingChance", out var swing)
                    && swing.ValueKind == JsonValueKind.Number
                    && swing.TryGetDouble(out double swingValue))
                {
                    swingChance = Math.Clamp(swingValue, 0.0, 1.0);
                }

                int maxSentences = 0;
                if (profile.TryGetProperty("maxSentencesPerSwing", out var max)
                    && max.ValueKind == JsonValueKind.Number
                    && max.TryGetInt32(out int maxValue))
                {
                    maxSentences = Math.Max(0, maxValue);
                }

                int paragraphTargetMs = 0;
                if (profile.TryGetProperty("paragraphTargetMs", out var target)
                    && target.ValueKind == JsonValueKind.Number
                    && target.TryGetInt32(out int targetValue))
                {
                    paragraphTargetMs = Math.Max(0, targetValue);
                }

                return new Profile
                {
                    SwingChance = swingChance,
                    MaxSentencesPerSwing = maxSentences,
                    StepChance = stepChance,
                    ParagraphTargetMs = paragraphTargetMs
                };
            }
            catch (Exception)
            {
                // Pacing must never be able to break combat. Any problem reading the file
                // falls back to the behaviour that shipped before this prototype.
                return Profile.Full;
            }
        }

        private static string? ResolvePath()
        {
            string? found = GameConstants.TryGetExistingGameDataFilePath(FileName);
            if (found != null)
            {
                return found;
            }

            foreach (string candidate in new[]
            {
                Path.Combine("GameData", FileName),
                Path.Combine("..", "GameData", FileName),
                Path.Combine("..", "..", "GameData", FileName),
                FileName
            })
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
