using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// One CADENCES-band triple: cadence family × enable × duration × mechanic pointers.
    /// Magnitudes stay in the referenced mechanic columns.
    /// </summary>
    public sealed class ActionCadenceBundle
    {
        /// <summary>TURN / ACTION / FIGHT / DUNGEON.</summary>
        [JsonPropertyName("cadence")]
        public string Cadence { get; set; } = "";

        /// <summary>Enable cell (usually "1"). Empty with mechanics+duration still counts as enabled.</summary>
        [JsonPropertyName("enable")]
        public string Enable { get; set; } = "";

        /// <summary>Application count (DURATION). Empty ⇒ 1 when enabled.</summary>
        [JsonPropertyName("duration")]
        public string Duration { get; set; } = "";

        /// <summary>Comma-separated mechanic IDs (pointers only).</summary>
        [JsonPropertyName("mechanics")]
        public string Mechanics { get; set; } = "";

        public bool IsEnabled =>
            IsTruthy(Enable)
            || !string.IsNullOrWhiteSpace(Duration)
            || !string.IsNullOrWhiteSpace(Mechanics);

        public int ResolveDurationCount()
        {
            if (int.TryParse((Duration ?? "").Trim(), out int d) && d > 0)
                return d;
            if (int.TryParse((Enable ?? "").Trim(), out int e) && e > 1)
                return e;
            return IsEnabled ? 1 : 0;
        }

        public IReadOnlyList<string> ParseMechanicIds()
        {
            if (string.IsNullOrWhiteSpace(Mechanics))
                return Array.Empty<string>();
            return Mechanics
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsTruthy(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            string t = raw.Trim();
            if (t is "0" or "-" or "false" or "FALSE" or "no" or "NO")
                return false;
            return true;
        }
    }
}
