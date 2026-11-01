using Avalonia.Media;
using RPGGame;
using RPGGame.Combat.Sequence;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// The board's turn timeline under the enemy strip: one lane each for the hero, the enemy and a hostile room, every
    /// card on one schedule clock and as wide as its turn runs. NOW sits at the divider: each lane's last two played turns
    /// run up to it (what has happened shows only behind NOW, its cooldown past NOW left blank; a turn playing on NOW is an empty box), and its turns ahead follow
    /// as they would go if every roll landed its combo action, so the earliest card ahead (lit) is whoever acts next. The
    /// scale is fitted each paint and every card slides to its new place and size, so they keep their true proportions as
    /// the clock advances. The projection is rebuilt each paint from the live schedule, so a miss (combo reset), a speed
    /// change or a long action moves cards along their lane. A change an act makes to the cards ahead is held until that
    /// act has played out and flown up, then plays as its own beat (the changed cards lit) before the next turn comes down.
    /// </summary>
    public partial class DungeonRenderer
    {
        private const int TimelinePast = 2;
        /// <summary>Turns projected per paint; the lanes show those that start before the axis runs out.</summary>
        private const int TimelineAhead = 16;
        private const int TimelineGap = 1;
        private const int TimelineLabelW = 6;
        /// <summary>The scale never zooms in past this many of the slowest fighter's plain (length 1.0) swings across the
        /// axis ahead of NOW.</summary>
        private const double TimelineMinHorizonSwings = 2.0;
        /// <summary>Turns ahead per lane the scale always fits (more show while they fit whole).</summary>
        private const int TimelineFitAhead = 2;
        /// <summary>Pause after an act settles before the row ahead it changed reshuffles.</summary>
        private const int TimelineShuffleDelayMs = 500;

        /// <summary>A played act: which of its actor's turns (0-based, this fight) it was, the turn it played in, when that
        /// turn began on the schedule clock, and when it finished playing out on NOW (null while it still plays).</summary>
        private sealed class TimelineAct
        {
            public TimelineAct(BoardAct act, int ordinal, long serial, double start)
            {
                Act = act;
                Ordinal = ordinal;
                Serial = serial;
                Start = start;
                PushedUtc = DateTimeOffset.UtcNow;
            }

            public BoardAct Act { get; }
            public int Ordinal { get; }
            public long Serial { get; }
            public double Start { get; }
            public DateTimeOffset PushedUtc { get; }
            public DateTimeOffset? CompletedUtc { get; set; }
        }

        private static readonly List<TimelineAct> _timelineHistory = new();
        private static readonly Dictionary<Actor, int> _timelineTurnsPlayed = new(ReferenceEqualityComparer.Instance);
        /// <summary>Combo step each actor stood on before its turn began; ComboStep moves on before the swing plays out.</summary>
        private static readonly Dictionary<Actor, int> _timelineRestStep = new(ReferenceEqualityComparer.Instance);
        private static readonly Dictionary<(int Actor, int Ordinal), (BoardRect From, BoardRect To, DateTimeOffset Start)> _timelineSlides = new();

        /// <summary>A card ahead, with the content it shows and when its turn starts and how long it runs.</summary>
        private readonly record struct TimelineAheadCard((int, int) Key, Actor Actor, string Title, string Detail, double Start, double Duration);

        /// <summary>The row ahead as last shown; held while the act that changed it still plays.</summary>
        private static List<TimelineAheadCard>? _timelineAheadShown;
        /// <summary>A held change waits for its act to settle; the serial of that act's turn (the next turn comes down only
        /// after the swap beat), when the swap beat ends, and the cards it changed.</summary>
        private static bool _timelineSwapPending;
        private static long _timelineSwapSerial = -1;
        private static DateTimeOffset _timelineSwapEndUtc;
        private static readonly HashSet<(int, int)> _timelineSwapChanged = new();

        /// <summary>The turn whose card came down into NOW (its callout done), and when.</summary>
        private static long _timelineEnteredSerial = -1;
        private static DateTimeOffset _timelineEnteredUtc;

        private static void ResetBoardTimeline()
        {
            _timelineHistory.Clear();
            _timelineTurnsPlayed.Clear();
            _timelineRestStep.Clear();
            _timelineSlides.Clear();
            _timelineEnteredSerial = -1;
            _timelineAheadShown = null;
            _timelineSwapPending = false;
            _timelineSwapSerial = -1;
            _timelineSwapChanged.Clear();
        }

        private static void RecordTimelineAct(BoardAct act)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var h in _timelineHistory)
                h.CompletedUtc ??= now; // a newer act took the stage
            _timelineTurnsPlayed.TryGetValue(act.Actor, out int ordinal);
            _timelineTurnsPlayed[act.Actor] = ordinal + 1;
            _timelineHistory.Add(new TimelineAct(act, ordinal, CombatTurnQueue.TurnSerial, CombatTurnQueue.TurnStart ?? 0.0));
            if (_timelineHistory.Count > 3 * (TimelinePast + 1) + 2)
                _timelineHistory.RemoveAt(0);
        }

        /// <summary>
        /// Per-paint turn state shared by NOW and the timeline: which acts have finished playing, the combo step each
        /// actor is held at, and whether the acting turn's card has come down into NOW (after its callout, and after the
        /// last act has flown back up).
        /// </summary>
        private static void UpdateBoardTimeline(BoardTurnView v, Character player, Enemy enemy)
        {
            var now = DateTimeOffset.UtcNow;
            long serial = CombatTurnQueue.TurnSerial;
            var acting = CombatTurnQueue.ActingEntity;

            // A swing is done once its sequence finishes; a hazard with no swing holds NOW for a few beats.
            foreach (var h in _timelineHistory)
            {
                if (h.CompletedUtc != null)
                    continue;
                if (h.Act.Swing is { } s)
                {
                    if (!ReferenceEquals(CombatSequenceHudState.ActiveSwing, s))
                        h.CompletedUtc = now;
                    else if (CombatSequenceHudState.IsFinished)
                        h.CompletedUtc = CombatSequenceHudState.FinishedUtc == default ? now : CombatSequenceHudState.FinishedUtc;
                }
                else if (h.Serial != serial || (now - h.PushedUtc).TotalMilliseconds >= CombatBoardAnimation.BeatMs * 3)
                    h.CompletedUtc = now;
            }

            v.HoldActor = acting != null && !v.Spent ? acting : null;
            foreach (var actor in new Actor[] { player, enemy })
            {
                if (!ReferenceEquals(actor, v.HoldActor) && actor is Character ch)
                    _timelineRestStep[actor] = ch.ComboStep;
            }

            // The acting turn enters once its callout is gone (a callout that starts late takes it back out).
            var call = CombatTurnAnnouncement.Current;
            bool callout = acting != null && call is { Defeated: false } c && ReferenceEquals(c.Actor, acting);
            if (acting != null && _timelineEnteredSerial != serial && !callout)
            {
                _timelineEnteredSerial = serial;
                _timelineEnteredUtc = now;
                foreach (var h in _timelineHistory)
                {
                    if (h.Serial != serial)
                        h.CompletedUtc ??= now;
                }
            }
            else if (callout && _timelineEnteredSerial == serial && call!.Value.StartedUtc > _timelineEnteredUtc)
                _timelineEnteredSerial = -1;
            bool entered = acting != null && _timelineEnteredSerial == serial;

            // The card comes down only after the last act has flown back up, and after the swap beat when that act
            // changed the row ahead.
            var downStart = _timelineEnteredUtc;
            var prior = _timelineHistory.FindLast(h => h.Serial != serial);
            if (prior?.CompletedUtc is { } done)
            {
                var clear = done.AddMilliseconds(prior.Serial == _timelineSwapSerial
                    ? CombatBoardAnimation.SlideMs * 2 + TimelineShuffleDelayMs
                    : CombatBoardAnimation.SlideMs);
                if (clear > downStart)
                    downStart = clear;
            }
            v.TurnInNow = entered && now >= downStart;
            v.DownT = v.TurnInNow ? CombatBoardAnimation.EaseOut(CombatBoardAnimation.Progress(downStart, CombatBoardAnimation.SlideMs)) : 0.0;
            v.DownLandedUtc = downStart.AddMilliseconds(CombatBoardAnimation.SlideMs);
            if (entered && v.DownT < 1.0)
                CombatBoardAnimation.RequestFrame();

            if (v.TurnInNow)
            {
                v.NowActor = acting;
                v.NowTurnAct = _timelineHistory.FindLast(h => h.Serial == serial && ReferenceEquals(h.Act.Actor, acting));
                v.NowProjected = ProjectTimelineTurn(acting!, 0, v).Action;
            }
            else
            {
                var last = _timelineHistory.Count > 0 ? _timelineHistory[^1] : null;
                if (last is { CompletedUtc: null } && !(acting != null && last.Serial == serial))
                    v.LingeringAct = last.Act;
            }
        }

        /// <summary>Lanes the timeline shows: the hero, the enemy, and a hostile room (dropped when the board can't spare it a lane).</summary>
        private static List<Actor> TimelineLanes(Character player, Enemy enemy, int availH)
        {
            var lanes = new List<Actor> { player, enemy };
            if (CombatTurnQueue.Room is { IsHostile: true } room && TimelineLaneH(availH, 3) > 0)
                lanes.Add(room);
            return lanes;
        }

        /// <summary>Rows each lane's cards take (0 when the board is too short to spare the band).</summary>
        private static int TimelineLaneH(int availH, int lanes)
        {
            int laneH = Math.Min(4, (availH - 18 - 2 - (lanes - 1)) / lanes);
            return laneH >= 3 ? laneH : 0;
        }

        /// <summary>Rows the timeline takes under the enemy strip: a caption row, the lanes one row apart, one clear row.</summary>
        private static int TimelineBandHeight(int availH, int lanes)
        {
            int laneH = TimelineLaneH(availH, lanes);
            return laneH > 0 ? 1 + lanes * laneH + (lanes - 1) + 1 : 0;
        }

        /// <summary>
        /// Columns the lanes share: the lane names, the clock behind NOW (from <paramref name="pastX"/>) and ahead of it,
        /// with NOW at <paramref name="anchorX"/> (NOW's left edge; a third of the way in without one). Null when either
        /// side would be too short to read.
        /// </summary>
        private static (int PastX, int AxisX, int AxisEnd)? TimelineColumns(int x, int w, int? anchorX)
        {
            int axisX = anchorX ?? x + w / 3;
            int axisEnd = x + w - 1;
            int pastX = x + 1 + TimelineLabelW;
            if (axisEnd - axisX < 24 || axisX - pastX < 8)
                return null;
            return (pastX, axisX, axisEnd);
        }

        /// <summary>One card in a lane: a played act, a turn ahead, or the open gap of a turn playing on NOW before its
        /// swing registers; it runs from <see cref="Start"/> to where the lane's next card starts.</summary>
        private readonly record struct TimelineBlock((int, int) Key, double Start, double End, TimelineAct? Act, TimelineAheadCard? Card);

        /// <summary>
        /// Paints the timeline across (<paramref name="x"/>, <paramref name="y"/>, <paramref name="w"/>): one lane per
        /// actor, every card on one clock and as wide as its turn runs, so cards keep their true sizes against each other.
        /// NOW sits at the divider: each lane's last two played turns run up to it (the one still recovering runs across
        /// it), and its turns ahead follow, so whichever lane's next card starts furthest left acts next. The scale is fitted
        /// each paint so those played turns and the next two ahead in each fighter's lane fit; when they no longer would,
        /// every card shrinks together (each slides to its new size). Call after the combo flow, so the divider lines up with
        /// NOW and cards fly over both.
        /// </summary>
        private void RenderBoardTimeline(Character player, Enemy enemy, BoardTurnView v, int x, int y, int w, int availH, ActionStripDamageLineMode mode)
        {
            var lanes = TimelineLanes(player, enemy, availH);
            int laneH = TimelineLaneH(availH, lanes.Count);
            BoardRect? nowR = CombatBoardLayout.NowRect is { } n ? new BoardRect(n.X, n.Y, n.W, n.H) : null;
            var cols = TimelineColumns(x, w, nowR?.X);
            if (laneH == 0 || cols is not { } c)
            {
                _timelineSlides.Clear();
                return;
            }
            var seen = new HashSet<(int, int)>();
            long serial = CombatTurnQueue.TurnSerial;
            var acting = CombatTurnQueue.ActingEntity;
            bool pastActing = v.TurnInNow || v.Spent;
            var now = DateTimeOffset.UtcNow;

            var ahead = ShowTimelineAhead(ProjectTimelineAhead(v, serial), v, serial);
            double t0 = CombatTurnQueue.TurnStart ?? (ahead.Count > 0 ? ahead.Min(a => a.Start) : 0.0);
            double unit = 0.01;
            foreach (var lane in lanes)
            {
                if (lane is Character ch && lane is not Environment)
                    unit = Math.Max(unit, ch.GetTotalAttackSpeed());
            }

            // --- Each lane's cards in clock order, each running until the lane's next one starts ---
            var byStart = ahead.OrderBy(a => a.Start).ToList();
            TimelineAheadCard? first = byStart.Count > 0 ? byStart[0] : null;
            var laneBlocks = new List<List<TimelineBlock>>(lanes.Count);
            foreach (var actor in lanes)
            {
                var starts = new List<(double Start, (int, int) Key, TimelineAct? Act, TimelineAheadCard? Card)>();
                var acts = new List<TimelineAct>(TimelinePast);
                for (int i = _timelineHistory.Count - 1; i >= 0 && acts.Count < TimelinePast; i--)
                {
                    if (ReferenceEquals(_timelineHistory[i].Act.Actor, actor))
                        acts.Insert(0, _timelineHistory[i]);
                }
                foreach (var h in acts)
                    starts.Add((h.Start, TimelineKey(actor, h.Ordinal), h, null));
                if (pastActing && ReferenceEquals(actor, acting) && (acts.Count == 0 || acts[^1].Serial != serial))
                {
                    _timelineTurnsPlayed.TryGetValue(actor, out int played);
                    starts.Add((t0, TimelineKey(actor, played), null, null));
                }
                double lastEnd = 0;
                foreach (var card in byStart)
                {
                    if (!ReferenceEquals(card.Actor, actor))
                        continue;
                    starts.Add((card.Start, card.Key, null, card));
                    lastEnd = card.Start + card.Duration;
                }
                starts.Sort((p, q) => p.Start.CompareTo(q.Start));
                var blocks = new List<TimelineBlock>(starts.Count);
                for (int i = 0; i < starts.Count; i++)
                {
                    var s0 = starts[i];
                    double end = i + 1 < starts.Count ? starts[i + 1].Start
                        : s0.Card != null ? lastEnd
                        : s0.Start + (actor is Character ch ? ch.GetTotalAttackSpeed() : unit);
                    if (end > s0.Start + 1e-6)
                        blocks.Add(new TimelineBlock(s0.Key, s0.Start, end, s0.Act, s0.Card));
                }
                laneBlocks.Add(blocks);
            }

            // --- The scale: the fighters' played cards fit behind NOW and their next turns ahead of it ---
            double pastSpan = 1e-6, futureSpan = 1e-6;
            for (int li = 0; li < lanes.Count; li++)
            {
                if (lanes[li] is Environment)
                    continue; // the room's slow clock would squash the fighters; its cards show where they fit
                int aheadCount = 0;
                foreach (var b in laneBlocks[li])
                {
                    if (b.Start < t0)
                        pastSpan = Math.Max(pastSpan, t0 - b.Start);
                    if (b.Card == null || aheadCount++ < TimelineFitAhead)
                        futureSpan = Math.Max(futureSpan, b.End - t0);
                }
            }
            int pastCols = c.AxisX - c.PastX, aheadCols = c.AxisEnd - c.AxisX;
            double scale = Math.Min(aheadCols / (TimelineMinHorizonSwings * unit), Math.Min(pastCols / pastSpan, aheadCols / futureSpan));
            int At(double t) => c.AxisX + (int)Math.Round((t - t0) * scale);

            double divX = c.AxisX - 0.5;
            int bandBottom = y + 1 + lanes.Count * (laneH + 1) - 1;
            canvas.AddLine(divX, y + 1, divX, bandBottom, BoardDimBorder, 1.5);

            // The newest played act keeps its outcome colors; older ones gray out.
            var newestDone = _timelineHistory.FindLast(h => h.CompletedUtc != null);
            bool swapBeat = now < _timelineSwapEndUtc;
            if (swapBeat)
                CombatBoardAnimation.RequestFrame();
            BoardRect? actingSpan = null;

            for (int li = 0; li < lanes.Count; li++)
            {
                var actor = lanes[li];
                int ly = y + 1 + li * (laneH + 1);
                canvas.AddText(x + 1, ly + (laneH - 1) / 2, TimelineLabel(actor, player), BoardActorColor(actor));
                bool actingHere = pastActing && ReferenceEquals(actor, acting);
                Color gapBorder = LerpColor(BoardActorColor(actor), BoardFadedBorder, 0.6);

                foreach (var b in laneBlocks[li])
                {
                    int bx = At(b.Start), right = At(b.End);
                    if (bx < c.PastX || bx >= c.AxisEnd - 2)
                        continue;
                    if (right > c.AxisEnd + 1)
                    {
                        if (b.Start >= t0)
                            break; // a turn ahead shows whole or not at all
                        right = c.AxisEnd + 1; // a turn already under way runs off the edge
                    }
                    int bw = right - bx - TimelineGap;
                    if (bw < 2)
                        continue;
                    var target = new BoardRect(bx, ly, bw, laneH);

                    if (b.Card is { } card)
                    {
                        var r = PlaceTimelineCard(card.Key, target, seen, null);
                        DrawTimelineAhead(r, card, player, lit: swapBeat && _timelineSwapChanged.Contains(card.Key), next: card == first);
                    }
                    else
                    {
                        // What has happened shows behind NOW, its card growing out of the divider as NOW passes; the
                        // cooldown it still runs past NOW is blank, as is a turn still playing on NOW.
                        var played = b.Act is { CompletedUtc: not null } a ? a : null;
                        int lw = Math.Min(right, c.AxisX) - bx - TimelineGap;
                        if (played != null && lw >= 2)
                        {
                            var left = target with { W = lw };
                            var r = PlaceTimelineCard(b.Key, left, seen, (left with { X = c.AxisX - TimelineGap, W = 0 }, now));
                            DrawTimelineAct(played.Act, r, grayed: !ReferenceEquals(played, newestDone));
                        }
                        int rx = Math.Max(bx, c.AxisX);
                        int rw = right - rx - TimelineGap;
                        if (played == null && actingHere && rw >= 2)
                            actingSpan = new BoardRect(rx, ly, rw, laneH); // where its card flies down into NOW from
                    }
                }
            }

            // --- The acting turn's card flying down from its lane into NOW ---
            if (v.TurnInNow && v.DownT < 1.0 && nowR is { } to && v.NowActor != null && actingSpan is { } fromSpan)
            {
                var r = BoardRect.Lerp(fromSpan, to, v.DownT);
                AddBoardBox(r.X, r.Y, r.W, r.H, BoardActorColor(v.NowActor), BoardCardBackground, borderThicknessPixels: 2);
                DrawBoardProjectedContent(v.NowActor, v.NowProjected, r, mode);
            }

            var stale = new List<(int, int)>();
            foreach (var key in _timelineSlides.Keys)
            {
                if (!seen.Contains(key))
                    stale.Add(key);
            }
            foreach (var key in stale)
                _timelineSlides.Remove(key);

            // What the cards ahead assume: every roll lands its combo action.
            const string caption = "IF ALL ROLLS LAND";
            if (c.AxisEnd - caption.Length > c.AxisX)
                canvas.AddText(c.AxisEnd - caption.Length, y, caption, AsciiArtAssets.Colors.Gray);
        }

        private static (int, int) TimelineKey(Actor actor, int ordinal) => (RuntimeHelpers.GetHashCode(actor), ordinal);

        /// <summary>Key of the empty box for the part of a turn still to run past NOW.</summary>
        private static (int, int) TimelineRestKey((int, int) key) => (key.Item1, ~key.Item2);

        /// <summary>The row ahead as the live schedule has it now.</summary>
        private static List<TimelineAheadCard> ProjectTimelineAhead(BoardTurnView v, long serial)
        {
            bool pastActing = v.TurnInNow || v.Spent;
            double actingLength = v.Spent ? v.ActingLength : v.NowProjected?.Length ?? 1.0;
            int Offset(Actor a) => pastActing && ReferenceEquals(a, v.HoldActor) ? 1 : 0;
            var upcoming = CombatTurnQueue.GetUpcoming(TimelineAhead, pastActing, actingLength,
                (a, k) => ProjectTimelineTurn(a, k + Offset(a), v).Action?.Length ?? 1.0);
            var counts = new Dictionary<Actor, int>(ReferenceEqualityComparer.Instance);
            var cards = new List<TimelineAheadCard>(TimelineAhead);
            for (int i = 0; i < upcoming.Count && i < TimelineAhead; i++)
            {
                var actor = upcoming[i].Actor;
                counts.TryGetValue(actor, out int k);
                counts[actor] = k + 1;
                int kk = k + Offset(actor);
                string title, detail;
                if (actor is Environment room)
                {
                    title = "ROOM ROLL";
                    detail = room.StrikeThreshold > 20 ? "won't act" : $"strikes {room.StrikeThreshold}+";
                }
                else
                {
                    var (action, slot, count) = ProjectTimelineTurn(actor, kk, v);
                    title = action?.Name ?? "ATTACK";
                    detail = action != null ? $"step {slot + 1}/{count}" : "";
                }
                cards.Add(new TimelineAheadCard(TimelineKey(actor, FirstUnplayedOrdinal(actor, v, serial) + kk), actor, title, detail,
                    upcoming[i].Start, upcoming[i].Duration));
            }
            return cards;
        }

        /// <summary>
        /// The row ahead to draw this paint. A change that comes in while an act plays (a miss resetting a combo, an
        /// action's length pushing a turn back) is held until that act has flown up into the timeline and a short pause
        /// (<see cref="TimelineShuffleDelayMs"/>) has passed, then swaps in as its own beat; other changes (the row moving on as a turn comes down) show at once.
        /// </summary>
        private static List<TimelineAheadCard> ShowTimelineAhead(List<TimelineAheadCard> live, BoardTurnView v, long serial)
        {
            var now = DateTimeOffset.UtcNow;
            var shown = _timelineAheadShown;
            if (shown == null || shown.SequenceEqual(live))
            {
                if (_timelineSwapPending)
                {
                    _timelineSwapPending = false; // the change came undone before it showed
                    _timelineSwapSerial = -1;
                }
                _timelineAheadShown = live;
                return live;
            }

            // The act that may have changed the row: the newest, unless it is the acting turn's and hasn't fired yet.
            var acting = CombatTurnQueue.ActingEntity;
            var trigger = _timelineHistory.Count > 0 ? _timelineHistory[^1] : null;
            if (trigger != null && acting != null && !v.Spent && trigger.Serial == serial)
                trigger = _timelineHistory.FindLast(h => h.Serial != serial);
            var settleAt = trigger?.CompletedUtc?.AddMilliseconds(CombatBoardAnimation.SlideMs + TimelineShuffleDelayMs);
            if (trigger != null && (settleAt == null || now < settleAt))
            {
                _timelineSwapPending = true;
                _timelineSwapSerial = trigger.Serial;
                CombatBoardAnimation.RequestFrame();
                return shown;
            }

            if (_timelineSwapPending)
            {
                _timelineSwapPending = false;
                _timelineSwapEndUtc = (settleAt ?? now).AddMilliseconds(CombatBoardAnimation.SlideMs);
                _timelineSwapChanged.Clear();
                for (int i = 0; i < live.Count; i++)
                {
                    if (i >= shown.Count || shown[i] != live[i])
                        _timelineSwapChanged.Add(live[i].Key);
                }
            }
            _timelineAheadShown = live;
            return live;
        }

        /// <summary>Ordinal of <paramref name="actor"/>'s next turn that hasn't fired (its pushed act counts until it fires).</summary>
        private static int FirstUnplayedOrdinal(Actor actor, BoardTurnView v, long serial)
        {
            _timelineTurnsPlayed.TryGetValue(actor, out int played);
            bool pushedUnfired = ReferenceEquals(actor, v.HoldActor) && _timelineHistory.Count > 0
                && _timelineHistory[^1].Serial == serial && ReferenceEquals(_timelineHistory[^1].Act.Actor, actor);
            return pushedUnfired ? played - 1 : played;
        }

        /// <summary>
        /// Where a card is drawn this paint: it slides toward <paramref name="target"/> whenever its slot changes. A card
        /// new to the row slides in from <paramref name="enterFrom"/> (from when given), else from the right.
        /// </summary>
        private static BoardRect PlaceTimelineCard((int, int) key, BoardRect target, HashSet<(int, int)> seen,
            (BoardRect From, DateTimeOffset Start)? enterFrom)
        {
            seen.Add(key);
            var now = DateTimeOffset.UtcNow;
            if (!_timelineSlides.TryGetValue(key, out var s))
            {
                s = enterFrom is { } e ? (e.From, target, e.Start) : (target with { X = target.X + target.W + 1 }, target, now);
                _timelineSlides[key] = s;
            }
            double raw = CombatBoardAnimation.Progress(s.Start, CombatBoardAnimation.SlideMs);
            var shown = BoardRect.Lerp(s.From, s.To, CombatBoardAnimation.EaseOut(raw));
            if (s.To != target)
            {
                _timelineSlides[key] = (shown, target, now);
                raw = 0.0;
            }
            if (raw < 1.0)
                CombatBoardAnimation.RequestFrame();
            return shown;
        }

        /// <summary>
        /// The combo action <paramref name="actor"/> would play on its <paramref name="k"/>-th unfired turn if every roll
        /// before it landed (each combo moves the strip on one slot). Null for the room, or an actor with an empty strip.
        /// </summary>
        private static (Action? Action, int Slot, int Count) ProjectTimelineTurn(Actor actor, int k, BoardTurnView v)
        {
            if (actor is Environment || actor is not Character ch)
                return (null, -1, 0);
            var actions = ch.GetComboActions();
            int count = actions.Count;
            if (count == 0)
                return (null, -1, 0);
            int step = ReferenceEquals(actor, v.HoldActor) && _timelineRestStep.TryGetValue(actor, out int held) ? held : ch.ComboStep;
            int slot = ((step + k) % count + count) % count;
            return (actions[slot], slot, count);
        }

        /// <summary>NOW's content for a turn whose roll hasn't landed: the strip card the actor is lined up on, or the room's roll.</summary>
        private void DrawBoardProjectedContent(Actor actor, Action? action, BoardRect r, ActionStripDamageLineMode mode)
        {
            if (actor is not Environment && action != null)
            {
                DrawBoardActContent(new BoardAct(actor, action, null), r, mode);
                return;
            }
            int tw = r.W - 2;
            canvas.AddText(r.X + 1, r.Y + 1, Truncate(actor is Environment ? "ROOM ROLL" : "ATTACK", tw), actor is Environment ? BoardHazardColor : AsciiArtAssets.Colors.White);
            if (actor is Environment room && r.H > 3)
                canvas.AddText(r.X + 1, r.Y + 2, Truncate(room.StrikeThreshold > 20 ? "won't act" : $"strikes {room.StrikeThreshold}+", tw), AsciiArtAssets.Colors.Gray);
        }

        /// <summary>A card ahead; <paramref name="lit"/> while the swap beat that changed it plays, <paramref name="next"/> on
        /// the earliest one (whoever acts next).</summary>
        private void DrawTimelineAhead(BoardRect r, TimelineAheadCard card, Character player, bool lit, bool next)
        {
            var actor = card.Actor;
            Color actorColor = BoardActorColor(actor);
            Color titleColor = actor is Environment ? BoardHazardColor
                : ReferenceEquals(actor, player) ? BoardComboBorder : AsciiArtAssets.Colors.White;
            Color border = lit || next ? actorColor : LerpColor(actorColor, BoardDimBorder, 0.7);
            DrawTimelineCard(r, border, lit ? 2 : 1,
                lit || next ? titleColor : LerpColor(titleColor, AsciiArtAssets.Colors.Gray, 0.25), card.Title, card.Detail);
        }

        /// <summary>A played turn's card: grayed once a newer act has played, in its outcome's colors while it is the newest.</summary>
        private void DrawTimelineAct(BoardAct act, BoardRect r, bool grayed)
        {
            var (title, color) = TimelineActTitle(act);
            if (grayed)
            {
                DrawTimelineCard(r, LerpColor(color, BoardFadedBorder, 0.6), 1, LerpColor(color, AsciiArtAssets.Colors.Gray, 0.6), title, TimelineActDetail(act));
                return;
            }
            DrawTimelineCard(r, color, 2, color, title, TimelineActDetail(act));
        }

        private void DrawTimelineCard(BoardRect r, Color border, int thickness, Color titleColor, string title, string detail)
        {
            AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);
            int tw = r.W - 2;
            if (tw <= 0)
                return;
            int bottom = (int)r.Bottom - 1;
            int ty = r.Y + 1;
            if (ty < bottom)
                canvas.AddText(r.X + 1, ty++, ClipTimelineText(title, tw), titleColor);
            if (detail.Length > 0 && ty < bottom)
                canvas.AddText(r.X + 1, ty, ClipTimelineText(detail, tw), AsciiArtAssets.Colors.Gray);
        }

        /// <summary><see cref="Truncate"/>, but hard-clipped on cards too narrow for an ellipsis.</summary>
        private static string ClipTimelineText(string text, int max) =>
            max <= 3 ? text.Substring(0, Math.Min(max, text.Length)) : Truncate(text, max);

        private static string TimelineLabel(Actor actor, Character player) => actor switch
        {
            Environment => "ROOM",
            _ when ReferenceEquals(actor, player) => "YOU",
            Enemy => "ENEMY",
            _ => actor.Name ?? "?"
        };

        /// <summary>A played turn's headline as the timeline shows it: the combo action it landed, or HIT / MISS / QUIET.</summary>
        private static (string Title, Color Color) TimelineActTitle(BoardAct act)
        {
            if (act.Quiet)
                return ("QUIET", AsciiArtAssets.Colors.Gray);
            if (act.Actor is Environment)
                return (act.Action.Name ?? "?", BoardHazardColor);
            return act.Swing switch
            {
                { Outcome: CombatSequenceSwingOutcome.Miss } s => (s.IsCriticalMiss ? "CRIT MISS" : "MISS", AsciiArtAssets.Colors.Red),
                { Outcome: CombatSequenceSwingOutcome.Hit } s => (s.IsCritical ? "CRIT HIT" : "HIT", AsciiArtAssets.Colors.White),
                _ => (act.Action.Name ?? "ATTACK", BoardComboBorder)
            };
        }

        private static string TimelineActDetail(BoardAct act) => act switch
        {
            { Quiet: true } => $"rolled {act.QuietRoll}",
            { Swing: { } s } when s.Source is not Environment => s.Outcome == CombatSequenceSwingOutcome.Combo ? $"rolled {s.Roll}" : "combo reset",
            _ => ""
        };
    }
}
