using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterMaterialBuildsTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            MaterialBuildsCsvHumanHeadersToCanonicalJson(ref run, ref pass, ref fail);
            MaterialBuildsJsonPushCsvRoundTrip(ref run, ref pass, ref fail);
            MaterialBuildsCanonicalizesMithirlAndDamascus(ref run, ref pass, ref fail);
        }

        private static void MaterialBuildsCsvHumanHeadersToCanonicalJson(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(MaterialBuildsCsvHumanHeadersToCanonicalJson));
            const string csv = """
            CLASS,MATERIAL,SYNTHESIS,CONVERT,FEED,2 STACK,3 STACK,5 STACK
            BARBARIAN,Iron,ON_ENEMY_HEALTH_THRESHOLD = CRISIS,IRON CULL,+/x per IRON,SYNTHESIS + CONVERT UNLOCK,+ feed,x feed
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.MaterialBuilds);
            using var a = JsonDocument.Parse(outJson);
            var row = a.RootElement[0];
            TestBase.AssertEqual("BARBARIAN", row.GetProperty("class").GetString(), "class", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Iron", row.GetProperty("material").GetString(), "material", ref run, ref pass, ref fail);
            TestBase.AssertEqual("ON_ENEMY_HEALTH_THRESHOLD = CRISIS", row.GetProperty("synthesis").GetString(), "synthesis", ref run, ref pass, ref fail);
            TestBase.AssertEqual("IRON CULL", row.GetProperty("convertAction").GetString(), "convertAction", ref run, ref pass, ref fail);
            TestBase.AssertEqual("+/x per IRON", row.GetProperty("feed").GetString(), "feed", ref run, ref pass, ref fail);
        }

        private static void MaterialBuildsJsonPushCsvRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(MaterialBuildsJsonPushCsvRoundTrip));
            const string json = """
            [
              {"class":"WARRIOR","material":"Gold","synthesis":"ON_SLOW = DRAG","convertAction":"GOLD ANCHOR","feed":"+/x per MITHRIL","stack2":"SYNTHESIS + CONVERT UNLOCK","stack3":"+ feed","stack5":"x feed"}
            ]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.MaterialBuilds);
            string csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.MaterialBuilds);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Gold", a.RootElement[0].GetProperty("material").GetString(), "material", ref run, ref pass, ref fail);
            TestBase.AssertEqual("GOLD ANCHOR", a.RootElement[0].GetProperty("convertAction").GetString(), "convertAction", ref run, ref pass, ref fail);
            TestBase.AssertEqual("+/x per MITHRIL", a.RootElement[0].GetProperty("feed").GetString(), "feed", ref run, ref pass, ref fail);
        }

        private static void MaterialBuildsCanonicalizesMithirlAndDamascus(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(MaterialBuildsCanonicalizesMithirlAndDamascus));
            const string csv = """
            CLASS,MATERIAL,SYNTHESIS,CONVERT,FEED,2 STACK,3 STACK,5 STACK
            WARRIOR,MITHIRL,ON_HIT = X,FLOW,+/x per GOLD,a,b,c
            BARBARIAN,Damascus,ON_HIT = Y,CULL,+/x per IRON,a,b,c
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.MaterialBuilds);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Mithril", a.RootElement[0].GetProperty("material").GetString(), "MITHIRL→Mithril", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Iron", a.RootElement[1].GetProperty("material").GetString(), "Damascus→Iron", ref run, ref pass, ref fail);
        }
    }
}
