using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterDungeonsTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            DungeonsRoundTrip(ref run, ref pass, ref fail);
            DungeonsPossibleEnemiesPipePushFormat(ref run, ref pass, ref fail);
            DungeonsPossibleEnemiesPipeDelimitedNormalized(ref run, ref pass, ref fail);
        }



        private static void DungeonsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(DungeonsRoundTrip));
            const string json = """
            [{"name":"Ancient Forest","theme":"Forest","minLevel":1,"maxLevel":10,"possibleEnemies":["Goblin","Wolf"]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Dungeons);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Dungeons);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Ancient Forest", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Forest", first.GetProperty("theme").GetString(), "theme", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, first.GetProperty("minLevel").GetInt32(), "minLevel", ref run, ref pass, ref fail);
            var pe = first.GetProperty("possibleEnemies");
            TestBase.AssertTrue(pe.ValueKind == JsonValueKind.Array, "possibleEnemies array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Goblin", pe[0].GetString(), "e0", ref run, ref pass, ref fail);
        }


        private static void DungeonsPossibleEnemiesPipePushFormat(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(DungeonsPossibleEnemiesPipePushFormat));
            const string json = """
            [{"name":"Ancient Forest","theme":"Forest","minLevel":1,"maxLevel":10,"possibleEnemies":["Goblin","Wolf"]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Dungeons);
            TestBase.AssertEqual(SheetConverter.DungeonsCanonicalHeaders.Length, rows[0].Count,
                "six canonical columns only", ref run, ref pass, ref fail);
            int peIdx = Array.IndexOf(SheetConverter.DungeonsCanonicalHeaders, "possibleEnemies");
            TestBase.AssertEqual("Goblin|Wolf", rows[1][peIdx]?.ToString(), "possibleEnemies pipe cell", ref run, ref pass, ref fail);
        }


        private static void DungeonsPossibleEnemiesPipeDelimitedNormalized(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(DungeonsPossibleEnemiesPipeDelimitedNormalized));
            const string csv = """
            name,theme,minLevel,maxLevel,possibleEnemies,colorOverride
            Haunted Crypt,Crypt,1,100,Skeleton|Zombie|Wraith,
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Dungeons);
            using var a = JsonDocument.Parse(outJson);
            var pe = a.RootElement[0].GetProperty("possibleEnemies");
            TestBase.AssertTrue(pe.ValueKind == JsonValueKind.Array, "array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Skeleton", pe[0].GetString(), "e0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wraith", pe[2].GetString(), "e2", ref run, ref pass, ref fail);
        }
    }
}
