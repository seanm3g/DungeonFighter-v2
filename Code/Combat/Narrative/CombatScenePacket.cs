using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// One combat-log beat as a scene packet (image-prompt friendly).
    /// JSON names match the exported combat-log schema.
    /// Establishing shots omit character/action/target/gear so the room is the only subject.
    /// <see cref="Composition"/> steers ComfyUI toward a room-dominant frame with combat in a corner.
    /// </summary>
    public sealed class CombatScenePacket
    {
        [JsonPropertyName("character")]
        public string? Character { get; init; }

        [JsonPropertyName("action")]
        public string? Action { get; init; }

        [JsonPropertyName("target")]
        public string? Target { get; init; }

        [JsonPropertyName("environment")]
        public string Environment { get; init; } = string.Empty;

        [JsonPropertyName("emotion")]
        public string Emotion { get; init; } = string.Empty;

        [JsonPropertyName("camera")]
        public string Camera { get; init; } = string.Empty;

        /// <summary>
        /// Composition lock for image tools (ComfyUI): keep the establishing room dominant;
        /// place fighters in a small corner region on action beats.
        /// </summary>
        [JsonPropertyName("composition")]
        public string? Composition { get; init; }

        [JsonPropertyName("gear")]
        public CombatSceneGear? Gear { get; init; }

        [JsonPropertyName("effects")]
        public IReadOnlyList<string> Effects { get; init; } = System.Array.Empty<string>();
    }

    /// <summary>Visible kit for the acting character. Cloak maps from a mantle/cloak/cape-named legs piece when present.</summary>
    public sealed class CombatSceneGear
    {
        [JsonPropertyName("helmet")]
        public string? Helmet { get; init; }

        [JsonPropertyName("cloak")]
        public string? Cloak { get; init; }

        [JsonPropertyName("armor")]
        public string? Armor { get; init; }

        [JsonPropertyName("weapon_main")]
        public string? WeaponMain { get; init; }

        [JsonPropertyName("weapon_offhand")]
        public string? WeaponOffhand { get; init; }
    }
}
