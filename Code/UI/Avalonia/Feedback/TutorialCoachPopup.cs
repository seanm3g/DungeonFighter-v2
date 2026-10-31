using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>
    /// Paints <see cref="TutorialCoach.Current"/>: the screen dims except the part the note points at (outlined in gold),
    /// and a framed box beside it holds the title, the wrapped text and a NEXT button. Clicking the box, Enter, Space or
    /// Right closes it (<see cref="TutorialCoach.Advance"/>); Esc skips the rest. The canvas paints it last.
    /// </summary>
    public static class TutorialCoachPopup
    {
        private const string NextLabel = "[ NEXT > ]";
        private const string SkipHint = "Esc: skip tutorial";
        private const int MaxBoxWidth = 62;

        private static readonly Color Gold = Color.FromRgb(255, 200, 40);
        private static readonly Color BoxFill = Color.FromRgb(12, 12, 18);
        private static readonly Color TextColor = Color.FromRgb(230, 230, 230);
        private static readonly Color HintColor = Color.FromRgb(130, 130, 130);
        private static readonly Color Dim = Color.FromArgb(150, 0, 0, 0);

        private static (int X, int Y, int W, int H)? _boxRect;

        /// <summary>Hooks the coach's repaint requests to the canvas (posted to the UI thread).</summary>
        public static void SetRefresh(System.Action? refresh) =>
            TutorialCoach.SetRequestInvalidate(refresh == null ? null : () => Dispatcher.UIThread.Post(refresh, DispatcherPriority.Render));

        /// <summary>
        /// While a popup is up every click is the popup's: one on the box presses NEXT, others are ignored so the board
        /// doesn't change under the note. Returns false when no popup is showing.
        /// </summary>
        public static bool TryHandleClick(int gridX, int gridY)
        {
            if (TutorialCoach.Current == null)
                return false;
            if (_boxRect is { } r && gridX >= r.X && gridX < r.X + r.W && gridY >= r.Y && gridY < r.Y + r.H)
                TutorialCoach.Advance();
            return true;
        }

        /// <summary>Where the note's focus sits on the grid now, or null when it isn't on screen.</summary>
        private static (int X, int Y, int W, int H)? FocusRect(TutorialCoachFocus focus)
        {
            bool board = CombatBoardLayout.IsActive;
            switch (focus)
            {
                case TutorialCoachFocus.None:
                    return null;
                case TutorialCoachFocus.Board:
                    var m = CombatBoardLayout.MiddleRect;
                    return board && m.Width > 0 && m.Height > 0 ? (m.X, m.Y, m.Width, m.Height) : null;
                case TutorialCoachFocus.HeroStrip:
                    int stripY = board ? LayoutConstants.ACTION_INFO_Y : 0;
                    return (LayoutConstants.ACTION_INFO_X, stripY, LayoutConstants.ACTION_INFO_WIDTH, LayoutConstants.ACTION_INFO_HEIGHT);
                case TutorialCoachFocus.EnemyStrip:
                case TutorialCoachFocus.Timeline:
                case TutorialCoachFocus.Sequence:
                case TutorialCoachFocus.TurnOrb:
                case TutorialCoachFocus.Die:
                case TutorialCoachFocus.Branches:
                case TutorialCoachFocus.ChosenBranch:
                    return board ? TutorialCoachTargets.Get(focus) : null;
                default:
                    return TutorialCoachTargets.Get(focus);
            }
        }

        public static void Paint(DrawingContext context, double contentWidth, double contentHeight, double charWidth, double charHeight,
            double fontSize)
        {
            _boxRect = null;
            if (TutorialCoach.Current is not { } popup || charWidth <= 0 || charHeight <= 0)
                return;

            int gridW = (int)(contentWidth / charWidth);
            int gridH = (int)(contentHeight / charHeight);
            int panelX = LayoutConstants.CENTER_PANEL_X;
            int panelW = LayoutConstants.CENTER_PANEL_WIDTH;
            if (panelW < 24 || gridH < 10)
                return;

            var focus = FocusRect(popup.Note.Focus);
            var brush = new SolidColorBrush(Dim);
            Rect Cells(int x, int y, int w, int h) => new(x * charWidth, y * charHeight, w * charWidth, h * charHeight);

            // Dim everything but the focus.
            if (focus is { } f)
            {
                context.FillRectangle(brush, new Rect(0, 0, contentWidth, Math.Max(0, f.Y * charHeight)));
                context.FillRectangle(brush, new Rect(0, (f.Y + f.H) * charHeight, contentWidth, Math.Max(0, contentHeight - (f.Y + f.H) * charHeight)));
                context.FillRectangle(brush, new Rect(0, f.Y * charHeight, Math.Max(0, f.X * charWidth), f.H * charHeight));
                context.FillRectangle(brush, new Rect((f.X + f.W) * charWidth, f.Y * charHeight,
                    Math.Max(0, contentWidth - (f.X + f.W) * charWidth), f.H * charHeight));
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Gold), 2), Cells(f.X, f.Y, f.W, f.H).Deflate(1));
            }
            else
                context.FillRectangle(brush, new Rect(0, 0, contentWidth, contentHeight));

            // The box: centered in the center column, on whichever side of the focus has room.
            int boxW = Math.Min(MaxBoxWidth, panelW - 4);
            int innerW = boxW - 4;
            var lines = Wrap(popup.Text, innerW);
            int boxH = lines.Count + 6; // frame, title, gap, text, gap, button row, frame
            int bx = panelX + (panelW - boxW) / 2;
            int by = (gridH - boxH) / 2;
            if (focus is { } fr)
            {
                bool leftPanel = fr.X + fr.W <= panelX;
                if (leftPanel)
                {
                    by = fr.Y;
                    bx = panelX + 2;
                }
                else if (fr.Y + fr.H / 2 < gridH / 2)
                    by = fr.Y + fr.H + 1;
                else
                    by = fr.Y - boxH - 1;
                // A focus that fills the column (the board middle) leaves no side free: sit over the top strips instead.
                if (by < 0 || by + boxH > gridH)
                    by = 0;
            }
            by = Math.Clamp(by, 0, Math.Max(0, gridH - boxH));
            _boxRect = (bx, by, boxW, boxH);

            context.FillRectangle(new SolidColorBrush(BoxFill), Cells(bx, by, boxW, boxH));
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Gold), 2), Cells(bx, by, boxW, boxH).Deflate(charWidth / 2));

            void Text(int x, int y, string text, Color color)
            {
                var textBrush = new SolidColorBrush(color);
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == ' ')
                        continue;
                    var glyph = new FormattedText(text[i].ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        GameFonts.ActiveTypeface, fontSize, textBrush);
                    context.DrawText(glyph, new Point((x + i) * charWidth, y * charHeight));
                }
            }

            int tx = bx + 2;
            Text(tx, by + 1, Truncate(popup.Title, innerW - 6), Gold);
            string number = popup.Number.ToString(CultureInfo.InvariantCulture);
            Text(bx + boxW - 2 - number.Length, by + 1, number, HintColor);
            for (int i = 0; i < lines.Count; i++)
                Text(tx, by + 3 + i, lines[i], TextColor);

            int buttonY = by + boxH - 2;
            int buttonX = bx + boxW - 2 - NextLabel.Length;
            context.FillRectangle(new SolidColorBrush(Gold), Cells(buttonX, buttonY, NextLabel.Length, 1));
            Text(buttonX, buttonY, NextLabel, BoxFill);
            if (SkipHint.Length + 2 <= buttonX - tx)
                Text(tx, buttonY, SkipHint, HintColor);
        }

        /// <summary>Word-wraps <paramref name="text"/> to <paramref name="width"/> columns.</summary>
        internal static List<string> Wrap(string text, int width)
        {
            var lines = new List<string>();
            if (width < 4)
                return lines;
            foreach (string paragraph in (text ?? string.Empty).Split('\n'))
            {
                string line = "";
                foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string w = word.Length > width ? word.Substring(0, width) : word;
                    if (line.Length == 0)
                        line = w;
                    else if (line.Length + 1 + w.Length <= width)
                        line += " " + w;
                    else
                    {
                        lines.Add(line);
                        line = w;
                    }
                }
                lines.Add(line);
            }
            return lines;
        }

        private static string Truncate(string text, int max) =>
            max <= 0 ? string.Empty : text.Length <= max ? text : text.Substring(0, max);
    }
}
