using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ClassPresentationSheetConverterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ClassPresentationSheetConverter Tests ===\n");
            int run = 0, pass = 0, fail = 0;
            MergeIntoTuningReplacesClassPresentation(ref run, ref pass, ref fail);
            VerticalPropertyValueMergeIntoTuningRoundTrip(ref run, ref pass, ref fail);
            TestBase.PrintSummary("ClassPresentationSheetConverter Tests", run, pass, fail);
        }

        private static void MergeIntoTuningReplacesClassPresentation(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(MergeIntoTuningReplacesClassPresentation));
            string dir = Path.Combine(Path.GetTempPath(), "df_sheets_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string tuningPath = Path.Combine(dir, "TuningConfig.json");
                var root = new JsonObject
                {
                    ["character"] = new JsonObject { ["playerBaseHealth"] = 1 },
                    ["classPresentation"] = JsonSerializer.SerializeToNode(new ClassPresentationConfig
                    {
                        DefaultNoPointsClassName = "Old"
                    }.EnsureNormalized(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                };
                File.WriteAllText(tuningPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                var updated = new ClassPresentationConfig { DefaultNoPointsClassName = "NewFighter" }.EnsureNormalized();
                string minified = JsonSerializer.Serialize(updated, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                string csv = "field,jsonPayload\nclassPresentation,\"" + minified.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

                ClassPresentationSheetConverter.MergeClassPresentationFromCsvIntoTuningFile(csv, tuningPath);

                string after = File.ReadAllText(tuningPath);
                using var doc = JsonDocument.Parse(after);
                TestBase.AssertEqual(1, doc.RootElement.GetProperty("character").GetProperty("playerBaseHealth").GetInt32(), "other section", ref run, ref pass, ref fail);
                string d = doc.RootElement.GetProperty("classPresentation").GetProperty("defaultNoPointsClassName").GetString() ?? "";
                TestBase.AssertEqual("NewFighter", d, "classPresentation updated", ref run, ref pass, ref fail);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }
        }

        private static void VerticalPropertyValueMergeIntoTuningRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(VerticalPropertyValueMergeIntoTuningRoundTrip));
            string dir = Path.Combine(Path.GetTempPath(), "df_sheets_flat_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string tuningPath = Path.Combine(dir, "TuningConfig.json");
                var root = new JsonObject
                {
                    ["character"] = new JsonObject { ["playerBaseHealth"] = 7 },
                    ["classPresentation"] = JsonSerializer.SerializeToNode(
                        new ClassPresentationConfig { DefaultNoPointsClassName = "Seed" }.EnsureNormalized(),
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                };
                File.WriteAllText(tuningPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                var cfg = new ClassPresentationConfig
                {
                    DefaultNoPointsClassName = "Champion",
                    MaceClassDisplayName = "Brute",
                    SwordClassDisplayName = "Vanguard",
                    TierThresholds = new[] { 3, 30, 90, 200 },
                    DaggerClassDisplayName = "Stabber"
                }.EnsureNormalized();

                var rows = ClassPresentationSheetConverter.BuildPushValueRows(cfg);
                string csv = EncodeCsvFromRows(rows);

                ClassPresentationSheetConverter.MergeClassPresentationFromCsvIntoTuningFile(csv, tuningPath);

                string after = File.ReadAllText(tuningPath);
                using var doc = JsonDocument.Parse(after);
                TestBase.AssertEqual(7, doc.RootElement.GetProperty("character").GetProperty("playerBaseHealth").GetInt32(), "other section", ref run, ref pass, ref fail);
                var cp = doc.RootElement.GetProperty("classPresentation");
                TestBase.AssertEqual("Champion", cp.GetProperty("defaultNoPointsClassName").GetString() ?? "", "defaultNoPointsClassName", ref run, ref pass, ref fail);
                TestBase.AssertEqual("Brute", cp.GetProperty("maceClassDisplayName").GetString() ?? "", "maceClassDisplayName", ref run, ref pass, ref fail);
                TestBase.AssertEqual("Vanguard", cp.GetProperty("swordClassDisplayName").GetString() ?? "", "swordClassDisplayName", ref run, ref pass, ref fail);
                TestBase.AssertEqual(3, cp.GetProperty("tierThresholds")[0].GetInt32(), "tierThresholds[0]", ref run, ref pass, ref fail);
                TestBase.AssertEqual(30, cp.GetProperty("tierThresholds")[1].GetInt32(), "tierThresholds[1]", ref run, ref pass, ref fail);
                TestBase.AssertEqual("Stabber", cp.GetProperty("daggerClassDisplayName").GetString() ?? "", "daggerClassDisplayName", ref run, ref pass, ref fail);
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
                    line.Add(EscapeCsvCell(s));
                }

                sb.Append(string.Join(",", line));
                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static string EscapeCsvCell(string s)
        {
            if (s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 || (s.Length > 0 && (char.IsWhiteSpace(s[0]) || char.IsWhiteSpace(s[^1]))))
                return "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            return s;
        }
    }
}
