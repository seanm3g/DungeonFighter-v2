using System;
using System.Linq;
using System.Text.Json;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class JsonArraySheetConverterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== JsonArraySheetConverter Tests ===\n");
            int run = 0, pass = 0, fail = 0;
            WeaponsRoundTrip(ref run, ref pass, ref fail);
            WeaponsWithNestedRequirements(ref run, ref pass, ref fail);
            ModificationsRoundTrip(ref run, ref pass, ref fail);
            EnemiesRoundTrip(ref run, ref pass, ref fail);
            EnvironmentsRoundTrip(ref run, ref pass, ref fail);
            TestBase.PrintSummary("JsonArraySheetConverter Tests", run, pass, fail);
        }

        private static void WeaponsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsRoundTrip));
            const string json = """
            [
              {"type":"Dagger","name":"Test","baseDamage":2,"attackSpeed":1.1,"tier":1,"attributeRequirements":null}
            ]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons);
            TestBase.AssertTrue(rows.Count >= 2, "header+data", ref run, ref pass, ref fail);
            string header = string.Join(",", rows[0].Select(o => o?.ToString() ?? ""));
            TestBase.AssertTrue(header.Contains("type", StringComparison.Ordinal), "has type col", ref run, ref pass, ref fail);

            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertTrue(a.RootElement.ValueKind == JsonValueKind.Array, "array", ref run, ref pass, ref fail);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Dagger", first.GetProperty("type").GetString(), "type", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, first.GetProperty("baseDamage").GetInt32(), "baseDamage", ref run, ref pass, ref fail);
        }

        private static void WeaponsWithNestedRequirements(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsWithNestedRequirements));
            const string json = """
            [{"type":"Sword","name":"Req","baseDamage":3,"attackSpeed":1,"tier":2,"attributeRequirements":{"strength":5}}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            var req = a.RootElement[0].GetProperty("attributeRequirements");
            TestBase.AssertTrue(req.ValueKind == JsonValueKind.Object, "req object", ref run, ref pass, ref fail);
            TestBase.AssertEqual(5, req.GetProperty("strength").GetInt32(), "str", ref run, ref pass, ref fail);
        }

        private static void ModificationsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsRoundTrip));
            const string json = """
            [{"DiceResult":1,"ItemRank":"Common","Name":"Worn","Description":"x","Effect":"damage","MinValue":-3,"MaxValue":-1}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Modifications);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Modifications);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Worn", a.RootElement[0].GetProperty("Name").GetString(), "Name", ref run, ref pass, ref fail);
            TestBase.AssertEqual(-3, a.RootElement[0].GetProperty("MinValue").GetDouble(), "Min", ref run, ref pass, ref fail);
        }

        private static void EnemiesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesRoundTrip));
            const string json = """
            [{"name":"Goblin","archetype":"Assassin","overrides":{"health":0.85,"agility":1.2},"actions":["JAB","TAUNT"],"isLiving":true,"description":"A goblin"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Goblin", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.85, first.GetProperty("overrides").GetProperty("health").GetDouble(), "override health", ref run, ref pass, ref fail);
            TestBase.AssertEqual("JAB", first.GetProperty("actions")[0].GetString(), "action0", ref run, ref pass, ref fail);
        }

        private static void EnvironmentsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnvironmentsRoundTrip));
            const string json = """
            [{"name":"Entrance","description":"Big door.","theme":"Generic","isHostile":false,"actions":[{"name":"Magical Barrier","weight":1}]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Environments);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Environments);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Entrance", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Magical Barrier", first.GetProperty("actions")[0].GetProperty("name").GetString(), "action name", ref run, ref pass, ref fail);
        }

        private static string RowsToCsv(System.Collections.Generic.List<System.Collections.Generic.IList<object>> rows)
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var row in rows)
            {
                var cells = new System.Collections.Generic.List<string>();
                foreach (var c in row)
                    cells.Add(EscapeCsvField(c?.ToString() ?? ""));
                lines.Add(string.Join(",", cells));
            }
            return string.Join("\n", lines);
        }

        private static string EscapeCsvField(string s)
        {
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            return s;
        }
    }
}
