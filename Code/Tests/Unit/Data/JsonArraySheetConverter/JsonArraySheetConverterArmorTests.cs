using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterArmorTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            ArmorRoundTrip(ref run, ref pass, ref fail);
            ArmorExtendedColumnsRoundTrip(ref run, ref pass, ref fail);
            ArmorWithTagsRoundTrip(ref run, ref pass, ref fail);
            ArmorCsvUtf8BomOnFirstHeaderImports(ref run, ref pass, ref fail);
            ArmorSpreadsheetTemplateHeadersRoundTrip(ref run, ref pass, ref fail);
            ArmorPushSortsBySlotThenTier(ref run, ref pass, ref fail);
        }



        private static void ArmorRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorRoundTrip));
            const string json = """
            [{"slot":"chest","name":"Tunic","armor":2,"tier":1,"attributeRequirements":null}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Armor);
            TestBase.AssertTrue(rows.Count >= 2, "header+data", ref run, ref pass, ref fail);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("chest", first.GetProperty("slot").GetString(), "slot", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, first.GetProperty("armor").GetInt32(), "armor", ref run, ref pass, ref fail);
        }


        private static void ArmorExtendedColumnsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorExtendedColumnsRoundTrip));
            const string json = """
            [{"slot":"feet","name":"Striders","armor":1,"tier":1,"strength":0,"agility":1,"technique":0,"intelligence":0,"hit":0,"combo":0,"crit":0,"extraActionSlots":1,"minActionBonuses":0,"attributeRequirements":null,"tags":null}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Armor);
            TestBase.AssertTrue(rows.Count >= 2, "header+data", ref run, ref pass, ref fail);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(1, first.GetProperty("extraActionSlots").GetInt32(), "extraActionSlots", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, first.GetProperty("agility").GetInt32(), "agility", ref run, ref pass, ref fail);
        }


        private static void ArmorWithTagsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorWithTagsRoundTrip));
            const string json = """
            [{"slot":"head","name":"Crown","armor":1,"tier":2,"tags":["setPiece","magical"]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Armor);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            TestBase.AssertTrue(csv.Contains("setpiece, magical") || csv.Contains("setpiece,magical"),
                "tags pushed as comma-separated cell", ref run, ref pass, ref fail);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
            using var a = JsonDocument.Parse(outJson);
            var t = a.RootElement[0].GetProperty("tags");
            TestBase.AssertTrue(t.ValueKind == JsonValueKind.Array, "tags array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("setpiece", t[0].GetString(), "tag0", ref run, ref pass, ref fail);
        }


        private static void ArmorCsvUtf8BomOnFirstHeaderImports(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorCsvUtf8BomOnFirstHeaderImports));
            string csv = "\uFEFFslot,name,armor,tier,attributeRequirements\nhead,Test Helm,2,1,\n";
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("head", first.GetProperty("slot").GetString(), "slot", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Test Helm", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
        }


        private static void ArmorSpreadsheetTemplateHeadersRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorSpreadsheetTemplateHeadersRoundTrip));
            string csv = """
                slot,name,armor,tags,STRENGTH,AGILITY,TECHNIQUE,INTELLIGENCE,HIT,COMBO,CRIT,# OF ACTION SLOTS,# OF BONUS ACTIONS,tier,attributeRequirements,requirement value
                head,Helm,2,,0,2,0,0,0,0,0,1,0,1,strength,5
                """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Armor);
            using (var a = JsonDocument.Parse(outJson))
            {
                var first = a.RootElement[0];
                TestBase.AssertEqual("head", first.GetProperty("slot").GetString(), "slot", ref run, ref pass, ref fail);
                TestBase.AssertEqual(2, first.GetProperty("agility").GetInt32(), "agility stat", ref run, ref pass, ref fail);
                TestBase.AssertEqual(1, first.GetProperty("extraActionSlots").GetInt32(), "extraActionSlots", ref run, ref pass, ref fail);
                TestBase.AssertEqual(0, first.GetProperty("minActionBonuses").GetInt32(), "minActionBonuses", ref run, ref pass, ref fail);
                var reqs = first.GetProperty("attributeRequirements");
                TestBase.AssertTrue(reqs.ValueKind == JsonValueKind.Object, "reqs object", ref run, ref pass, ref fail);
                TestBase.AssertEqual(5, reqs.GetProperty("strength").GetInt32(), "str req", ref run, ref pass, ref fail);
            }

            const string roundJson = """
                [{"slot":"feet","name":"Boot","armor":1,"tier":2,"tags":["rare"],"strength":0,"agility":0,"technique":0,"intelligence":0,"hit":0,"combo":0,"crit":0,"extraActionSlots":2,"extraActionSlotsMin":0,"extraActionSlotsMax":0,"minActionBonuses":1,"attackSpeed":0,"attributeRequirements":{"technique":3}}]
                """;
            var pushRows = SheetConverter.BuildPushValueRows(roundJson, GameDataTabularSheetKind.Armor);
            var hdr = pushRows[0].Select(x => x?.ToString() ?? "").ToArray();
            TestBase.AssertEqual(SheetConverter.ArmorCanonicalHeaders.Length + 3, hdr.Length,
                "canonical + extraActionSlotsMin/Max/attackSpeed", ref run, ref pass, ref fail);
            for (int i = 0; i < SheetConverter.ArmorCanonicalHeaders.Length; i++)
            {
                TestBase.AssertEqual(SheetConverter.ArmorCanonicalHeaders[i], hdr[i], "hdr " + i, ref run, ref pass, ref fail);
            }

            int attrIdx = Array.IndexOf(hdr, "attributeRequirements");
            int reqIdx = Array.IndexOf(hdr, "requirement value");
            TestBase.AssertTrue(attrIdx >= 0 && reqIdx >= 0, "requirement columns", ref run, ref pass, ref fail);
            TestBase.AssertEqual("TECHNIQUE", pushRows[1][attrIdx]?.ToString(), "pushed attr abbrev", ref run, ref pass, ref fail);
            TestBase.AssertEqual("3", pushRows[1][reqIdx]?.ToString(), "pushed req val", ref run, ref pass, ref fail);
        }


        private static void ArmorPushSortsBySlotThenTier(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorPushSortsBySlotThenTier));
            const string json = """
            [
              {"slot":"feet","name":"Zeta","armor":1,"tier":2},
              {"slot":"head","name":"Alpha","armor":1,"tier":1},
              {"slot":"head","name":"Beta","armor":1,"tier":2},
              {"slot":"chest","name":"Gamma","armor":1,"tier":1}
            ]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Armor);
            var headers = rows[0].Select(c => c?.ToString() ?? "").ToList();
            int slotIdx = headers.FindIndex(h => string.Equals(h, "slot", StringComparison.OrdinalIgnoreCase));
            int nameIdx = headers.FindIndex(h => string.Equals(h, "name", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(slotIdx >= 0 && nameIdx >= 0, "slot/name columns", ref run, ref pass, ref fail);
            TestBase.AssertEqual("head", rows[1][slotIdx]?.ToString(), "row1 slot", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Alpha", rows[1][nameIdx]?.ToString(), "row1 name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("head", rows[2][slotIdx]?.ToString(), "row2 slot", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Beta", rows[2][nameIdx]?.ToString(), "row2 name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("chest", rows[3][slotIdx]?.ToString(), "row3 slot", ref run, ref pass, ref fail);
            TestBase.AssertEqual("feet", rows[4][slotIdx]?.ToString(), "row4 slot", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Zeta", rows[4][nameIdx]?.ToString(), "row4 name", ref run, ref pass, ref fail);
        }
    }
}
