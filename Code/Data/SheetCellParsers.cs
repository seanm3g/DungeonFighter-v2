using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    /// <summary>CSV cell → JSON node parsing for sheet import.</summary>
    internal static class SheetCellParsers
    {
        internal static JsonNode? CellToJsonNode(string cell)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return null;

            cell = cell.Trim();
            if (cell.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            if (cell.Length > 0 && (cell[0] == '{' || cell[0] == '['))
            {
                try
                {
                    return JsonNode.Parse(cell);
                }
                catch
                {
                    return JsonValue.Create(cell);
                }
            }

            if (bool.TryParse(cell, out bool b))
                return JsonValue.Create(b);

            if (double.TryParse(cell, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double d))
            {
                if (d >= long.MinValue && d <= long.MaxValue && Math.Abs(d - (long)d) < 1e-9)
                    return JsonValue.Create((long)d);
                return JsonValue.Create(d);
            }

            return JsonValue.Create(cell);
        }
    }
}
