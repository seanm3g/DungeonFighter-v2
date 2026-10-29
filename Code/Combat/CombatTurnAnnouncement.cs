using System;
using System.Threading.Tasks;

namespace RPGGame
{
    /// <summary>
    /// Full-screen kill callout for on-screen fights: <see cref="AnnounceDefeatAsync"/> raises it when the enemy falls
    /// and holds the loop while it plays; the canvas paints it from <see cref="Current"/>. The combat loop also calls
    /// <see cref="AnnounceAsync"/> as each turn begins, which only counts handovers (restarting with each fight,
    /// <see cref="CombatTurnQueue.Bind"/>); turns no longer get a callout.
    /// </summary>
    public static class CombatTurnAnnouncement
    {
        private static readonly object Gate = new();
        private static Actor? _lastActor;
        private static Actor? _showing;
        private static bool _showingDefeat;
        private static int _turnNumber;
        private static DateTimeOffset _startedUtc;
        private static int _durationMs;
        private static System.Action? _requestInvalidate;

        /// <summary>Pacing input for the kill callout, scaled by the combat speed setting: about 1.8s at 1x.</summary>
        public const int DefeatBaseDurationMs = 900;

        private const int MinDefeatDurationMs = 1600;

        /// <param name="Defeated">A kill callout: <paramref name="Actor"/> is the enemy that fell, not whose turn it is.</param>
        public readonly record struct Callout(Actor Actor, int TurnNumber, DateTimeOffset StartedUtc, int DurationMs, bool Defeated = false);

        /// <summary>The callout on screen, or null.</summary>
        public static Callout? Current
        {
            get
            {
                lock (Gate)
                    return _showing != null ? new Callout(_showing, _turnNumber, _startedUtc, _durationMs, _showingDefeat) : null;
            }
        }

        /// <summary>Called (off the UI thread) when a callout starts; the canvas posts its repaint.</summary>
        public static void SetRequestInvalidate(System.Action? invalidate) => _requestInvalidate = invalidate;

        public static void Reset()
        {
            lock (Gate)
            {
                _lastActor = null;
                _showing = null;
                _showingDefeat = false;
                _turnNumber = 0;
            }
        }

        /// <summary>
        /// Counts a handover when <paramref name="actor"/> takes over from someone else. The turn itself no longer gets a
        /// callout (the views show whose turn it is), so nothing holds the loop here.
        /// </summary>
        public static Task AnnounceAsync(Actor actor)
        {
            if (!CombatTurnQueue.IsBound || CombatManager.DisableCombatUIOutput || UIManager.GetCustomUIManager() == null)
                return Task.CompletedTask;
            lock (Gate)
            {
                if (!ReferenceEquals(actor, _lastActor))
                {
                    _lastActor = actor;
                    _turnNumber++;
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raises the kill callout over the board when <paramref name="enemy"/> falls, and waits it out, so the fight
        /// ends on it before the screen goes back to the log.
        /// </summary>
        public static async Task AnnounceDefeatAsync(Actor enemy)
        {
            if (!CombatTurnQueue.IsBound || CombatManager.DisableCombatUIOutput || UIManager.GetCustomUIManager() == null)
                return;
            int ms = DeveloperModeState.ScaleDelayMs(DefeatBaseDurationMs);
            if (ms <= 0 || ArenaHidesCallouts)
                return;
            await ShowAsync(enemy, Math.Max(MinDefeatDurationMs, ms), defeated: true);
        }

        /// <summary>The arena view skips the kill callout (and its hold); its plates already show the fall.</summary>
        private static bool ArenaHidesCallouts => RPGGame.UI.Avalonia.Layout.CombatBoardLayout.ShouldBeArena(GameState.Combat);

        private static async Task ShowAsync(Actor actor, int ms, bool defeated)
        {
            lock (Gate)
            {
                _showing = actor;
                _showingDefeat = defeated;
                _startedUtc = DateTimeOffset.UtcNow;
                _durationMs = ms;
            }
            _requestInvalidate?.Invoke();
            try
            {
                await Task.Delay(ms);
                await DeveloperModeState.WaitWhilePausedAsync();
            }
            finally
            {
                lock (Gate)
                {
                    if (ReferenceEquals(_showing, actor))
                        _showing = null;
                }
                _requestInvalidate?.Invoke();
            }
        }
    }
}
