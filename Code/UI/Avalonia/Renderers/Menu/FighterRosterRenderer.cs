using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Renderers.Helpers;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>One card on the fighter roster.</summary>
    public sealed class RosterCard
    {
        public string Key { get; init; } = "";
        /// <summary>Hotkey label on the first row, e.g. "[1]".</summary>
        public string Label { get; init; } = "";
        public string[] Face { get; init; } = Array.Empty<string>();
        public string Name { get; init; } = "";
        public string Sub { get; init; } = "";
        public string? Status { get; init; }
        public Color? StatusColor { get; init; }
        public Color Accent { get; init; }
        public Color? FaceColor { get; init; }
        public int Border { get; init; } = 1;
        public bool Selected { get; init; }
        /// <summary>When set, a small [x] in the top-right corner sends this key.</summary>
        public string? CloseKey { get; init; }
    }

    /// <summary>A line of the detail panel; lines with a key are clickable and flow side by side.</summary>
    public sealed record RosterLine(string Text, Color Color, string? Key = null);

    /// <summary>Hover / selection panel: portrait and title block, then body lines.</summary>
    public sealed class RosterDetail
    {
        public string[] Face { get; init; } = Array.Empty<string>();
        public Color FaceColor { get; init; }
        public string Title { get; init; } = "";
        public Color TitleColor { get; init; }
        /// <summary>Lines beside the portrait (class, level, ...).</summary>
        public List<RosterLine> Header { get; init; } = new();
        public List<RosterLine> Body { get; init; } = new();
        public Color BorderColor { get; init; }
    }

    /// <summary>Everything the roster screen shows; the main menu and the Stable grid each build one.</summary>
    public sealed class RosterScreen
    {
        public bool StableMode { get; init; }
        public string ToggleKey { get; init; } = "f";
        public RosterLine? Summary { get; init; }
        public RosterLine? Notice { get; init; }
        public List<RosterCard> Cards { get; init; } = new();
        /// <summary>Shown in the panel area when nothing is hovered or selected.</summary>
        public RosterDetail? Detail { get; init; }
        public List<RosterLine> Placeholder { get; init; } = new();
        /// <summary>Clickable key hints along the bottom left.</summary>
        public List<RosterLine> Footer { get; init; } = new();
        /// <summary>Bottom-right stack, top to bottom.</summary>
        public List<(string Label, string Key)> SideButtons { get; init; } = new();
    }

    /// <summary>
    /// Shared layout of the main menu and the Stable grid, so toggling Stable mode keeps the same screen: mode toggle
    /// top left, a row of fighter cards (new fighter first), a detail panel for the hovered card, and
    /// History / Settings / Quit bottom right.
    /// </summary>
    public sealed class FighterRosterRenderer
    {
        public const int CardW = 28;
        public const int CardH = 10;
        public const int Gap = 3;
        private const int SideW = 22;
        private const int MaxPanelW = 72;
        private const int MinPanelW = 40;

        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickables;

        public FighterRosterRenderer(GameCanvasControl canvas, List<ClickableElement> clickables)
        {
            this.canvas = canvas;
            this.clickables = clickables;
        }

        private static int Left(int x, int width) => x + Math.Max(2, width * 13 / 100);
        private static int Right(int x, int width) => x + width - Math.Max(2, width * 8 / 100);
        private static int PerRow(int x, int width) => Math.Max(2, (Right(x, width) - Left(x, width) + Gap) / (CardW + Gap));
        private static int MaxRows(int height) => height >= 40 ? 2 : 1;

        /// <summary>How many cards fit (the new-fighter card included).</summary>
        public static int CardCapacity(int x, int width, int height) => PerRow(x, width) * MaxRows(height);

        public void Render(int x, int y, int width, int height, RosterScreen screen)
        {
            int left = Left(x, width);
            int right = Right(x, width);
            int perRow = PerRow(x, width);
            int maxRows = MaxRows(height);
            var gold = ColorPalette.Gold.GetColor();

            // Mode toggle, top left: the same button flips between the roster and the Stable.
            int toggleY = y + Math.Max(1, height * 6 / 100);
            string toggleLabel = $"[{screen.ToggleKey.ToUpperInvariant()}] Stable mode: {(screen.StableMode ? "ON " : "off")}";
            int toggleW = toggleLabel.Length + 4;
            var toggleColor = screen.StableMode ? ColorPalette.Cyan.GetColor() : gold;
            BoxButton.Draw(canvas, clickables, left, toggleY, toggleW, 3, toggleLabel, screen.ToggleKey, toggleColor,
                screen.StableMode ? 2 : 1, hovered: IsOver(left, toggleY, toggleW, 3));
            int infoX = left + toggleW + 3;
            int infoW = Math.Max(1, right - infoX);
            if (screen.Summary != null)
                canvas.AddText(infoX, toggleY + 1, Fit(screen.Summary.Text, infoW), screen.Summary.Color);
            if (screen.Notice != null)
                canvas.AddText(infoX, toggleY + 2, Fit(screen.Notice.Text, infoW), screen.Notice.Color);

            // Cards
            int cardsTop = y + Math.Max(5, height * 24 / 100);
            int count = Math.Min(screen.Cards.Count, perRow * maxRows);
            for (int i = 0; i < count; i++)
                DrawCard(screen.Cards[i], left + (i % perRow) * (CardW + Gap), cardsTop + (i / perRow) * (CardH + 2));

            // Bottom right stack
            int sideX = right - SideW;
            int quitY = y + height - Math.Max(6, height * 14 / 100);
            for (int i = 0; i < screen.SideButtons.Count; i++)
            {
                var (label, key) = screen.SideButtons[i];
                int by = quitY - 4 * (screen.SideButtons.Count - 1 - i);
                BoxButton.Draw(canvas, clickables, sideX, by, SideW, 3, label, key, hovered: IsOver(sideX, by, SideW, 3));
            }

            // Footer hints along the bottom left, level with the last side button.
            int footerY = quitY + 1;
            if (screen.Footer.Count > 0)
                DrawFlow(screen.Footer, left, footerY, Math.Max(10, sideX - 3 - left), 1);

            // Detail panel: beside the last card when the row has room, else under the cards.
            int rowsUsed = Math.Max(1, (count + perRow - 1) / perRow);
            int lastRow = count % perRow;
            int panelX, panelY;
            int panelW;
            if (lastRow != 0 && Math.Min(MaxPanelW, sideX - 3 - (left + lastRow * (CardW + Gap))) >= MinPanelW)
            {
                panelX = left + lastRow * (CardW + Gap);
                panelY = cardsTop + (rowsUsed - 1) * (CardH + 2);
            }
            else
            {
                panelX = left;
                panelY = cardsTop + rowsUsed * (CardH + 2);
            }
            panelW = Math.Min(MaxPanelW, sideX - 3 - panelX);
            int panelBottom = (screen.Footer.Count > 0 ? footerY - 2 : y + height - 2);
            if (panelW < 24 || panelBottom - panelY < 6)
                return;

            if (screen.Detail != null)
                DrawDetail(screen.Detail, panelX, panelY, panelW, panelBottom);
            else
            {
                int row = panelY + 1;
                foreach (var line in screen.Placeholder)
                {
                    if (row > panelBottom)
                        break;
                    foreach (var wrapped in TextWrapper.WrapText(line.Text, panelW))
                    {
                        if (row > panelBottom)
                            break;
                        canvas.AddText(panelX, row++, wrapped, line.Color);
                    }
                }
            }
        }

        private void DrawCard(RosterCard card, int cx, int cy)
        {
            bool hovered = IsOver(cx, cy, CardW, CardH);
            var element = new ClickableElement
            {
                X = cx,
                Y = cy,
                Width = CardW,
                Height = CardH,
                Type = ElementType.MenuOption,
                Value = card.Key,
                DisplayText = card.Name,
                IsHovered = hovered
            };
            clickables.Add(element);

            var hover = BoxButton.HoverColor;
            var borderColor = hovered ? hover : card.Selected ? ColorPalette.Yellow.GetColor() : card.Accent;
            int thickness = card.Border + (hovered || card.Selected ? 1 : 0);
            canvas.AddBorder(cx, cy, CardW, CardH, borderColor, thickness);

            var lines = new List<(string text, Color color)>
            {
                (card.Label, hovered ? hover : card.Accent)
            };
            var faceColor = hovered ? hover : card.FaceColor ?? ColorLayerSystem.GetWhite(WhiteTemperature.Neutral);
            foreach (var f in card.Face)
                lines.Add((f, faceColor));
            lines.Add((card.Name, hovered ? hover : ColorLayerSystem.GetWhite(WhiteTemperature.Cool)));
            if (!string.IsNullOrEmpty(card.Sub))
                lines.Add((card.Sub, AsciiArtAssets.Colors.Gray));
            if (!string.IsNullOrEmpty(card.Status))
                lines.Add((card.Status!, card.StatusColor ?? AsciiArtAssets.Colors.Gray));

            int inner = CardH - 2;
            int textY = cy + 1 + Math.Max(0, (inner - lines.Count) / 2);
            for (int i = 0; i < lines.Count && i < inner; i++)
                Centered(cx, CardW - 2, textY + i, lines[i].text, lines[i].color);

            // Close button, top right; registered after the card so it wins the click.
            if (card.CloseKey != null)
            {
                var close = new ClickableElement
                {
                    X = cx + CardW - 5,
                    Y = cy + 1,
                    Width = 3,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = card.CloseKey,
                    DisplayText = "[x]"
                };
                close.IsHovered = FighterRosterHoverState.IsOver(close);
                clickables.Add(close);
                canvas.AddText(close.X, close.Y, "[x]", close.IsHovered ? ColorPalette.Orange.GetColor() : AsciiArtAssets.Colors.Gray);
            }
        }

        private void DrawDetail(RosterDetail d, int px, int py, int pw, int bottom)
        {
            int innerX = px + 2;
            int innerW = pw - 4;

            // Body flowed first so the panel can be sized to it.
            var bodyRows = new List<List<RosterLine>>();
            var keyed = new List<RosterLine>();
            void FlushKeyed()
            {
                if (keyed.Count == 0) return;
                bodyRows.AddRange(Flow(keyed, innerW, 3));
                keyed = new List<RosterLine>();
            }
            foreach (var line in d.Body)
            {
                if (line.Key != null)
                {
                    keyed.Add(line);
                    continue;
                }
                FlushKeyed();
                if (line.Text.Length == 0)
                    bodyRows.Add(new List<RosterLine> { line });
                else
                    foreach (var w in TextWrapper.WrapText(line.Text, innerW))
                        bodyRows.Add(new List<RosterLine> { line with { Text = w } });
            }
            FlushKeyed();

            const int portraitW = 11;
            const int portraitH = 5;
            int headerRows = Math.Max(portraitH, 1 + d.Header.Count);
            int wanted = 1 + headerRows + (bodyRows.Count > 0 ? 1 + bodyRows.Count : 0) + 1;
            int ph = Math.Min(wanted, bottom - py + 1);
            canvas.AddBorder(px, py, pw, ph, d.BorderColor, 1);

            // Portrait
            int top = py + 1;
            canvas.AddBorder(innerX, top, portraitW, portraitH, AsciiArtAssets.Colors.Gray, 1);
            for (int i = 0; i < d.Face.Length && i < portraitH - 2; i++)
                Centered(innerX, portraitW - 2, top + 1 + i, d.Face[i], d.FaceColor);

            // Title block beside it
            int tx = innerX + portraitW + 2;
            int tw = Math.Max(1, px + pw - 2 - tx);
            canvas.AddText(tx, top, Fit(d.Title, tw), d.TitleColor);
            for (int i = 0; i < d.Header.Count && top + 1 + i < py + ph - 1; i++)
                canvas.AddText(tx, top + 1 + i, Fit(d.Header[i].Text, tw), d.Header[i].Color);

            int row = top + headerRows + 1;
            foreach (var r in bodyRows)
            {
                if (row >= py + ph - 1)
                    break;
                DrawButtons(r, innerX, row++, 3);
            }
        }

        private void DrawFlow(List<RosterLine> items, int x, int y, int maxW, int maxRows)
        {
            var rows = Flow(items, maxW, 3);
            for (int i = 0; i < rows.Count && i < maxRows; i++)
                DrawButtons(rows[i], x, y + i, 3);
        }

        private void DrawButtons(List<RosterLine> items, int x, int y, int gap)
        {
            int cursor = x;
            foreach (var b in items)
            {
                var color = b.Color;
                if (b.Key != null)
                {
                    var element = new ClickableElement
                    {
                        X = cursor,
                        Y = y,
                        Width = Math.Max(1, b.Text.Length),
                        Height = 1,
                        Type = ElementType.MenuOption,
                        Value = b.Key,
                        DisplayText = b.Text
                    };
                    element.IsHovered = FighterRosterHoverState.IsOver(element);
                    clickables.Add(element);
                    if (element.IsHovered)
                        color = BoxButton.HoverColor;
                }
                canvas.AddText(cursor, y, b.Text, color);
                cursor += b.Text.Length + gap;
            }
        }

        private static List<List<RosterLine>> Flow(List<RosterLine> items, int maxW, int gap)
        {
            var rows = new List<List<RosterLine>>();
            var cur = new List<RosterLine>();
            int used = 0;
            foreach (var raw in items)
            {
                var b = raw.Text.Length > maxW ? raw with { Text = Fit(raw.Text, maxW) } : raw;
                int need = b.Text.Length + (cur.Count > 0 ? gap : 0);
                if (cur.Count > 0 && used + need > maxW)
                {
                    rows.Add(cur);
                    cur = new List<RosterLine>();
                    used = 0;
                    need = b.Text.Length;
                }
                cur.Add(b);
                used += need;
            }
            if (cur.Count > 0)
                rows.Add(cur);
            return rows;
        }

        private void Centered(int x, int innerW, int y, string text, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;
            text = Fit(text, innerW);
            canvas.AddText(x + 1 + Math.Max(0, (innerW - text.Length) / 2), y, text, color);
        }

        private static bool IsOver(int x, int y, int w, int h) =>
            FighterRosterHoverState.IsOver(new ClickableElement { X = x, Y = y, Width = w, Height = h });

        private static string Fit(string text, int max)
        {
            if (max <= 1)
                return "";
            return text.Length <= max ? text : text.Substring(0, max - 1) + ".";
        }
    }
}
