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
    /// The board's turn timeline, a row of small cards under the enemy strip: the two turns played before the last
    /// (grayed, behind a divider), the current slot (lined up over NOW: empty while a turn plays, then holding its act once
    /// it flies back up), and the next turns as they would go if every roll landed its combo action. The projection is
    /// rebuilt each paint from the live schedule, so a miss (combo reset), a speed change or a reordered turn reshuffles
    /// it; each card slides from where it was to its new slot. A change an act makes to the row is held until that act has
    /// played out and flown up, then plays as its own beat (the changed cards lit) before the next turn comes down.
    /// </summary>
    public partial class DungeonRenderer
    {
        private const int TimelinePast = 2;
        private const int TimelineAhead = 4;
        private const int TimelineGap = 1, TimelineDividerGap = 3;
        /// <summary>Pause after an act settles before the row ahead it changed reshuffles.</summary>
        private const int TimelineShuffleDelayMs = 500;

        /// <summary>A played act: which of its actor's turns (0-based, this fight) it was, the turn it played in, and when
        /// it finished playing out on NOW (null while it still plays).</summary>
        private sealed class TimelineAct
        {
            public TimelineAct(BoardAct act, int ordinal, long serial)
            {
                Act = act;
                Ordinal = ordinal;
                Serial = serial;
                PushedUtc = DateTimeOffset.UtcNow;
            }

            public BoardAct Act { get; }
            public int Ordinal { get; }
            public long Serial { get; }
            public DateTimeOffset PushedUtc { get; }
            public DateTimeOffset? CompletedUtc { get; set; }
        }

        private static readonly List<TimelineAct> _timelineHistory = new();
        private static readonly Dictionary<Actor, int> _timelineTurnsPlayed = new(ReferenceEqualityComparer.Instance);
        /// <summary>Combo step each actor stood on before its turn began; ComboStep moves on before the swing plays out.</summary>
        private static readonly Dictionary<Actor, int> _timelineRestStep = new(ReferenceEqualityComparer.Instance);
        private static readonly Dictionary<(int Actor, int Ordinal), (BoardRect From, BoardRect To, DateTimeOffset Start)> _timelineSlides = new();

        /// <summary>A card in the row ahead, with the content it shows.</summary>
        private readonly record struct TimelineAheadCard((int, int) Key, Actor Actor, string Title, string Detail);

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
            _timelineHistory.Add(new TimelineAct(act, ordinal, CombatTurnQueue.TurnSerial));
            if (_timelineHistory.Count > TimelinePast + 4)
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

        /// <summary>Rows the timeline takes under the enemy strip (0 when the board is too short to spare them).</summary>
        private static int TimelineBandHeight(int availH)
        {
            int bandH = 1 + TimelineCardH(availH) + 1 + 1; // labels, cards (the current one a row taller), one clear row
            return availH - bandH >= 18 ? bandH : 0;
        }

        private static int TimelineCardH(int availH) => Math.Clamp(availH / 7, 3, 5);

        /// <summary>
        /// Slot rects left to right (past, current, ahead), with the current slot's left edge on <paramref name="anchorX"/>
        /// (NOW's) when given, else centered. Cards narrow until the row fits; null when it can't.
        /// </summary>
        private static BoardRect[]? TimelineSlots(int x, int y, int w, int cardH, int? anchorX)
        {
            for (int cw = 18; cw >= 8; cw--)
            {
                int nw = cw * 3 / 2;
                int leftNeed = TimelinePast * cw + (TimelinePast - 1) * TimelineGap + TimelineDividerGap;
                int rightNeed = nw + TimelineAhead * (TimelineGap + cw);
                int curX;
                if (anchorX is int ax)
                {
                    if (ax - leftNeed < x + 1 || ax + rightNeed > x + w - 1)
                        continue;
                    curX = ax;
                }
                else
                {
                    if (leftNeed + rightNeed > w - 2)
                        continue;
                    curX = x + (w - leftNeed - rightNeed) / 2 + leftNeed;
                }

                var slots = new BoardRect[TimelinePast + 1 + TimelineAhead];
                int px = curX - TimelineDividerGap;
                for (int i = TimelinePast - 1; i >= 0; i--)
                {
                    px -= cw;
                    slots[i] = new BoardRect(px, y, cw, cardH);
                    px -= TimelineGap;
                }
                slots[TimelinePast] = new BoardRect(curX, y, nw, cardH + 1);
                int ax2 = curX + nw;
                for (int i = 0; i < TimelineAhead; i++)
                {
                    ax2 += TimelineGap;
                    slots[TimelinePast + 1 + i] = new BoardRect(ax2, y, cw, cardH);
                    ax2 += cw;
                }
                return slots;
            }
            return null;
        }

        /// <summary>Paints the timeline across (<paramref name="x"/>, <paramref name="y"/>, <paramref name="w"/>). Call after
        /// the combo flow, so the current slot lines up with NOW and cards fly over both.</summary>
        private void RenderBoardTimeline(Character player, BoardTurnView v, int x, int y, int w, int availH, ActionStripDamageLineMode mode)
        {
            BoardRect? nowR = CombatBoardLayout.NowRect is { } n ? new BoardRect(n.X, n.Y, n.W, n.H) : null;
            var slots = TimelineSlots(x, y + 1, w, TimelineCardH(availH), nowR?.X);
            if (slots == null)
            {
                _timelineSlides.Clear();
                return;
            }
            var seen = new HashSet<(int, int)>();
            long serial = CombatTurnQueue.TurnSerial;
            var curSlot = slots[TimelinePast];

            // Divider between the grayed past and the current slot.
            double divX = curSlot.X - TimelineDividerGap / 2.0;
            canvas.AddLine(divX, y, divX, curSlot.Bottom, BoardDimBorder, 1.5);

            // --- Played turns: the newest sits in the current slot until the next turn comes down into NOW ---
            var done = _timelineHistory.FindAll(h => h.CompletedUtc != null);
            TimelineAct? current = null;
            if (done.Count > 0 && !(v.TurnInNow && done[^1].Serial != serial))
                current = done[^1];
            var past = new List<TimelineAct>(TimelinePast);
            for (int i = done.Count - 1; i >= 0 && past.Count < TimelinePast; i--)
            {
                if (!ReferenceEquals(done[i], current))
                    past.Insert(0, done[i]);
            }
            for (int i = 0; i < TimelinePast; i++)
            {
                int pi = i - (TimelinePast - past.Count);
                if (pi < 0)
                {
                    AddBoardBox(slots[i].X, slots[i].Y, slots[i].W, slots[i].H, BoardFadedBorder, BoardCardBackground);
                    continue;
                }
                var r = PlaceTimelineCard(TimelineKey(past[pi].Act.Actor, past[pi].Ordinal), slots[i], seen, null);
                DrawTimelineAct(past[pi].Act, r, player, grayed: true);
            }

            // --- Turns ahead (not yet in NOW) ---
            var ahead = ShowTimelineAhead(ProjectTimelineAhead(v, serial), v, serial);
            bool swapBeat = DateTimeOffset.UtcNow < _timelineSwapEndUtc;
            if (swapBeat)
                CombatBoardAnimation.RequestFrame();
            for (int i = 0; i < ahead.Count; i++)
            {
                var r = PlaceTimelineCard(ahead[i].Key, slots[TimelinePast + 1 + i], seen, null);
                DrawTimelineAhead(r, ahead[i], player, lit: swapBeat && _timelineSwapChanged.Contains(ahead[i].Key));
            }

            // --- Current slot: an open gap while a turn plays (NOW shows it); its act flies up from NOW once it has played out ---
            if (current != null)
            {
                (BoardRect, DateTimeOffset)? fromNow = nowR is { } nr ? (nr, current.CompletedUtc!.Value) : null;
                var r = PlaceTimelineCard(TimelineKey(current.Act.Actor, current.Ordinal), curSlot, seen, fromNow);
                DrawTimelineAct(current.Act, r, player, grayed: false);
            }

            // --- The acting turn's card flying down from the row into NOW ---
            if (v.TurnInNow && v.DownT < 1.0 && nowR is { } to && v.NowActor != null)
            {
                var r = BoardRect.Lerp(slots[TimelinePast + 1], to, v.DownT);
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

            // What the row ahead assumes: every roll lands its combo action.
            var last = slots[^1];
            const string caption = "IF ALL ROLLS LAND";
            if (last.Right + 2 + caption.Length <= x + w)
                canvas.AddText((int)last.Right + 2, last.Y + 1, caption, AsciiArtAssets.Colors.Gray);
        }

        private static (int, int) TimelineKey(Actor actor, int ordinal) => (RuntimeHelpers.GetHashCode(actor), ordinal);

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
                cards.Add(new TimelineAheadCard(TimelineKey(actor, FirstUnplayedOrdinal(actor, v, serial) + kk), actor, title, detail));
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

        /// <summary>A card ahead; <paramref name="lit"/> while the swap beat that changed it plays.</summary>
        private void DrawTimelineAhead(BoardRect r, TimelineAheadCard card, Character player, bool lit)
        {
            var actor = card.Actor;
            Color actorColor = BoardActorColor(actor);
            Color titleColor = actor is Environment ? BoardHazardColor
                : ReferenceEquals(actor, player) ? BoardComboBorder : AsciiArtAssets.Colors.White;
            Color border = lit ? actorColor : LerpColor(actorColor, BoardDimBorder, 0.7);
            Color label = LerpColor(actorColor, AsciiArtAssets.Colors.Gray, 0.3);
            DrawTimelineCard(r, TimelineLabel(actor, player), label, border, lit ? 2 : 1,
                lit ? titleColor : LerpColor(titleColor, AsciiArtAssets.Colors.Gray, 0.25), card.Title, card.Detail);
        }

        /// <summary>A played turn's card: grayed in the past, in its outcome's colors in the current slot.</summary>
        private void DrawTimelineAct(BoardAct act, BoardRect r, Character player, bool grayed)
        {
            var (title, color) = TimelineActTitle(act);
            Color actorColor = BoardActorColor(act.Actor);
            if (grayed)
            {
                DrawTimelineCard(r, TimelineLabel(act.Actor, player), LerpColor(actorColor, AsciiArtAssets.Colors.Gray, 0.6),
                    LerpColor(color, BoardFadedBorder, 0.6), 1, LerpColor(color, AsciiArtAssets.Colors.Gray, 0.6), title, TimelineActDetail(act));
                return;
            }
            DrawTimelineCard(r, TimelineLabel(act.Actor, player), actorColor, color, 2, color, title, TimelineActDetail(act));
        }

        private void DrawTimelineCard(BoardRect r, string label, Color labelColor, Color border, int thickness,
            Color titleColor, string title, string detail)
        {
            AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);
            DrawTimelineLabel(r, label, labelColor);
            int tw = r.W - 2;
            int bottom = (int)r.Bottom - 1;
            int ty = r.Y + 1;
            if (ty < bottom)
                canvas.AddText(r.X + 1, ty++, Truncate(title, tw), titleColor);
            if (detail.Length > 0 && ty < bottom)
                canvas.AddText(r.X + 1, ty, Truncate(detail, tw), AsciiArtAssets.Colors.Gray);
        }

        private void DrawTimelineLabel(BoardRect r, string label, Color color)
        {
            label = Truncate(label, r.W);
            canvas.AddText(r.X + Math.Max(0, (r.W - label.Length) / 2), r.Y - 1, label, color);
        }

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
