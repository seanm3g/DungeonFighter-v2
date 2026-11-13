using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>
    /// Esc menu over a dungeon run (combat included): Resume, Settings, Main Menu, Quit. Combat keeps running underneath.
    /// Painted by the canvas as its last layer (like <see cref="TutorialCoachPopup"/>) so combat repaints never wipe it.
    /// While open it owns every click and key; Esc or Resume closes it.
    /// </summary>
    public static class CombatEscapeMenu
    {
        public const string ResumeToken = "combat:esc:resume";
        public const string SettingsToken = "combat:esc:settings";
        public const string MainMenuToken = "combat:esc:mainmenu";
        public const string QuitToken = "combat:esc:quit";

        /// <summary>Menu rows: label and the input each sends (number keys 1-4 pick them too).</summary>
        public static readonly (string Label, string Token)[] Options =
        {
            ("Resume", ResumeToken),
            ("Settings", SettingsToken),
            ("Main Menu", MainMenuToken),
            ("Quit Game", QuitToken),
        };

        private const int BoxW = 34;
        private const int ButtonH = 3;

        private static readonly Color Gold = Color.FromRgb(255, 200, 40);
        private static readonly Color BoxFill = Color.FromRgb(12, 12, 18);
        private static readonly Color ButtonFill = Color.FromRgb(28, 28, 38);
        private static readonly Color TextColor = Color.FromRgb(230, 230, 230);
        private static readonly Color HintColor = Color.FromRgb(130, 130, 130);
        private static readonly Color Dim = Color.FromArgb(150, 0, 0, 0);

        private static volatile bool _isOpen;
        private static System.Action? _refresh;
        private static (int X, int Y)? _boxOrigin;

        public static bool IsOpen => _isOpen;

        /// <summary>Hooks repaint requests to the canvas (posted to the UI thread).</summary>
        public static void SetRefresh(System.Action? refresh) => _refresh = refresh;

        public static void SetOpen(bool open)
        {
            if (_isOpen == open)
                return;
            _isOpen = open;
            if (_refresh is { } r)
                Dispatcher.UIThread.Post(r, DispatcherPriority.Render);
        }

        public static void Toggle() => SetOpen(!_isOpen);

        /// <summary>Maps typed input (1-4 or a token) to a menu token, or null for anything else.</summary>
        public static string? TokenForInput(string input)
        {
            string s = (input ?? string.Empty).Trim();
            if (s.Length == 1 && char.IsDigit(s[0]))
            {
                int n = s[0] - '0';
                return n >= 1 && n <= Options.Length ? Options[n - 1].Token : null;
            }
            foreach (var (_, token) in Options)
                if (s == token)
                    return token;
            return null;
        }

        /// <summary>
        /// While open every click is the menu's; one on a button returns its token. False when the menu is closed.
        /// </summary>
        public static bool TryHandleClick(int gridX, int gridY, out string? token)
        {
            token = null;
            if (!_isOpen)
                return false;
            if (_boxOrigin is { } o)
            {
                for (int i = 0; i < Options.Length; i++)
                {
                    var (bx, by, bw, bh) = ButtonRect(o.X, o.Y, i);
                    if (gridX >= bx && gridX < bx + bw && gridY >= by && gridY < by + bh)
                    {
                        token = Options[i].Token;
                        break;
                    }
                }
            }
            return true;
        }

        private static int BoxH => 4 + Options.Length * (ButtonH + 1) + 1;

        private static (int X, int Y, int W, int H) ButtonRect(int boxX, int boxY, int index) =>
            (boxX + 3, boxY + 3 + index * (ButtonH + 1), BoxW - 6, ButtonH);

        public static void Paint(DrawingContext context, double contentWidth, double contentHeight, double charWidth, double charHeight,
            double fontSize)
        {
            _boxOrigin = null;
            if (!_isOpen || charWidth <= 0 || charHeight <= 0)
                return;

            int gridH = (int)(contentHeight / charHeight);
            int panelX = LayoutConstants.CENTER_PANEL_X;
            int panelW = LayoutConstants.CENTER_PANEL_WIDTH;
            if (panelW < BoxW || gridH < BoxH)
                return;

            Rect Cells(int x, int y, int w, int h) => new(x * charWidth, y * charHeight, w * charWidth, h * charHeight);

            context.FillRectangle(new SolidColorBrush(Dim), new Rect(0, 0, contentWidth, contentHeight));

            int bx = panelX + (panelW - BoxW) / 2;
            int by = (gridH - BoxH) / 2;
            _boxOrigin = (bx, by);

            context.FillRectangle(new SolidColorBrush(BoxFill), Cells(bx, by, BoxW, BoxH));
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Gold), 2), Cells(bx, by, BoxW, BoxH).Deflate(charWidth / 2));

            void Text(int x, int y, string text, Color color)
            {
                var brush = new SolidColorBrush(color);
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == ' ')
                        continue;
                    var glyph = new FormattedText(text[i].ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        GameFonts.ActiveTypeface, fontSize, brush);
                    context.DrawText(glyph, new Point((x + i) * charWidth, y * charHeight));
                }
            }

            const string title = "MENU";
            Text(bx + (BoxW - title.Length) / 2, by + 1, title, Gold);
            var buttonPen = new Pen(new SolidColorBrush(Gold), 1);
            for (int i = 0; i < Options.Length; i++)
            {
                var (x, y, w, h) = ButtonRect(bx, by, i);
                var rect = Cells(x, y, w, h).Deflate(charWidth / 4);
                context.FillRectangle(new SolidColorBrush(ButtonFill), rect);
                context.DrawRectangle(null, buttonPen, rect);
                string label = $"{i + 1}  {Options[i].Label}";
                Text(x + Math.Max(0, (w - label.Length) / 2), y + h / 2, label, TextColor);
            }
            const string hint = "Esc: resume";
            Text(bx + (BoxW - hint.Length) / 2, by + BoxH - 1, hint, HintColor);
        }
    }
}
