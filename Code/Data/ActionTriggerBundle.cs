using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// One spreadsheet trigger family on an action: WHEN × optional SCOPE × mechanic column pointers.
    /// Magnitudes stay in the referenced mechanic columns (DAMAGE MOD, WEAKEN, …).
    /// </summary>
    public sealed class ActionTriggerBundle
    {
        /// <summary>Canonical WHEN token (e.g. ONKILL, ONHIT, ONMISS).</summary>
        [JsonPropertyName("when")]
        public string When { get; set; } = "";

        /// <summary>Enable/count cell (usually "1"). For ONROLLVALUE this is the face (1–20).</summary>
        [JsonPropertyName("count")]
        public string Count { get; set; } = "";

        /// <summary>
        /// Cadence scope for lasting grants (TURN / ACTION / FIGHT / DUNGEON). Blank = instant one-shot.
        /// </summary>
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = "";

        /// <summary>Comma-separated mechanic IDs pointing at existing columns (no magnitudes here).</summary>
        [JsonPropertyName("mechanics")]
        public string Mechanics { get; set; } = "";

        public bool IsEnabled => !string.IsNullOrWhiteSpace(Count);

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
    }
}
