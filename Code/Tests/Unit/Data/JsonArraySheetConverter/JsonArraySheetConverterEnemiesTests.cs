using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterEnemiesTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            EnemiesRoundTrip(ref run, ref pass, ref fail);
            EnemiesWithTagsRoundTrip(ref run, ref pass, ref fail);
            EnemiesFullStatsRoundTrip(ref run, ref pass, ref fail);
            EnemiesActionsPipeDelimitedNormalized(ref run, ref pass, ref fail);
            EnemiesTagsPlainStringAndListNormalized(ref run, ref pass, ref fail);
            EnemiesPushUsesFlatStatColumns(ref run, ref pass, ref fail);
            EnemiesTwoRowHeaderCsvImports(ref run, ref pass, ref fail);
            EnemiesTwoRowHeaderMergedCategoryBandImports(ref run, ref pass, ref fail);
            EnemiesActionsQuotedCommaListNormalizes(ref run, ref pass, ref fail);
            EnemiesPushOmitsLegacyRootStatExtraColumns(ref run, ref pass, ref fail);
            EnemiesNewSheetLayoutImport(ref run, ref pass, ref fail);
            EnemiesArchetypeCanonicalization(ref run, ref pass, ref fail);
            EnemiesActionsPipePushFormat(ref run, ref pass, ref fail);
        }



        private static void EnemiesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesRoundTrip));
            const string json = """
            [{"name":"Goblin","archetype":"Assassin","healthPercent":40,"actions":["JAB","TAUNT"],"isLiving":true,"description":"A goblin"}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            TestBase.AssertEqual(3, rows.Count, "category+header+data", ref run, ref pass, ref fail);
            string cat = string.Join(",", rows[0].Select(o => o?.ToString() ?? ""));
            string hdr = string.Join(",", rows[1].Select(o => o?.ToString() ?? ""));
            TestBase.AssertTrue(cat.Contains("base attributes", StringComparison.OrdinalIgnoreCase), "category base", ref run, ref pass, ref fail);
            TestBase.AssertTrue(cat.Contains("HEALTH", StringComparison.OrdinalIgnoreCase), "category HEALTH", ref run, ref pass, ref fail);
            TestBase.AssertTrue(hdr.Contains("region", StringComparison.Ordinal), "region col", ref run, ref pass, ref fail);
            TestBase.AssertTrue(hdr.Contains("healthPercent", StringComparison.Ordinal), "healthPercent col", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!hdr.Contains("overrides.", StringComparison.Ordinal), "no dotted overrides in row2", ref run, ref pass, ref fail);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Goblin", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual(40, first.GetProperty("healthPercent").GetInt32(), "healthPercent", ref run, ref pass, ref fail);
            TestBase.AssertEqual("JAB", first.GetProperty("actions")[0].GetString(), "action0", ref run, ref pass, ref fail);
        }


        private static void EnemiesWithTagsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesWithTagsRoundTrip));
            const string json = """
            [{"name":"BossImp","archetype":"Sage","healthPercent":50,"actions":["BOLT"],"isLiving":true,"description":"x","tags":["boss","demon"]}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            TestBase.AssertTrue(csv.Contains("boss, demon") || csv.Contains("boss,demon"),
                "tags pushed as comma-separated cell", ref run, ref pass, ref fail);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var t = a.RootElement[0].GetProperty("tags");
            TestBase.AssertTrue(t.ValueKind == JsonValueKind.Array, "tags array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("boss", t[0].GetString(), "tag0", ref run, ref pass, ref fail);
        }


        private static void EnemiesFullStatsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesFullStatsRoundTrip));
            const string json = """
            [{"name":"TestMob","archetype":"Berserker","baseAttributes":{"strength":3,"agility":2,"technique":2,"intelligence":1},"growthPerLevel":{"strength":0.2,"agility":0.1,"technique":0.1,"intelligence":0.05},"healthPercent":40,"healthGrowthPercent":2.5,"actions":["SLAM"],"isLiving":true,"description":"x"}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(40, first.GetProperty("healthPercent").GetInt32(), "healthPercent", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2.5, first.GetProperty("healthGrowthPercent").GetDouble(), "healthGrowthPercent", ref run, ref pass, ref fail);
            TestBase.AssertEqual(3, first.GetProperty("baseAttributes").GetProperty("strength").GetInt32(), "str", ref run, ref pass, ref fail);
        }


        private static void EnemiesActionsPipeDelimitedNormalized(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesActionsPipeDelimitedNormalized));
            const string csv = """
            name,archetype,actions,isLiving,description
            Goblin,Assassin,JAB|TAUNT,true,A goblin
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var actions = a.RootElement[0].GetProperty("actions");
            TestBase.AssertTrue(actions.ValueKind == JsonValueKind.Array, "actions array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("JAB", actions[0].GetString(), "action0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("TAUNT", actions[1].GetString(), "action1", ref run, ref pass, ref fail);
        }


        private static void EnemiesTagsPlainStringAndListNormalized(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesTagsPlainStringAndListNormalized));
            // Plain cell and quoted comma list (JSON array in a cell must be quoted or commas break CSV columns).
            const string csv = """
            name,archetype,actions,isLiving,description,tags
            Goblin,Assassin,JAB,true,A goblin,goblin
            Orc,Berserker,SLAM,true,An orc,"orc, brute"
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var gob = a.RootElement[0].GetProperty("tags");
            TestBase.AssertTrue(gob.ValueKind == JsonValueKind.Array && gob.GetArrayLength() == 1, "goblin one tag", ref run, ref pass, ref fail);
            TestBase.AssertEqual("goblin", gob[0].GetString(), "gob tag", ref run, ref pass, ref fail);
            var orc = a.RootElement[1].GetProperty("tags");
            TestBase.AssertTrue(orc.GetArrayLength() == 2, "orc two tags", ref run, ref pass, ref fail);
        }


        private static void EnemiesPushUsesFlatStatColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesPushUsesFlatStatColumns));
            const string json = """
            [{"name":"X","archetype":"Berserker","baseAttributes":{"strength":2,"agility":3,"technique":4,"intelligence":1},"growthPerLevel":{"strength":0.1,"agility":0.2,"technique":0.15,"intelligence":0.05},"healthPercent":10,"healthGrowthPercent":1.5,"actions":["A"],"isLiving":true,"description":"d"}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            string hdr = string.Join(",", rows[1].Select(o => o?.ToString() ?? ""));
            TestBase.AssertTrue(hdr.Contains("strength", StringComparison.Ordinal), "base str short header", ref run, ref pass, ref fail);
            TestBase.AssertTrue(hdr.Contains("agility", StringComparison.Ordinal), "growth agi short header", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!hdr.Contains("baseAttributes.", StringComparison.Ordinal), "no baseAttributes. in row2", ref run, ref pass, ref fail);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(10, first.GetProperty("healthPercent").GetInt32(), "healthPercent", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, first.GetProperty("baseAttributes").GetProperty("strength").GetInt32(), "ba str", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.2, first.GetProperty("growthPerLevel").GetProperty("agility").GetDouble(), "gp agi", ref run, ref pass, ref fail);
        }



        private static void EnemiesTwoRowHeaderCsvImports(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesTwoRowHeaderCsvImports));
            const string csv = """
            ,,base attributes,growth
            name,archetype,strength,strength
            Imp,Assassin,3,0.1
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(3, first.GetProperty("baseAttributes").GetProperty("strength").GetInt32(), "ba.str", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.1, first.GetProperty("growthPerLevel").GetProperty("strength").GetDouble(), "gp.str", ref run, ref pass, ref fail);
        }


        private static void EnemiesTwoRowHeaderMergedCategoryBandImports(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesTwoRowHeaderMergedCategoryBandImports));
            // Simulates a sheet export where the category band uses merged cells: only the first column of each
            // block contains the category label, subsequent columns are blank.
            const string csv = """
            ,,base attributes,,,,growth,,,
            name,archetype,strength,agility,technique,intelligence,strength,agility,technique,intelligence
            Imp,Assassin,3,4,5,6,0.1,0.2,0.3,0.4
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            var ba = first.GetProperty("baseAttributes");
            var gp = first.GetProperty("growthPerLevel");
            TestBase.AssertEqual(3, ba.GetProperty("strength").GetInt32(), "ba.str", ref run, ref pass, ref fail);
            TestBase.AssertEqual(4, ba.GetProperty("agility").GetInt32(), "ba.agi", ref run, ref pass, ref fail);
            TestBase.AssertEqual(5, ba.GetProperty("technique").GetInt32(), "ba.tec", ref run, ref pass, ref fail);
            TestBase.AssertEqual(6, ba.GetProperty("intelligence").GetInt32(), "ba.int", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.1, gp.GetProperty("strength").GetDouble(), "gp.str", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.2, gp.GetProperty("agility").GetDouble(), "gp.agi", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.3, gp.GetProperty("technique").GetDouble(), "gp.tec", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.4, gp.GetProperty("intelligence").GetDouble(), "gp.int", ref run, ref pass, ref fail);
        }


        private static void EnemiesActionsQuotedCommaListNormalizes(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesActionsQuotedCommaListNormalizes));
            const string csv =
                "name,archetype,healthPercent,actions,isLiving,description\n" +
                "Goblin,Assassin,10,\"\"\"JAB\"\",\n\"\"TAUNT\"\"\",true,G\n";
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            var actions = first.GetProperty("actions");
            TestBase.AssertTrue(actions.ValueKind == JsonValueKind.Array, "actions array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("JAB", actions[0].GetString(), "a0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("TAUNT", actions[1].GetString(), "a1", ref run, ref pass, ref fail);
        }


        /// <summary>Historical root <c>strength</c> / <c>agility</c> must not create extra trailing sheet columns; they map into canonical base/growth cells.</summary>
        private static void EnemiesPushOmitsLegacyRootStatExtraColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesPushOmitsLegacyRootStatExtraColumns));
            const string json = """
            [{"name":"G","archetype":"Assassin","strength":1.05,"agility":0.22,"healthPercent":40,"actions":["J"],"isLiving":true,"description":"d"}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            TestBase.AssertTrue(rows.Count >= 3, "category+header+data", ref run, ref pass, ref fail);
            var shortHeaders = rows[1].Select(o => o?.ToString() ?? "").ToList();
            TestBase.AssertEqual(SheetConverter.EnemiesCanonicalHeaders.Length, shortHeaders.Count,
                "no trailing legacy root columns", ref run, ref pass, ref fail);
        }


        /// <summary>New ENEMIES layout: Region-first row-2 headers, HEALTH band, mixed casing.</summary>
        private static void EnemiesNewSheetLayoutImport(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesNewSheetLayoutImport));
            const string csv = """
            ,,,,,,base attributes,,,,growth,,,,HEALTH,,,,,
            Region,Biome,Location,Rarity,Name,tags,Archetype,Strength,agility,technique,Intelligence,strength,agility,technique,Intelligence,healthPercent,healthGrowthPercent,actions,isLiving,description,colorOverride
            forest,Forest,,Common,Goblin,goblin,Assassin,3,4,5,6,0.1,0.2,0.3,0.4,57.14%,3.36%,"[""JAB"",""TAUNT""]",true,A quick goblin,
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("forest", first.GetProperty("region").GetString(), "region", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Forest", first.GetProperty("biome").GetString(), "biome", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Common", first.GetProperty("rarity").GetString(), "rarity", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Goblin", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertTrue(Math.Abs(first.GetProperty("healthPercent").GetDouble() - 57.14) < 0.01,
                "healthPercent", ref run, ref pass, ref fail);
            TestBase.AssertEqual(3.36, first.GetProperty("healthGrowthPercent").GetDouble(), "healthGrowthPercent", ref run, ref pass, ref fail);
            TestBase.AssertEqual(3, first.GetProperty("baseAttributes").GetProperty("strength").GetInt32(), "ba.str", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.4, first.GetProperty("growthPerLevel").GetProperty("intelligence").GetDouble(), "gp.int", ref run, ref pass, ref fail);
            TestBase.AssertEqual("JAB", first.GetProperty("actions")[0].GetString(), "action0", ref run, ref pass, ref fail);
        }


        private static void EnemiesArchetypeCanonicalization(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesArchetypeCanonicalization));
            const string json = """
            [{"name":"Sage Mob","archetype":"sage","healthPercent":40,"actions":["J"],"isLiving":true,"description":"d"}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var doc = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Sage", doc.RootElement[0].GetProperty("archetype").GetString(), "archetype title case", ref run, ref pass, ref fail);
        }


        private static void EnemiesActionsPipePushFormat(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesActionsPipePushFormat));
            const string json = """
            [{"name":"Goblin","archetype":"Assassin","healthPercent":40,"actions":["JAB","TAUNT"],"isLiving":true,"description":"A goblin"}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            int actionsIdx = Array.IndexOf(SheetConverter.EnemiesCanonicalHeaders, "actions");
            TestBase.AssertEqual("JAB|TAUNT", rows[2][actionsIdx]?.ToString(), "actions pipe cell", ref run, ref pass, ref fail);
        }
    }
}
