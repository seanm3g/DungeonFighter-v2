using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.Services.Llm;
using RPGGame.Utils;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// Generates a post-dungeon-victory limerick via OpenAI when enabled, otherwise FlavorText fallback.
    /// Never throws to callers; returns null when nothing usable can be produced.
    /// Prefetch starts on the final room's killing blow; delivery happens on the dungeon completion screen.
    /// </summary>
    public sealed class VictoryLimerickService
    {
        public const string SystemPrompt =
            "You write one limerick (AABBA rhyme) summarizing a fantasy RPG fight. " +
            "Rules: Exactly 5 lines. No digit characters. Use the enemy name given. " +
            "Never print the hero's real first name, last name, or full name. " +
            "Refer to the hero only via a pun or metaphor inspired by their name " +
            "(you may use or riff on the suggested epithet). " +
            "Stay in-world; no meta talk about dice, UI, APIs, or being an AI. " +
            "You may weave in any gear, move, or loot names provided, but none are required. " +
            "Return ONLY the limerick, no title or commentary.";
        private readonly Func<OpenAiPreferences> _prefsFactory;
        private readonly Func<OpenAiPreferences, ILlmTextClient> _clientFactory;
        private readonly object _prefetchLock = new();
        private Task<string?>? _prefetchTask;
        private BattleLimerickPacket? _pendingPacket;
        private int _prefetchGeneration;
        private bool _finaleRoomPrefetchAllowed;

        public VictoryLimerickService(
            Func<OpenAiPreferences>? prefsFactory = null,
            Func<OpenAiPreferences, ILlmTextClient>? clientFactory = null)
        {
            _prefsFactory = prefsFactory ?? (() => GeneralSettingsStore.Load().OpenAiPreferences);
            _clientFactory = clientFactory ?? (prefs => new OpenAiChatClient(prefs));
        }

        /// <summary>Shared singleton for live play (safe: service is stateless aside from factories/prefetch).</summary>
        public static VictoryLimerickService Instance { get; } = new();

        /// <summary>
        /// When true, kill-resolution may start a limerick prefetch (final dungeon room only).
        /// Set by the dungeon room processor around encounters.
        /// </summary>
        public bool IsFinaleRoomPrefetchAllowed
        {
            get
            {
                lock (_prefetchLock)
                    return _finaleRoomPrefetchAllowed;
            }
        }

        /// <summary>Enables or disables kill-time prefetch for the current room.</summary>
        public void SetFinaleRoomPrefetchAllowed(bool allowed)
        {
            lock (_prefetchLock)
                _finaleRoomPrefetchAllowed = allowed;
        }

        /// <summary>True when a prefetch task is outstanding for the current fight.</summary>
        public bool HasPrefetch
        {
            get
            {
                lock (_prefetchLock)
                    return _prefetchTask != null;
            }
        }

        /// <summary>True when a pending victory packet is available for dungeon-completion delivery.</summary>
        public bool HasPendingPacket
        {
            get
            {
                lock (_prefetchLock)
                    return _pendingPacket != null;
            }
        }

        public async Task<string?> GenerateAsync(BattleLimerickPacket packet, CancellationToken cancellationToken = default)
        {
            if (packet == null || !packet.HeroWon)
                return null;

            OpenAiPreferences prefs = _prefsFactory() ?? new OpenAiPreferences();
            prefs.ValidateAndFix();

            // Feature is opt-in. When enabled without a key (or after API failure), use FlavorText.
            if (!prefs.VictoryLimerickEnabled)
                return null;

            if (prefs.HasApiKey())
            {
                ILlmTextClient? client = null;
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(prefs.TimeoutSeconds, 3, 120)));

                    client = _clientFactory(prefs);
                    string? raw = await client.CompleteAsync(SystemPrompt, packet.ToPromptBlock(), cts.Token)
                        .ConfigureAwait(false);

                    if (VictoryLimerickValidator.TryNormalize(raw, packet.HeroName, out string limerick))
                        return limerick;
                }
                catch (OperationCanceledException)
                {
                    // Timeout / cancel — fall through to FlavorText.
                }
                catch (Exception ex)
                {
                    // Optional feature: keep combat log clean; details go to scroll_debug.log.
                    ScrollDebugLogger.LogAlways(
                        $"VictoryLimerickService.GenerateAsync: OpenAI limerick failed ({ex.GetType().Name}: {ex.Message}); using FlavorText fallback");
                }
                finally
                {
                    (client as IDisposable)?.Dispose();
                }
            }

            return TryFlavorTextFallback(packet);
        }

        /// <summary>
        /// Starts generation immediately after the final room's killing blow (before display finishes).
        /// No-op when disabled, muted, finale-room latch is off, or a prefetch is already running.
        /// </summary>
        public void BeginPrefetch(BattleLimerickPacket packet)
        {
            if (packet == null || !packet.HeroWon)
                return;

            OpenAiPreferences prefs = _prefsFactory() ?? new OpenAiPreferences();
            prefs.ValidateAndFix();
            if (!prefs.VictoryLimerickEnabled)
                return;

            lock (_prefetchLock)
            {
                if (!_finaleRoomPrefetchAllowed)
                    return;
                if (_prefetchTask != null)
                    return;

                _prefetchGeneration++;
                _pendingPacket = packet;
                var captured = packet;
                _prefetchTask = Task.Run(async () =>
                {
                    try
                    {
                        return await GenerateAsync(captured).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.LogAlways(
                            $"VictoryLimerickService.BeginPrefetch: {ex.GetType().Name}: {ex.Message}");
                        return null;
                    }
                });
            }
        }

        /// <summary>
        /// Clears any in-flight prefetch and pending packet (new fight / early exit / cleanup).
        /// Outstanding work may still finish but is ignored.
        /// </summary>
        public void ClearPrefetch()
        {
            lock (_prefetchLock)
            {
                _prefetchGeneration++;
                _prefetchTask = null;
                _pendingPacket = null;
            }
        }

        /// <summary>
        /// Fire-and-forget: awaits a prefetched result when available, otherwise generates from
        /// <paramref name="packetFallback"/> (or the packet stored at prefetch), then invokes <paramref name="onReady"/>.
        /// </summary>
        /// <summary>
        /// Like <see cref="DeliverPrefetchOrRequest"/>, but when <paramref name="lootNames"/> has entries the verse is
        /// regenerated from the stored packet with the loot attached (the prefetch was made before loot dropped).
        /// </summary>
        public void DeliverWithLoot(IReadOnlyList<string>? lootNames, System.Action<string> onReady)
        {
            if (lootNames == null || lootNames.Count == 0)
            {
                DeliverPrefetchOrRequest(null, onReady);
                return;
            }
            BattleLimerickPacket? pending;
            lock (_prefetchLock)
                pending = _pendingPacket;
            if (pending == null)
            {
                DeliverPrefetchOrRequest(null, onReady);
                return;
            }
            DeliverPrefetchOrRequest(BattleLimerickPacketBuilder.WithLoot(pending, lootNames), onReady, preferFallback: true);
        }

        public void DeliverPrefetchOrRequest(
            BattleLimerickPacket? packetFallback,
            System.Action<string> onReady)
            => DeliverPrefetchOrRequest(packetFallback, onReady, preferFallback: false);

        private void DeliverPrefetchOrRequest(
            BattleLimerickPacket? packetFallback,
            System.Action<string> onReady,
            bool preferFallback)
        {
            if (onReady == null)
                return;

            Task<string?>? existing;
            BattleLimerickPacket? pending;
            lock (_prefetchLock)
            {
                existing = _prefetchTask;
                pending = _pendingPacket;
                _prefetchTask = null;
                _pendingPacket = null;
                _finaleRoomPrefetchAllowed = false;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    string? limerick = null;
                    if (preferFallback && packetFallback != null && packetFallback.HeroWon)
                        limerick = await GenerateAsync(packetFallback).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(limerick) && existing != null)
                        limerick = await existing.ConfigureAwait(false);

                    var fallback = packetFallback ?? pending;
                    if (string.IsNullOrWhiteSpace(limerick) && fallback != null && fallback.HeroWon)
                        limerick = await GenerateAsync(fallback).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(limerick))
                        return;
                    onReady(limerick);
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.LogAlways(
                        $"VictoryLimerickService.DeliverPrefetchOrRequest: {ex.GetType().Name}: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Fire-and-forget helper: generates then invokes <paramref name="onReady"/> with display lines.
        /// Prefer <see cref="DeliverPrefetchOrRequest"/> on the live victory path.
        /// </summary>
        public void RequestAndDeliver(
            BattleLimerickPacket packet,
            System.Action<string> onReady)
        {
            DeliverPrefetchOrRequest(packet, onReady);
        }

        public static string? TryFlavorTextFallback(BattleLimerickPacket packet)
        {
            try
            {
                var data = FlavorText.GetData();
                if (data?.CombatNarratives == null ||
                    !data.CombatNarratives.TryGetValue("victoryLimerick", out string[]? bank) ||
                    bank == null || bank.Length == 0)
                {
                    return null;
                }

                string template = FlavorText.GetRandomName(bank);
                if (string.IsNullOrWhiteSpace(template))
                    return null;

                string epithet = VictoryHeroEpithet.FromName(packet.HeroName);
                string filled = template
                    .Replace("{hero}", epithet, StringComparison.OrdinalIgnoreCase)
                    .Replace("{enemy}", packet.EnemyName, StringComparison.OrdinalIgnoreCase)
                    .Replace("{place}", packet.Location, StringComparison.OrdinalIgnoreCase)
                    .Replace("{name}", epithet, StringComparison.OrdinalIgnoreCase);

                // Templates may use " | " or "\n" as line breaks.
                filled = filled.Replace(" | ", System.Environment.NewLine, StringComparison.Ordinal);

                return VictoryLimerickValidator.TryNormalize(filled, packet.HeroName, out string limerick)
                    ? limerick
                    : null;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways(
                    $"VictoryLimerickService.TryFlavorTextFallback: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

    }
}
