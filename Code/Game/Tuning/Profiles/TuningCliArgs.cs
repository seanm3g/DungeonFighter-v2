using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning.Profiles
{
    public static class TuningCliArgs
    {

        public static bool HasFlag(string[] args, string flag) =>
            args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

        public static int? GetIntFlag(string[] args, params string[] flags)
        {
            string? value = GetFlagValue(args, flags);
            return value != null && int.TryParse(value, out int n) ? n : null;
        }

        /// <summary>
        /// <c>--seed N</c> for every sim mode. Values inside int range are used as-is (existing seeded runs replay
        /// unchanged). Larger values, such as the per-shard seeds the scripts build as <c>seed*1_000_000 + level*1000 + i</c>
        /// for <c>--seed</c> &gt;= 2148, fold deterministically into int range instead of being dropped; before
        /// 2026-11-05 they failed <c>int.TryParse</c> and the run went silently unseeded. An unreadable seed throws.
        /// </summary>
        public static int? GetSeedFlag(string[] args)
        {
            string? value = GetFlagValue(args, "--seed");
            if (value == null)
                return null;
            if (!long.TryParse(value, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out long n))
                throw new ArgumentException($"--seed '{value}' is not an integer.");
            return FoldSeed(n);
        }

        /// <summary>Maps any 64-bit seed to an int: identity inside int range, else a positive residue mod int.MaxValue.</summary>
        public static int FoldSeed(long n)
        {
            if (n >= int.MinValue && n <= int.MaxValue)
                return (int)n;
            long m = n % int.MaxValue;
            if (m < 0)
                m += int.MaxValue;
            return (int)m;
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

    }
}
