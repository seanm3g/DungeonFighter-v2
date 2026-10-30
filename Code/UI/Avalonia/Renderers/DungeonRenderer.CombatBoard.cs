using Avalonia.Media;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Combat.Sequence;
using RPGGame.Data;
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
            var enemyCards = new List<(int X, int Y, int W, int H)>();
            var enemyCard = RenderBoardEnemyStrip(enemy, colX, 0, colW, enemyStripH, mode, enemyCards);
            var turn = BuildBoardTurnView(player, enemy);
            // The enemy's lined-up card reads NEXT when it acts after the turn now playing (or at rest).
            if (enemyCard is { } nextCard && nextCard.W > 9 && turn.Queue.Count > 0
                && ReferenceEquals(turn.Queue[0].Actor, enemy) && !turn.Queue[0].IsActing)
                canvas.AddText(nextCard.X + 1, nextCard.Y, " NEXT ", AsciiArtAssets.Colors.Gold);

            var heroCard = HeroCardRect(player, LiveSwingFor(player));
            UpdateTurnSpotlight(player, enemy, turn, enemyCard, heroCard, (colX, 0, colW, enemyStripH), enemyCards);

            // Health and thresholds live in the side panels. The rest of the column, from under the enemy strip to the
            // hero strip, is the sequence panel and the combo flow, with one band row on each side for what lands there.
            int barW = CombatBoardLayout.BarWidth(colW);
            int barX = colX + (colW - barW) / 2;
            int top = enemyStripH + 1;
            int heroBandY = boardH - 1;

            // The turn timeline takes the full column under the enemy strip; it paints after the combo flow so its
            // current card can line up with NOW and cards can fly between the two.
            int timelineH = TimelineBandHeight(heroBandY - top);
            int enemyBandY = top + timelineH;

            // --- Sequence (left edge, between the timeline and the hero strip) ---
            int seqW = RenderBoardSequencePanel(colX, enemyBandY, colW, barW, boardH - enemyBandY);
            int flowX = colX + seqW;
            int flowW = colW - seqW;

            // --- Combo flow (middle) ---
            int midTop = enemyBandY + 2; // the cards' labels sit one row above them
            int midBottom = heroBandY;   // exclusive
            CombatBoardLayout.MiddleRect = (flowX, midTop, flowW, midBottom - midTop);
            CombatBoardLayout.NowRect = null;
            CombatBoardLayout.FlowCardsRect = null;
            _boardDieCenter = null;
            if (CombatBoardLayout.ShowLog && log != null)
                RenderBoardLog(player, log, barX, midTop, barW, midBottom - midTop, enemyNamesForLogAlignment);
            else
                RenderBoardComboFlow(player, enemy, turn, flowX, midTop, flowW, midBottom - midTop, enemyBandY, heroBandY, mode);
            if (timelineH > 0)
                RenderBoardTimeline(player, turn, colX, top, colW, heroBandY - top, mode);
            else
                _timelineSlides.Clear();

            // --- Turn orbs: enemy above the die, hero below it (log view: enemy top-right, hero bottom-left) ---
            var side = CombatTurnSpotlight.ActiveSide;
            int orbW = BoardOrbArt[0].Length, orbH = BoardOrbArt.Length;
            int enemyOrbX = colX + colW - 1 - orbW, enemyOrbY = enemyBandY;
            int heroOrbX = colX + 1, heroOrbY = heroBandY - 2;
            if (_boardDieCenter is { } die)
            {
                // Clear of the outcome word (row above the die) and the math caption (row below it).
                int dieTop = (int)Math.Round(die.Y - BoardDieH / 2.0);
                enemyOrbX = heroOrbX = Math.Clamp((int)Math.Round(die.X - orbW / 2.0), colX + 1, colX + colW - 1 - orbW);
                enemyOrbY = Math.Max(midTop - 1, dieTop - 2 - orbH);
                heroOrbY = Math.Min(midBottom - orbH, dieTop + BoardDieH + 2);
            }
            RenderBoardTurnOrb(heroOrbX, heroOrbY, AsciiArtAssets.Colors.Green, side == CombatTurnSpotlight.Side.Hero);
            RenderBoardTurnOrb(enemyOrbX, enemyOrbY, AsciiArtAssets.Colors.Red, side == CombatTurnSpotlight.Side.Enemy);
        }

        /// <summary>Where the die sits between NOW and the queue in the last combo-flow paint (null in log view).</summary>
        private static (double X, double Y)? _boardDieCenter;

        private static readonly string[] BoardOrbArt = { " .-. ", "(   )", " '-' " };
        private static readonly string[] BoardOrbLitArt = { " .-. ", "(@@@)", " '-' " };
        private static readonly Color BoardOrbDark = Color.FromRgb(70, 70, 70);

        /// <summary>A small ASCII orb at grid (<paramref name="x"/>, <paramref name="y"/>): filled and bright on its side's turn, a dark outline otherwise.</summary>
        private void RenderBoardTurnOrb(int x, int y, Color color, bool lit)
        {
            var art = lit ? BoardOrbLitArt : BoardOrbArt;
            for (int i = 0; i < art.Length; i++)
                canvas.AddText(x, y + i, art[i], lit ? color : BoardOrbDark);
        }

        /// <summary>Hero strip card each actor was lined up on at rest; a swing keeps spotlighting it once ComboStep moves on.</summary>
        private static readonly Dictionary<Actor, int> _boardRestSlot = new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// The hero's lined-up card in the bottom strip: the card that fired while the hero swings (the card it was lined up
        /// on when the roll misses; none on a plain HIT, which fires no strip card), the next combo step at rest.
        /// </summary>
        private static BoardRect? HeroCardRect(Character player, CombatSequenceSwingInfo? swing)
        {
            var actions = player.GetComboActions();
            int filled = actions.Count;
            if (filled == 0)
                return null;
            int slot = swing != null ? FindSlot(actions, swing.Action) : -1;
            if (swing == null)
                _boardRestSlot[player] = slot = player.ComboStep % filled;
            else if (slot < 0 || slot >= filled)
            {
                if (swing.Outcome != CombatSequenceSwingOutcome.Miss)
                    return null;
                slot = _boardRestSlot.TryGetValue(player, out int held) && held < filled ? held : player.ComboStep % filled;
            }
            ActionInfoStripLayout.GetPanelRect(slot, ActionInfoStripLayout.GetDisplayPanelCount(filled), out int px, out int py, out int pw, out int ph);
            return new BoardRect(px, py, pw, ph);
        }

        /// <summary>
        /// Lights the half of the screen whose turn it is: the swing's attacker, the room on a hazard turn, otherwise whoever
        /// acts next. The attacker's lined-up card glows faintly at rest and flares on the swing's ATTACKER result beat.
        /// </summary>
        private static void UpdateTurnSpotlight(Character player, Enemy enemy, BoardTurnView turn, BoardRect? enemyCard, BoardRect? heroCard,
            (int X, int Y, int W, int H) enemyStrip, IReadOnlyList<(int X, int Y, int W, int H)> enemyCards)
        {
            Actor? actor = TurnActor(turn);
            var side = actor switch
            {
                null => CombatTurnSpotlight.Side.None,
                Environment => CombatTurnSpotlight.Side.Room,
                Enemy => CombatTurnSpotlight.Side.Enemy,
                _ => CombatTurnSpotlight.Side.Hero
            };
            // Only the cards dim, so the room plate between them stays as bright as the rest of the board.
            int heroCount = ActionInfoStripLayout.GetDisplayPanelCount(player.GetComboActions().Count);
            var heroCards = new List<(int X, int Y, int W, int H)>(heroCount);
            for (int i = 0; i < heroCount; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, heroCount, out int px, out int py, out int pw, out int ph);
                heroCards.Add((px, py, pw, ph));
            }
            CombatTurnSpotlight.Set(side, enemyStrip, enemyCards, heroCards);

            BoardRect? card = side == CombatTurnSpotlight.Side.Hero ? heroCard : side == CombatTurnSpotlight.Side.Enemy ? enemyCard : null;
            double strength = 0.35;
            if (turn.LiveSwing != null)
                strength = turn.Stage >= 3 ? 0.0 : CardFocused(turn.Stage) ? 1.0 : 0.35;
            CombatTurnSpotlight.SetFocus(card is { } c ? (c.X, c.Y, c.W, c.H) : null, actor != null ? BoardActorColor(actor) : default, strength);
        }

        /// <summary>Whose turn it is: the swing's attacker, the room on a hazard turn, otherwise whoever acts next.</summary>
        private static Actor? TurnActor(BoardTurnView turn) =>
            turn.LiveSwing?.Source ?? turn.HazardAct?.Actor ?? (turn.Queue.Count > 0 ? turn.Queue[0].Actor : null);

        // ATTACKER plays in three parts across its beats: the turn lights up and the odds stand out, the blank die
        // appears, then (on its result beat) the attacker's lined-up card flares. ROLL then tumbles the die.

        /// <summary>Share of ATTACKER's title beats spent on the odds before the die appears.</summary>
        private const double DieAppearsAt = 0.5;

        private static bool DieShown(int stage, double raw) => stage > 0 || CombatSequenceHudState.ResultRevealed || raw >= DieAppearsAt;

        /// <summary>The attacker's card flares on ATTACKER's result beat, and holds through the roll and outcome.</summary>
        private static bool CardFocused(int stage) => stage is 1 or 2 || (stage == 0 && CombatSequenceHudState.ResultRevealed);

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

        /// <summary>OUTCOME splits in two: the result word stamps over the first half, the path to the branch taken grows over the second.</summary>
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

        /// <summary>Board stage per sequence column: 0 attacker, 1 roll, 2 outcome, 3 action, 4 defense, 5 target/effects, 6 damage/heal.</summary>
        private static int BoardStageOf(CombatSequenceStepKind kind) => kind switch
        {
            CombatSequenceStepKind.Attacker => 0,
            CombatSequenceStepKind.Roll => 1,
            CombatSequenceStepKind.Outcome => 2,
            CombatSequenceStepKind.Action => 3,
            CombatSequenceStepKind.Defense => 4,
            CombatSequenceStepKind.Target => 5,
            CombatSequenceStepKind.Effect => 5,
            CombatSequenceStepKind.Damage => 6,
            CombatSequenceStepKind.Heal => 6,
            _ => 7
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
            // ATTACKER holds its title for several beats (odds + blank die) and ROLL for part of one (die tumble); fit their motion to match.
            double beats = CombatSequenceHudState.ResultRevealed ? 1 : CombatSequencePresenter.TitleBeats(kind.Value);
            double raw = CombatBoardAnimation.PhaseProgress((int)Math.Ceiling(CombatBoardAnimation.BeatMs * beats));
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

        /// <summary>Outcome color of a swing; a room's strike roll wears the hazard color, or gray when it rolls quiet.</summary>
        private static Color SwingColor(CombatSequenceSwingInfo swing) => swing.Source is Environment
            ? swing.Outcome == CombatSequenceSwingOutcome.Miss ? AsciiArtAssets.Colors.Gray : BoardHazardColor
            : OutcomeColor(swing.Outcome);

        /// <summary>Border of a settled act still on NOW: its outcome, the room's color for a hazard, dim when empty.</summary>
        private static Color BoardRestBorder(BoardAct? act) => act switch
        {
            { Quiet: true } => BoardDimBorder,
            { Swing: { } s } => SwingColor(s),
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

        private BoardRect? RenderBoardEnemyStrip(Enemy enemy, int x, int y, int w, int h, ActionStripDamageLineMode mode,
            List<(int X, int Y, int W, int H)> cardRects)
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
            // A plain HIT fires a basic attack, not a strip card: nothing flashes. A miss still dulls the card it was lined up on.
            bool cardFires = false;
            if (swing != null)
            {
                int slot = FindSlot(actions, swing.Action);
                if (slot >= 0 && slot < filled)
                {
                    selected = slot;
                    cardFires = true;
                }
                else
                    cardFires = swing.Outcome == CombatSequenceSwingOutcome.Miss;
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
                if (isSel && cardFires && swing != null)
                {
                    // The firing card flares red on ATTACKER's result beat, holds through the roll, then wears its outcome.
                    border = stage >= 2 ? OutcomeColor(swing.Outcome)
                        : CardFocused(stage) ? AsciiArtAssets.Colors.Red : AsciiArtAssets.Colors.White;
                    thickness = CardFocused(stage) ? 4 : 3;
                }
                Color fill = BoardCardBackground;
                if (isSel && cardFires && swing != null)
                    (border, fill, thickness) = AccentForBeat(swing, stage, CombatBoardAnimation.PhaseProgress(CombatBoardAnimation.BeatMs), border, thickness);
                AddBoardBox(cx, y, cw, cardH, border, fill, borderThicknessPixels: thickness);
                DrawActionCardContent(enemy, panelData[i], actions[i], i, cx, y, cw, cardH, mode);
                cardRects.Add((cx, y, cw, cardH));
                if (isSel && (swing == null || cardFires))
                    selectedRect = new BoardRect(cx, y, cw, cardH);
                cx += cw + gap;
            }
            return selectedRect;
        }

        private static readonly Color BoardHazardColor = Color.FromRgb(90, 210, 160);
        private const int BoardQueueLength = 1;

        /// <summary>One resolved action as the board shows it on NOW and the timeline (a swing, or a room hazard with no swing).</summary>
        private sealed record BoardAct(Actor Actor, Action Action, CombatSequenceSwingInfo? Swing, int QuietRoll = 0, int QuietNeed = 0)
        {
            /// <summary>A room turn that rolled under its strike threshold: shown as a QUIET card, nothing happens.</summary>
            public bool Quiet => QuietRoll > 0;
        }

        /// <summary>The newest act, pushed once per swing / hazard turn.</summary>
        private static BoardAct? _boardNowAct;
        private static CombatSequenceSwingInfo? _boardRegisteredSwing;
        private static long _boardSwingTurn = -1, _boardHazardTurn = -1;
        private static Enemy? _boardEnemy;

        /// <summary>The branch box OUTCOME picked for the live swing; on ACTION its card slides from there into NOW,
        /// unless NOW already holds that action (<see cref="_boardLandKeep"/>).</summary>
        private static CombatSequenceSwingInfo? _boardLandSwing;
        private static BoardRect _boardLandFrom;
        private static bool _boardLandKeep;

        /// <summary>The swing whose card is flying from its branch into NOW, and when it left.</summary>
        private static CombatSequenceSwingInfo? _boardLandStartedFor;
        private static DateTimeOffset _boardLandStartUtc;

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

            /// <summary>The acting entity's turn counts as spent in <see cref="Queue"/>, and the length it is pushed out by.</summary>
            public bool Spent;
            public double ActingLength = 1.0;

            /// <summary>The acting entity while its turn hasn't fired: its combo step is held where it stood before the turn.</summary>
            public Actor? HoldActor;

            /// <summary>The acting turn has come down from the timeline into NOW (after its callout), and how far its card
            /// has flown (1 once it sits on NOW).</summary>
            public bool TurnInNow;
            public double DownT = 1.0;

            /// <summary>When the acting turn's card settled on NOW (the end of its fly-down).</summary>
            public DateTimeOffset DownLandedUtc;

            /// <summary>Who holds NOW, the act its turn has pushed (null before the swing registers), and the action it
            /// is lined up on (null for the room).</summary>
            public Actor? NowActor;
            public TimelineAct? NowTurnAct;
            public Action? NowProjected;

            /// <summary>An act still playing out after its turn ended (shown on NOW until it completes).</summary>
            public BoardAct? LingeringAct;

            /// <summary>ACTION has fired, so the queue moves on to whoever acts after this turn.</summary>
            public bool Triggered => LiveSwing != null ? Stage >= 3 : HazardAct != null;
        }

        private static void PushBoardAct(BoardAct act)
        {
            RecordTimelineAct(act);
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
                _boardNowAct = null;
                _boardRegisteredSwing = last;
                ResetBoardTimeline();
            }

            v.LiveSwing = LiveSwingFor(player) ?? LiveSwingFor(enemy) ?? (last?.Source is Environment r ? LiveSwingFor(r) : null);
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
                // A room roll that comes up short lands on NOW as a QUIET card.
                PushBoardAct(swing.Source is Environment && swing.Outcome == CombatSequenceSwingOutcome.Miss
                    ? new BoardAct(swing.Source, swing.Action, swing, swing.Roll, swing.Thresholds?.MinHit ?? 0)
                    : new BoardAct(swing.Source, swing.Action, swing));
            }

            // A room turn lands on NOW whether it strikes or rolls quiet, so a miss still takes its place in the sequence.
            // When the room's roll plays as a sequence (the die tumbles like a swing) the swing above registers it;
            // this places it directly only when no sequence plays (instant log).
            if (v.LiveSwing == null && acting is Environment room && _boardSwingTurn != turn)
            {
                var hazard = CombatTurnQueue.ActingAction;
                bool rollPlays = CombatSequencePresenter.ShouldPlay() || CombatSequenceFlavorPresenter.ShouldPlay();
                if ((hazard != null && !rollPlays) || CombatTurnQueue.ActingQuiet)
                {
                    if (_boardHazardTurn != turn)
                    {
                        _boardHazardTurn = turn;
                        PushBoardAct(hazard != null
                            ? new BoardAct(room, hazard, null)
                            : new BoardAct(room, CombatSequenceBuilder.QuietRoomAction, null, room.LastStrikeRoll, room.LastStrikeRollThreshold));
                    }
                    v.HazardAct = _boardNowAct;
                }
            }

            // Turn handlers advance the actor's timeline only after the swing displays, so once ACTION fires
            // (or the swing has finished inside the same turn) the queue projects the actor past this turn.
            bool spent = acting != null && (v.Triggered || (v.LiveSwing == null && _boardSwingTurn == turn));
            double length = (v.LiveSwing?.Action ?? v.HazardAct?.Action ?? _boardNowAct?.Action)?.Length ?? 1.0;
            v.Spent = spent;
            v.ActingLength = length;
            v.Queue = CombatTurnQueue.GetUpcoming(BoardQueueLength, spent, length);
            UpdateBoardTimeline(v, player, enemy);
            return v;
        }

        private static Color BoardActorColor(Actor actor) => actor switch
        {
            Environment => BoardHazardColor,
            Enemy => AsciiArtAssets.Colors.Red,
            _ => AsciiArtAssets.Colors.Gold
        };

        /// <summary>NOW and the gap to the branch column, centered in the combo flow's area.</summary>
        private static (BoardRect Now, int LineGap, int NextW) BoardFlowLayout(int x, int y, int w, int h)
        {
            int curW = Math.Clamp(w * 26 / 100, 22, 40);
            int nextW = Math.Clamp(w * 24 / 100, 20, 34);
            int lineGap = Math.Clamp(w * 9 / 100, 10, 16);
            int total = curW + lineGap + nextW;
            if (total > w - 2)
            {
                double s = (double)(w - 2) / total;
                curW = Math.Max(14, (int)(curW * s));
                nextW = Math.Max(12, (int)(nextW * s));
                lineGap = Math.Max(8, (int)(lineGap * s));
                total = curW + lineGap + nextW;
            }
            int left = x + Math.Max(1, (w - total) / 2);
            int curH = Math.Clamp(h * 6 / 10, 8, Math.Max(8, h));
            return (new BoardRect(left, y + (h - curH) / 2, curW, curH), lineGap, nextW);
        }

        /// <summary>
        /// Middle of the board, read right to left: the next roll's outcome branches (one box each), the d20, NOW.
        /// A turn plays as: the turn callout; the actor's lined-up card flies down from the timeline into NOW; the
        /// branches fill in with their odds; ROLL tumbles the die; OUTCOME stamps the result and lights the branch taken;
        /// ACTION flies that branch's card into NOW (NOW keeps its card when the roll landed the action it was lined up on);
        /// EFFECTS and DAMAGE land on the target's band (under the timeline, or over the hero strip); once the swing
        /// finishes, its card flies up into the timeline's current slot and what it set up for the attacker's next action
        /// shows on their band. Room turns with no swing land on NOW directly - a hazard, or a QUIET card.
        /// </summary>
        private void RenderBoardComboFlow(Character player, Enemy enemy, BoardTurnView turn,
            int x, int y, int w, int h, int enemyBandY, int heroBandY, ActionStripDamageLineMode mode)
        {
            if (h < 9)
                return;

            // --- Layout (rest positions) ---
            var (curR, lineGap, nextW) = BoardFlowLayout(x, y, w, h);
            int nextX = curR.X + curR.W + lineGap;

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
            _boardDieCenter = junction;

            var swing = turn.LiveSwing;
            int stage = turn.Stage;
            double p = turn.P, rawP = turn.Raw;
            Color strikeColor = swing?.Source switch
            {
                Enemy => AsciiArtAssets.Colors.Red,
                Environment => BoardHazardColor,
                _ => BoardConnectorColor
            };

            // The branch the roll picked waits on OUTCOME, then on ACTION its card leaves the column and flies into NOW -
            // unless the roll landed the action NOW was already lined up on, which stays put.
            if (nextRolling && stage == 2)
            {
                int pick = TakenBranch(branches, swing!);
                if (pick >= 0 && pick < slots)
                {
                    _boardLandSwing = swing;
                    _boardLandFrom = queueR[pick];
                    _boardLandKeep = swing!.Outcome == CombatSequenceSwingOutcome.Combo && turn.NowProjected != null
                        && string.Equals(turn.NowProjected.Name, swing.Action.Name, StringComparison.Ordinal);
                }
            }
            bool picked = swing != null && ReferenceEquals(_boardLandSwing, swing);
            bool flies = picked && !_boardLandKeep;

            // The act reaches NOW on ACTION (OUTCOME if it has no branch to come from); until then NOW shows the card
            // the actor is lined up on.
            bool landed = swing == null || stage >= (picked ? 3 : 2);
            double shift = 1.0;
            if (flies && landed)
            {
                if (!ReferenceEquals(_boardLandStartedFor, swing))
                {
                    _boardLandStartedFor = swing;
                    _boardLandStartUtc = DateTimeOffset.UtcNow;
                }
                double raw = CombatBoardAnimation.Progress(_boardLandStartUtc, CombatBoardAnimation.SlideMs);
                if (raw < 1.0)
                {
                    shift = CombatBoardAnimation.EaseOut(raw);
                    CombatBoardAnimation.RequestFrame();
                }
            }
            bool cardInFlight = flies && landed && shift < 1.0;

            // What NOW holds: the lined-up card once it has flown down from the timeline, the act once it lands, nothing
            // once the act has flown back up to the timeline.
            BoardAct? nowAct = null;
            bool nowProjected = false;
            if (turn.TurnInNow)
            {
                if (turn.DownT >= 1.0)
                {
                    var act = turn.NowTurnAct;
                    if (act == null || (act.Act.Swing != null && ReferenceEquals(act.Act.Swing, swing) && !landed))
                        nowProjected = true;
                    else if (act.CompletedUtc == null)
                        nowAct = act.Act;
                }
            }
            else
                nowAct = turn.LingeringAct;

            // --- NOW ---
            var nowR = curR;
            CombatBoardLayout.NowRect = (nowR.X, nowR.Y, nowR.W, nowR.H);
            int flowTop = curR.Y, flowBottom = (int)curR.Bottom;
            if (slots > 0)
            {
                flowTop = Math.Min(flowTop, queueR[0].Y);
                flowBottom = Math.Max(flowBottom, (int)queueR[slots - 1].Bottom);
            }
            flowTop = Math.Max(y, flowTop - 1); // the cards' labels sit one row above them
            int flowRight = Math.Max((int)curR.Right, nextX + nextW);
            CombatBoardLayout.FlowCardsRect = (curR.X, flowTop, flowRight - curR.X, flowBottom - flowTop);
            Color nowBorder = BoardRestBorder(nowAct);
            Color nowFill = BoardCardBackground;
            int nowThickness = 1;
            if (swing != null && landed && nowAct != null && ReferenceEquals(nowAct.Swing, swing))
            {
                nowBorder = LerpColor(AsciiArtAssets.Colors.White, SwingColor(swing), stage > 2 ? 1.0 : CombatBoardAnimation.EaseOut(rawP));
                nowThickness = 3;
                (nowBorder, nowFill, nowThickness) = AccentForBeat(swing, stage, rawP, nowBorder, nowThickness);
            }
            else if (turn.HazardAct != null && nowAct != null)
            {
                nowBorder = turn.HazardAct.Quiet ? AsciiArtAssets.Colors.Gray : BoardHazardColor;
                nowThickness = 3;
            }
            else if (nowProjected)
            {
                nowBorder = BoardActorColor(turn.NowActor!);
                nowThickness = 2;
            }
            if (cardInFlight)
                (nowBorder, nowThickness) = (BoardDimBorder, 1);
            AddBoardBox(nowR.X, nowR.Y, nowR.W, nowR.H, nowBorder, nowFill, borderThicknessPixels: nowThickness);
            if (nowAct != null && !cardInFlight)
            {
                canvas.AddText(nowR.X + 1, nowR.Y - 1, Truncate($"NOW  {nowAct.Actor.Name}", nowR.W), BoardActorColor(nowAct.Actor));
                DrawBoardActContent(nowAct, nowR, mode);
            }
            else if (nowProjected)
            {
                canvas.AddText(nowR.X + 1, nowR.Y - 1, Truncate($"NOW  {turn.NowActor!.Name}", nowR.W), BoardActorColor(turn.NowActor));
                DrawBoardProjectedContent(turn.NowActor, turn.NowProjected, nowR, mode);
            }
            else
                canvas.AddText(nowR.X + 1, nowR.Y - 1, "NOW", AsciiArtAssets.Colors.White);

            // Once the turn's card settles on NOW the branches wait a beat, then the die another; ROLL holds for the die.
            var now = DateTimeOffset.UtcNow;
            bool settled = turn.TurnInNow && turn.DownT >= 1.0;
            var branchesAt = turn.DownLandedUtc.AddMilliseconds(CombatBoardAnimation.BranchRevealDelayMs);
            var dieAt = branchesAt.AddMilliseconds(CombatBoardAnimation.DieRevealDelayMs);
            bool pastAttacker = swing != null && (stage > 0 || CombatSequenceHudState.ResultRevealed);
            bool branchesDue = pastAttacker || (settled && now >= branchesAt);
            bool dieDue = pastAttacker || (settled && now >= dieAt);
            if (swing != null && !pastAttacker)
            {
                bool ownsNow = settled && ReferenceEquals(turn.NowActor, swing.Source);
                CombatSequencePresenter.ReportRollGate(swing, ownsNow ? dieAt : null);
                CombatBoardAnimation.RequestFrame();
            }
            else if (turn.TurnInNow && !dieDue)
                CombatBoardAnimation.RequestFrame();

            // --- Die between NOW and the queue ---
            // The die belongs to the roll: it leaves as ACTION sends the card to NOW, so it never reads as the next roll.
            bool dieVisible = swing != null && stage <= 2 && dieDue && (pastAttacker || DieShown(stage, rawP));
            BoardRect dieR = default;
            if (dieVisible)
                dieR = DrawBoardDie(junction, swing!, stage, rawP, x, w);

            (double X, double Y) junctionIn = dieVisible ? (dieR.X, junction.Y) : junction;
            (double X, double Y) junctionOut = dieVisible ? (dieR.Right, junction.Y) : junction;
            canvas.AddLine(nowR.Right, nowR.CenterY, junctionIn.X, junctionIn.Y,
                dieVisible ? strikeColor : BoardIdleLine, dieVisible ? 2.0 : 1.5);

            // --- Next roll: one branch per outcome ---
            // The next actor's branches wait until the flying card has cleared the column (text draws above every box),
            // and stay hidden until that actor's turn has started and its callout has played.
            bool branchesRevealed = nextEntry.IsActing && settled && branchesDue
                && !(CombatTurnAnnouncement.Current is { } callout && ReferenceEquals(callout.Actor, nextEntry.Actor));
            if (hasNext && slots > 0 && !(cardInFlight && shift < 0.5))
            {
                var actor = nextEntry.Actor;
                string label = nextRolling ? "ROLLING" : nextEntry.IsActing ? "ACTING" : "NEXT";
                canvas.AddText(nextX + 1, queueR[0].Y - 1, Truncate($"{label}  {actor.Name}", nextW), BoardActorColor(actor));
                double fade = stage > 2 ? 1.0 : CombatBoardAnimation.EaseOut(rawP);
                int taken = nextRolling ? TakenBranch(branches, swing!) : -1;
                // The odds light up in the attacker's color as the turn starts and stay lit while the die rolls.
                double oddsEmphasis = !nextRolling || stage > 1 ? 0.0
                    : stage == 1 || CombatSequenceHudState.ResultRevealed ? 1.0
                    : CombatBoardAnimation.EaseOut(Math.Clamp(rawP / (DieAppearsAt * 0.6), 0.0, 1.0));
                for (int i = 0; i < slots; i++)
                {
                    var b = branches[i];
                    bool resolved = nextRolling && stage >= 2 && b.Outcome != null;
                    bool chosen = resolved && i == taken;
                    canvas.AddLine(junctionOut.X, junctionOut.Y, queueR[i].X, queueR[i].CenterY,
                        resolved && !chosen ? BoardFadedBorder : BoardIdleLine, 1.0);
                    if (chosen)
                        AddGrowingLine(junctionOut, (queueR[i].X, queueR[i].CenterY), OutcomeHalf(stage, rawP, second: true), strikeColor, 2.0);
                    DrawBoardBranch(b, queueR[i], resolved, chosen, fade, oddsEmphasis, BoardActorColor(actor), branchesRevealed);
                }
            }

            if (cardInFlight)
                DrawBoardAct(turn.NowTurnAct?.Act ?? _boardNowAct, BoardRect.Lerp(_boardLandFrom, nowR, shift), SwingColor(swing!), 3, mode);

            // The act on NOW reads as its combat-log action block in a small box above it. A swing still playing shows
            // it once its effects or damage land, the same beat the target's band would.
            bool blockShown = false;
            if (!cardInFlight && nowAct?.Swing is { LogBlock: { Count: > 0 } block } nowSwing
                && (!ReferenceEquals(nowSwing, swing) || ImpactLanded(nowSwing, stage)))
                blockShown = DrawBoardLogBlock(block, nowAct.Actor, nowR, x, w, enemyBandY);

            // What the swing is doing to its target while it plays; once it ends, what it set up for the attacker's next action.
            var resultSwing = swing ?? (CombatSequenceHudState.IsFinished ? CombatSequenceHudState.ActiveSwing : null);
            if (resultSwing != null && !cardInFlight)
            {
                if (swing != null)
                {
                    if (!blockShown)
                        DrawBoardImpact(resultSwing, player, enemy, nowR, x, w, enemyBandY, heroBandY);
                }
                else
                    DrawBoardNextBonus(resultSwing, x, w, enemyBandY, heroBandY);
            }
        }

        /// <summary>
        /// The target's band (under the enemy strip, or over the hero strip): the target's name, then its status effects
        /// once EFFECTS plays and the damage (or heal) once DAMAGE does, each popping in with a glow. A line ties it to NOW.
        /// Only while the swing plays.
        /// </summary>
        private void DrawBoardImpact(CombatSequenceSwingInfo swing, Character player, Enemy enemy, BoardRect nowR, int x, int w,
            int enemyBandY, int heroBandY)
        {
            var steps = CombatSequenceHudState.Steps;
            int cur = CombatSequenceHudState.CurrentIndex;
            var (effectAt, amountAt) = ImpactSteps(swing);
            bool showEffect = ImpactStepShown(effectAt), showAmount = ImpactStepShown(amountAt);
            if (!showEffect && !showAmount)
                return;

            bool heal = amountAt >= 0 && steps[amountAt].Kind == CombatSequenceStepKind.Heal;
            Actor target = heal ? swing.Source : swing.Source is Enemy or Environment ? player : enemy;
            bool onEnemy = target is Enemy;
            int bandY = onEnemy ? enemyBandY : heroBandY;

            string amount = "";
            if (showAmount)
            {
                // While DAMAGE counts through its math, follow the number it is on; then the final figure.
                string? n = cur == amountAt ? PickNumber(Flatten(CombatSequenceHudState.VisibleResult), first: false) : null;
                amount = heal ? $"+{n ?? PickNumber(Flatten(steps[amountAt].Result), first: false) ?? "?"}" : $"-{n ?? swing.Damage.ToString()}";
            }
            string effect = showEffect ? Flatten(steps[effectAt].Result) : "";
            string name = target.Name ?? "";
            int len = name.Length + (amount.Length > 0 ? amount.Length + 2 : 0) + (effect.Length > 0 ? effect.Length + 2 : 0);
            int tx = Math.Clamp((int)Math.Round(nowR.CenterX - len / 2.0), x + 1, Math.Max(x + 1, x + w - len - 1));

            // A short tie from NOW's edge to the band.
            double edgeY = onEnemy ? nowR.Y - 1 : nowR.Bottom;
            double bandEdge = onEnemy ? bandY + 1 : bandY;
            if (Math.Abs(edgeY - bandEdge) > 0.5)
                canvas.AddLine(nowR.CenterX, bandEdge, nowR.CenterX, edgeY, LerpColor(BoardActorColor(target), BoardCardBackground, 0.5), 1.0);

            canvas.AddText(tx, bandY, name, BoardActorColor(target));
            tx += name.Length + 2;
            if (amount.Length > 0)
            {
                Color c = heal ? BoardHealLine : swing.IsCritical ? AsciiArtAssets.Colors.Gold : AsciiArtAssets.Colors.Red;
                double pop = cur == amountAt ? 1.0 - CombatBoardAnimation.PhaseProgress(CombatBoardAnimation.BeatMs) : 0.0;
                if (pop > 0.0)
                    CombatBoardAnimation.RequestFrame();
                canvas.AddText(tx, bandY, amount, c, c, 0.35 + 0.65 * pop, pop > 0.3 ? 5 : 3);
                tx += amount.Length + 2;
            }
            if (effect.Length > 0)
            {
                double pop = cur == effectAt ? 1.0 - CombatBoardAnimation.PhaseProgress(CombatBoardAnimation.BeatMs) : 0.0;
                if (pop > 0.0)
                    CombatBoardAnimation.RequestFrame();
                Color c = AsciiArtAssets.Colors.Cyan;
                canvas.AddText(tx, bandY, Truncate(effect, Math.Max(4, x + w - tx - 1)), c, c, 0.2 + 0.6 * pop, 3);
            }
        }

        /// <summary>Indexes of the swing's EFFECTS and DAMAGE/HEAL steps in the playing sequence (-1 when absent).</summary>
        private static (int EffectAt, int AmountAt) ImpactSteps(CombatSequenceSwingInfo swing)
        {
            var steps = CombatSequenceHudState.Steps;
            int effectAt = -1, amountAt = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                if (!ReferenceEquals(steps[i].Swing, swing))
                    continue;
                if (steps[i].Kind == CombatSequenceStepKind.Effect && effectAt < 0)
                    effectAt = i;
                else if (steps[i].Kind is CombatSequenceStepKind.Damage or CombatSequenceStepKind.Heal && amountAt < 0)
                    amountAt = i;
            }
            return (effectAt, amountAt);
        }

        private static bool ImpactStepShown(int at)
        {
            int cur = CombatSequenceHudState.CurrentIndex;
            return at >= 0 && (cur > at || (cur == at && CombatSequenceHudState.ResultRevealed));
        }

        /// <summary>The live swing's effects or damage have landed (once ACTION fires when it has neither).</summary>
        private static bool ImpactLanded(CombatSequenceSwingInfo swing, int stage)
        {
            var (effectAt, amountAt) = ImpactSteps(swing);
            return effectAt < 0 && amountAt < 0 ? stage >= 3 : ImpactStepShown(effectAt) || ImpactStepShown(amountAt);
        }

        /// <summary>
        /// The act's combat-log action block in a small black box centered above NOW, a few rows clear of NOW's label
        /// and no higher than <paramref name="topY"/>, with a red line dropping from it to NOW. Lines wrap; rows that
        /// don't fit are dropped from the bottom. Returns false when there is no room for it.
        /// </summary>
        private bool DrawBoardLogBlock(List<List<ColoredText>> block, Actor actor, BoardRect nowR, int x, int w, int topY)
        {
            const int idealGap = 2; // rows between the box and NOW's label, for the line
            // Centered on NOW: as wide as the narrower side of the column allows on both sides.
            int halfW = (int)Math.Floor(Math.Min(nowR.CenterX - (x + 1), x + w - 1 - nowR.CenterX));
            int maxInner = halfW * 2 - 4;
            int maxRows = nowR.Y - 3 - topY;
            if (maxInner < 12 || maxRows < 1)
                return false;

            var rows = new List<List<(char C, Color Color)>>();
            bool first = true;
            foreach (var line in block)
            {
                int before = rows.Count;
                WrapLogLine(line, maxInner, first ? 0 : 2, rows);
                first &= rows.Count == before;
            }
            if (rows.Count == 0)
                return false;
            int keep = maxRows > 1 ? maxRows - 1 : maxRows; // leave the line at least one row
            if (rows.Count > keep)
                rows.RemoveRange(keep, rows.Count - keep);

            int inner = rows.Max(r => r.Count);
            int bw = inner + 4, bh = rows.Count + 2;
            int gap = Math.Min(idealGap, maxRows - rows.Count);
            int bx = (int)Math.Round(nowR.CenterX - bw / 2.0);
            int by = nowR.Y - 1 - gap - bh;
            AddBoardBox(bx, by, bw, bh, LerpColor(BoardActorColor(actor), BoardCardBackground, 0.5), BoardCardBackground);
            if (gap > 0)
                canvas.AddLine(nowR.CenterX, by + bh, nowR.CenterX, nowR.Y - 1, AsciiArtAssets.Colors.Red, 1.5);
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int k = 0; k < row.Count;)
                {
                    int j = k;
                    while (j < row.Count && row[j].Color == row[k].Color)
                        j++;
                    string run = new string(row.GetRange(k, j - k).Select(c => c.C).ToArray());
                    if (run.Trim().Length > 0)
                        canvas.AddText(bx + 2 + k, by + 1 + r, run, row[k].Color);
                    k = j;
                }
            }
            return true;
        }

        /// <summary>
        /// Word-wraps one log line into rows of at most <paramref name="width"/> characters (each row led by
        /// <paramref name="indent"/> spaces), keeping each character's color. Leading and trailing blanks are dropped.
        /// </summary>
        private static void WrapLogLine(List<ColoredText> line, int width, int indent, List<List<(char C, Color Color)>> rows)
        {
            var chars = new List<(char C, Color Color)>();
            foreach (var seg in line)
                foreach (char c in seg.Text ?? "")
                    chars.Add((c, seg.Color));
            int i = 0, end = chars.Count;
            while (i < end && char.IsWhiteSpace(chars[i].C))
                i++;
            while (end > i && char.IsWhiteSpace(chars[end - 1].C))
                end--;
            int room = Math.Max(4, width - indent);
            while (i < end)
            {
                int take = Math.Min(room, end - i);
                if (i + take < end)
                {
                    int space = chars.FindLastIndex(i + take, take + 1, c => c.C == ' ');
                    if (space > i + room / 2)
                        take = space - i;
                }
                var row = new List<(char C, Color Color)>(indent + take);
                for (int k = 0; k < indent; k++)
                    row.Add((' ', default));
                row.AddRange(chars.GetRange(i, take));
                rows.Add(row);
                i += take;
                while (i < end && chars[i].C == ' ')
                    i++;
            }
        }

        /// <summary>
        /// After a hero's swing lands: what the action set up for the hero's next action / turns (cadence bonuses, a
        /// defensive stance), fading in on the hero's band at the right. Stays until the next swing starts.
        /// </summary>
        private void DrawBoardNextBonus(CombatSequenceSwingInfo swing, int x, int w, int enemyBandY, int heroBandY)
        {
            if (swing.Outcome == CombatSequenceSwingOutcome.Miss || swing.Source is not Character || swing.Source is Enemy)
                return;
            var parts = new List<string>();
            string stance = HeroDefenseHudFormatter.FormatActionBlockSuffix(swing.Action);
            if (!string.IsNullOrWhiteSpace(stance))
                parts.Add(stance.Trim());
            foreach (var group in swing.Action.ActionAttackBonuses?.BonusGroups ?? new List<ActionAttackBonusGroup>())
            {
                if (group?.Bonuses == null || group.Bonuses.Count == 0)
                    continue;
                int count = ActionCadenceDurationResolver.GetDisplayCount(swing.Action, group);
                parts.AddRange(CadenceCardLineFormatter.FormatGroupLines(group, count).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()));
            }
            if (parts.Count == 0)
                return;

            double fade = CombatBoardAnimation.EaseOut(CombatBoardAnimation.Progress(CombatSequenceHudState.FinishedUtc, CombatBoardAnimation.BeatMs));
            if (fade < 1.0)
                CombatBoardAnimation.RequestFrame();
            string text = Truncate("NEXT: " + string.Join("  ", parts), Math.Max(8, w / 2 - 2));
            int tx = x + w - text.Length - 1;
            Color c = LerpColor(BoardCardBackground, AsciiArtAssets.Colors.Green, fade);
            canvas.AddText(tx, heroBandY, text, c, c, 0.5 * fade, 3);
        }

        /// <summary>One possible result of the next roll: its odds (faces or percent), what it does, and any detail lines.</summary>
        private sealed record BoardBranch(string Odds, string Text, Color Color, CombatSequenceSwingOutcome? Outcome, List<string> Detail,
            bool Critical = false, bool OwnColor = false);

        /// <summary>
        /// One outcome box of the next roll. While that roll plays, OUTCOME lights the branch taken and fades the rest.
        /// Until the actor's turn starts the box stays empty.
        /// </summary>
        private void DrawBoardBranch(BoardBranch b, BoardRect r, bool resolved, bool chosen, double fade, double oddsEmphasis, Color actorColor,
            bool filled)
        {
            Color border = BoardDimBorder;
            int thickness = 1;
            Color color = b.Color;
            if (oddsEmphasis > 0.0)
            {
                border = LerpColor(BoardDimBorder, actorColor, oddsEmphasis);
                thickness = 2;
            }
            if (resolved)
            {
                border = chosen ? (b.Critical || b.OwnColor ? b.Color : OutcomeColor(b.Outcome!.Value)) : LerpColor(BoardDimBorder, BoardFadedBorder, fade);
                thickness = chosen ? 3 : 1;
                if (!chosen)
                    color = LerpColor(color, BoardFadedBorder, fade * 0.85);
            }
            AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);
            if (!filled)
                return;

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
                // The room rolls a d20 to act (strikes on a high face), then picks a hazard from its pool. While that
                // roll plays, keep the threshold it rolled against (a quiet roll lowers the next one).
                int strikeAt = rolling?.Thresholds?.MinHit ?? room.StrikeThreshold;
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
                list.Add(new BoardBranch(strikeFaces, "strikes:", BoardHazardColor, CombatSequenceSwingOutcome.Hit, hazards, OwnColor: true));
                list.Add(new BoardBranch(quietFaces, "quiet", AsciiArtAssets.Colors.Gray, CombatSequenceSwingOutcome.Miss, new(), OwnColor: true));
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

        /// <summary>A strip action gets its full card; a normal hit, a miss, a room hazard or a quiet room roll gets a short title card.</summary>
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
            if (act.Quiet)
            {
                title = "QUIET";
                sub = $"rolled {act.QuietRoll}, needed {act.QuietNeed}+";
                color = AsciiArtAssets.Colors.Gray;
            }
            else if (act.Actor is Environment)
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
            const int dieW = 6, dieH = BoardDieH;
            bool tumbling = stage == 1 && !CombatSequenceHudState.ResultRevealed;
            long tick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 55;
            int dx = Math.Clamp((int)Math.Round(center.X - dieW / 2.0), clampX, clampX + clampW - dieW);
            int dy = (int)Math.Round(center.Y - dieH / 2.0);
            var r = new BoardRect(dx, dy, dieW, dieH);

            string face;
            Color border;
            int thickness;
            if (stage == 0)
            {
                // Waiting to roll: a blank die in the attacker's color.
                face = "?";
                border = BoardActorColor(swing.Source);
                thickness = 2;
            }
            else if (tumbling)
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
                border = SwingColor(swing);
                thickness = stage == 2 && raw < 0.5 ? 4 : 2;
                if (stage == 2)
                {
                    // Outcome word stamps above the die.
                    string word = swing.Outcome switch
                    {
                        _ when swing.Source is Environment => swing.Outcome == CombatSequenceSwingOutcome.Miss ? "QUIET" : "STRIKES!",
                        CombatSequenceSwingOutcome.Combo => "ACTION!",
                        CombatSequenceSwingOutcome.Hit => "HIT",
                        _ => "MISS"
                    };
                    if (swing.IsCritical)
                        word = "CRIT " + word;
                    // The result stamps with a glow that settles over the first half of OUTCOME; ACTION! glows hardest.
                    int wx = Math.Max(clampX, (int)Math.Round(center.X - word.Length / 2.0));
                    Color wc = swing.IsCritical ? AsciiArtAssets.Colors.Gold : border;
                    double pop = 1.0 - Math.Clamp(raw * 2.0, 0.0, 1.0);
                    bool big = swing.Outcome == CombatSequenceSwingOutcome.Combo || swing.IsCritical;
                    canvas.AddText(wx, dy - 1, word, wc, wc, (big ? 0.6 : 0.3) + 0.4 * pop, big ? 6 : 3);
                }
            }

            AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);
            // Odd-length faces can't sit on the cell grid's center of an even-width die: shift them half a cell.
            int slack = Math.Max(2, dieW - face.Length);
            canvas.AddTextNudged(r.X + slack / 2, r.Y + 1, face, tumbling ? AsciiArtAssets.Colors.Gray : AsciiArtAssets.Colors.White, slack % 2 * 0.5);
            return r;
        }

        private const int BoardDieH = 3;

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
            if (miss && swing.Source is Environment)
                return (border, BoardCardBackground, thickness); // a quiet room roll is nothing happening, not a failed swing
            if (miss && stage >= 3)
                return (AsciiArtAssets.Colors.DarkRed, BoardCardBackground, thickness);

            Color flash;
            if (stage == 3)
                flash = AsciiArtAssets.Colors.White;
            else if (stage == 6)
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
