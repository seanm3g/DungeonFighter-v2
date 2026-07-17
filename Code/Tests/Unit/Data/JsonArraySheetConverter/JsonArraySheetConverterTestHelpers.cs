using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using System.Collections.Generic;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterTestHelpers
    {



        internal static string RowsToCsv(List<IList<object>> rows)
        {
            var lines = new List<string>();
            foreach (var row in rows)
            {
                var cells = new List<string>();
                foreach (var c in row)
                    cells.Add(EscapeCsvField(c?.ToString() ?? ""));
                lines.Add(string.Join(",", cells));
            }
            return string.Join("\n", lines);
        }


        internal static string EscapeCsvField(string s)
        {
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            return s;
        }
    }
}
