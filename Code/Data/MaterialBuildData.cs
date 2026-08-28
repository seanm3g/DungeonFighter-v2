using System;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RPGGame.Data
{
    /// <summary>
    /// One MATERIAL BUILDS row. Sheet columns round-trip; <see cref="WhenToken"/> / <see cref="Keyword"/> /
    /// <see cref="FeedMaterial"/> are parsed at load for combat.
    /// </summary>
    public sealed class MaterialBuildData
    {
        public const int StackUnlockCount = 2;
        public const int StackAdditiveCount = 3;
        public const int StackMultiplyCount = 5;

        [JsonPropertyName("class")]
        public string AssociatedClass { get; set; } = "";

        [JsonPropertyName("material")]
        public string Material { get; set; } = "";

        [JsonPropertyName("synthesis")]
        public string Synthesis { get; set; } = "";

        [JsonPropertyName("convertAction")]
        public string ConvertAction { get; set; } = "";

        [JsonPropertyName("feed")]
        public string Feed { get; set; } = "";

        [JsonPropertyName("stack2")]
        public string Stack2 { get; set; } = "SYNTHESIS + CONVERT UNLOCK";

        [JsonPropertyName("stack3")]
        public string Stack3 { get; set; } = "+ feed";

        [JsonPropertyName("stack5")]
        public string Stack5 { get; set; } = "x feed";

        [JsonIgnore]
        public string WhenToken { get; private set; } = "";

        [JsonIgnore]
        public string Keyword { get; private set; } = "";

        [JsonIgnore]
        public string FeedMaterial { get; private set; } = "";

        public void RefreshParsedFields()
        {
            Material = CanonicalMaterialName(Material);
            ParseSynthesis(Synthesis, out string when, out string keyword);
            WhenToken = when;
            Keyword = keyword;
            FeedMaterial = ParseFeedMaterial(Feed, Material);
        }

        /// <summary>Mint amount when the synthesis WHEN fires. Zero below 2 stacks.</summary>
        public static int ComputeMintAmount(int unlockMaterialCount, int feedMaterialCount)
        {
            if (unlockMaterialCount < StackUnlockCount)
                return 0;

            int amount = 1;
            if (unlockMaterialCount >= StackAdditiveCount)
                amount = Math.Max(0, feedMaterialCount);
            if (unlockMaterialCount >= StackMultiplyCount)
                amount *= Math.Max(1, feedMaterialCount);
            return amount;
        }

        public static string CanonicalMaterialName(string? raw)
        {
            string t = (raw ?? "").Trim();
            if (t.Length == 0)
                return "";
            if (t.Equals("MITHIRL", StringComparison.OrdinalIgnoreCase))
                return "Mithril";
            if (t.Equals("DAMASCUS", StringComparison.OrdinalIgnoreCase))
                return "Iron";
            if (t.Length == 1)
                return t.ToUpperInvariant();
            return char.ToUpper(t[0], CultureInfo.InvariantCulture) + t.Substring(1).ToLowerInvariant();
        }

        public static void ParseSynthesis(string? synthesis, out string whenToken, out string keyword)
        {
            whenToken = "";
            keyword = "";
            string raw = (synthesis ?? "").Trim();
            if (raw.Length == 0)
                return;

            int eq = raw.IndexOf('=');
            if (eq < 0)
            {
                whenToken = NormalizeWhenToken(raw);
                return;
            }

            whenToken = NormalizeWhenToken(raw.Substring(0, eq));
            keyword = raw.Substring(eq + 1).Trim().ToUpperInvariant();
        }

        public static string ParseFeedMaterial(string? feed, string fallbackMaterial)
        {
            string raw = (feed ?? "").Trim();
            if (raw.Length == 0)
                return CanonicalMaterialName(fallbackMaterial);

            var match = Regex.Match(raw, @"per\s+([A-Za-z]+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return CanonicalMaterialName(match.Groups[1].Value);

            return CanonicalMaterialName(fallbackMaterial);
        }

        /// <summary>Player-facing WHEN for combat log, e.g. <c>ON_HIT = GRAZE</c> → <c>ON HIT</c>.</summary>
        public static string FormatWhenLabel(string? synthesis)
        {
            string raw = (synthesis ?? "").Trim();
            if (raw.Length == 0)
                return "";
            int eq = raw.IndexOf('=');
            if (eq >= 0)
                raw = raw.Substring(0, eq).Trim();
            return raw.Replace('_', ' ').Replace('-', ' ');
        }

        /// <summary>Strip spaces/underscores so sheet <c>ON_HIT</c> matches gate tokens.</summary>
        public static string NormalizeWhenToken(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";
            var chars = (raw ?? "").Trim().ToUpperInvariant().ToCharArray();
            int w = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c == '_' || c == ' ' || c == '-')
                    continue;
                chars[w++] = c;
            }
            return new string(chars, 0, w);
        }
    }
}
