using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// Live envelope served to ComfyUI (and mirrored to <c>live.json</c>).
    /// Wraps the same scene packets as right-click export with a monotonic revision.
    /// </summary>
    public sealed class CombatSceneLiveFeedSnapshot
    {
        public static CombatSceneLiveFeedSnapshot Empty { get; } = new()
        {
            Revision = 0,
            UpdatedUtc = DateTime.UnixEpoch,
            FightId = "",
            BeatIndex = -1,
            BeatCount = 0,
            Status = "idle",
            Latest = null,
            Scenes = Array.Empty<CombatScenePacket>()
        };

        [JsonPropertyName("revision")]
        public long Revision { get; init; }

        [JsonPropertyName("updatedUtc")]
        public DateTime UpdatedUtc { get; init; }

        [JsonPropertyName("fightId")]
        public string FightId { get; init; } = "";

        /// <summary>0-based index of <see cref="Latest"/> inside <see cref="Scenes"/>; -1 when empty.</summary>
        [JsonPropertyName("beatIndex")]
        public int BeatIndex { get; init; }

        [JsonPropertyName("beatCount")]
        public int BeatCount { get; init; }

        /// <summary><c>idle</c> when no fight is bound; <c>active</c> while a fight context is live.</summary>
        [JsonPropertyName("status")]
        public string Status { get; init; } = "idle";

        [JsonPropertyName("latest")]
        public CombatScenePacket? Latest { get; init; }

        [JsonPropertyName("scenes")]
        public IReadOnlyList<CombatScenePacket> Scenes { get; init; } = Array.Empty<CombatScenePacket>();
    }
}
