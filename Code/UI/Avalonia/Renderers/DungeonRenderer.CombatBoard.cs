using Avalonia.Media;
using RPGGame;
using RPGGame.Combat.Sequence;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Board-style combat center column (<see cref="CombatBoardLayout"/>): enemy combo strip on top, enemy health,
    /// PREV → NOW cards with the turn queue (hero, enemy, room in speed order) beside them, hero health, and (drawn separately by
    /// <see cref="RenderActionInfoStrip"/>) the hero strip along the bottom.
    /// </summary>
    public partial class DungeonRenderer
    {
        private static readonly Color BoardConnectorColor = Color.FromRgb(255, 96, 32);
        private static readonly Color BoardCardBackground = Color.FromRgb(10, 10, 10);
        private static readonly Color BoardDimBorder = Color.FromRgb(90, 90, 90);
        private static readonly Color BoardComboBorder = Color.FromRgb(255, 200, 40);

        private const int EnemyStripMinSlots = 4;

        /// <summary>Paints the whole board above the hero strip. Call only while <see cref="CombatBoardLayout.IsActive"/>.</summary>
        public void RenderCombatBoard(Character player, Enemy enemy, DisplayBuffer? log = null, IReadOnlyList<string>? enemyNamesForLogAlignment = null)
        {
            const ActionStripDamageLineMode mode = ActionStripDamageLineMode.EffectiveWithComboAmp;
            int colX = LayoutConstants.CENTER_PANEL_X;
            int colW = LayoutConstants.CENTER_PANEL_WIDTH;
            int boardH = LayoutConstants.ACTION_INFO_Y;
            if (colW < 20 || boardH < 16)
                return;

            canvas.ClearTextInArea(colX, 0, colW, boardH);
            canvas.ClearBoxesInArea(colX, 0, colW, boardH);
            canvas.ClearProgressBarsInArea(colX, 0, colW, boardH);
            canvas.ClearSegmentedBarsInArea(colX, 0, colW, boardH);
            canvas.ClearLinesInArea(colX, 0, colW, boardH);

            // --- Enemy strip (top) ---
            int enemyStripH = Math.Clamp(boardH / 4, 5, 10);
            var enemyCard = RenderBoardEnemyStrip(enemy, colX, 0, colW, enemyStripH, mode);
            var turn = BuildBoardTurnView(player, enemy);
            // The enemy's lined-up card reads NEXT when it acts after the turn now playing (or at rest).
            if (enemyCard is { } nextCard && nextCard.W > 9 && turn.Queue.Count > 0
                && ReferenceEquals(turn.Queue[0].Actor, enemy) && !turn.Queue[0].IsActing)
                canvas.AddText(nextCard.X + 1, nextCard.Y, " NEXT ", AsciiArtAssets.Colors.Gold);

            // --- Enemy name + health / d20 bar ---
            int barW = CombatBoardLayout.BarWidth(colW);
            int barX = colX + (colW - barW) / 2;
            int enemyNameY = enemyStripH + 1;
            canvas.AddText(barX, enemyNameY, enemy.Name, AsciiArtAssets.Colors.Red);
            int enemyBarY = enemyNameY + 1;
            int enemyHp = RPGGame.Combat.UI.HealthBarDisplayHold.Resolve($"enemy_{enemy.Name}", enemy.CurrentHealth);
            canvas.AddHealthBar(barX, enemyBarY, barW, enemyHp, enemy.MaxHealth,
                entityId: $"enemy_{enemy.Name}",
                heightScale: D20ThresholdBarRenderer.CombatHealthHeightScale,
                dividerFractions: enemy.IsPack ? enemy.PackDividerFractions : null);
            _boardBarX = barX;
            _boardBarW = barW;
            _enemyRollY = enemyBarY + D20ThresholdBarRenderer.CombatStripVerticalOffsetNoArmor + D20ThresholdBarRenderer.CombatBoardStripHeightScale;
            RenderBoardThresholdBar(enemy, ThresholdBarPanel.Enemy, barX, enemyBarY, barW, D20ThresholdBarRenderer.CombatStripVerticalOffsetNoArmor);
            canvas.AddText(barX + barW + 2, enemyBarY, $"{enemyHp}/{enemy.MaxHealth}", AsciiArtAssets.Colors.White);
            int enemyBlockBottom = enemyBarY + 2;

            // --- Hero d20 / health bar + name (bottom, mirrored) ---
            int heroNameY = boardH - 2;
            int heroBarY = heroNameY - 1;
            int heroD20Y = heroBarY - 1;
            int heroHp = RPGGame.Combat.UI.HealthBarDisplayHold.Resolve($"player_{player.Name}", player.CurrentHealth);
            int heroMax = player.GetEffectiveMaxHealth();
            _heroRollY = heroBarY - D20ThresholdBarRenderer.CombatBoardStripHeightScale;
            RenderBoardThresholdBar(player, ThresholdBarPanel.Hero, barX, heroD20Y - 1, barW, 2.0 - D20ThresholdBarRenderer.CombatBoardStripHeightScale);
            canvas.AddHealthBar(barX, heroBarY, barW, heroHp, heroMax,
                entityId: $"player_{player.Name}",
                heightScale: D20ThresholdBarRenderer.CombatHealthHeightScale);
            canvas.AddText(barX + barW + 2, heroBarY, $"{heroHp}/{heroMax}", AsciiArtAssets.Colors.White);
            string heroName = player.Name ?? "";
            canvas.AddText(barX + Math.Max(0, (barW - heroName.Length) / 2), heroNameY, heroName, AsciiArtAssets.Colors.Gold);

            // --- Sequence (left edge, between the strips) ---
            int seqW = RenderBoardSequencePanel(colX, enemyNameY, colW, barW, heroNameY + 1 - enemyNameY);
            int flowX = colX + seqW;
            int flowW = colW - seqW;

            // --- Combo flow (middle) ---
            int midTop = enemyBlockBottom + 1;
            int midBottom = heroD20Y - 1; // exclusive
            CombatBoardLayout.MiddleRect = (flowX, midTop, flowW, midBottom - midTop);
            if (CombatBoardLayout.ShowLog && log != null)
                RenderBoardLog(player, log, barX, midTop, barW, midBottom - midTop, enemyNamesForLogAlignment);
            else
                RenderBoardComboFlow(player, turn, flowX, midTop, flowW, midBottom - midTop, mode);
        }

        /// <summary>
        /// The sequence HUD's columns (ATTACKER .. EFFECTS) stacked in a framed panel down the left of the board, in the
        /// margin beside the health bars. Returns the columns it takes from the middle (0 when the column is too narrow).
        /// </summary>
        private int RenderBoardSequencePanel(int colX, int top, int colW, int barW, int availH)
        {
            int margin = (colW - barW) / 2;
            int w = Math.Min(Math.Clamp(colW * 17 / 100, 14, 24), margin - 1);
            if (w < 12 || availH < 9)
                return 0;
            int innerH = Math.Min(availH - 2, CombatSequenceHudRenderer.VerticalContentHeight(CombatSequenceHudRenderer.VerticalRowsPerEntry(availH - 2)));
            int h = innerH + 2;
            int y = top + (availH - h) / 2;
            AddBoardBox(colX, y, w, h, BoardDimBorder, BoardCardBackground);
            canvas.AddText(colX + 1, y, " SEQUENCE ", AsciiArtAssets.Colors.Cyan);
            CombatSequenceHudRenderer.RenderVertical(canvas, colX + 1, y + 1, w - 2, innerH);
            return w + 1;
        }

        /// <summary>Framed combat log between the health bars (click the middle to swap back to the combo cards).</summary>
        private void RenderBoardLog(Character player, DisplayBuffer log, int x, int y, int w, int h, IReadOnlyList<string>? enemyNamesForLogAlignment)
        {
            if (h < 4 || w < 10)
                return;
            CombatBoardLayout.IsPaintingBoardLog = true;
            try
            {
                new DisplayRenderer(textWriter).Render(log, x + 1, y + 1, w - 2, h - 2, clearContent: true,
                    combatEnemyNamesForPrimaryLineRightAlign: enemyNamesForLogAlignment,
                    combatHeroNameForLineAlignment: player.Name);
            }
            finally
            {
                CombatBoardLayout.IsPaintingBoardLog = false;
            }
            AddBoardBox(x, y, w, h, AsciiArtAssets.Colors.Cyan, BoardCardBackground);
            canvas.AddText(x + 2, y, " LOG ", AsciiArtAssets.Colors.Cyan);
        }

        private readonly record struct BoardRect(int X, int Y, int W, int H)
        {
            public double CenterX => X + W / 2.0;
            public double CenterY => Y + H / 2.0;
            public double Right => X + W;
            public double Bottom => Y + H;

            public static BoardRect Lerp(BoardRect a, BoardRect b, double t) =>
                new(CombatBoardAnimation.Lerp(a.X, b.X, t), CombatBoardAnimation.Lerp(a.Y, b.Y, t),
                    Math.Max(4, CombatBoardAnimation.Lerp(a.W, b.W, t)), Math.Max(3, CombatBoardAnimation.Lerp(a.H, b.H, t)));
        }

        /// <summary>Board boxes sit above the room backdrop so cards read as solid panels, not floating text.</summary>
        private void AddBoardBox(int x, int y, int w, int h, Color border, Color background, int borderThicknessPixels = 1) =>
            canvas.AddBox(x, y, w, h, border, background, borderThicknessPixels: borderThicknessPixels, aboveBackdrop: true);

        // d20 bar geometry from the last board paint: the roll's face on the bar is where the swing's line starts.
        private int _boardBarX, _boardBarW;
        private double _heroRollY, _enemyRollY;

        /// <summary>Point on a d20 bar under the rolled face (same mapping as the roll caret); hero bar top, enemy bar bottom.</summary>
        private (double X, double Y) RollPoint(int roll, bool hero) =>
            (_boardBarX + (Math.Clamp(roll, 1, 20) - 0.5) / 20.0 * _boardBarW, hero ? _heroRollY : _enemyRollY);

        /// <summary>Bands each bar showed before the swing now playing, so ATTACKER can ease them to the swing's bands.</summary>
        private readonly Dictionary<ThresholdBarPanel, CombatSequenceThresholds> _settledBands = new();

        /// <summary>
        /// d20 bar for one side, in swing order: ATTACKER eases the bands to the thresholds this swing rolls against, ROLL
        /// flickers the caret across the faces until it lands on the roll, and the caret stays for the rest of the swing.
        /// At rest (or on the other side's swing) the bar shows live thresholds and the usual roll feedback.
        /// </summary>
        private void RenderBoardThresholdBar(Actor actor, ThresholdBarPanel panel, int barX, int barY, int barW, double verticalOffsetScale)
        {
            var live = D20ThresholdBarRenderer.CurrentThresholds(actor);
            var swing = LiveSwingFor(actor);
            if (swing == null)
            {
                _settledBands[panel] = live;
                D20ThresholdBarRenderer.RenderBar(canvas, barX, barY, barW, actor, panel,
                    heightScale: D20ThresholdBarRenderer.CombatBoardStripHeightScale, verticalOffsetScale: verticalOffsetScale);
                return;
            }

            var (stage, p, raw) = LiveStage();
            var from = _settledBands.TryGetValue(panel, out var settled) ? settled : live;
            var to = swing.Thresholds ?? live;
            double t = stage == 0 ? p : 1.0;
            var bands = new CombatSequenceThresholds(
                CombatBoardAnimation.Lerp(from.Crit, to.Crit, t),
                CombatBoardAnimation.Lerp(from.Combo, to.Combo, t),
                CombatBoardAnimation.Lerp(from.MinHit, to.MinHit, t),
                CombatBoardAnimation.Lerp(from.CritMiss, to.CritMiss, t));

            int roll = Math.Clamp(swing.Roll, 1, 20);
            Func<int?> marker = () =>
            {
                if (stage < 1)
                    return null;
                // Land a little before the die reveals so the settled caret reads before OUTCOME draws from it.
                if (stage == 1)
                    return ThresholdBarFeedback.TumbleFace(roll, raw / RollLandsAt);
                return roll;
            };
            D20ThresholdBarRenderer.RenderBar(canvas, barX, barY, barW, actor, panel,
                heightScale: D20ThresholdBarRenderer.CombatBoardStripHeightScale,
                verticalOffsetScale: verticalOffsetScale, thresholds: bands, rollMarker: marker);
        }

        /// <summary>Fraction of the ROLL beat the caret tumbles before it lands on the roll.</summary>
        private const double RollLandsAt = 0.9;

        /// <summary>The line from the roll appears on OUTCOME, after the bands settle and the caret lands.</summary>
        private static bool RollKnown(int stage) => stage >= 2;

        /// <summary>OUTCOME splits in two: the roll line grows over the first half, the branch path over the second.</summary>
        private static double OutcomeHalf(int stage, double raw, bool second)
        {
            if (stage > 2)
                return 1.0;
            if (stage < 2)
                return 0.0;
            double t = second ? (raw - 0.5) * 2.0 : raw * 2.0;
            return CombatBoardAnimation.EaseOut(Math.Clamp(t, 0.0, 1.0));
        }

        private static readonly Color BoardFadedBorder = Color.FromRgb(38, 38, 38);
        private static readonly Color BoardIdleLine = Color.FromRgb(120, 52, 20);
        private static readonly Color BoardHealLine = Color.FromRgb(80, 220, 120);

        /// <summary>Board stage per sequence column: 0 attacker, 1 roll, 2 outcome, 3 action, 4 defense, 5 damage/heal, 6 effects.</summary>
        private static int BoardStageOf(CombatSequenceStepKind kind) => kind switch
        {
            CombatSequenceStepKind.Attacker => 0,
            CombatSequenceStepKind.Roll => 1,
            CombatSequenceStepKind.Outcome => 2,
            CombatSequenceStepKind.Action => 3,
            CombatSequenceStepKind.Defense => 4,
            CombatSequenceStepKind.Damage => 5,
            CombatSequenceStepKind.Heal => 5,
            _ => 6
        };

        /// <summary>Swing in progress for <paramref name="actor"/> (not yet finished), or null.</summary>
        private static CombatSequenceSwingInfo? LiveSwingFor(Actor actor)
        {
            var swing = CombatSequenceHudState.ActiveSwing;
            return swing != null && !CombatSequenceHudState.IsFinished && ReferenceEquals(swing.Source, actor) ? swing : null;
        }

        /// <summary>Stage, eased progress and raw progress of the live column (stage 0 at rest before an F7 reveal).</summary>
        private static (int Stage, double P, double Raw) LiveStage()
        {
            var kind = CombatSequenceHudState.ActiveKind;
            if (kind == null)
                return (0, 0.0, 0.0);
            // ROLL tumbles for several beats before the die lands; stretch its motion to match.
            int beats = kind == CombatSequenceStepKind.Roll ? CombatSequencePresenter.RollTumbleBeats : 1;
            double raw = CombatBoardAnimation.PhaseProgress(CombatBoardAnimation.BeatMs * beats);
            if (raw < 1.0)
                CombatBoardAnimation.RequestFrame();
            return (BoardStageOf(kind.Value), CombatBoardAnimation.EaseOut(raw), raw);
        }

        private static int FindSlot(List<Action> actions, Action? action)
        {
            if (action == null)
                return -1;
            int slot = actions.FindIndex(a => ReferenceEquals(a, action));
            if (slot < 0)
                slot = actions.FindIndex(a => string.Equals(a.Name, action.Name, StringComparison.Ordinal));
            return slot;
        }

        private static Color OutcomeColor(CombatSequenceSwingOutcome outcome) => outcome switch
        {
            CombatSequenceSwingOutcome.Combo => BoardComboBorder,
            CombatSequenceSwingOutcome.Hit => AsciiArtAssets.Colors.White,
            _ => AsciiArtAssets.Colors.Red
        };

        /// <summary>Border of a settled act still on NOW: its outcome, the room's color for a hazard, dim when empty.</summary>
        private static Color BoardRestBorder(BoardAct? act) => act switch
        {
            { Swing: { } s } => OutcomeColor(s.Outcome),
            { Actor: Environment } => BoardHazardColor,
            _ => BoardDimBorder
        };

        private static Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            return Color.FromRgb(
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t));
        }

        private static string Flatten(IReadOnlyList<ColoredText> segments)
        {
            var sb = new StringBuilder();
            foreach (var s in segments)
                sb.Append(s.Text);
            return sb.ToString().Trim();
        }

        /// <summary>Line from <paramref name="a"/> toward <paramref name="b"/>, drawn <paramref name="t"/> of the way.</summary>
        private void AddGrowingLine((double X, double Y) a, (double X, double Y) b, double t, Color color, double thickness = 1.5)
        {
            if (t <= 0.0)
                return;
            canvas.AddLine(a.X, a.Y, CombatBoardAnimation.Lerp(a.X, b.X, t), CombatBoardAnimation.Lerp(a.Y, b.Y, t), color, thickness);
        }

        /// <summary>
        /// <see cref="AddGrowingLine"/> with the stretches that cross <paramref name="under"/> left out. Lines draw above
        /// every box, so this is how a line reads as passing beneath the panels it crosses.
        /// </summary>
        private void AddGrowingLineUnder((double X, double Y) a, (double X, double Y) b, double t, Color color, double thickness,
            IEnumerable<BoardRect> under)
        {
            if (t <= 0.0)
                return;
            double dx = (b.X - a.X) * t, dy = (b.Y - a.Y) * t;
            var hidden = new List<(double From, double To)>();
            foreach (var r in under)
            {
                // Liang-Barsky: the part of the segment (in 0..1) that lies inside r.
                double lo = 0.0, hi = 1.0;
                if (!Clip(-dx, a.X - r.X, ref lo, ref hi) || !Clip(dx, r.Right - a.X, ref lo, ref hi)
                    || !Clip(-dy, a.Y - r.Y, ref lo, ref hi) || !Clip(dy, r.Bottom - a.Y, ref lo, ref hi))
                    continue;
                if (hi > lo)
                    hidden.Add((lo, hi));
            }
            hidden.Sort((p, q) => p.From.CompareTo(q.From));
            double at = 0.0;
            foreach (var (from, to) in hidden.Append((1.0, 1.0)))
            {
                if (from > at)
                    canvas.AddLine(a.X + dx * at, a.Y + dy * at, a.X + dx * from, a.Y + dy * from, color, thickness);
                at = Math.Max(at, to);
            }

            static bool Clip(double p, double q, ref double lo, ref double hi)
            {
                if (p == 0.0)
                    return q >= 0.0;
                double r = q / p;
                if (p < 0.0)
                {
                    if (r > hi) return false;
                    lo = Math.Max(lo, r);
                }
                else
                {
                    if (r < lo) return false;
                    hi = Math.Min(hi, r);
                }
                return true;
            }
        }

        /// <summary>Where the segment from <paramref name="a"/> to the center of <paramref name="r"/> crosses its border
        /// (<paramref name="a"/> itself when it is already inside).</summary>
        private static (double X, double Y) EntryPoint((double X, double Y) a, BoardRect r)
        {
            double cx = r.CenterX, cy = r.CenterY, t = 0.0;
            if (a.X < r.X) t = Math.Max(t, (r.X - a.X) / (cx - a.X));
            else if (a.X > r.Right) t = Math.Max(t, (a.X - r.Right) / (a.X - cx));
            if (a.Y < r.Y) t = Math.Max(t, (r.Y - a.Y) / (cy - a.Y));
            else if (a.Y > r.Bottom) t = Math.Max(t, (a.Y - r.Bottom) / (a.Y - cy));
            return (CombatBoardAnimation.Lerp(a.X, cx, t), CombatBoardAnimation.Lerp(a.Y, cy, t));
        }

        private BoardRect? RenderBoardEnemyStrip(Enemy enemy, int x, int y, int w, int h, ActionStripDamageLineMode mode)
        {
            var actions = enemy.GetComboActions();
            var panelData = CombatActionStripBuilder.BuildPanelData(enemy);
            int filled = Math.Min(actions.Count, panelData.Count);
            int count = Math.Max(filled, EnemyStripMinSlots);
            int selected = filled > 0 ? enemy.ComboStep % filled : -1;

            // While the enemy swings, its firing card stays selected (ComboStep has already moved on).
            var swing = LiveSwingFor(enemy);
            int stage = -1;
            double p = 1.0;
            if (swing != null)
            {
                int slot = FindSlot(actions, swing.Action);
                if (slot >= 0 && slot < filled)
                    selected = slot;
                (stage, p, _) = LiveStage();
            }

            const int inset = 1;
            int gap = 1;
            int avail = Math.Max(count, w - inset * 2 - gap * (count - 1));
            int cardW = avail / count;
            int cardH = Math.Max(3, h - 1);
            // Enemy sequences are short (most enemies carry one or two actions): cards keep the min-slot width and
            // sit centered, with no empty placeholder slots.
            int rowW = filled * cardW + Math.Max(0, filled - 1) * gap;
            int cx = x + Math.Max(inset, (w - rowW) / 2);
            BoardRect? selectedRect = null;
            for (int i = 0; i < filled; i++)
            {
                int cw = cardW;
                bool isSel = i == selected;
                Color border = isSel ? AsciiArtAssets.Colors.White : BoardDimBorder;
                int thickness = isSel ? 2 : 1;
                if (isSel && swing != null)
                {
                    // Attacker beat: the firing card flares red, then holds its outcome color.
                    border = stage == 0
                        ? LerpColor(AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.Red, Math.Sin(p * Math.PI))
                        : stage >= 2 ? OutcomeColor(swing.Outcome) : AsciiArtAssets.Colors.White;
                    thickness = 3;
                }
                Color fill = BoardCardBackground;
                if (isSel && swing != null)
                    (border, fill, thickness) = AccentForBeat(swing, stage, CombatBoardAnimation.PhaseProgress(CombatBoardAnimation.BeatMs), border, thickness);
                AddBoardBox(cx, y, cw, cardH, border, fill, borderThicknessPixels: thickness);
                DrawActionCardContent(enemy, panelData[i], actions[i], i, cx, y, cw, cardH, mode);
                if (isSel)
                    selectedRect = new BoardRect(cx, y, cw, cardH);
                cx += cw + gap;
            }
            return selectedRect;
        }

        private static readonly Color BoardHazardColor = Color.FromRgb(90, 210, 160);
        private const int BoardQueueLength = 1;

        /// <summary>One resolved action as the board shows it on NOW / PREV (a swing, or a room hazard with no swing).</summary>
        private sealed record BoardAct(Actor Actor, Action Action, CombatSequenceSwingInfo? Swing);

        /// <summary>The newest act and the two before it. Pushed once per swing / hazard turn; the newest only reaches NOW
        /// once it lands, so until then NOW still holds the previous act and PREV the one before that.</summary>
        private static BoardAct? _boardNowAct, _boardPrevAct, _boardPrev2Act;
        private static CombatSequenceSwingInfo? _boardRegisteredSwing;
        private static long _boardSwingTurn = -1, _boardHazardTurn = -1;
        private static Enemy? _boardEnemy;

        /// <summary>The branch box OUTCOME picked for the live swing; on ACTION its card slides from there into NOW.</summary>
        private static CombatSequenceSwingInfo? _boardLandSwing;
        private static BoardRect _boardLandFrom;

        /// <summary>The act whose landing started the NOW -> PREV shift, and when (the incoming card and the outgoing one move together).</summary>
        private static BoardAct? _boardShiftAct;
        private static DateTimeOffset _boardShiftStartUtc;

        /// <summary>Combo action each actor was lined up on at rest; a rolling entry keeps it after ComboStep moves on.</summary>
        private static readonly Dictionary<Actor, string> _boardRestComboName = new(ReferenceEqualityComparer.Instance);

        /// <summary>The board's reading of the turn in progress, built once per paint.</summary>
        private sealed class BoardTurnView
        {
            public CombatSequenceSwingInfo? LiveSwing;
            public int Stage = 7;
            public double P = 1.0, Raw = 1.0;
            public BoardAct? HazardAct;
            public List<CombatTurnQueueEntry> Queue = new();

            /// <summary>ACTION has fired, so the queue moves on to whoever acts after this turn.</summary>
            public bool Triggered => LiveSwing != null ? Stage >= 3 : HazardAct != null;
        }

        private static void PushBoardAct(BoardAct act)
        {
            _boardPrev2Act = _boardPrevAct;
            _boardPrevAct = _boardNowAct;
            _boardNowAct = act;
        }

        private static BoardTurnView BuildBoardTurnView(Character player, Enemy enemy)
        {
            var v = new BoardTurnView();
            var last = CombatSequenceHudState.ActiveSwing;
            bool finished = CombatSequenceHudState.IsFinished;
            if (!ReferenceEquals(_boardEnemy, enemy))
            {
                // New fight: drop the last fight's cards and don't replay its final swing.
                _boardEnemy = enemy;
                _boardNowAct = _boardPrevAct = _boardPrev2Act = null;
                _boardShiftAct = null;
                _boardRegisteredSwing = last;
            }

            v.LiveSwing = LiveSwingFor(player) ?? LiveSwingFor(enemy);
            if (v.LiveSwing != null)
                (v.Stage, v.P, v.Raw) = LiveStage();

            long turn = CombatTurnQueue.TurnSerial;
            var acting = CombatTurnQueue.ActingEntity;
            var swing = v.LiveSwing ?? (finished ? last : null);
            if (swing != null && !ReferenceEquals(swing, _boardRegisteredSwing))
            {
                _boardRegisteredSwing = swing;
                if (ReferenceEquals(swing.Source, acting))
                    _boardSwingTurn = turn;
                PushBoardAct(new BoardAct(swing.Source, swing.Action, swing));
            }

            if (v.LiveSwing == null && acting is Environment room && CombatTurnQueue.ActingAction is { } hazard)
            {
                if (_boardHazardTurn != turn)
                {
                    _boardHazardTurn = turn;
                    PushBoardAct(new BoardAct(room, hazard, null));
                }
                v.HazardAct = _boardNowAct;
            }

            // Turn handlers advance the actor's timeline only after the swing displays, so once ACTION fires
            // (or the swing has finished inside the same turn) the queue projects the actor past this turn.
            bool spent = acting != null && (v.Triggered || (v.LiveSwing == null && _boardSwingTurn == turn));
            double length = (v.LiveSwing?.Action ?? v.HazardAct?.Action ?? _boardNowAct?.Action)?.Length ?? 1.0;
            v.Queue = CombatTurnQueue.GetUpcoming(BoardQueueLength, spent, length);
            return v;
        }

        private static Color BoardActorColor(Actor actor) => actor switch
        {
            Environment => BoardHazardColor,
            Enemy => AsciiArtAssets.Colors.Red,
            _ => AsciiArtAssets.Colors.Gold
        };

        /// <summary>
        /// Middle of the board, read right to left: the next roll's outcome branches (one box each), the d20, NOW, PREV.
        /// Only the next actor is shown - nothing further ahead. A swing plays in sequence-column order:
        /// ATTACKER pulses the branches, ROLL tumbles the die, OUTCOME applies the thresholds (the branch taken lights up),
        /// ACTION flies its card into NOW while the act NOW held slides into PREV, and the branches move on to whoever acts
        /// after, then DEFENSE / DAMAGE / EFFECTS write the result under NOW. NOW keeps that act through the next swing's
        /// setup. Room hazards have no roll and land on NOW directly.
        /// </summary>
        private void RenderBoardComboFlow(Character player, BoardTurnView turn, int x, int y, int w, int h, ActionStripDamageLineMode mode)
        {
            if (h < 9)
                return;

            // --- Layout (rest positions) ---
            int curW = Math.Clamp(w * 26 / 100, 22, 40);
            int prevW = Math.Clamp(curW * 2 / 3, 16, 28);
            int nextW = Math.Clamp(w * 24 / 100, 20, 34);
            int lineGap = Math.Clamp(w * 9 / 100, 10, 16);
            int prevGap = 2;
            int total = prevW + prevGap + curW + lineGap + nextW;
            if (total > w - 2)
            {
                double s = (double)(w - 2) / total;
                prevW = Math.Max(10, (int)(prevW * s));
                curW = Math.Max(14, (int)(curW * s));
                nextW = Math.Max(12, (int)(nextW * s));
                lineGap = Math.Max(8, (int)(lineGap * s));
                total = prevW + prevGap + curW + lineGap + nextW;
            }

            int left = x + Math.Max(1, (w - total) / 2);
            int curH = Math.Clamp(h * 6 / 10, 8, Math.Max(8, h));
            int prevH = Math.Max(5, curH * 2 / 3);
            var curR = new BoardRect(left + prevW + prevGap, y + (h - curH) / 2, curW, curH);
            var prevR = new BoardRect(left, curR.Y + (curH - prevH) / 2, prevW, prevH);
            int nextX = curR.X + curW + lineGap;

            // The next actor's roll branches stack down the right column, one box per possible outcome.
            bool hasNext = turn.Queue.Count > 0;
            var nextEntry = hasNext ? turn.Queue[0] : default;
            bool nextRolling = hasNext && turn.LiveSwing != null && nextEntry.IsActing
                && ReferenceEquals(turn.LiveSwing.Source, nextEntry.Actor);
            var branches = hasNext ? BoardBranches(nextEntry.Actor, nextRolling ? turn.LiveSwing : null) : new List<BoardBranch>();
            const int branchGap = 1;
            int slots = branches.Count;
            var branchH = new int[slots];
            for (int i = 0; i < slots; i++)
                branchH[i] = 2 + Math.Max(1, branches[i].Detail.Count + 1);
            int stackH() { int s = Math.Max(0, slots - 1) * branchGap; foreach (var bh in branchH) s += bh; return s; }
            for (int i = slots - 1; i >= 0 && stackH() > h - 1; i--)
                branchH[i] = 3; // squeeze detail lines (hazard lists) before dropping boxes
            while (slots > 1 && stackH() > h - 1)
                slots--;
            int queueTop = y + 1 + Math.Max(0, (h - 1 - stackH()) / 2);
            var queueR = new BoardRect[slots];
            for (int i = 0, by = queueTop; i < slots; by += branchH[i] + branchGap, i++)
                queueR[i] = new BoardRect(nextX, by, nextW, branchH[i]);
            (double X, double Y) junction = (curR.Right + lineGap / 2.0, curR.CenterY);

            var swing = turn.LiveSwing;
            int stage = turn.Stage;
            double p = turn.P, rawP = turn.Raw;
            Color strikeColor = swing?.Source is Enemy ? AsciiArtAssets.Colors.Red : BoardConnectorColor;

            // The branch the roll picked waits on OUTCOME, then on ACTION its card leaves the column and slides into NOW
            // while the act NOW held slides into PREV - one move, both cards together.
            if (nextRolling && stage == 2)
            {
                int pick = TakenBranch(branches, swing!);
                if (pick >= 0 && pick < slots)
                {
                    _boardLandSwing = swing;
                    _boardLandFrom = queueR[pick];
                }
            }
            bool flies = swing != null && ReferenceEquals(_boardLandSwing, swing);

            // The newest act reaches NOW on ACTION (OUTCOME if it has no branch to fly from); a hazard or a finished
            // swing is already there. Until then NOW keeps the previous act - the setup never plays in the center.
            bool landed = swing == null || stage >= (flies ? 3 : 2);
            double shift = 1.0;
            if (landed && _boardNowAct != null)
            {
                if (!ReferenceEquals(_boardShiftAct, _boardNowAct))
                {
                    _boardShiftAct = _boardNowAct;
                    _boardShiftStartUtc = DateTimeOffset.UtcNow;
                }
                double raw = CombatBoardAnimation.Progress(_boardShiftStartUtc, CombatBoardAnimation.SlideMs);
                if (raw < 1.0)
                {
                    shift = CombatBoardAnimation.EaseOut(raw);
                    CombatBoardAnimation.RequestFrame();
                }
            }
            bool shifting = landed && shift < 1.0;
            bool cardInFlight = flies && shifting;
            var nowAct = landed ? _boardNowAct : _boardPrevAct;

            // --- NOW ---
            var nowR = curR;
            Color nowBorder = BoardRestBorder(nowAct);
            Color nowFill = BoardCardBackground;
            int nowThickness = 1;
            if (swing != null && landed)
            {
                nowBorder = LerpColor(AsciiArtAssets.Colors.White, OutcomeColor(swing.Outcome), stage > 2 ? 1.0 : CombatBoardAnimation.EaseOut(rawP));
                nowThickness = 3;
                (nowBorder, nowFill, nowThickness) = AccentForBeat(swing, stage, rawP, nowBorder, nowThickness);
            }
            else if (turn.HazardAct != null)
            {
                nowBorder = BoardHazardColor;
                nowThickness = 3;
            }
            if (cardInFlight)
                (nowBorder, nowThickness) = (BoardDimBorder, 1);
            AddBoardBox(nowR.X, nowR.Y, nowR.W, nowR.H, nowBorder, nowFill, borderThicknessPixels: nowThickness);
            if (nowAct != null && !cardInFlight)
            {
                canvas.AddText(nowR.X + 1, nowR.Y - 1, Truncate($"NOW  {nowAct.Actor.Name}", nowR.W), BoardActorColor(nowAct.Actor));
                DrawBoardActContent(nowAct, nowR, mode);
            }
            else
                canvas.AddText(nowR.X + 1, nowR.Y - 1, "NOW", AsciiArtAssets.Colors.White);

            // --- PREV (and, while the new act lands, the old NOW sliding into it) ---
            BoardAct? prevAct;
            if (shifting && _boardPrevAct != null)
            {
                if (shift < 0.5)
                    DrawBoardAct(_boardPrev2Act, prevR, LerpColor(BoardDimBorder, BoardFadedBorder, shift * 2), 1, mode);
                DrawBoardAct(_boardPrevAct, BoardRect.Lerp(curR, prevR, shift),
                    LerpColor(BoardRestBorder(_boardPrevAct), BoardDimBorder, shift), 1, mode);
                prevAct = shift < 0.5 ? _boardPrev2Act : _boardPrevAct; // the label follows whichever card holds the slot
            }
            else
            {
                prevAct = landed ? _boardPrevAct : _boardPrev2Act;
                if (prevAct == null && CombatSequenceHudState.Steps.Count == 0 && player.LastAction != null)
                    prevAct = new BoardAct(player, player.LastAction, null); // instant log: no swings to track
                DrawBoardAct(prevAct, prevR, BoardDimBorder, 1, mode);
            }
            if (prevAct != null)
                canvas.AddText(prevR.X + 1, prevR.Y - 1, Truncate($"PREV  {prevAct.Actor.Name}", prevR.W),
                    LerpColor(BoardActorColor(prevAct.Actor), AsciiArtAssets.Colors.Gray, 0.35));
            else
                canvas.AddText(prevR.X + 1, prevR.Y - 1, "PREV", AsciiArtAssets.Colors.Gray);

            // --- Die between NOW and the queue ---
            bool dieVisible = swing != null && stage >= 1 && !cardInFlight;
            BoardRect dieR = default;
            if (dieVisible)
                dieR = DrawBoardDie(junction, swing!, stage, rawP, x, w);

            // Roll line: the face on the actor's d20 bar -> the center of NOW (hero bar below, enemy bar above). Lines sit
            // above boxes, so it stops at NOW's border and skips the other panels it crosses (queue cards, their label,
            // PREV, the die) to read as running under them. Gone at rest.
            if (swing != null && RollKnown(stage))
            {
                var rollPoint = RollPoint(swing.Roll, hero: swing.Source is not Enemy);
                var under = new List<BoardRect>(queueR) { prevR, new BoardRect(prevR.X, prevR.Y - 1, prevR.W, 1) };
                if (slots > 0)
                    under.Add(new BoardRect(nextX, queueR[0].Y - 1, nextW, 1));
                if (dieVisible)
                    under.Add(dieR);
                AddGrowingLineUnder(rollPoint, EntryPoint(rollPoint, nowR), OutcomeHalf(stage, rawP, second: false), strikeColor, 2.0, under);
            }
            (double X, double Y) junctionIn = dieVisible ? (dieR.X, junction.Y) : junction;
            (double X, double Y) junctionOut = dieVisible ? (dieR.Right, junction.Y) : junction;
            canvas.AddLine(nowR.Right, nowR.CenterY, junctionIn.X, junctionIn.Y,
                dieVisible ? strikeColor : BoardIdleLine, dieVisible ? 2.0 : 1.5);

            // --- Next roll: one branch per outcome ---
            // The next actor's branches wait until the flying card has cleared the column (text draws above every box).
            if (hasNext && slots > 0 && !(cardInFlight && shift < 0.5))
            {
                var actor = nextEntry.Actor;
                string label = nextRolling ? "ROLLING" : nextEntry.IsActing ? "ACTING" : "NEXT";
                canvas.AddText(nextX + 1, queueR[0].Y - 1, Truncate($"{label}  {actor.Name}", nextW), BoardActorColor(actor));
                double fade = stage > 2 ? 1.0 : CombatBoardAnimation.EaseOut(rawP);
                int taken = nextRolling ? TakenBranch(branches, swing!) : -1;
                for (int i = 0; i < slots; i++)
                {
                    var b = branches[i];
                    bool resolved = nextRolling && stage >= 2 && b.Outcome != null;
                    bool chosen = resolved && i == taken;
                    canvas.AddLine(junctionOut.X, junctionOut.Y, queueR[i].X, queueR[i].CenterY,
                        resolved && !chosen ? BoardFadedBorder : BoardIdleLine, 1.0);
                    if (chosen)
                        AddGrowingLine(junctionOut, (queueR[i].X, queueR[i].CenterY), OutcomeHalf(stage, rawP, second: true), strikeColor, 2.0);
                    DrawBoardBranch(b, queueR[i], resolved, chosen, fade, nextRolling && stage == 0 ? p : 0.0, BoardActorColor(actor));
                }
            }

            if (cardInFlight)
                DrawBoardAct(_boardNowAct, BoardRect.Lerp(_boardLandFrom, nowR, shift), OutcomeColor(swing!.Outcome), 3, mode);
        }

        /// <summary>One possible result of the next roll: its odds (faces or percent), what it does, and any detail lines.</summary>
        private sealed record BoardBranch(string Odds, string Text, Color Color, CombatSequenceSwingOutcome? Outcome, List<string> Detail, bool Critical = false);

        /// <summary>
        /// One outcome box of the next roll. While that roll plays, OUTCOME lights the branch taken and fades the rest.
        /// </summary>
        private void DrawBoardBranch(BoardBranch b, BoardRect r, bool resolved, bool chosen, double fade, double attackerPulse, Color actorColor)
        {
            Color border = BoardDimBorder;
            int thickness = 1;
            Color color = b.Color;
            if (attackerPulse > 0.0)
            {
                border = LerpColor(BoardDimBorder, actorColor, Math.Sin(attackerPulse * Math.PI));
                thickness = 2;
            }
            if (resolved)
            {
                border = chosen ? (b.Critical ? b.Color : OutcomeColor(b.Outcome!.Value)) : LerpColor(BoardDimBorder, BoardFadedBorder, fade);
                thickness = chosen ? 3 : 1;
                if (!chosen)
                    color = LerpColor(color, BoardFadedBorder, fade * 0.85);
            }
            AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);

            int tx = r.X + 1, ty = r.Y + 1, tw = r.W - 2;
            int bottom = (int)r.Bottom - 1;
            string line = b.Odds.Length > 0 ? $"{b.Odds,-6}{b.Text}" : b.Text;
            canvas.AddText(tx, ty++, Truncate(line, tw), color);
            foreach (var d in b.Detail)
            {
                if (ty >= bottom)
                    break;
                canvas.AddText(tx, ty++, Truncate("  " + d, tw), color);
            }
        }

        /// <summary>
        /// What the next roll could become: COMBO (the strip action the actor is lined up on), HIT (a normal attack) or
        /// MISS, with the faces each needs - or, for the room, quiet vs. strikes (with the hazards it may throw).
        /// </summary>
        private static List<BoardBranch> BoardBranches(Actor actor, CombatSequenceSwingInfo? rolling)
        {
            var list = new List<BoardBranch>();
            if (actor is Environment room)
            {
                // The room rolls a d20 to act (strikes on a high face), then picks a hazard from its pool.
                int strikeAt = room.StrikeThreshold;
                if (strikeAt > 20)
                {
                    list.Add(new BoardBranch("", "won't act", AsciiArtAssets.Colors.Gray, null, new()));
                    return list;
                }
                var hazards = new List<string>();
                foreach (var (hazard, _) in room.ActionPool)
                    hazards.Add(hazard.Name ?? "?");
                string strikeFaces = strikeAt == 20 ? "20" : $"{strikeAt}-20";
                string quietFaces = strikeAt - 1 == 1 ? "1" : $"1-{strikeAt - 1}";
                list.Add(new BoardBranch(strikeFaces, "strikes:", BoardHazardColor, null, hazards));
                list.Add(new BoardBranch(quietFaces, "quiet", AsciiArtAssets.Colors.Gray, null, new()));
                return list;
            }

            var t = rolling?.Thresholds ?? D20ThresholdBarRenderer.CurrentThresholds(actor);
            string combo;
            if (rolling != null)
            {
                // ComboStep has already moved on mid-swing; keep the name it was lined up on.
                combo = rolling.Outcome == CombatSequenceSwingOutcome.Combo ? rolling.Action.Name
                    : _boardRestComboName.TryGetValue(actor, out var held) ? held : "-";
            }
            else
            {
                combo = "-";
                if (actor is Character ch)
                {
                    var actions = ch.GetComboActions();
                    if (actions.Count > 0)
                        combo = actions[ch.ComboStep % actions.Count].Name ?? "-";
                }
                _boardRestComboName[actor] = combo;
            }

            // Same exclusive bands as the d20 bar: crit, combo, hit, miss, crit miss. Crit and crit miss only show when
            // the actor can roll them.
            static string Faces(int lo, int hi) => lo > hi ? "--" : lo == hi ? $"{lo}" : $"{lo}-{hi}";
            int critAt = Math.Clamp(t.Crit, 1, 21);
            int comboAt = Math.Clamp(t.Combo, 1, critAt);
            int hitAt = Math.Clamp(t.MinHit, 1, comboAt);
            int critMissTop = Math.Clamp(t.CritMiss, 0, hitAt - 1);
            if (critAt <= 20)
                list.Add(new BoardBranch(Faces(critAt, 20), $"CRIT {combo}", ThresholdDisplayFormatting.D20ThresholdBarColors.Crit, CombatSequenceSwingOutcome.Combo, new(), Critical: true));
            list.Add(new BoardBranch(Faces(comboAt, critAt - 1), combo, BoardComboBorder, CombatSequenceSwingOutcome.Combo, new()));
            list.Add(new BoardBranch(Faces(hitAt, comboAt - 1), "hit", AsciiArtAssets.Colors.White, CombatSequenceSwingOutcome.Hit, new()));
            list.Add(new BoardBranch(Faces(critMissTop + 1, hitAt - 1), "miss", AsciiArtAssets.Colors.Red, CombatSequenceSwingOutcome.Miss, new()));
            if (critMissTop >= 1)
                list.Add(new BoardBranch(Faces(1, critMissTop), "CRIT MISS", ThresholdDisplayFormatting.D20ThresholdBarColors.CritMiss, CombatSequenceSwingOutcome.Miss, new(), Critical: true));
            return list;
        }

        /// <summary>
        /// The branch a swing took: a crit lights the crit box and a crit miss the crit-miss box; falls back to the plain
        /// outcome when that box isn't shown (e.g. a crit from a natural-face trigger outside the crit band).
        /// </summary>
        private static int TakenBranch(List<BoardBranch> branches, CombatSequenceSwingInfo swing)
        {
            bool miss = swing.Outcome == CombatSequenceSwingOutcome.Miss;
            bool critical = miss ? swing.IsCriticalMiss : swing.IsCritical;
            int exact = branches.FindIndex(b => b.Outcome != null && b.Critical == critical
                && (b.Outcome == swing.Outcome || (critical && !miss && b.Outcome != CombatSequenceSwingOutcome.Miss)));
            return exact >= 0 ? exact : branches.FindIndex(b => b.Outcome == swing.Outcome);
        }

        private void DrawBoardAct(BoardAct? act, BoardRect r, Color border, int thickness, ActionStripDamageLineMode mode)
        {
            AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);
            if (act != null)
                DrawBoardActContent(act, r, mode);
            else
                canvas.AddText(r.X + 1, r.Y + 1, "-", AsciiArtAssets.Colors.Gray);
        }

        /// <summary>A strip action gets its full card; a normal hit, a miss or a room hazard gets a short title card.</summary>
        private void DrawBoardActContent(BoardAct act, BoardRect r, ActionStripDamageLineMode mode)
        {
            bool miss = act.Swing?.Outcome == CombatSequenceSwingOutcome.Miss;
            if (!miss && act.Actor is Character ch && act.Action.IsComboAction)
            {
                var actions = ch.GetComboActions();
                int slot = FindSlot(actions, act.Action);
                var panelData = slot >= 0 ? CombatActionStripBuilder.BuildPanelData(ch) : null;
                if (panelData != null && slot < panelData.Count)
                {
                    DrawActionCardContent(ch, panelData[slot], actions[slot], slot, r.X, r.Y, r.W, r.H, mode);
                    return;
                }
            }

            string title, sub;
            Color color;
            if (act.Actor is Environment)
            {
                title = act.Action.Name ?? "?";
                sub = "room hazard";
                color = BoardHazardColor;
            }
            else if (miss)
            {
                title = act.Swing!.IsCriticalMiss ? "CRIT MISS" : "MISS";
                sub = "combo resets to 1";
                color = act.Swing.IsCriticalMiss ? ThresholdDisplayFormatting.D20ThresholdBarColors.CritMiss : AsciiArtAssets.Colors.Red;
            }
            else if (act.Swing?.Outcome == CombatSequenceSwingOutcome.Hit)
            {
                title = act.Swing.IsCritical ? "CRIT HIT" : "HIT";
                sub = "combo resets to 1";
                color = AsciiArtAssets.Colors.White;
            }
            else
            {
                title = string.IsNullOrEmpty(act.Action.Name) ? "NORMAL ATTACK" : act.Action.Name;
                sub = "";
                color = AsciiArtAssets.Colors.White;
            }
            int tw = r.W - 2;
            int bottom = (int)r.Bottom - 1;
            int ty = r.Y + 1;
            canvas.AddText(r.X + 1, ty++, Truncate(title, tw), color);
            if (sub.Length > 0 && ty < bottom)
                canvas.AddText(r.X + 1, ty++, Truncate(sub, tw), AsciiArtAssets.Colors.Gray);
            foreach (var line in CombatActionStripBuilder.BuildActionStripDescriptionLines(act.Action, tw, Math.Max(0, bottom - ty)))
                canvas.AddText(r.X + 1, ty++, line, AsciiArtAssets.Colors.Gray);
        }

        /// <summary>
        /// d20 at <paramref name="center"/>: flickers through faces in place until the ROLL column reveals, lands on the rolled
        /// value with a pop, then wears the outcome color. Math beats (luck dice, roll bonus) caption it underneath.
        /// </summary>
        private BoardRect DrawBoardDie((double X, double Y) center, CombatSequenceSwingInfo swing, int stage, double raw, int clampX, int clampW)
        {
            const int dieW = 6, dieH = 3;
            bool tumbling = stage == 1 && !CombatSequenceHudState.ResultRevealed;
            long tick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 55;
            int dx = Math.Clamp((int)Math.Round(center.X - dieW / 2.0), clampX, clampX + clampW - dieW);
            int dy = (int)Math.Round(center.Y - dieH / 2.0);
            var r = new BoardRect(dx, dy, dieW, dieH);

            string face;
            Color border;
            int thickness;
            if (tumbling)
            {
                face = (1 + (int)((tick * 7919 + 13) % 20)).ToString();
                border = tick % 2 == 0 ? AsciiArtAssets.Colors.White : AsciiArtAssets.Colors.Gray;
                thickness = 2;
                CombatBoardAnimation.RequestFrame();
            }
            else if (stage == 1)
            {
                // Revealed math beat: show the number this beat lands on (the kept die, then the total).
                string text = Flatten(CombatSequenceHudState.VisibleResult);
                face = PickNumber(text, first: text.Contains('/')) ?? swing.Roll.ToString();
                border = AsciiArtAssets.Colors.White;
                thickness = raw < 0.35 ? 4 : 2;
                if (text.Length > 0 && !string.Equals(text, face, StringComparison.Ordinal))
                    canvas.AddText(Math.Max(clampX, (int)Math.Round(center.X - text.Length / 2.0)), dy + dieH, text, AsciiArtAssets.Colors.Gray);
            }
            else
            {
                face = swing.Roll.ToString();
                border = OutcomeColor(swing.Outcome);
                thickness = stage == 2 && raw < 0.5 ? 4 : 2;
                if (stage == 2)
                {
                    // Outcome word stamps above the die.
                    string word = swing.Outcome switch
                    {
                        CombatSequenceSwingOutcome.Combo => "ACTION!",
                        CombatSequenceSwingOutcome.Hit => "HIT",
                        _ => "MISS"
                    };
                    if (swing.IsCritical)
                        word = "CRIT " + word;
                    int wx = Math.Max(clampX, (int)Math.Round(center.X - word.Length / 2.0));
                    canvas.AddText(wx, dy - 1, word, swing.IsCritical ? AsciiArtAssets.Colors.Gold : border);
                }
            }

            AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);
            canvas.AddText(r.X + Math.Max(1, (dieW - face.Length) / 2), r.Y + 1, face, tumbling ? AsciiArtAssets.Colors.Gray : AsciiArtAssets.Colors.White);
            return r;
        }

        private static string? PickNumber(string text, bool first)
        {
            var matches = Regex.Matches(text, @"\d+");
            if (matches.Count == 0)
                return null;
            return first ? matches[0].Value : matches[matches.Count - 1].Value;
        }

        /// <summary>
        /// Border / fill accent for the card that is acting: ACTION flashes it bright, a landed DAMAGE beat flashes it red
        /// (gold on a crit, green for heals), a miss dulls it. Decays over the beat; nothing moves.
        /// </summary>
        private static (Color Border, Color Fill, int Thickness) AccentForBeat(CombatSequenceSwingInfo swing, int stage, double raw, Color border, int thickness)
        {
            bool miss = swing.Outcome == CombatSequenceSwingOutcome.Miss;
            if (miss && stage >= 3)
                return (AsciiArtAssets.Colors.DarkRed, BoardCardBackground, thickness);

            Color flash;
            if (stage == 3)
                flash = AsciiArtAssets.Colors.White;
            else if (stage == 5)
                flash = swing.Action.Type == ActionType.Heal ? BoardHealLine
                    : swing.IsCritical ? AsciiArtAssets.Colors.Gold : AsciiArtAssets.Colors.Red;
            else
                return (border, BoardCardBackground, thickness);

            double pulse = 1.0 - Math.Clamp(raw, 0.0, 1.0);
            var tint = Color.FromRgb((byte)(flash.R / 5), (byte)(flash.G / 5), (byte)(flash.B / 5));
            return (LerpColor(border, flash, pulse), LerpColor(BoardCardBackground, tint, pulse), pulse > 0.4 ? 4 : thickness);
        }

        private static string Truncate(string text, int max) =>
            max <= 3 || text.Length <= max ? text : text.Substring(0, max - 3) + "...";
    }
}
