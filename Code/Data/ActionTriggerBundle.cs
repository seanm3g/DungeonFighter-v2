using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// One spreadsheet trigger family on an action: WHEN × optional SCOPE × mechanic column pointers.
    /// Magnitudes usually stay in the referenced mechanic columns (DAMAGE MOD, WEAKEN, …).
    /// Item catalog procs may supply <see cref="Value"/> when there is no sheet column.
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

        /// <summary>
        /// Optional magnitude for item (or sheet-less) procs. Used when action sheet fields are zero
        /// (heal amount, threshold steps, next-action %, weapon flat, etc.).
        /// </summary>
        [JsonPropertyName("value")]
        public double? Value { get; set; }

        /// <summary>
        /// Optional non-outcome filters AND'd with the WHEN (e.g. <c>IFCLUTCH</c>, <c>IFTARGETUNDERDOT</c>).
        /// Copied onto the carrier action's <c>TriggerConditions</c> for item procs.
        /// </summary>
        [JsonPropertyName("filters")]
        public List<string>? Filters { get; set; }

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
