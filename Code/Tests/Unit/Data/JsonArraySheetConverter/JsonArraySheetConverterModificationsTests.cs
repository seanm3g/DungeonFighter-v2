using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterModificationsTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            ModificationsRoundTrip(ref run, ref pass, ref fail);
            ModificationsPushIncludesTagsColumn(ref run, ref pass, ref fail);
            ModificationsCsvImportTagsColumn(ref run, ref pass, ref fail);
            ModificationsCsvImportMinMaxAndAttributeRequirementColumns(ref run, ref pass, ref fail);
            ModificationsCsvImportLegacyValueColumnMapsToMinMax(ref run, ref pass, ref fail);
            ModificationsCsvImportLiveSheetColumnOrder(ref run, ref pass, ref fail);
            ModificationsPushSplitsAttributeRequirements(ref run, ref pass, ref fail);
            MergeJsonRootArraysCoreThenExtra(ref run, ref pass, ref fail);
            SplitModificationsMergedJsonMaterialQualityToSecondFile(ref run, ref pass, ref fail);
        }



        private static void ModificationsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsRoundTrip));
            const string json = """
            [{"DiceResult":1,"ItemRank":"Common","Name":"Worn","Description":"x","Effect":"damage","MinValue":-3,"MaxValue":-1}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Modifications);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Modifications);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Worn", a.RootElement[0].GetProperty("Name").GetString(), "Name", ref run, ref pass, ref fail);
            TestBase.AssertEqual(-3, a.RootElement[0].GetProperty("MinValue").GetDouble(), "Min", ref run, ref pass, ref fail);
        }


        private static void ModificationsPushIncludesTagsColumn(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsPushIncludesTagsColumn));
            const string json = """
            [{"DiceResult":0,"ItemRank":"Common","Name":"Bone","prefixCategory":"MATERIAL","MinValue":2,"MaxValue":2,"tags":["bone","barbarian"]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Modifications);
            var headers = rows[0].Select(c => c?.ToString() ?? "").ToList();
            TestBase.AssertTrue(headers.Contains("tags"), "push headers include tags", ref run, ref pass, ref fail);
            int tagsIx = headers.FindIndex(h => string.Equals(h, "tags", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertEqual("bone, barbarian", rows[1][tagsIx]?.ToString(), "tags cell", ref run, ref pass, ref fail);
        }


        private static void ModificationsCsvImportTagsColumn(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsCsvImportTagsColumn));
            const string csv = """
            DiceResult,ItemRank,prefixCategory,Name,Description,Effect,MaxValue,MinValue,ATTRIBUTE REQUIREMENT,REQUIREMENT VALUE,ATTRIBUTE REQUREMENT,RolledValue,tags
            0,Common,ADJECTIVE,flaming,,BURN,10,10,,,,0,fire
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Modifications);
            using var doc = JsonDocument.Parse(outJson);
            var tags = doc.RootElement[0].GetProperty("tags");
            TestBase.AssertEqual(1, tags.GetArrayLength(), "one tag", ref run, ref pass, ref fail);
            TestBase.AssertEqual("fire", tags[0].GetString(), "fire tag", ref run, ref pass, ref fail);
        }


        /// <summary>PREFIX sheet: <c>MinValue</c>/<c>MaxValue</c> import directly; <c>ATTRIBUTE REQUIREMENT</c> + <c>REQUIREMENT VALUE</c> → <c>attributeRequirements</c>.</summary>
        private static void ModificationsCsvImportMinMaxAndAttributeRequirementColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsCsvImportMinMaxAndAttributeRequirementColumns));
            const string csv = """
            DiceResult,ItemRank,prefixCategory,Name,Description,Effect,MaxValue,MinValue,ATTRIBUTE REQUIREMENT,REQUIREMENT VALUE
            0,Common,ADJECTIVE,Reinforced,,ARMOR,1,1,strength,2
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Modifications);
            using var a = JsonDocument.Parse(outJson);
            var row = a.RootElement[0];
            TestBase.AssertEqual(1, row.GetProperty("MinValue").GetDouble(), "MinValue", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, row.GetProperty("MaxValue").GetDouble(), "MaxValue", ref run, ref pass, ref fail);
            var reqs = row.GetProperty("attributeRequirements");
            TestBase.AssertEqual(2, reqs.GetProperty("strength").GetInt32(), "strength requirement", ref run, ref pass, ref fail);
        }


        /// <summary>Legacy PREFIX exports with a <c>value</c> column still map to Min/Max on import.</summary>
        private static void ModificationsCsvImportLegacyValueColumnMapsToMinMax(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsCsvImportLegacyValueColumnMapsToMinMax));
            const string csv = """
            DiceResult,ItemRank,Name,Description,Effect,value,prefixCategory
            0,Common,Reinforced,,ARMOR,1,ADJECTIVE
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Modifications);
            using var a = JsonDocument.Parse(outJson);
            var row = a.RootElement[0];
            TestBase.AssertEqual(1, row.GetProperty("MinValue").GetDouble(), "value→MinValue", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, row.GetProperty("MaxValue").GetDouble(), "value→MaxValue", ref run, ref pass, ref fail);
            TestBase.AssertFalse(row.TryGetProperty("value", out _), "value column removed", ref run, ref pass, ref fail);
        }


        /// <summary>Live PREFIX tab: stat in <c>ATTRIBUTE REQUREMENT</c>, value in <c>REQUIREMENT VALUE</c> (any column order).</summary>
        private static void ModificationsCsvImportLiveSheetColumnOrder(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsCsvImportLiveSheetColumnOrder));
            const string csv = """
            DiceResult,ItemRank,prefixCategory,Name,Description,Effect,MaxValue,MinValue,ATTRIBUTE REQUIREMENT,REQUIREMENT VALUE,ATTRIBUTE REQUREMENT,RolledValue,tags
            0,Common,ADJECTIVE,Reinforced,,ARMOR,1,1,,2,strength,0,
            0,Common,MATERIAL,Bone,Barbarian,STRENGTH,2,2,,10,strength,0,Bone tag to the item
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Modifications);
            using var doc = JsonDocument.Parse(outJson);
            var reinforced = doc.RootElement[0];
            var bone = doc.RootElement[1];
            TestBase.AssertEqual(2, reinforced.GetProperty("attributeRequirements").GetProperty("strength").GetInt32(),
                "Reinforced strength gate", ref run, ref pass, ref fail);
            TestBase.AssertEqual(10, bone.GetProperty("attributeRequirements").GetProperty("strength").GetInt32(),
                "Bone strength gate", ref run, ref pass, ref fail);
            TestBase.AssertEqual("ADJECTIVE", reinforced.GetProperty("prefixCategory").GetString(),
                "prefixCategory", ref run, ref pass, ref fail);
        }


        private static void ModificationsPushSplitsAttributeRequirements(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsPushSplitsAttributeRequirements));
            const string json = """
            [{"DiceResult":0,"ItemRank":"Common","Name":"Reinforced","Effect":"ARMOR","prefixCategory":"ADJECTIVE","MinValue":1,"MaxValue":1,"attributeRequirements":{"strength":2}}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Modifications);
            var headers = rows[0].Select(c => c?.ToString() ?? "").ToList();
            int reqIx = headers.FindIndex(h => string.Equals(h, "REQUIREMENT VALUE", StringComparison.OrdinalIgnoreCase));
            int attrIx = headers.FindIndex(h => string.Equals(h, "ATTRIBUTE REQUIREMENT", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(reqIx >= 0 && attrIx >= 0, "requirement columns present", ref run, ref pass, ref fail);
            TestBase.AssertEqual("2", rows[1][reqIx]?.ToString(), "requirement value cell", ref run, ref pass, ref fail);
            TestBase.AssertEqual("strength", rows[1][attrIx]?.ToString(), "attribute requirement cell", ref run, ref pass, ref fail);
            TestBase.AssertFalse(headers.Any(h => string.Equals(h, "attributeRequirements", StringComparison.OrdinalIgnoreCase)),
                "no raw attributeRequirements column", ref run, ref pass, ref fail);
        }


        private static void ModificationsPushOmitsValueColumn(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsPushOmitsValueColumn));
            const string json = """
            [{"DiceResult":1,"ItemRank":"Common","Name":"Worn","Effect":"damage","MinValue":-3,"MaxValue":-1}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Modifications);
            var headers = rows[0].Select(c => c?.ToString() ?? "").ToList();
            TestBase.AssertFalse(headers.Any(h => string.Equals(h, "value", StringComparison.OrdinalIgnoreCase)),
                "push omits value column", ref run, ref pass, ref fail);
            TestBase.AssertTrue(headers.Any(h => string.Equals(h, "MinValue", StringComparison.OrdinalIgnoreCase)),
                "push includes MinValue", ref run, ref pass, ref fail);
            TestBase.AssertTrue(headers.Any(h => string.Equals(h, "MaxValue", StringComparison.OrdinalIgnoreCase)),
                "push includes MaxValue", ref run, ref pass, ref fail);
        }


        private static void MergeJsonRootArraysCoreThenExtra(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(MergeJsonRootArraysCoreThenExtra));
            const string core = """[{"DiceResult":1,"Name":"Core"}]""";
            const string extra = """[{"DiceResult":202,"Name":"BRONZE"}]""";
            string merged = SheetConverter.MergeJsonRootArrays(core, extra);
            using var doc = JsonDocument.Parse(merged);
            TestBase.AssertEqual(2, doc.RootElement.GetArrayLength(), "merged length", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Core", doc.RootElement[0].GetProperty("Name").GetString(), "first from core", ref run, ref pass, ref fail);
            TestBase.AssertEqual("BRONZE", doc.RootElement[1].GetProperty("Name").GetString(), "second from extra", ref run, ref pass, ref fail);
        }


        /// <summary>After sheet pull, Material/Quality rows must land in PrefixMaterialQuality.json, not only Modifications.json.</summary>
        private static void SplitModificationsMergedJsonMaterialQualityToSecondFile(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(SplitModificationsMergedJsonMaterialQualityToSecondFile));
            const string merged = """
            [
              {"DiceResult":1,"ItemRank":"Common","Name":"Sharp","Effect":"damage","MinValue":1,"MaxValue":2},
              {"DiceResult":201,"ItemRank":"Common","prefixCategory":"Material","Name":"BONE","Effect":"equipmentStr","MinValue":1,"MaxValue":2},
              {"DiceResult":101,"ItemRank":"Common","prefixCategory":"Quality","Name":"Broken","Effect":"gearPrimaryStatMultiplier","MinValue":0.5,"MaxValue":0.75}
            ]
            """;
            var (core, pmq) = SheetConverter.SplitModificationsMergedJson(merged);
            using var a = JsonDocument.Parse(core);
            using var b = JsonDocument.Parse(pmq);
            TestBase.AssertEqual(1, a.RootElement.GetArrayLength(), "core count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Sharp", a.RootElement[0].GetProperty("Name").GetString(), "core row", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, b.RootElement.GetArrayLength(), "pmq count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("BONE", b.RootElement[0].GetProperty("Name").GetString(), "material", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Broken", b.RootElement[1].GetProperty("Name").GetString(), "quality", ref run, ref pass, ref fail);
        }
    }
}
