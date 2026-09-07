using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class VariablesSheetConverterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== VariablesSheetConverter Tests ===\n");
            int run = 0, pass = 0, fail = 0;
            FlattenSkipsClassPresentation(ref run, ref pass, ref fail);
            RoundTripMergeUpdatesLeafPreservesClassPresentation(ref run, ref pass, ref fail);
            MergeCreatesMissingNestedPath(ref run, ref pass, ref fail);
            MergeIgnoresClassPresentationRows(ref run, ref pass, ref fail);
            IndexedArrayLeavesRoundTrip(ref run, ref pass, ref fail);
            TestBase.PrintSummary("VariablesSheetConverter Tests", run, pass, fail);
        }

        private static void FlattenSkipsClassPresentation(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(FlattenSkipsClassPresentation));
            var root = new JsonObject
            {
                ["character"] = new JsonObject { ["playerBaseHealth"] = 100 },
                ["classPresentation"] = new JsonObject { ["defaultNoPointsClassName"] = "Fighter" }
            };
            var rows = VariablesSheetConverter.BuildPushValueRows(root);
            TestBase.AssertEqual(2, rows.Count, "header + 1 leaf", ref run, ref pass, ref fail);
            TestBase.AssertEqual("property", rows[0][0]?.ToString() ?? "", "header property", ref run, ref pass, ref fail);
            TestBase.AssertEqual("character.playerBaseHealth", rows[1][0]?.ToString() ?? "", "path", ref run, ref pass, ref fail);
            TestBase.AssertEqual("100", rows[1][1]?.ToString() ?? "", "value", ref run, ref pass, ref fail);
        }

        private static void RoundTripMergeUpdatesLeafPreservesClassPresentation(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(RoundTripMergeUpdatesLeafPreservesClassPresentation));
            string dir = Path.Combine(Path.GetTempPath(), "df_vars_rt_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string tuningPath = Path.Combine(dir, "TuningConfig.json");
                var root = new JsonObject
                {
                    ["character"] = new JsonObject { ["playerBaseHealth"] = 50 },
                    ["lootSystem"] = new JsonObject { ["magicFindEffectiveness"] = 1.0 },
                    ["classPresentation"] = new JsonObject { ["defaultNoPointsClassName"] = "KeepMe" }
                };
                File.WriteAllText(tuningPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                var pushRows = VariablesSheetConverter.BuildPushValueRowsFromJsonText(File.ReadAllText(tuningPath));
                // Bump health on the sheet rows
                for (int i = 1; i < pushRows.Count; i++)
                {
                    if (string.Equals(pushRows[i][0]?.ToString(), "character.playerBaseHealth", StringComparison.Ordinal))
                        pushRows[i][1] = "99";
                }

                string csv = EncodeCsvFromRows(pushRows);
                VariablesSheetConverter.MergeVariablesFromCsvIntoTuningFile(csv, tuningPath);

                using var doc = JsonDocument.Parse(File.ReadAllText(tuningPath));
                TestBase.AssertEqual(99, doc.RootElement.GetProperty("character").GetProperty("playerBaseHealth").GetInt32(), "health updated", ref run, ref pass, ref fail);
                TestBase.AssertEqual(1.0, doc.RootElement.GetProperty("lootSystem").GetProperty("magicFindEffectiveness").GetDouble(), "sibling preserved", ref run, ref pass, ref fail);
                TestBase.AssertEqual("KeepMe",
                    doc.RootElement.GetProperty("classPresentation").GetProperty("defaultNoPointsClassName").GetString() ?? "",
                    "classPresentation untouched", ref run, ref pass, ref fail);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }
        }

        private static void MergeCreatesMissingNestedPath(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(MergeCreatesMissingNestedPath));
            string dir = Path.Combine(Path.GetTempPath(), "df_vars_new_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string tuningPath = Path.Combine(dir, "TuningConfig.json");
                File.WriteAllText(tuningPath, """{"character":{"playerBaseHealth":1}}""");
                const string csv = "property,value\nlootSystem.dropChancePerLevel,0.25\n";
                VariablesSheetConverter.MergeVariablesFromCsvIntoTuningFile(csv, tuningPath);
                using var doc = JsonDocument.Parse(File.ReadAllText(tuningPath));
                TestBase.AssertEqual(1, doc.RootElement.GetProperty("character").GetProperty("playerBaseHealth").GetInt32(), "existing preserved", ref run, ref pass, ref fail);
                TestBase.AssertEqual(0.25, doc.RootElement.GetProperty("lootSystem").GetProperty("dropChancePerLevel").GetDouble(), "new leaf", ref run, ref pass, ref fail);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }
        }

        private static void MergeIgnoresClassPresentationRows(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(MergeIgnoresClassPresentationRows));
            string dir = Path.Combine(Path.GetTempPath(), "df_vars_skip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string tuningPath = Path.Combine(dir, "TuningConfig.json");
                File.WriteAllText(tuningPath, """
                {
                  "character": { "playerBaseHealth": 10 },
                  "classPresentation": { "defaultNoPointsClassName": "Original" }
                }
                """);
                const string csv = """
                property,value
                classPresentation.defaultNoPointsClassName,Hacked
                character.playerBaseHealth,20
                """;
                VariablesSheetConverter.MergeVariablesFromCsvIntoTuningFile(csv, tuningPath);
                using var doc = JsonDocument.Parse(File.ReadAllText(tuningPath));
                TestBase.AssertEqual(20, doc.RootElement.GetProperty("character").GetProperty("playerBaseHealth").GetInt32(), "health applied", ref run, ref pass, ref fail);
                TestBase.AssertEqual("Original",
                    doc.RootElement.GetProperty("classPresentation").GetProperty("defaultNoPointsClassName").GetString() ?? "",
                    "classPresentation ignored", ref run, ref pass, ref fail);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }
        }

        private static void IndexedArrayLeavesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(IndexedArrayLeavesRoundTrip));
            string dir = Path.Combine(Path.GetTempPath(), "df_vars_arr_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string tuningPath = Path.Combine(dir, "TuningConfig.json");
                var root = new JsonObject
                {
                    ["progression"] = new JsonObject
                    {
                        ["thresholds"] = new JsonArray { 1, 2, 3 }
                    }
                };
                File.WriteAllText(tuningPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                var rows = VariablesSheetConverter.BuildPushValueRowsFromJsonText(File.ReadAllText(tuningPath));
                TestBase.AssertTrue(rows.Count >= 4, "3 array leaves + header", ref run, ref pass, ref fail);

                for (int i = 1; i < rows.Count; i++)
                {
                    if (string.Equals(rows[i][0]?.ToString(), "progression.thresholds.1", StringComparison.Ordinal))
                        rows[i][1] = "42";
                }

                VariablesSheetConverter.MergeVariablesFromCsvIntoTuningFile(EncodeCsvFromRows(rows), tuningPath);
                using var doc = JsonDocument.Parse(File.ReadAllText(tuningPath));
                var arr = doc.RootElement.GetProperty("progression").GetProperty("thresholds");
                TestBase.AssertEqual(1, arr[0].GetInt32(), "index 0", ref run, ref pass, ref fail);
                TestBase.AssertEqual(42, arr[1].GetInt32(), "index 1 updated", ref run, ref pass, ref fail);
                TestBase.AssertEqual(3, arr[2].GetInt32(), "index 2", ref run, ref pass, ref fail);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }
        }

        private static string EncodeCsvFromRows(List<IList<object>> rows)
        {
            var sb = new StringBuilder();
            for (int r = 0; r < rows.Count; r++)
            {
                var line = new List<string>();
                foreach (object? cell in rows[r])
                {
                    string s = cell?.ToString() ?? "";
                    if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
                        s = "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
                    line.Add(s);
                }

                sb.Append(string.Join(",", line));
                if (r < rows.Count - 1)
                    sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}
