using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterWeaponsTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            WeaponsRoundTrip(ref run, ref pass, ref fail);
            WeaponsImportStripsInternalColumnsAndCanonicalizesStats(ref run, ref pass, ref fail);
            WeaponsWithNestedRequirements(ref run, ref pass, ref fail);
            WeaponsWithTagsRoundTrip(ref run, ref pass, ref fail);
            WeaponsImportMapsTypoMinBonusHeaders(ref run, ref pass, ref fail);
            WeaponsPushSortsByTypeThenTier(ref run, ref pass, ref fail);
            WeaponsCsvImportSortsByTypeThenTier(ref run, ref pass, ref fail);
        }



        private static void WeaponsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsRoundTrip));
            const string json = """
            [
              {"type":"Dagger","name":"Test","baseDamage":2,"attackSpeed":1.1,"tier":1,"attributeRequirements":null}
            ]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons);
            TestBase.AssertTrue(rows.Count >= 2, "header+data", ref run, ref pass, ref fail);
            string header = string.Join(",", rows[0].Select(o => o?.ToString() ?? ""));
            TestBase.AssertTrue(header.Contains("type", StringComparison.Ordinal), "has type col", ref run, ref pass, ref fail);

            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertTrue(a.RootElement.ValueKind == JsonValueKind.Array, "array", ref run, ref pass, ref fail);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Dagger", first.GetProperty("type").GetString(), "type", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, first.GetProperty("baseDamage").GetInt32(), "baseDamage", ref run, ref pass, ref fail);
        }


        /// <summary>
        /// Sheet columns <c>dps</c> / <c>balance</c> are internal; CSV import should drop them and emit canonical <c>baseDamage</c> / <c>attackSpeed</c>.
        /// </summary>
        private static void WeaponsImportStripsInternalColumnsAndCanonicalizesStats(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsImportStripsInternalColumnsAndCanonicalizesStats));
            const string csv = """
            Type,Name,DPS,balance,BaseDamage,attackSpeed,Tier,attributeRequirements,Compelled Action
            Dagger,Twig,2.0,28.28%,0.57,1.43,1,,
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertFalse(first.TryGetProperty("dps", out _), "dps stripped", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("DPS", out _), "DPS stripped", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("balance", out _), "balance stripped", ref run, ref pass, ref fail);
            TestBase.AssertTrue(first.TryGetProperty("baseDamage", out var bd), "baseDamage camelCase", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, bd.GetInt32(), "fractional sheet damage rounds to whole baseDamage", ref run, ref pass, ref fail);
            TestBase.AssertTrue(first.TryGetProperty("attackSpeed", out var asp), "attackSpeed camelCase", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1.43, asp.GetDouble(), "attackSpeed preserved as number", ref run, ref pass, ref fail);
        }


        private static void WeaponsWithNestedRequirements(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsWithNestedRequirements));
            const string json = """
            [{"type":"Sword","name":"Req","baseDamage":3,"attackSpeed":1,"tier":2,"attributeRequirements":{"strength":5}}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            var req = a.RootElement[0].GetProperty("attributeRequirements");
            TestBase.AssertTrue(req.ValueKind == JsonValueKind.Object, "req object", ref run, ref pass, ref fail);
            TestBase.AssertEqual(5, req.GetProperty("strength").GetInt32(), "str", ref run, ref pass, ref fail);
        }


        private static void WeaponsWithTagsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsWithTagsRoundTrip));
            const string json = """
            [{"type":"Dagger","name":"Tagged","baseDamage":2,"attackSpeed":1.1,"tier":1,"tags":["magical","starter"]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            TestBase.AssertTrue(csv.Contains("magical, starter") || csv.Contains("magical,starter"),
                "tags pushed as comma-separated cell", ref run, ref pass, ref fail);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            var t = a.RootElement[0].GetProperty("tags");
            TestBase.AssertTrue(t.ValueKind == JsonValueKind.Array, "tags array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("magical", t[0].GetString(), "tag0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("starter", t[1].GetString(), "tag1", ref run, ref pass, ref fail);
        }


        /// <summary>Sheet typo <c>Min BOnus</c> and <c>Max Bonus</c> map to <c>damageBonusMin</c> / <c>damageBonusMax</c>.</summary>
        private static void WeaponsImportMapsTypoMinBonusHeaders(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsImportMapsTypoMinBonusHeaders));
            const string csv = """
            Type,Name,DPS,Base Damage,Min BOnus,Max Bonus,Attack Speed,Tier,tags
            Dagger,Twig,4.0,3,0,2,0.77,1,["starter"]
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(3, first.GetProperty("baseDamage").GetInt32(), "baseDamage", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0, first.GetProperty("damageBonusMin").GetInt32(), "damageBonusMin", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, first.GetProperty("damageBonusMax").GetInt32(), "damageBonusMax", ref run, ref pass, ref fail);
        }


        private static void WeaponsPushSortsByTypeThenTier(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsPushSortsByTypeThenTier));
            const string json = """
            [
              {"type":"Wand","name":"Zeta","baseDamage":2,"attackSpeed":1,"tier":2},
              {"type":"Mace","name":"Alpha","baseDamage":2,"attackSpeed":1,"tier":1},
              {"type":"Mace","name":"Beta","baseDamage":2,"attackSpeed":1,"tier":2},
              {"type":"Sword","name":"Gamma","baseDamage":2,"attackSpeed":1,"tier":1}
            ]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons);
            var headers = rows[0].Select(c => c?.ToString() ?? "").ToList();
            int typeIdx = headers.FindIndex(h => string.Equals(h, "type", StringComparison.OrdinalIgnoreCase));
            int nameIdx = headers.FindIndex(h => string.Equals(h, "name", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(typeIdx >= 0 && nameIdx >= 0, "type/name columns", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Mace", rows[1][typeIdx]?.ToString(), "row1 type", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Alpha", rows[1][nameIdx]?.ToString(), "row1 name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Mace", rows[2][typeIdx]?.ToString(), "row2 type", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Beta", rows[2][nameIdx]?.ToString(), "row2 name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Sword", rows[3][typeIdx]?.ToString(), "row3 type", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wand", rows[4][typeIdx]?.ToString(), "row4 type", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Zeta", rows[4][nameIdx]?.ToString(), "row4 name", ref run, ref pass, ref fail);
        }


        private static void WeaponsCsvImportSortsByTypeThenTier(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsCsvImportSortsByTypeThenTier));
            const string csv = """
            type,name,baseDamage,attackSpeed,tier
            Wand,Zeta,2,1,2
            Mace,Alpha,2,1,1
            Mace,Beta,2,1,2
            Sword,Gamma,2,1,1
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Weapons);
            using var doc = JsonDocument.Parse(outJson);
            TestBase.AssertEqual(4, doc.RootElement.GetArrayLength(), "four rows", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Mace", doc.RootElement[0].GetProperty("type").GetString(), "row1 type", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Alpha", doc.RootElement[0].GetProperty("name").GetString(), "row1 name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Mace", doc.RootElement[1].GetProperty("type").GetString(), "row2 type", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Beta", doc.RootElement[1].GetProperty("name").GetString(), "row2 name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Sword", doc.RootElement[2].GetProperty("type").GetString(), "row3 type", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wand", doc.RootElement[3].GetProperty("type").GetString(), "row4 type", ref run, ref pass, ref fail);
        }
    }
}
