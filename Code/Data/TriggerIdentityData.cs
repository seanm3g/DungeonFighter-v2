using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// One row from the <c>triggers</c> sheet / <c>Triggers.json</c> — reusable item WHEN×SCOPE×mechanics identity.
    /// </summary>
    public sealed class TriggerIdentityData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>Player-facing one-liner for tooltips / sheet authoring.</summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("when")]
        public string When { get; set; } = "";

        [JsonPropertyName("count")]
        public string Count { get; set; } = "1";

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = "";

        [JsonPropertyName("mechanics")]
        public string Mechanics { get; set; } = "";

        [JsonPropertyName("value")]
        public double? Value { get; set; }

        /// <summary>Comma-separated filter tokens on the sheet; stored as a string for tabular round-trip.</summary>
        [JsonPropertyName("filters")]
        public string? Filters { get; set; }

        /// <summary><c>combat</c> → <c>triggerBundles</c>; <c>equip</c> → <c>equipEffects</c>.</summary>
        [JsonPropertyName("channel")]
        public string Channel { get; set; } = "combat";

        /// <summary>Optional magnitude scale source (see <see cref="ActionTriggerBundle.ScaleFrom"/>).</summary>
        [JsonPropertyName("scaleFrom")]
        public string? ScaleFrom { get; set; }

        [JsonIgnore]
        public bool IsEquipEffect =>
            string.Equals(Channel?.Trim(), "equip", System.StringComparison.OrdinalIgnoreCase);

        public IReadOnlyList<string> ParseFilters()
        {
            if (string.IsNullOrWhiteSpace(Filters))
                return System.Array.Empty<string>();
            var list = new List<string>();
            foreach (var part in Filters.Split(new[] { ',', ';', '|' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string t = part.Trim();
                if (t.Length > 0)
                    list.Add(t);
            }

            return list;
        }

        public ActionTriggerBundle ToBundle()
        {
            var filters = ParseFilters();
            return new ActionTriggerBundle
            {
                When = When ?? "",
                Count = string.IsNullOrWhiteSpace(Count) ? "1" : Count.Trim(),
                Scope = Scope ?? "",
                Mechanics = Mechanics ?? "",
                Value = Value,
                Filters = filters.Count == 0 ? null : new List<string>(filters),
                ScaleFrom = string.IsNullOrWhiteSpace(ScaleFrom) ? null : ScaleFrom.Trim()
            };
        }
    }
}
