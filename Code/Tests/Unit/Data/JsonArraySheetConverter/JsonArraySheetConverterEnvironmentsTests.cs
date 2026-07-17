using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterEnvironmentsTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            EnvironmentsRoundTrip(ref run, ref pass, ref fail);
            EnvironmentsLegacyLocationPush(ref run, ref pass, ref fail);
            EnvironmentsSheetColumnsPull(ref run, ref pass, ref fail);
            EnvironmentsWithEnemiesRoundTrip(ref run, ref pass, ref fail);
        }




        private static void EnvironmentsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnvironmentsRoundTrip));
            const string json = """
            [{"region":"north","biome":"Forest","location":"Entrance","tags":["overgrown"],"description":"Big door.","actions":[{"name":"Magical Barrier","weight":1}]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Environments);
            TestBase.AssertEqual(7, rows[0].Count, "seven canonical columns only", ref run, ref pass, ref fail);
            TestBase.AssertEqual("region", rows[0][0]?.ToString(), "header region", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Magical Barrier", rows[1][5]?.ToString(), "actions pipe cell", ref run, ref pass, ref fail);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Environments);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Entrance", first.GetProperty("location").GetString(), "location", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Forest", first.GetProperty("biome").GetString(), "biome", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Magical Barrier", first.GetProperty("actions")[0].GetProperty("name").GetString(), "action name", ref run, ref pass, ref fail);
        }


        private static void EnvironmentsLegacyLocationPush(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnvironmentsLegacyLocationPush));
            const string json = """
            [{"Location":"Entrance","description":"Big door.","actions":[{"name":"Magical Barrier","weight":1},{"name":"Trap Spring","weight":0.2}]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Environments);
            TestBase.AssertEqual(7, rows[0].Count, "no extra Location column", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Entrance", rows[1][2]?.ToString(), "legacy Location -> location column", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Magical Barrier:1|Trap Spring:0.2", rows[1][5]?.ToString(), "weighted actions cell", ref run, ref pass, ref fail);
        }


        private static void EnvironmentsSheetColumnsPull(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnvironmentsSheetColumnsPull));
            const string csv = """
            Region,Biome,Location,Tags,description,actions,enemies
            north,Forest,Entrance,"fire, scorched",Big door.,Magical Barrier|Trap Spring,
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Environments);
            using var doc = JsonDocument.Parse(outJson);
            var first = doc.RootElement[0];
            TestBase.AssertEqual("Entrance", first.GetProperty("location").GetString(), "location from sheet", ref run, ref pass, ref fail);
            TestBase.AssertEqual("fire", first.GetProperty("tags")[0].GetString(), "tag0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Trap Spring", first.GetProperty("actions")[1].GetProperty("name").GetString(), "action1", ref run, ref pass, ref fail);
        }


        private static void EnvironmentsWithEnemiesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnvironmentsWithEnemiesRoundTrip));
            const string json = """
            [{"region":"","biome":"Forest","location":"Wolf Den","description":"Den.","actions":[{"name":"Trap Spring","weight":1}],"enemies":[{"name":"Wolf","weight":0.7},{"name":"Spider","weight":0.3}]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Environments);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Environments);
            using var a = JsonDocument.Parse(outJson);
            var enemies = a.RootElement[0].GetProperty("enemies");
            TestBase.AssertTrue(enemies.ValueKind == JsonValueKind.Array, "enemies array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wolf", enemies[0].GetProperty("name").GetString(), "enemy0", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.7, enemies[0].GetProperty("weight").GetDouble(), "w0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wolf:0.7|Spider:0.3", rows[1][6]?.ToString(), "weighted enemies push cell", ref run, ref pass, ref fail);
        }
    }
}
