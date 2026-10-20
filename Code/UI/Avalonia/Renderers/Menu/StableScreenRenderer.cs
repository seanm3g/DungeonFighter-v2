using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Full-screen Stable: title and summary on top, a grid of fighter cards (status lines, recent log, actions pinned to
    /// the bottom), an "add a hero" slot, and a clickable key bar along the bottom. The add-hero list replaces the grid.
    /// </summary>
    public class StableScreenRenderer
    {
        private const int Margin = 2;
        private const int MaxCardHeight = 22;

        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;

        public StableScreenRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }

        public void Render(StableMenuView view)
        {
            clickableElements.Clear();
            canvas.Clear();

            int width = Math.Max(60, LayoutConstants.RIGHT_PANEL_X + LayoutConstants.RIGHT_PANEL_WIDTH);
            int height = Math.Max(20, LayoutConstants.SCREEN_HEIGHT);

            // Header
            canvas.AddText(Margin, 1, view.Title, ColorPalette.Gold.GetColor());
            int summaryX = Margin + view.Title.Length + 4;
            canvas.AddText(summaryX, 1, Fit(view.Summary.Text, width - summaryX - Margin), ToneColor(view.Summary.Tone));
            if (view.Notice != null)
                canvas.AddText(Margin, 2, Fit(view.Notice.Text, width - 2 * Margin), ToneColor(view.Notice.Tone));

            int top = 4;
            int footerY = height - 2;
            int bottom = footerY - 2;

            if (view.Watch != null)
                RenderWatch(view.Watch, width, top, bottom);
            else if (view.PanelTitle != null)
                RenderPanel(view, width, top, bottom);
            else
                RenderGrid(view, width, top, bottom);

            RenderButtonRow(view.Footer, Margin, footerY, width - 2 * Margin, "   ");
            canvas.Refresh();
        }

        private void RenderGrid(StableMenuView view, int width, int top, int bottom)
        {
            int slots = view.Cards.Count + (view.ShowAddSlot ? 1 : 0);
            if (slots == 0)
                return;
            int cols = slots <= 1 ? 1 : slots <= 4 ? 2 : 3;
            int rows = (slots + cols - 1) / cols;
            int gridWidth = width - 2 * Margin;
            int cardWidth = cols == 1 ? Math.Min(gridWidth, gridWidth / 2) : (gridWidth - (cols - 1) * Margin) / cols;
            int cardHeight = Math.Min(MaxCardHeight, (bottom - top - (rows - 1)) / rows);
            if (cardHeight < 6)
                cardHeight = 6;

            for (int i = 0; i < slots; i++)
            {
                int x = Margin + (i % cols) * (cardWidth + Margin);
                int y = top + (i / cols) * (cardHeight + 1);
                if (y + cardHeight > bottom + 1)
                    break;
                if (i < view.Cards.Count)
                    RenderCard(view.Cards[i], x, y, cardWidth, cardHeight);
                else
                    RenderAddSlot(x, y, cardWidth, cardHeight);
            }
        }

        private void RenderCard(StableCardView card, int x, int y, int w, int h)
        {
            // Body click selects the card. Registered first: the last registered element wins, so buttons beat it.
            clickableElements.Add(new ClickableElement
            {
                X = x,
                Y = y,
                Width = w,
                Height = h,
                Type = ElementType.MenuOption,
                Value = card.SelectKey,
                DisplayText = card.Title
            });

            canvas.AddBorder(x, y, w, h, ToneColor(card.BorderTone), card.Selected ? 3 : card.NeedsDecision ? 2 : 1);

            int innerX = x + 2;
            int innerW = w - 4;
            int row = y + 1;
            canvas.AddText(innerX, row++, Fit(card.Title, innerW), card.Selected ? ToneColor(StableLineTone.Selected) : ToneColor(card.BorderTone == StableLineTone.Normal ? StableLineTone.Header : card.BorderTone));

            // Actions pinned to the bottom, flowed into rows.
            var actionRows = Flow(card.Actions, innerW, "   ");
            int actionTop = y + h - 1 - actionRows.Count;

            foreach (var line in card.Lines)
            {
                if (row >= actionTop)
                    break;
                canvas.AddText(innerX, row++, Fit(line.Text, innerW), ToneColor(line.Tone));
            }

            row++;
            foreach (var line in card.Log)
            {
                if (row >= actionTop - 1)
                    break;
                canvas.AddText(innerX, row++, Fit(line.Text, innerW), ToneColor(line.Tone));
            }

            for (int r = 0; r < actionRows.Count; r++)
                RenderButtons(actionRows[r], innerX, actionTop + r, "   ");
        }

        /// <summary>
        /// One fighter, full screen: hero / enemy / dungeon panels down the left, the live feed on the right,
        /// and its actions along the bottom.
        /// </summary>
        private void RenderWatch(StableWatchView watch, int width, int top, int bottom)
        {
            int gridWidth = width - 2 * Margin;
            int leftW = Math.Clamp(gridWidth * 2 / 5, 44, 70);
            int rightX = Margin + leftW + Margin;
            int rightW = width - Margin - rightX;

            var actionRows = Flow(watch.Actions, gridWidth - 4, "     ");
            int actionsH = actionRows.Count + 2;
            int panelsBottom = bottom - actionsH;

            int y = top;
            y = RenderBox(watch.Name, watch.NameTone, watch.Hero, Margin, y, leftW, 3) + 1;
            y = RenderBox("ENEMY", StableLineTone.Bad, watch.Enemy, Margin, y, leftW, 2) + 1;
            RenderBox("DUNGEON", StableLineTone.Header, watch.Dungeon, Margin, y, leftW, 2);

            // Live feed, newest at the bottom.
            int feedH = Math.Max(5, panelsBottom - top);
            canvas.AddBorder(rightX, top, rightW, feedH, ToneColor(StableLineTone.Dim), 1);
            canvas.AddText(rightX + 2, top + 1, "LIVE", ToneColor(StableLineTone.Header));
            int feedRows = feedH - 3;
            var feed = watch.Feed.Skip(Math.Max(0, watch.Feed.Count - feedRows)).ToList();
            for (int i = 0; i < feed.Count; i++)
                canvas.AddText(rightX + 2, top + 2 + i, Fit(feed[i].Text, rightW - 4), ToneColor(feed[i].Tone));

            // Actions bar
            int barY = panelsBottom + 1;
            canvas.AddBorder(Margin, barY, gridWidth, actionsH, ToneColor(watch.NameTone == StableLineTone.Alert ? StableLineTone.Alert : StableLineTone.Dim), watch.NameTone == StableLineTone.Alert ? 2 : 1);
            for (int r = 0; r < actionRows.Count; r++)
                RenderButtons(actionRows[r], Margin + 2, barY + 1 + r, "     ");
        }

        /// <summary>Titled box sized to its lines (at least <paramref name="minLines"/>); returns the row below it.</summary>
        private int RenderBox(string title, StableLineTone tone, List<StableLine> lines, int x, int y, int w, int minLines)
        {
            int h = Math.Max(minLines, lines.Count) + 3;
            canvas.AddBorder(x, y, w, h, ToneColor(tone), tone == StableLineTone.Alert ? 2 : 1);
            canvas.AddText(x + 2, y + 1, Fit(title, w - 4), ToneColor(tone));
            for (int i = 0; i < lines.Count; i++)
                canvas.AddText(x + 2, y + 2 + i, Fit(lines[i].Text, w - 4), ToneColor(lines[i].Tone));
            return y + h;
        }

        private void RenderAddSlot(int x, int y, int w, int h)
        {
            var dim = ToneColor(StableLineTone.Dim);
            canvas.AddBorder(x, y, w, h, dim, 1);
            int midY = y + h / 2 - 1;
            var buttons = new List<StableLine>
            {
                new("[A] Add a saved hero", StableLineTone.Option, "a"),
                new("[N] Create a new hero", StableLineTone.Option, "n")
            };
            for (int i = 0; i < buttons.Count; i++)
            {
                int textX = x + Math.Max(2, (w - buttons[i].Text.Length) / 2);
                RenderButtons(new List<StableLine> { buttons[i] }, textX, midY + i * 2, "");
            }
        }

        private void RenderPanel(StableMenuView view, int width, int top, int bottom)
        {
            int panelW = Math.Min(90, width - 8);
            int panelX = (width - panelW) / 2;
            int panelH = Math.Min(bottom - top + 1, view.Panel.Count + 4);
            canvas.AddBorder(panelX, top, panelW, panelH, ToneColor(StableLineTone.Header), 2);
            canvas.AddText(panelX + 2, top + 1, view.PanelTitle!, ToneColor(StableLineTone.Header));

            int row = top + 3;
            foreach (var line in view.Panel)
            {
                if (row >= top + panelH - 1)
                    break;
                if (line.Key != null)
                    RenderButtons(new List<StableLine> { line }, panelX + 2, row, "");
                else if (line.Text.Length > 0)
                    canvas.AddText(panelX + 2, row, Fit(line.Text, panelW - 4), ToneColor(line.Tone));
                row++;
            }
        }

        private void RenderButtonRow(List<StableLine> buttons, int x, int y, int maxWidth, string gap)
        {
            foreach (var rowButtons in Flow(buttons, maxWidth, gap).Take(1))
                RenderButtons(rowButtons, x, y, gap);
        }

        /// <summary>Draws buttons left to right; those with a key are clickable and highlight on hover.</summary>
        private void RenderButtons(List<StableLine> buttons, int x, int y, string gap)
        {
            int cursor = x;
            foreach (var b in buttons)
            {
                var color = ToneColor(b.Tone);
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
                    clickableElements.Add(element);
                    if (element.IsHovered)
                        color = ColorPalette.Yellow.GetColor();
                }
                canvas.AddText(cursor, y, b.Text, color);
                cursor += b.Text.Length + gap.Length;
            }
        }

        /// <summary>Packs buttons into rows no wider than <paramref name="maxWidth"/>.</summary>
        private static List<List<StableLine>> Flow(List<StableLine> buttons, int maxWidth, string gap)
        {
            var rows = new List<List<StableLine>>();
            var current = new List<StableLine>();
            int used = 0;
            foreach (var b in buttons)
            {
                var button = b.Text.Length > maxWidth ? new StableLine(Fit(b.Text, maxWidth), b.Tone, b.Key) : b;
                int need = button.Text.Length + (current.Count > 0 ? gap.Length : 0);
                if (current.Count > 0 && used + need > maxWidth)
                {
                    rows.Add(current);
                    current = new List<StableLine>();
                    used = 0;
                    need = button.Text.Length;
                }
                current.Add(button);
                used += need;
            }
            if (current.Count > 0)
                rows.Add(current);
            return rows;
        }

        private static string Fit(string text, int max)
        {
            if (max <= 1)
                return "";
            return text.Length <= max ? text : text.Substring(0, max - 1) + ".";
        }

        private static Color ToneColor(StableLineTone tone) => tone switch
        {
            StableLineTone.Dim => AsciiArtAssets.Colors.Gray,
            StableLineTone.Header => ColorPalette.Cyan.GetColor(),
            StableLineTone.Alert => ColorPalette.Gold.GetColor(),
            StableLineTone.Good => ColorPalette.Success.GetColor(),
            StableLineTone.Bad => ColorPalette.Orange.GetColor(),
            StableLineTone.Option => ColorLayerSystem.GetWhite(WhiteTemperature.Cool),
            StableLineTone.Selected => ColorPalette.Yellow.GetColor(),
            _ => ColorLayerSystem.GetWhite(WhiteTemperature.Neutral)
        };
    }
}
