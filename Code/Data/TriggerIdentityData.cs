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

        /// <summary>
        /// Authoring hint: who the effect applies to (<c>hero</c>/<c>enemy</c>/<c>self</c>/<c>foe</c>/<c>strip</c>/<c>system</c>).
        /// Combat still resolves from mechanic ids — not a runtime authority.
        /// </summary>
        [JsonPropertyName("effectTarget")]
        public string EffectTarget { get; set; } = "";

        [JsonPropertyName("when")]
        public string When { get; set; } = "";

        /// <summary>Colon payload from <see cref="When"/> (e.g. <c>7</c> from <c>ONNATURALROLL:7</c>).</summary>
        [JsonPropertyName("whenArg")]
        public string WhenArg { get; set; } = "";

        [JsonPropertyName("count")]
        public string Count { get; set; } = "1";

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = "";

        [JsonPropertyName("mechanics")]
        public string Mechanics { get; set; } = "";

        /// <summary>Colon payload from <see cref="Mechanics"/> (e.g. <c>2</c> from <c>retrigger_slot:2</c>).</summary>
        [JsonPropertyName("mechanicArg")]
        public string MechanicArg { get; set; } = "";

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
                IdentityName = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim(),
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
