using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Combat.Sequence;

namespace RPGGame
{
    /// <summary>Part of the combat screen a coach note points at (the popup outlines it and sits clear of it).</summary>
    public enum TutorialCoachFocus
    {
        None,
        EnemyStrip,
        Timeline,
        Board,
        Sequence,
        HeroStrip,
        StatusEffects,
        Thresholds,
        /// <summary>The lit turn orb beside the die (whose turn it is).</summary>
        TurnOrb,
        /// <summary>The d20 between NOW and the outcome boxes, with the result word above it and the math below.</summary>
        Die,
        /// <summary>The column of outcome boxes with their odds.</summary>
        Branches,
        /// <summary>The outcome box the roll landed on.</summary>
        ChosenBranch
    }

    /// <summary>
    /// One popup in the Training Ground tutorial. <see cref="Step"/> null shows it straight away (intro / outro);
    /// otherwise it waits for that column of the swing's sequence to reveal. Text takes tokens filled when shown:
    /// <c>{die1}</c> <c>{die2}</c> <c>{kept}</c> (the scripted faces), <c>{critmiss}</c> <c>{missmax}</c> <c>{hit}</c>
    /// <c>{hitmax}</c> <c>{combo}</c> <c>{crit}</c> (the bands in force) and <c>{action}</c>.
    /// </summary>
    public sealed class TutorialCoachNote
    {
        public TutorialCoachNote(string title, string text, TutorialCoachFocus focus = TutorialCoachFocus.None,
            CombatSequenceStepKind? step = null, bool requiresLucky = false)
        {
            Title = title ?? string.Empty;
            Text = text ?? string.Empty;
            Focus = focus;
            Step = step;
            RequiresLucky = requiresLucky;
        }

        public string Title { get; }
        public string Text { get; }
        public TutorialCoachFocus Focus { get; }
        public CombatSequenceStepKind? Step { get; }

        /// <summary>Only shown while the hero has the Leather <c>Lucky</c> status (the note talks about the second die).</summary>
        public bool RequiresLucky { get; }
    }

    /// <summary>
    /// Pauses Training Ground combat on popups that explain the board, each closed with Next (Enter / Space / click;
    /// Esc skips the rest). The turn handler arms a swing's notes; <see cref="CombatSequencePresenter"/> stops on each
    /// note's column as it reveals. Notes only wait while a canvas is attached and combat UI is live, so headless
    /// and muted fights never block.
    /// </summary>
    public static class TutorialCoach
    {
        public sealed record Popup(TutorialCoachNote Note, string Title, string Text, int Number);

        private static readonly object Gate = new();
        private static readonly List<TutorialCoachNote> _armed = new();
        private static IReadOnlyDictionary<string, string> _armedTokens = new Dictionary<string, string>();
        private static TaskCompletionSource<bool>? _advance;
        private static Popup? _current;
        private static bool _skipped;
        private static int _shownCount;
        private static System.Action? _requestInvalidate;

        /// <summary>The popup on screen, or null.</summary>
        public static Popup? Current => _current;

        public static bool IsWaiting => _current != null;

        /// <summary>Repaint callback (posted to the UI thread by the caller); null detaches the coach from the screen.</summary>
        public static void SetRequestInvalidate(System.Action? invalidate) => _requestInvalidate = invalidate;

        /// <summary>When true, notes pass straight through (tests).</summary>
        internal static bool SuppressForTests { get; set; }

        private static bool CanShow() =>
            !SuppressForTests
            && !_skipped
            && _requestInvalidate != null
            && !CombatManager.DisableCombatUIOutput
            && !CombatUiMuteScope.IsMuted;

        /// <summary>Starts a tutorial fight: popups numbered from 1, Esc-skip cleared.</summary>
        public static void BeginSession()
        {
            lock (Gate)
            {
                _skipped = false;
                _shownCount = 0;
                _armed.Clear();
            }
        }

        /// <summary>Closes the popup on screen (Next).</summary>
        public static bool Advance()
        {
            var tcs = _advance;
            return tcs != null && tcs.TrySetResult(true);
        }

        /// <summary>Closes the popup and lets the rest of the fight play without more (Esc).</summary>
        public static void SkipAll()
        {
            lock (Gate)
            {
                _skipped = true;
                _armed.Clear();
            }
            Advance();
        }

        /// <summary>Notes for the swing about to resolve, with its token values.</summary>
        public static void ArmSwing(IEnumerable<TutorialCoachNote>? notes, IReadOnlyDictionary<string, string> tokens)
        {
            lock (Gate)
            {
                _armed.Clear();
                if (notes != null)
                    _armed.AddRange(notes);
                _armedTokens = tokens;
            }
        }

