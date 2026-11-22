using System;
using System.Collections.Generic;

namespace RPGGame.UI
{
    /// <summary>Small text helpers for board callouts (NEW cards, Fernis's bubble).</summary>
    public static class BoardText
    {
        /// <summary>
        /// Word-wraps <paramref name="text"/> to <paramref name="width"/> columns, at most <paramref name="maxRows"/> rows;
        /// a word longer than a row is cut, and text past the last row ends that row with "...".
        /// </summary>
        public static List<string> Wrap(string? text, int width, int maxRows)
        {
            var rows = new List<string>();
            if (string.IsNullOrWhiteSpace(text) || width <= 0 || maxRows <= 0)
                return rows;
            string row = "";
            bool truncated = false;
            foreach (string raw in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string word = raw.Length > width ? raw[..width] : raw;
                if (row.Length == 0)
                    row = word;
                else if (row.Length + 1 + word.Length <= width)
                    row += " " + word;
                else
                {
                    rows.Add(row);
                    row = word;
                    if (rows.Count == maxRows)
                    {
                        truncated = true;
                        break;
                    }
                }
            }
            if (!truncated && row.Length > 0)
                rows.Add(row);
            if (truncated)
            {
                string last = rows[^1];
                rows[^1] = last.Length + 3 <= width ? last + "..." : last[..Math.Max(0, width - 3)] + "...";
            }
            return rows;
        }
    }
}
