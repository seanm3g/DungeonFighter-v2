using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media;
using RPGGame.Combat.UI;
using RPGGame.Config.TextDelay;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Renderers.Text;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// F7 narrative combat log: plays each swing as growing prose in the combat log while the
    /// two-row HUD strip stays reserved. Each prose beat that maps to a HUD column (attacker, roll,
    /// outcome, action, defense, damage, effects) reveals that column when the sentence appears;
    /// narrative-only beats (crit / stance / tempo) leave the bar unchanged. Sentences are planned
    /// once per swing; the full open paragraph soft-wraps first, then types with a paragraph
    /// budget (~8000ms by default, re-allocated when the paragraph grows) subdivided equally
    /// sentence → word → character, plus additive sentence pauses. New attacker paragraphs start
    /// with a book-style first-line indent and a blank line from actor-change spacing.
    /// Same-attacker swings continue the open paragraph with a <c>sequenceFollowUp</c> bridge beat
    /// (skips the gather-will opener). Environment hazards play as their own indented paragraph:
    /// room action prose plus one sentence per status application (e.g. weaken on each victim).
    /// Enemy right-align is disabled so wrapped prose stays left-justified.
    /// </summary>
    public static class CombatSequenceFlavorPresenter
    {
        /// <summary>Book-style first-line indent for a new narrative paragraph.</summary>
        public const string NarrativeParagraphIndent = "    ";

        private static readonly NarrativeTextProvider NarrativeProvider = new NarrativeTextProvider();

        private static List<CombatSequenceStep>? _pendingSteps;
        private static CombatSequenceFlavorTokens? _pendingTokens;
        private static bool _playedThisBlock;

        /// <summary>Open prose paragraph for same-attacker continuation (ReplaceLast growth).</summary>
        private static List<ColoredText>? _openParagraph;
        private static string? _openParagraphAttacker;
        private static List<List<ColoredText>>? _openParagraphHoverInfo;

        /// <summary>Mechanical combat-log tip lines for the pending swing (set by BlockDisplayManager).</summary>
        private static List<List<ColoredText>>? _pendingHoverInfo;

        /// <summary>Characters typed in the current battle (drives typewriter ramp).</summary>
        private static int _narrativeCharsTyped;

        internal static List<CombatSequenceCue> CuesFiredForTests { get; } = new();
        internal static List<string> LinesWrittenForTests { get; } = new();
        internal static bool RecordForTests { get; set; }
        internal static bool BypassCanvasCheckForTests { get; set; }
        internal static bool SkipDelaysForTests { get; set; }
        internal static int NarrativeCharsTypedForTests => _narrativeCharsTyped;

        /// <summary>True after <see cref="PlayPendingAsync"/> wrote narrative lines for this action block.</summary>
        public static bool PlayedThisBlock => _playedThisBlock;

        public static bool HasPending =>
            _pendingSteps != null && _pendingSteps.Count > 0 && _pendingTokens != null;

        /// <summary>
        /// True when the F7 narrative log should play (live GUI, delays on, narrative mode).
        /// </summary>
        public static bool ShouldPlay()
        {
            if (!DeveloperModeState.IsNarrativeCombatLog)
                return false;
            if (CombatManager.DisableCombatUIOutput)
                return false;
            if (DeveloperModeState.IsCombatLogInstant)
                return false;
            if (!BypassCanvasCheckForTests && UIManager.GetCustomUIManager() == null)
                return false;
            return true;
        }

        public static void SetPending(
            List<CombatSequenceStep>? steps,
            CombatSequenceFlavorTokens? tokens,
            IReadOnlyList<(string EntityId, int Health)>? healthHolds = null)
        {
            _pendingSteps = steps;
            _pendingTokens = tokens;
            if (steps != null && steps.Count > 0 && tokens != null && ShouldPlay())
                HealthBarDisplayHold.SetFrom(healthHolds);
        }

        public static void ClearPending()
        {
            _pendingSteps = null;
            _pendingTokens = null;
            _pendingHoverInfo = null;
            _playedThisBlock = false;
        }

        /// <summary>
        /// Stores the mechanical combat-log block for the pending swing's prose hover tip.
        /// </summary>
        public static void SetPendingHoverInfo(List<List<ColoredText>>? infoLines)
        {
            _pendingHoverInfo = CombatLogProseHoverInfo.CloneLines(infoLines);
        }

        /// <summary>Drops open-paragraph continuation state (new battle / mode reset).</summary>
        public static void ClearOpenParagraph()
        {
            _openParagraph = null;
            _openParagraphAttacker = null;
            _openParagraphHoverInfo = null;
        }

        /// <summary>Resets typewriter ramp counter for a new battle.</summary>
        public static void ResetNarrativePacing()
        {
            _narrativeCharsTyped = 0;
        }

        /// <summary>
        /// Builds the final narrative paragraph from pending tokens without writing or delaying.
        /// Used when the mechanical log is showing so F7 can still swap to prose later.
        /// Consumes pending steps/tokens (not hover). Returns false when nothing was pending.
        /// </summary>
        public static bool TryBuildSilentParagraphFromPending(out List<ColoredText>? paragraph)
        {
            paragraph = null;
            var steps = _pendingSteps;
            var tokens = _pendingTokens;
            _pendingSteps = null;
            _pendingTokens = null;

            if (steps == null || steps.Count == 0 || tokens == null)
                return false;

            paragraph = BuildParagraphCore(steps, tokens, continueOpenParagraph: false);
            return paragraph != null && PlainLength(paragraph) > NarrativeParagraphIndent.Length;
        }

        /// <summary>One planned prose beat for a swing (templates resolved once before typing).</summary>
        private readonly struct PlannedNarrativeBeat
        {
            /// <summary>Null = same-attacker follow-up bridge (no HUD column reveal).</summary>
            public CombatSequenceStepKind? Kind { get; }
            public string Sentence { get; }

            public PlannedNarrativeBeat(CombatSequenceStepKind? kind, string sentence)
            {
                Kind = kind;
                Sentence = sentence;
            }
        }

        /// <summary>
        /// Assembles the full prose paragraph for the pending swing (no UI writes).
        /// </summary>
        private static List<ColoredText> BuildParagraphCore(
            List<CombatSequenceStep> steps,
            CombatSequenceFlavorTokens tokens,
            bool continueOpenParagraph)
        {
            RegisterNameColors(tokens);

            bool continuing = false;
            List<ColoredText> paragraph = new List<ColoredText>
            {
                new ColoredText(NarrativeParagraphIndent, Colors.White)
            };
            if (continueOpenParagraph && TrySeedOpenParagraph(tokens, out var seeded))
            {
                continuing = true;
                paragraph = seeded;
            }

            if (tokens.IsEnvironmental && continuing)
            {
                continuing = false;
                paragraph = new List<ColoredText>
                {
                    new ColoredText(NarrativeParagraphIndent, Colors.White)
                };
            }

            var beats = PlanNarrativeBeats(steps, tokens, continuing);
            foreach (var beat in beats)
                CombatSequenceNarrativeEmphasis.AppendSentence(paragraph, beat.Sentence, tokens);

            int wrapWidth = Math.Max(1, LayoutConstants.CenterPanelTextColumnWidth);
            return TextWrappingHelper.ApplySoftWraps(paragraph, wrapWidth);
        }

        /// <summary>
        /// Resolves all narrative sentences for the swing once so typewriter budgeting and playback
        /// share the same text (no second random draw mid-reveal).
        /// </summary>
        private static List<PlannedNarrativeBeat> PlanNarrativeBeats(
            List<CombatSequenceStep> steps,
            CombatSequenceFlavorTokens tokens,
            bool continuing)
        {
            var beats = new List<PlannedNarrativeBeat>();
            var kindsPresent = new HashSet<CombatSequenceStepKind>();
            foreach (var step in steps)
                kindsPresent.Add(step.Kind);

            var replacements = tokens.ToReplacements();

            if (continuing)
            {
                string followUpTemplate = NarrativeProvider.GetRandomNarrative("sequenceFollowUp");
                string followUpFilled = NarrativeProvider.ReplacePlaceholders(followUpTemplate, replacements);
                if (!string.IsNullOrWhiteSpace(followUpFilled))
                    beats.Add(new PlannedNarrativeBeat(null, NormalizeProseSentence(followUpFilled)));
            }

            foreach (var kind in CombatSequenceFlavorTokens.NarrativeBeatOrder)
            {
                if (continuing && kind == CombatSequenceStepKind.Attacker)
                    continue;

                if (!kindsPresent.Contains(kind) && !tokens.ShouldInclude(kind))
                    continue;
                if (!tokens.ShouldInclude(kind))
                    continue;

                if ((kind is CombatSequenceStepKind.Defense
                        or CombatSequenceStepKind.Damage
                        or CombatSequenceStepKind.Heal
                        or CombatSequenceStepKind.Effect)
                    && !kindsPresent.Contains(kind)
                    && !(tokens.IsEnvironmental && kind == CombatSequenceStepKind.Effect && tokens.IncludeEffects))
                    continue;

                if (kind == CombatSequenceStepKind.Effect
                    && tokens.IsEnvironmental
                    && tokens.EffectSentences.Count > 0)
                {
                    foreach (var effectSentence in tokens.EffectSentences)
                    {
                        string filledEffect = NormalizeProseSentence(effectSentence);
                        if (string.IsNullOrWhiteSpace(filledEffect))
                            continue;
                        beats.Add(new PlannedNarrativeBeat(kind, filledEffect));
                    }

                    continue;
                }

                string bankKey = tokens.ResolveBankKey(kind);
                string template = NarrativeProvider.GetRandomNarrative(bankKey);
                string filled = NarrativeProvider.ReplacePlaceholders(template, replacements);
                if (string.IsNullOrWhiteSpace(filled))
                    continue;

                beats.Add(new PlannedNarrativeBeat(kind, NormalizeProseSentence(filled)));
            }

            return beats;
        }

        public static async Task PlayPendingAsync(Character? character = null)
        {
            _playedThisBlock = false;
            var steps = _pendingSteps;
            var tokens = _pendingTokens;
            _pendingSteps = null;
            _pendingTokens = null;

            if (steps == null || steps.Count == 0 || tokens == null || !ShouldPlay())
            {
                HealthBarDisplayHold.ReleaseAll();
                return;
            }

            // Correlate HUD columns with prose: empty bar → reveal each kind as its sentence appears.
            CombatSequencePresenter.BeginNarrativeSyncedReveal(steps);

            RegisterNameColors(tokens);

            _playedThisBlock = true;
            var cueByKind = new Dictionary<CombatSequenceStepKind, CombatSequenceCue>();
            foreach (var step in steps)
            {
                if (step.Cue != CombatSequenceCue.None)
                    cueByKind[step.Kind] = step.Cue;
            }

            bool continuing = TrySeedOpenParagraph(tokens, out var paragraph);
            bool wroteAny = false;
            bool startedParagraph = continuing;

            // Environment hazards never continue a fighter paragraph — always a fresh indented block.
            if (tokens.IsEnvironmental && continuing)
            {
                continuing = false;
                startedParagraph = false;
                paragraph = new List<ColoredText>
                {
                    new ColoredText(NarrativeParagraphIndent, Colors.White)
                };
            }

            // Same-attacker bridge skips the gather-will line but still shows ATTACKER on the bar.
            if (continuing)
                CombatSequencePresenter.RevealNarrativeColumn(CombatSequenceStepKind.Attacker);

            var swingHover = _pendingHoverInfo;
            _pendingHoverInfo = null;
            List<List<ColoredText>>? boundHover = ResolveBoundHoverInfo(continuing, tokens.IsEnvironmental, swingHover);

            var beats = PlanNarrativeBeats(steps, tokens, continuing);
            if (beats.Count == 0)
            {
                CombatSequencePresenter.FinishNarrativeSyncedReveal();
                HealthBarDisplayHold.ReleaseAll();
                return;
            }

            // Seed snapshot (already typed when continuing) before appending new sentences.
            var seedParagraph = new List<ColoredText>(paragraph);
            int seedContentCount = CharacterRevealRhythmCalculator.CountContentChars(
                ColoredTextRenderer.RenderAsPlainText(seedParagraph));

            foreach (var beat in beats)
                CombatSequenceNarrativeEmphasis.AppendSentence(paragraph, beat.Sentence, tokens);

            int wrapWidth = Math.Max(1, LayoutConstants.CenterPanelTextColumnWidth);
            var finalWrapped = TextWrappingHelper.ApplySoftWraps(paragraph, wrapWidth);
            string plainWrapped = ColoredTextRenderer.RenderAsPlainText(finalWrapped);
            int[] schedule = CombatDelayManager.BuildNarrativeParagraphSchedule(plainWrapped);
            bool useBudget = schedule.Length == plainWrapped.Length && plainWrapped.Length > 0;

            int visibleLen = CharacterRevealRhythmCalculator.WrappedLengthForContentCount(
                plainWrapped, seedContentCount);

            // Sync already-visible open-paragraph text to the reflowed full wrap before typing more.
            if (startedParagraph && visibleLen > 0)
            {
                await PaintParagraphSliceAsync(
                    finalWrapped, visibleLen, character, alreadyStarted: true, boundHover, bindHover: true);
            }

            var progressive = new List<ColoredText>(seedParagraph);
            bool envEffectColumnRevealed = false;

            foreach (var beat in beats)
            {
                if (beat.Kind.HasValue)
                {
                    // Environment effect sentences share one HUD reveal for the column.
                    if (beat.Kind.Value == CombatSequenceStepKind.Effect
                        && tokens.IsEnvironmental
                        && tokens.EffectSentences.Count > 0)
                    {
                        if (!envEffectColumnRevealed)
                        {
                            CombatSequencePresenter.RevealNarrativeColumn(beat.Kind.Value);
                            envEffectColumnRevealed = true;
                        }
                    }
                    else
                    {
                        CombatSequencePresenter.RevealNarrativeColumn(beat.Kind.Value);
                    }
                }

                CombatSequenceNarrativeEmphasis.AppendSentence(progressive, beat.Sentence, tokens);
                int contentSoFar = CharacterRevealRhythmCalculator.CountContentChars(
                    ColoredTextRenderer.RenderAsPlainText(progressive));
                int targetLen = CharacterRevealRhythmCalculator.WrappedLengthForContentCount(
                    plainWrapped, contentSoFar);

                await WriteOrGrowParagraphRangeAsync(
                    finalWrapped,
                    plainWrapped,
                    schedule,
                    useBudget,
                    character,
                    startedParagraph,
                    boundHover,
                    visibleLen,
                    targetLen);
                visibleLen = targetLen;
                startedParagraph = true;
                wroteAny = true;

                if (RecordForTests)
                    LinesWrittenForTests.Add(ColoredTextRenderer.RenderAsPlainText(
                        ColoredTextRenderer.Truncate(finalWrapped, visibleLen)));

                if (beat.Kind.HasValue)
                {
                    var kind = beat.Kind.Value;
                    if (cueByKind.TryGetValue(kind, out var cue))
                        FireCue(cue);
                    else if (kind == CombatSequenceStepKind.Outcome)
                        FireCue(CombatSequenceCue.StripFlashAndSfx);
                    else if (kind is CombatSequenceStepKind.Damage or CombatSequenceStepKind.Heal)
                        FireCue(CombatSequenceCue.HealthBar);
                }

                if (!SkipDelaysForTests)
                    await CombatDelayManager.DelayAfterNarrativeSentenceAsync();
            }

            // Any HUD columns without a matching prose beat (e.g. env ATTACKER) finish with the swing.
            CombatSequencePresenter.FinishNarrativeSyncedReveal();

            if (wroteAny)
            {
                // Environment paragraphs do not continue into the next fighter swing.
                if (tokens.IsEnvironmental)
                {
                    ClearOpenParagraph();
                }
                else
                {
                    _openParagraph = new List<ColoredText>(paragraph);
                    _openParagraphAttacker = tokens.Attacker;
                    _openParagraphHoverInfo = CombatLogProseHoverInfo.CloneLines(boundHover);
                }
            }
            else
            {
                HealthBarDisplayHold.ReleaseAll();
            }
        }

        /// <summary>
        /// Continues the open paragraph when the same attacker swings again with no intervening
        /// non-combat block (DoT / environment would force a fresh paragraph).
        /// </summary>
        private static bool TrySeedOpenParagraph(CombatSequenceFlavorTokens tokens, out List<ColoredText> paragraph)
        {
            bool sameAttacker = _openParagraph != null
                && !string.IsNullOrEmpty(_openParagraphAttacker)
                && !string.IsNullOrEmpty(tokens.Attacker)
                && string.Equals(_openParagraphAttacker, tokens.Attacker, StringComparison.Ordinal);

            bool lastWasCombat =
                TextSpacingSystem.GetLastBlockType() == TextSpacingSystem.BlockType.CombatAction;

            if (sameAttacker && lastWasCombat && _openParagraph != null)
            {
                paragraph = new List<ColoredText>(_openParagraph);
                return true;
            }

            paragraph = new List<ColoredText>
            {
                new ColoredText(NarrativeParagraphIndent, Colors.White)
            };
            return false;
        }

        private static void RegisterNameColors(CombatSequenceFlavorTokens tokens)
        {
            // Register bare names only — leading "the" must stay uncolored in prose.
            if (!string.IsNullOrWhiteSpace(tokens.Attacker))
                RPGGame.UI.ColorSystem.KeywordColorSystem.RegisterCharacterName(
                    CombatSequenceFlavorTokens.StripLeadingArticle(tokens.Attacker),
                    ColorPalette.Gold.GetColor());
            if (!string.IsNullOrWhiteSpace(tokens.Target))
                RPGGame.UI.ColorSystem.KeywordColorSystem.RegisterCharacterName(
                    CombatSequenceFlavorTokens.StripLeadingArticle(tokens.Target),
                    ColorPalette.Enemy.GetColor());
            if (tokens.AdditionalNames != null)
            {
                foreach (var name in tokens.AdditionalNames)
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        RPGGame.UI.ColorSystem.KeywordColorSystem.RegisterCharacterName(
                            CombatSequenceFlavorTokens.StripLeadingArticle(name),
                            ColorPalette.Gold.GetColor());
                    }
                }
            }
        }

        private static string NormalizeProseSentence(string text)
        {
            string t = text.Trim();
            if (t.Length == 0)
                return t;

            // Soften combat-yell punctuation so sentences join as prose.
            if (t.EndsWith('!'))
                t = t.TrimEnd('!') + ".";
            if (!t.EndsWith('.') && !t.EndsWith('?') && !t.EndsWith('—'))
                t += ".";

            // Enemies are common nouns ("the Wraith"); capitalize when they open the sentence.
            return CombatSequenceFlavorTokens.CapitalizeLeadingArticle(t);
        }

        private static int PlainLength(List<ColoredText> segments) =>
            ColoredTextRenderer.RenderAsPlainText(segments).Length;

        private static List<List<ColoredText>>? ResolveBoundHoverInfo(
            bool continuing,
            bool isEnvironmental,
            List<List<ColoredText>>? swingHover)
        {
            if (isEnvironmental || !continuing || _openParagraphHoverInfo == null)
                return CombatLogProseHoverInfo.CloneLines(swingHover);

            if (swingHover == null || swingHover.Count == 0)
                return CombatLogProseHoverInfo.CloneLines(_openParagraphHoverInfo);

            return CombatLogProseHoverInfo.AppendBlocks(_openParagraphHoverInfo, swingHover);
        }

        private static async Task WriteOrGrowParagraphRangeAsync(
            List<ColoredText> finalWrapped,
            string plainWrapped,
            int[] schedule,
            bool useBudget,
            Character? character,
            bool alreadyStarted,
            List<List<ColoredText>>? hoverInfoLines,
            int previousWrappedLength,
            int targetWrappedLength)
        {
            int fullLength = PlainLength(finalWrapped);
            int from = Math.Max(0, previousWrappedLength);
            int to = Math.Clamp(targetWrappedLength, from, fullLength);
            bool bindHover = hoverInfoLines != null && hoverInfoLines.Count > 0;

            // Tests / no new glyphs: paint the target slice once (still count chars for ramp continuity).
            if (SkipDelaysForTests || from >= to)
            {
                int added = CountContentChars(finalWrapped, from, to);
                if (added > 0)
                    _narrativeCharsTyped += added;

                if (to > 0)
                {
                    await PaintParagraphSliceAsync(
                        finalWrapped,
                        to,
                        character,
                        alreadyStarted,
                        hoverInfoLines,
                        bindHover);
                }
                return;
            }

            bool lineStarted = alreadyStarted;
            for (int len = from + 1; len <= to; len++)
            {
                await PaintParagraphSliceAsync(
                    finalWrapped,
                    len,
                    character,
                    lineStarted,
                    hoverInfoLines,
                    bindHover && (!lineStarted || len == to));
                lineStarted = true;

                char justTyped = CharAtPlainIndex(finalWrapped, len - 1);
                bool isBreak = justTyped == '\n' || justTyped == '\r';
                if (!isBreak)
                {
                    int charsBeforeThis = _narrativeCharsTyped;
                    _narrativeCharsTyped++;

                    if (len < to)
                    {
                        if (useBudget && schedule.Length > len - 1)
                        {
                            await CombatDelayManager.DelayAfterNarrativeScheduledMsAsync(schedule[len - 1]);
                        }
                        else
                        {
                            var ctx = CharacterRevealRhythmCalculator.ResolveContext(plainWrapped, len - 1);
                            await CombatDelayManager.DelayAfterNarrativeCharAsync(charsBeforeThis, ctx);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Legacy entry used by tests that grow an unwrapped paragraph slice-by-slice.
        /// Soft-wraps the current paragraph and types only the new span.
        /// </summary>
        private static async Task WriteOrGrowParagraphAsync(
            List<ColoredText> paragraph,
            Character? character,
            bool alreadyStarted,
            List<List<ColoredText>>? hoverInfoLines,
            int previousLength)
        {
            int wrapWidth = Math.Max(1, LayoutConstants.CenterPanelTextColumnWidth);

            int previousWrappedLength = 0;
            if (previousLength > 0)
            {
                var previousSlice = ColoredTextRenderer.Truncate(paragraph, previousLength);
                previousWrappedLength = PlainLength(
                    TextWrappingHelper.ApplySoftWraps(previousSlice, wrapWidth));
            }

            var wrapped = TextWrappingHelper.ApplySoftWraps(paragraph, wrapWidth);
            string plainWrapped = ColoredTextRenderer.RenderAsPlainText(wrapped);
            int[] schedule = CombatDelayManager.BuildNarrativeParagraphSchedule(plainWrapped);
            bool useBudget = schedule.Length == plainWrapped.Length && plainWrapped.Length > 0;

            await WriteOrGrowParagraphRangeAsync(
                wrapped,
                plainWrapped,
                schedule,
                useBudget,
                character,
                alreadyStarted,
                hoverInfoLines,
                previousWrappedLength,
                PlainLength(wrapped));
        }

        /// <summary>Counts non-linebreak characters in the plain span [start, end).</summary>
        private static int CountContentChars(List<ColoredText> segments, int start, int end)
        {
            if (end <= start)
                return 0;
            string plain = ColoredTextRenderer.RenderAsPlainText(segments);
            if (start < 0)
                start = 0;
            if (end > plain.Length)
                end = plain.Length;
            int count = 0;
            for (int i = start; i < end; i++)
            {
                char c = plain[i];
                if (c != '\n' && c != '\r')
                    count++;
            }
            return count;
        }

        private static char CharAtPlainIndex(List<ColoredText> segments, int index)
        {
            if (index < 0)
                return '\0';
            int cursor = 0;
            foreach (var seg in segments)
            {
                string text = seg.Text ?? string.Empty;
                if (index < cursor + text.Length)
                    return text[index - cursor];
                cursor += text.Length;
            }
            return '\0';
        }

        private static async Task PaintParagraphSliceAsync(
            List<ColoredText> paragraph,
            int visibleLength,
            Character? character,
            bool alreadyStarted,
            List<List<ColoredText>>? hoverInfoLines,
            bool bindHover)
        {
            var snapshot = ColoredTextRenderer.Truncate(paragraph, visibleLength);
            if (alreadyStarted)
            {
                var canvas = UIManager.GetCustomUIManager() as CanvasUICoordinator;
                canvas?.ReplaceLastColoredSegments(
                    snapshot,
                    character,
                    UIMessageType.Combat,
                    hoverInfoLines,
                    setHoverInfoLines: bindHover);
                return;
            }

            var renderer = BlockRendererFactory.GetRenderer();
            await renderer.RenderMessageGroupsAsync(
                new List<(List<ColoredText> segments, UIMessageType messageType)>
                {
                    (snapshot, UIMessageType.Combat)
                },
                delayMs: 0,
                character);

            if (bindHover)
            {
                var coord = UIManager.GetCustomUIManager() as CanvasUICoordinator;
                coord?.SetLastLineHoverInfoLines(hoverInfoLines, character);
            }
        }

        private static void FireCue(CombatSequenceCue cue)
        {
            if (RecordForTests)
                CuesFiredForTests.Add(cue);

            switch (cue)
            {
                case CombatSequenceCue.StripFlashAndSfx:
                    PunchlineRevealFeedback.CommitQueued();
                    break;
                case CombatSequenceCue.HealthBar:
                    HealthBarDisplayHold.ReleaseAll();
                    break;
            }
        }

        public static void CancelPlaybackHolds()
        {
            HealthBarDisplayHold.ReleaseAll();
            ClearPending();
        }

        internal static void ResetForTests()
        {
            ClearPending();
            ClearOpenParagraph();
            ResetNarrativePacing();
            RecordForTests = false;
            BypassCanvasCheckForTests = false;
            SkipDelaysForTests = false;
            CuesFiredForTests.Clear();
            LinesWrittenForTests.Clear();
            HealthBarDisplayHold.ResetForTests();
        }
    }
}
