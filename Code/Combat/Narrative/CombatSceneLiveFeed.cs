using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using RPGGame.Combat;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// In-memory live combat-scene feed for ComfyUI. Rebuilds scene packets from the
    /// bound fight context on each notify; mirrors JSON under <c>GameData/CombatLogs/</c>.
    /// </summary>
    public static class CombatSceneLiveFeed
    {
        public const string LiveFileName = "live.json";
        public const string LiveLatestFileName = "live-latest.json";

        private static readonly object Gate = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static FightBinding? _binding;
        private static CombatSceneLiveFeedSnapshot _snapshot = CombatSceneLiveFeedSnapshot.Empty;
        private static string _envelopeJson = SerializeSnapshot(CombatSceneLiveFeedSnapshot.Empty);
        private static string _latestJson = "{}";
        private static string _scenesJson = "[]";
        private static long _revision;
        private static readonly ManualResetEventSlim ChangedPulse = new(false);

        /// <summary>Raised after a successful publish (may run on a combat thread).</summary>
        public static event Action<CombatSceneLiveFeedSnapshot>? Changed;

        public static CombatSceneLiveFeedSnapshot Current
        {
            get
            {
                lock (Gate)
                    return _snapshot;
            }
        }

        public static string EnvelopeJson
        {
            get
            {
                lock (Gate)
                    return _envelopeJson;
            }
        }

        public static string LatestJson
        {
            get
            {
                lock (Gate)
                    return _latestJson;
            }
        }

        public static string ScenesJson
        {
            get
            {
                lock (Gate)
                    return _scenesJson;
            }
        }

        /// <summary>
        /// Binds the active fight so <see cref="NotifyChanged"/> can rebuild packets.
        /// No-op when combat UI is muted (sims / headless).
        /// </summary>
        public static void BindFight(
            Character hero,
            Enemy enemy,
            Environment? room,
            Dungeon? dungeon,
            Func<BattleNarrative?> narrativeProvider,
            Func<Action?>? lastPlayerActionProvider = null)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            if (narrativeProvider == null) throw new ArgumentNullException(nameof(narrativeProvider));
            if (CombatUiMuteScope.IsMuted)
                return;

            lock (Gate)
            {
                _binding = new FightBinding(
                    hero,
                    enemy,
                    room,
                    dungeon,
                    narrativeProvider,
                    lastPlayerActionProvider ?? (() => null),
                    Guid.NewGuid().ToString("N"));
            }

            PublishFromBinding();
        }

        /// <summary>Clears the fight binding (feed stays at last snapshot until the next bind).</summary>
        public static void UnbindFight()
        {
            lock (Gate)
            {
                _binding = null;
            }
        }

        /// <summary>Rebuilds and publishes from the bound fight when live combat is active.</summary>
        public static void NotifyChanged()
        {
            if (CombatUiMuteScope.IsMuted)
                return;
            PublishFromBinding();
        }

        /// <summary>
        /// Publishes an explicit scene list (right-click export / tests) without requiring a bind.
        /// </summary>
        public static void PublishScenes(
            IReadOnlyList<CombatScenePacket> scenes,
            string? fightId = null,
            string status = "active")
        {
            if (scenes == null || scenes.Count == 0)
                return;

            CombatSceneLiveFeedSnapshot snapshot;
            lock (Gate)
            {
                long revision = Interlocked.Increment(ref _revision);
                string id = !string.IsNullOrWhiteSpace(fightId)
                    ? fightId!
                    : (_binding?.FightId ?? Guid.NewGuid().ToString("N"));
                snapshot = BuildSnapshot(scenes, revision, id, status);
                StoreLocked(snapshot);
            }

            MirrorToDisk(snapshot);
            Pulse(snapshot);
        }

        /// <summary>
        /// Blocks until <see cref="Current"/>.Revision &gt; <paramref name="sinceRevision"/>
        /// or <paramref name="timeoutMs"/> elapses. Returns the current snapshot either way.
        /// </summary>
        public static CombatSceneLiveFeedSnapshot WaitForRevision(long sinceRevision, int timeoutMs)
        {
            if (timeoutMs < 0)
                timeoutMs = 0;
            if (timeoutMs > 120_000)
                timeoutMs = 120_000;

            var deadline = global::System.Environment.TickCount64 + timeoutMs;
            while (true)
            {
                var current = Current;
                if (current.Revision > sinceRevision)
                    return current;

                int remaining = (int)Math.Max(0, deadline - global::System.Environment.TickCount64);
                if (remaining == 0)
                    return current;

                ChangedPulse.Reset();
                current = Current;
                if (current.Revision > sinceRevision)
                    return current;

                ChangedPulse.Wait(Math.Min(remaining, 250));
            }
        }

        /// <summary>Test hook: clears bind + snapshot without touching the HTTP server.</summary>
        internal static void ResetForTests()
        {
            lock (Gate)
            {
                _binding = null;
                _revision = 0;
                _snapshot = CombatSceneLiveFeedSnapshot.Empty;
                _envelopeJson = SerializeSnapshot(_snapshot);
                _latestJson = "{}";
                _scenesJson = "[]";
            }

            ChangedPulse.Set();
        }

        private static void PublishFromBinding()
        {
            FightBinding? binding;
            lock (Gate)
                binding = _binding;
            if (binding == null)
                return;

            BattleNarrative? narrative = null;
            Action? lastAction = null;
            try
            {
                narrative = binding.NarrativeProvider();
                lastAction = binding.LastPlayerActionProvider();
            }
            catch
            {
                // Binding callbacks must never break combat.
            }

            var scenes = CombatScenePacketBuilder.BuildLog(
                binding.Hero,
                binding.Enemy,
                binding.Room,
                binding.Dungeon,
                narrative,
                lastAction);
            if (scenes.Count == 0)
                return;

            PublishScenes(scenes, binding.FightId, status: "active");
        }

        private static CombatSceneLiveFeedSnapshot BuildSnapshot(
            IReadOnlyList<CombatScenePacket> scenes,
            long revision,
            string fightId,
            string status)
        {
            int beatIndex = scenes.Count - 1;
            return new CombatSceneLiveFeedSnapshot
            {
                Revision = revision,
                UpdatedUtc = DateTime.UtcNow,
                FightId = fightId,
                BeatIndex = beatIndex,
                BeatCount = scenes.Count,
                Status = string.IsNullOrWhiteSpace(status) ? "active" : status,
                Latest = scenes[beatIndex],
                Scenes = scenes
            };
        }

        private static void StoreLocked(CombatSceneLiveFeedSnapshot snapshot)
        {
            _snapshot = snapshot;
            _envelopeJson = SerializeSnapshot(snapshot);
            _latestJson = snapshot.Latest == null
                ? "{}"
                : JsonSerializer.Serialize(snapshot.Latest, JsonOptions);
            _scenesJson = CombatScenePacketBuilder.Serialize(snapshot.Scenes);
        }

        private static string SerializeSnapshot(CombatSceneLiveFeedSnapshot snapshot)
            => JsonSerializer.Serialize(snapshot, JsonOptions);

        private static void MirrorToDisk(CombatSceneLiveFeedSnapshot snapshot)
        {
            try
            {
                string directory = CombatSceneLogWriter.GetDefaultDirectory();
                Directory.CreateDirectory(directory);

                string envelopePath = Path.Combine(directory, LiveFileName);
                string latestPath = Path.Combine(directory, LiveLatestFileName);

                string envelopeJson;
                string latestJson;
                lock (Gate)
                {
                    envelopeJson = _envelopeJson;
                    latestJson = _latestJson;
                }

                WriteAtomic(envelopePath, envelopeJson);
                WriteAtomic(latestPath, latestJson);
            }
            catch
            {
                // Disk mirror is best-effort; HTTP feed still works.
            }
        }

        private static void WriteAtomic(string path, string json)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, json, Encoding.UTF8);
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        private static void Pulse(CombatSceneLiveFeedSnapshot snapshot)
        {
            ChangedPulse.Set();
            try
            {
                Changed?.Invoke(snapshot);
            }
            catch
            {
                // Subscribers must not break combat.
            }
        }

        private sealed class FightBinding
        {
            public FightBinding(
                Character hero,
                Enemy enemy,
                Environment? room,
                Dungeon? dungeon,
                Func<BattleNarrative?> narrativeProvider,
                Func<Action?> lastPlayerActionProvider,
                string fightId)
            {
                Hero = hero;
                Enemy = enemy;
                Room = room;
                Dungeon = dungeon;
                NarrativeProvider = narrativeProvider;
                LastPlayerActionProvider = lastPlayerActionProvider;
                FightId = fightId;
            }

            public Character Hero { get; }
            public Enemy Enemy { get; }
            public Environment? Room { get; }
            public Dungeon? Dungeon { get; }
            public Func<BattleNarrative?> NarrativeProvider { get; }
            public Func<Action?> LastPlayerActionProvider { get; }
            public string FightId { get; }
        }
    }
}
