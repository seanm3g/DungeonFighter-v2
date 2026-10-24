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
using System.Text;
using System.Text.RegularExpressions;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Board-style combat center column (<see cref="CombatBoardLayout"/>): enemy combo strip on top, enemy health,
    /// the hero's previous → current → next combo cards with connector lines, hero health, and (drawn separately by
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

            // --- Enemy name + health / d20 bar ---
            int barW = Math.Clamp(colW * 11 / 20, 20, colW - 12);
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
            _enemyRollY = enemyBarY + D20ThresholdBarRenderer.CombatStripVerticalOffsetNoArmor + D20ThresholdBarRenderer.CombatStripHeightScale;
            RenderBoardThresholdBar(enemy, ThresholdBarPanel.Enemy, barX, enemyBarY, barW, D20ThresholdBarRenderer.CombatStripVerticalOffsetNoArmor);
            canvas.AddText(barX + barW + 2, enemyBarY, $"{enemyHp}/{enemy.MaxHealth}", AsciiArtAssets.Colors.White);
            int enemyBlockBottom = enemyBarY + 2;

            // --- Hero d20 / health bar + name (bottom, mirrored) ---
            int heroNameY = boardH - 2;
            int heroBarY = heroNameY - 1;
            int heroD20Y = heroBarY - 1;
            int heroHp = RPGGame.Combat.UI.HealthBarDisplayHold.Resolve($"player_{player.Name}", player.CurrentHealth);
            int heroMax = player.GetEffectiveMaxHealth();
            _heroRollY = heroD20Y + (1.0 - D20ThresholdBarRenderer.CombatStripHeightScale);
            RenderBoardThresholdBar(player, ThresholdBarPanel.Hero, barX, heroD20Y, barW, 1.0 - D20ThresholdBarRenderer.CombatStripHeightScale);
            canvas.AddHealthBar(barX, heroBarY, barW, heroHp, heroMax,
                entityId: $"player_{player.Name}",
                heightScale: D20ThresholdBarRenderer.CombatHealthHeightScale);
            canvas.AddText(barX + barW + 2, heroBarY, $"{heroHp}/{heroMax}", AsciiArtAssets.Colors.White);
            string heroName = player.Name ?? "";
            canvas.AddText(barX + Math.Max(0, (barW - heroName.Length) / 2), heroNameY, heroName, AsciiArtAssets.Colors.Gold);

            // --- Combo flow (middle) ---
            int midTop = enemyBlockBottom + 1;
            int midBottom = heroD20Y - 1; // exclusive
            CombatBoardLayout.MiddleRect = (colX, midTop, colW, midBottom - midTop);
            if (CombatBoardLayout.ShowLog && log != null)
                RenderBoardLog(player, log, barX, midTop, barW, midBottom - midTop, enemyNamesForLogAlignment);
            else
                RenderBoardComboFlow(player, enemy, colX, midTop, colW, midBottom - midTop,
                    (barX + barW / 2.0, enemyBarY + 1.0), enemyCard, mode);
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
                D20ThresholdBarRenderer.RenderBar(canvas, barX, barY, barW, actor, panel, verticalOffsetScale: verticalOffsetScale);
                return;
            }

            var (stage, p, _) = LiveStage();
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
                if (stage == 1 && !CombatSequenceHudState.ResultRevealed)
                    return 1 + (int)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 55 * 7919 + 13) % 20);
                return roll;
            };
            D20ThresholdBarRenderer.RenderBar(canvas, barX, barY, barW, actor, panel,
                verticalOffsetScale: verticalOffsetScale, thresholds: bands, rollMarker: marker);
        }

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

        /// <summary>
        /// The hero's PREV card as it stood before the swing now playing. <see cref="Character.LastAction"/> already
        /// names the swing itself once execution resolves, so the board keeps the settled value until the slide ends.
        /// </summary>
        private static Action? _boardSettledPrev;

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
            double raw = CombatBoardAnimation.PhaseProgress(CombatBoardAnimation.BeatMs);
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
            int rem = avail % count;
            int cardH = Math.Max(3, h - 1);
            int cx = x + inset;
            BoardRect? selectedRect = null;
            for (int i = 0; i < count; i++)
            {
                int cw = cardW + (i < rem ? 1 : 0);
                if (i >= filled)
                {
                    canvas.AddBorder(cx, y, cw, cardH, AsciiArtAssets.Colors.Black);
                }
                else
                {
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
                }
                cx += cw + gap;
            }
            return selectedRect;
        }

        /// <summary>
        /// Middle of the board. At rest: PREV, NOW, then NOW forks through a junction into the COMBO / HIT / MISS next cards.
        /// While the hero's swing plays, each sequence column animates: ATTACKER draws the hero's line up to NOW, ROLL tumbles
        /// a d20 at the junction, OUTCOME lights the branch taken and fades the rest, ACTION lunges NOW and throws a strike
        /// line at the enemy bar, DEFENSE / DAMAGE ride that line, EFFECTS type out under the card. When the swing finishes the
        /// taken branch slides into NOW and NOW slides into PREV. Enemy swings throw their own line from the firing strip card.
        /// </summary>
        private void RenderBoardComboFlow(Character player, Enemy enemy, int x, int y, int w, int h,
            (double X, double Y) enemyAnchor, BoardRect? enemyCard, ActionStripDamageLineMode mode)
        {
            if (h < 9)
                return;

            var actions = player.GetComboActions();
            var panelData = CombatActionStripBuilder.BuildPanelData(player);
            int filled = Math.Min(actions.Count, panelData.Count);
            if (filled == 0)
                return;

            var heroSwing = LiveSwingFor(player);
            var lastSwing = CombatSequenceHudState.ActiveSwing;
            CombatSequenceSwingInfo? slideSwing = null;
            double slide = 1.0;
            if (heroSwing == null && lastSwing != null && CombatSequenceHudState.IsFinished && ReferenceEquals(lastSwing.Source, player))
            {
                double raw = CombatBoardAnimation.SlideProgress();
                if (raw < 1.0)
                {
                    slideSwing = lastSwing;
                    slide = CombatBoardAnimation.EaseOut(raw);
                    CombatBoardAnimation.RequestFrame();
                }
            }
            if (heroSwing == null && slideSwing == null)
                _boardSettledPrev = player.LastAction;

            // NOW at rest is the live combo step. Without a sequence playing (instant log) the strip's held slot still applies.
            int current = player.ComboStep % filled;
            if (CombatSequenceHudState.Steps.Count == 0
                && HeroActionStripFeedback.TryGetHeldSelectedIndex(out int held) && held >= 0 && held < filled)
                current = held;
            var anim = heroSwing ?? slideSwing;
            int fired = anim != null ? FindSlot(actions, anim.Action) : -1;
            if (heroSwing != null && fired >= 0 && fired < filled)
                current = fired;

            // --- Layout (rest positions) ---
            int curW = Math.Clamp(w * 26 / 100, 22, 40);
            int prevW = Math.Clamp(curW * 2 / 3, 16, 28);
            int nextW = Math.Clamp(w * 20 / 100, 18, 32);
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

            // COMBO gets a full card (the next combo action); HIT and MISS are small generic outcome boxes.
            const int outcomeH = 4;
            int nextGapRows = 1;
            int comboH = Math.Clamp(h - 2 * outcomeH - 2 * nextGapRows, 5, curH);
            int branchTotal = comboH + 2 * outcomeH + 2 * nextGapRows;
            int nextTop = y + Math.Max(0, (h - branchTotal) / 2);
            var branchR = new[]
            {
                new BoardRect(nextX, nextTop, nextW, comboH),
                new BoardRect(nextX, nextTop + comboH + nextGapRows, nextW, outcomeH),
                new BoardRect(nextX, nextTop + comboH + outcomeH + 2 * nextGapRows, nextW, outcomeH),
            };
            (double X, double Y) junction = (curR.Right + lineGap / 2.0, curR.CenterY);

            if (slideSwing != null)
            {
                RenderBoardSlide(player, actions, panelData, filled, slideSwing, fired, slide, prevR, curR, branchR, mode);
                return;
            }

            // Branches from NOW, per the d20 bands: a combo roll advances, a plain hit or miss restarts at slot 1.
            var branches = new (string Label, CombatSequenceSwingOutcome Outcome, string Note)[]
            {
                ("ACTION", CombatSequenceSwingOutcome.Combo, ""),
                ("HIT", CombatSequenceSwingOutcome.Hit, "normal hit"),
                ("MISS", CombatSequenceSwingOutcome.Miss, "whiff"),
            };

            int stage = 7;
            double p = 1.0, rawP = 1.0;
            if (heroSwing != null)
                (stage, p, rawP) = LiveStage();

            // --- PREV ---
            var prevAction = heroSwing != null ? _boardSettledPrev : player.LastAction;
            DrawBoardCard(player, actions, panelData, filled, FindSlot(actions, prevAction), prevAction, prevR, BoardDimBorder, 1, mode);
            canvas.AddText(prevR.X + 1, prevR.Y - 1, "PREV", AsciiArtAssets.Colors.Gray);

            // --- NOW (accents by border and fill only; cards never move mid-swing) ---
            var nowR = curR;
            Color nowBorder = AsciiArtAssets.Colors.White;
            Color nowFill = BoardCardBackground;
            int nowThickness = 2;
            if (heroSwing != null)
            {
                if (stage == 0)
                {
                    nowBorder = LerpColor(BoardDimBorder, AsciiArtAssets.Colors.White, p);
                    nowThickness = rawP < 1.0 ? 3 : 2;
                }
                else if (stage >= 2)
                {
                    nowBorder = OutcomeColor(heroSwing.Outcome);
                    nowThickness = 3;
                }
                (nowBorder, nowFill, nowThickness) = AccentForBeat(heroSwing, stage, rawP, nowBorder, nowThickness);
            }
            AddBoardBox(nowR.X, nowR.Y, nowR.W, nowR.H, nowBorder, nowFill, borderThicknessPixels: nowThickness);
            canvas.AddText(nowR.X + 1, nowR.Y - 1, $"NOW  {current + 1}/{filled}", AsciiArtAssets.Colors.White);
            DrawActionCardContent(player, panelData[current], actions[current], current, nowR.X, nowR.Y, nowR.W, nowR.H, mode);

            // Hero health -> NOW: drawn up the screen on the ATTACKER beat.
            // Your turn only: the roll's face on your d20 bar -> NOW. Gone at rest and on the enemy's turn.
            if (heroSwing != null && RollKnown(stage))
                AddGrowingLine(RollPoint(heroSwing.Roll, hero: true), (nowR.CenterX, nowR.Bottom), OutcomeHalf(stage, rawP, second: false), BoardConnectorColor, 2.0);

            // --- Junction die ---
            bool dieVisible = heroSwing != null && stage >= 1;
            BoardRect dieR = default;
            if (dieVisible)
                dieR = DrawBoardDie(junction, heroSwing!, stage, rawP, x, w);
            (double X, double Y) junctionIn = dieVisible ? (dieR.X, junction.Y) : junction;
            (double X, double Y) junctionOut = dieVisible ? (dieR.Right, junction.Y) : junction;
            canvas.AddLine(nowR.Right, nowR.CenterY, junctionIn.X, junctionIn.Y,
                dieVisible ? BoardConnectorColor : BoardIdleLine, dieVisible ? 2.0 : 1.5);

            // --- Branches ---
            for (int i = 0; i < branches.Length; i++)
            {
                var (label, outcome, note) = branches[i];
                var r = branchR[i];
                Color baseColor = OutcomeColor(outcome);
                bool chosen = heroSwing != null && heroSwing.Outcome == outcome;
                Color border = i == 0 ? BoardComboBorder : BoardDimBorder;
                Color labelColor = baseColor;
                Color lineColor = BoardIdleLine;
                double lineThickness = 1.5;
                int thickness = 1;
                if (heroSwing != null && stage >= 2)
                {
                    double t = OutcomeHalf(stage, rawP, second: true);
                    if (chosen)
                    {
                        border = LerpColor(BoardDimBorder, baseColor, t);
                        lineColor = baseColor;
                        thickness = 2;
                        lineThickness = 2.5;
                    }
                    else
                    {
                        border = LerpColor(border, BoardFadedBorder, t);
                        labelColor = LerpColor(baseColor, BoardFadedBorder, t * 0.85);
                        lineColor = LerpColor(BoardIdleLine, BoardFadedBorder, t);
                    }
                }

                (double X, double Y) target = (r.X, r.CenterY);
                if (chosen && stage == 2)
                {
                    // The taken path fills in from the die toward its card.
                    canvas.AddLine(junctionOut.X, junctionOut.Y, target.X, target.Y, BoardIdleLine, 1.5);
                    AddGrowingLine(junctionOut, target, OutcomeHalf(stage, rawP, second: true), lineColor, lineThickness);
                }
                else
                {
                    canvas.AddLine(junctionOut.X, junctionOut.Y, target.X, target.Y, lineColor, lineThickness);
                }

                AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);
                if (outcome == CombatSequenceSwingOutcome.Combo)
                {
                    int slot = (current + 1) % filled;
                    DrawActionCardContent(player, panelData[slot], actions[slot], slot, r.X, r.Y, r.W, r.H, mode);
                }
                else
                {
                    // Generic outcome: no action of its own, the combo just starts over.
                    bool faded = heroSwing != null && stage >= 2 && !chosen;
                    canvas.AddText(r.X + 2, r.Y + 1, Truncate(note, r.W - 3), faded ? labelColor : AsciiArtAssets.Colors.White);
                    canvas.AddText(r.X + 2, r.Y + 2, Truncate("combo resets to 1", r.W - 3), faded ? labelColor : AsciiArtAssets.Colors.Gray);
                }
                if (r.W > label.Length + 3)
                    canvas.AddText(r.X + 1, r.Y, $" {label} ", labelColor);
            }

            if (heroSwing != null && stage >= 3)
                RenderBoardResultLine(heroSwing, stage, p, nowR, x + w - nowR.X - 1);

            if (heroSwing == null && LiveSwingFor(enemy) is { } enemySwing)
                RenderBoardEnemySwing(enemySwing, enemyCard, enemyAnchor, x, y, w);
        }

        private void DrawBoardCard(Character player, List<Action> actions, List<ActionPanelInfo> panelData, int filled,
            int slot, Action? fallback, BoardRect r, Color border, int thickness, ActionStripDamageLineMode mode)
        {
            AddBoardBox(r.X, r.Y, r.W, r.H, border, BoardCardBackground, borderThicknessPixels: thickness);
            if (slot >= 0 && slot < filled)
                DrawActionCardContent(player, panelData[slot], actions[slot], slot, r.X, r.Y, r.W, r.H, mode);
            else
                canvas.AddText(r.X + 1, r.Y + 1, fallback != null ? Truncate(fallback.Name ?? "?", r.W - 2) : "-", AsciiArtAssets.Colors.Gray);
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

        /// <summary>
        /// ACTION onward, written as a result line under <paramref name="anchor"/>: "whiff" on a miss, the DEFENSE and DAMAGE
        /// math as each beat reveals, then EFFECTS typed out. The acting card carries the accent (<see cref="AccentForBeat"/>).
        /// </summary>
        private void RenderBoardResultLine(CombatSequenceSwingInfo swing, int stage, double p, BoardRect anchor, int maxW)
        {
            int tx = anchor.X + 1;
            int ty = anchor.Y + anchor.H;
            int w = Math.Max(4, maxW);
            var text = Flatten(CombatSequenceHudState.VisibleResult);
            if (swing.Outcome == CombatSequenceSwingOutcome.Miss && stage == 3)
                canvas.AddText(tx, ty, "whiff", AsciiArtAssets.Colors.Gray);
            else if ((stage == 4 || stage == 5) && text.Length > 0)
            {
                Color color = stage == 4 ? AsciiArtAssets.Colors.Cyan
                    : swing.Action.Type == ActionType.Heal ? BoardHealLine
                    : swing.IsCritical ? AsciiArtAssets.Colors.Gold : AsciiArtAssets.Colors.Red;
                canvas.AddText(tx, ty, Truncate(text, w), color);
            }
            else if (stage == 6 && text.Length > 0)
                TypeOut(text, p, tx, ty, w);
        }

        /// <summary>Types <paramref name="text"/> out over the first ~60% of the beat.</summary>
        private void TypeOut(string text, double p, int tx, int ty, int maxW)
        {
            int shown = (int)Math.Ceiling(text.Length * Math.Min(1.0, p * 1.6));
            if (shown > 0)
                canvas.AddText(tx, ty, Truncate(text.Substring(0, Math.Min(shown, text.Length)), maxW), AsciiArtAssets.Colors.Cyan);
        }

        /// <summary>
        /// Enemy swing over the resting hero cards: a line rises from the roll on the enemy d20 bar to the firing strip card, a d20 shows below it,
        /// the outcome stamps above the die, and the result line (whiff / defense / damage / effects) appears under it.
        /// The strip card itself carries the accent.
        /// </summary>
        private void RenderBoardEnemySwing(CombatSequenceSwingInfo swing, BoardRect? enemyCard, (double X, double Y) enemyAnchor, int x, int y, int w)
        {
            var (stage, p, raw) = LiveStage();
            (double X, double Y) cardBottom = enemyCard is { } card ? (card.CenterX, card.Bottom) : enemyAnchor;
            (double X, double Y) diePoint = (cardBottom.X, y + 1.5);

            if (stage < 1)
                return;
            // Their turn only: the roll's face on the enemy d20 bar -> the firing strip card.
            if (RollKnown(stage))
                AddGrowingLine(RollPoint(swing.Roll, hero: false), cardBottom, OutcomeHalf(stage, raw, second: false), AsciiArtAssets.Colors.Red, 2.0);
            var dieR = DrawBoardDie(diePoint, swing, stage, raw, x, w);
            if (stage >= 3)
                RenderBoardResultLine(swing, stage, p, dieR, x + w - dieR.X - 1);
        }

        /// <summary>
        /// After a hero swing: the branch taken slides into NOW (linked to the fired card), the fired card slides into PREV,
        /// and the old PREV fades out in place.
        /// The new branches appear once the slide lands.
        /// </summary>
        private void RenderBoardSlide(Character player, List<Action> actions, List<ActionPanelInfo> panelData, int filled,
            CombatSequenceSwingInfo swing, int fired, double s, BoardRect prevR, BoardRect curR, BoardRect[] branchR,
            ActionStripDamageLineMode mode)
        {
            int branchIndex = swing.Outcome switch
            {
                CombatSequenceSwingOutcome.Combo => 0,
                CombatSequenceSwingOutcome.Hit => 1,
                _ => 2
            };
            int incoming = player.ComboStep % filled;

            if (s < 0.5)
            {
                DrawBoardCard(player, actions, panelData, filled, FindSlot(actions, _boardSettledPrev), _boardSettledPrev, prevR,
                    LerpColor(BoardDimBorder, BoardFadedBorder, s * 2), 1, mode);
            }

            var firedR = BoardRect.Lerp(curR, prevR, s);
            DrawBoardCard(player, actions, panelData, filled, fired, swing.Action, firedR,
                LerpColor(OutcomeColor(swing.Outcome), BoardDimBorder, s), 1, mode);

            var inR = BoardRect.Lerp(branchR[branchIndex], curR, s);
            DrawBoardCard(player, actions, panelData, filled, incoming, null, inR,
                LerpColor(OutcomeColor(swing.Outcome), AsciiArtAssets.Colors.White, s), 2, mode);

            canvas.AddText(prevR.X + 1, prevR.Y - 1, "PREV", AsciiArtAssets.Colors.Gray);
            canvas.AddText(curR.X + 1, curR.Y - 1, $"NOW  {incoming + 1}/{filled}", AsciiArtAssets.Colors.White);
            // Potential -> action: the taken branch stays linked to the card that fired while it becomes NOW.
            if (inR.X > firedR.Right)
                canvas.AddLine(firedR.Right, firedR.CenterY, inR.X, inR.CenterY, OutcomeColor(swing.Outcome), 2.5);
        }

        private static string Truncate(string text, int max) =>
            max <= 3 || text.Length <= max ? text : text.Substring(0, max - 3) + "...";
    }
}
