using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterConsumablesTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            ConsumablesCsvHumanHeadersToCanonicalJson(ref run, ref pass, ref fail);
            ConsumablesJsonPushCsvRoundTrip(ref run, ref pass, ref fail);
        }



        private static void ConsumablesCsvHumanHeadersToCanonicalJson(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ConsumablesCsvHumanHeadersToCanonicalJson));
            const string csv = """
            Display name,Internal kind,Effect (dungeon-scoped until run ends),Typical potency*
            Waxed Apple,Food,heal,1
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Consumables);
            using var a = JsonDocument.Parse(outJson);
            var row = a.RootElement[0];
            TestBase.AssertEqual("Waxed Apple", row.GetProperty("displayName").GetString(), "displayName", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Food", row.GetProperty("internalKind").GetString(), "internalKind", ref run, ref pass, ref fail);
            var potency = row.GetProperty("potency");
            bool potencyOk = potency.ValueKind == JsonValueKind.String && potency.GetString() == "1"
                || potency.ValueKind == JsonValueKind.Number && potency.GetInt32() == 1;
            TestBase.AssertTrue(potencyOk, "potency 1 as string or number", ref run, ref pass, ref fail);
        }


        private static void ConsumablesJsonPushCsvRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ConsumablesJsonPushCsvRoundTrip));
            const string json = """
            [
              {"displayName":"Waxed Apple","internalKind":"Food","effect":"heal","potency":"1"}
            ]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Consumables);
            string csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Consumables);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Waxed Apple", a.RootElement[0].GetProperty("displayName").GetString(), "displayName", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Food", a.RootElement[0].GetProperty("internalKind").GetString(), "internalKind", ref run, ref pass, ref fail);
        }
    }
}