        /// <summary>Drops whatever the swing did not show (its column never played, e.g. a miss has no DAMAGE).</summary>
        public static void DisarmSwing()
        {
            lock (Gate)
                _armed.Clear();
        }

        /// <summary>Shows the armed notes for <paramref name="step"/>'s column, one after another.</summary>
        public static async Task OnStepRevealedAsync(CombatSequenceStep step)
        {
            if (step == null)
                return;
            while (true)
            {
                TutorialCoachNote? note;
                IReadOnlyDictionary<string, string> tokens;
                lock (Gate)
                {
                    int i = _armed.FindIndex(n => n.Step == step.Kind);
                    if (i < 0)
                        return;
                    note = _armed[i];
                    _armed.RemoveAt(i);
                    tokens = _armedTokens;
                }
                await ShowAsync(note, tokens).ConfigureAwait(false);
            }
        }

        /// <summary>Shows <paramref name="notes"/> in order, each waiting for Next.</summary>
        public static async Task ShowAllAsync(IEnumerable<TutorialCoachNote>? notes, IReadOnlyDictionary<string, string> tokens,
            CancellationToken cancellationToken = default)
        {
            if (notes == null)
                return;
            foreach (var note in notes)
                await ShowAsync(note, tokens, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ShowAsync(TutorialCoachNote note, IReadOnlyDictionary<string, string> tokens,
            CancellationToken cancellationToken = default)
        {
            if (!CanShow())
                return;
            if (note.RequiresLucky && !LeatherSetBonus.HasLuckyStatus(CurrentHero))
                return;

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int number;
            lock (Gate)
                number = ++_shownCount;
            _advance = tcs;
            _current = new Popup(note, Fill(note.Title, tokens), Fill(note.Text, tokens), number);
            _requestInvalidate?.Invoke();
            try
            {
                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                    await tcs.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(_advance, tcs))
                {
                    _advance = null;
                    _current = null;
                }
                _requestInvalidate?.Invoke();
            }
        }

        /// <summary>The hero the session's Lucky check reads (set by the turn handler with the tokens).</summary>
        public static Character? CurrentHero { get; set; }

        internal static string Fill(string text, IReadOnlyDictionary<string, string> tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens.Count == 0)
                return text;
            foreach (var (key, value) in tokens)
                text = text.Replace("{" + key + "}", value, StringComparison.Ordinal);
            return text;
        }

        /// <summary>
        /// Token values for <paramref name="hero"/>'s bands right now (thresholds as the threshold manager has them) and
        /// the scripted faces of <paramref name="tutorialEvent"/> when given (kept with Lucky only on the hero's own rolls).
        /// </summary>
        public static Dictionary<string, string> BuildTokens(Character hero, TrainingGroundTutorialEvent? tutorialEvent = null,
            bool heroRolls = true)
        {
            var thresholds = RollModificationManager.GetThresholdManager();
            int critMiss = thresholds.GetCriticalMissThreshold(hero);
            int missMax = thresholds.GetHitThreshold(hero);
            int combo = thresholds.GetComboThreshold(hero);
            int crit = thresholds.GetCriticalHitThreshold(hero);
            var tokens = new Dictionary<string, string>
            {
                ["hero"] = hero.Name,
                ["action"] = GameConstants.TrainingGroundTutorialActionName,
                ["critmiss"] = critMiss.ToString(),
                ["missmax"] = missMax.ToString(),
                ["hit"] = (missMax + 1).ToString(),
                ["hitmax"] = (combo - 1).ToString(),
                ["combo"] = combo.ToString(),
                ["crit"] = crit.ToString()
            };
            if (tutorialEvent != null)
            {
                bool lucky = heroRolls && tutorialEvent.LuckRoll != null && LeatherSetBonus.HasLuckyStatus(hero);
                tokens["die1"] = tutorialEvent.Roll.ToString();
                tokens["die2"] = (tutorialEvent.LuckRoll ?? tutorialEvent.Roll).ToString();
                tokens["kept"] = (lucky ? Math.Max(tutorialEvent.Roll, tutorialEvent.LuckRoll!.Value) : tutorialEvent.Roll).ToString();
            }
            CurrentHero = hero;
            return tokens;
        }

        internal static void ResetForTests()
        {
            lock (Gate)
            {
                _armed.Clear();
                _skipped = false;
                _shownCount = 0;
            }
            var tcs = _advance;
            _advance = null;
            _current = null;
            tcs?.TrySetCanceled();
            SuppressForTests = false;
        }
    }
}
