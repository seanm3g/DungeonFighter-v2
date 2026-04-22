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
            StatBonusesRoundTrip(ref run, ref pass, ref fail);
            EnemiesRoundTrip(ref run, ref pass, ref fail);
            EnemiesFullStatsRoundTrip(ref run, ref pass, ref fail);
            EnemiesActionsPipeDelimitedNormalized(ref run, ref pass, ref fail);
            EnemiesPushUsesFlatStatColumns(ref run, ref pass, ref fail);
            EnemiesLegacyOverridesJsonBlobStillImports(ref run, ref pass, ref fail);
            EnemiesTwoRowHeaderCsvImports(ref run, ref pass, ref fail);
            EnemiesPushOmitsLegacyRootStatExtraColumns(ref run, ref pass, ref fail);
            EnemiesCsvHoistsLegacyRootStatsIntoNested(ref run, ref pass, ref fail);
            EnvironmentsRoundTrip(ref run, ref pass, ref fail);
            EnvironmentsWithEnemiesRoundTrip(ref run, ref pass, ref fail);
            DungeonsRoundTrip(ref run, ref pass, ref fail);
            DungeonsPossibleEnemiesPipeDelimitedNormalized(ref run, ref pass, ref fail);
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

        private static void StatBonusesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesRoundTrip));
            const string json = """
            [{"Name":"of Swiftness","Description":"+0.005 attack speed","Value":0.005,"Weight":15,"StatType":"AttackSpeed"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.StatBonuses);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("of Swiftness", first.GetProperty("Name").GetString(), "Name", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.005, first.GetProperty("Value").GetDouble(), "Value", ref run, ref pass, ref fail);
            TestBase.AssertEqual(15, first.GetProperty("Weight").GetInt32(), "Weight", ref run, ref pass, ref fail);
        }

        private static void EnemiesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesRoundTrip));
            const string json = """
            [{"name":"Goblin","archetype":"Assassin","baseHealth":40,"actions":["JAB","TAUNT"],"isLiving":true,"description":"A goblin"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            TestBase.AssertEqual(3, rows.Count, "category+header+data", ref run, ref pass, ref fail);
            string cat = string.Join(",", rows[0].Select(o => o?.ToString() ?? ""));
            string hdr = string.Join(",", rows[1].Select(o => o?.ToString() ?? ""));
            TestBase.AssertTrue(cat.Contains("base attributes", StringComparison.OrdinalIgnoreCase), "category base", ref run, ref pass, ref fail);
            TestBase.AssertTrue(hdr.Contains("baseHealth", StringComparison.Ordinal), "baseHealth col", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!hdr.Contains("overrides.", StringComparison.Ordinal), "no dotted overrides in row2", ref run, ref pass, ref fail);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Goblin", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual(40, first.GetProperty("baseHealth").GetInt32(), "baseHealth", ref run, ref pass, ref fail);
            TestBase.AssertEqual("JAB", first.GetProperty("actions")[0].GetString(), "action0", ref run, ref pass, ref fail);
        }

        private static void EnemiesFullStatsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesFullStatsRoundTrip));
            const string json = """
            [{"name":"TestMob","archetype":"Berserker","baseAttributes":{"strength":3,"agility":2,"technique":2,"intelligence":1},"growthPerLevel":{"strength":0.2,"agility":0.1,"technique":0.1,"intelligence":0.05},"baseHealth":40,"healthGrowthPerLevel":2.5,"actions":["SLAM"],"isLiving":true,"description":"x"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(40, first.GetProperty("baseHealth").GetInt32(), "baseHealth", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2.5, first.GetProperty("healthGrowthPerLevel").GetDouble(), "healthGrowthPerLevel", ref run, ref pass, ref fail);
            TestBase.AssertEqual(3, first.GetProperty("baseAttributes").GetProperty("strength").GetInt32(), "str", ref run, ref pass, ref fail);
        }

        private static void EnemiesActionsPipeDelimitedNormalized(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesActionsPipeDelimitedNormalized));
            const string csv = """
            name,archetype,actions,isLiving,description
            Goblin,Assassin,JAB|TAUNT,true,A goblin
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var actions = a.RootElement[0].GetProperty("actions");
            TestBase.AssertTrue(actions.ValueKind == JsonValueKind.Array, "actions array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("JAB", actions[0].GetString(), "action0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("TAUNT", actions[1].GetString(), "action1", ref run, ref pass, ref fail);
        }

        private static void EnemiesPushUsesFlatStatColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesPushUsesFlatStatColumns));
            const string json = """
            [{"name":"X","archetype":"Berserker","baseAttributes":{"strength":2,"agility":3,"technique":4,"intelligence":1},"growthPerLevel":{"strength":0.1,"agility":0.2,"technique":0.15,"intelligence":0.05},"baseHealth":10,"healthGrowthPerLevel":1.5,"actions":["A"],"isLiving":true,"description":"d"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            string hdr = string.Join(",", rows[1].Select(o => o?.ToString() ?? ""));
            TestBase.AssertTrue(hdr.Contains("strength", StringComparison.Ordinal), "base str short header", ref run, ref pass, ref fail);
            TestBase.AssertTrue(hdr.Contains("agility", StringComparison.Ordinal), "growth agi short header", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!hdr.Contains("baseAttributes.", StringComparison.Ordinal), "no baseAttributes. in row2", ref run, ref pass, ref fail);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(10, first.GetProperty("baseHealth").GetInt32(), "baseHealth", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, first.GetProperty("baseAttributes").GetProperty("strength").GetInt32(), "ba str", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.2, first.GetProperty("growthPerLevel").GetProperty("agility").GetDouble(), "gp agi", ref run, ref pass, ref fail);
        }

        private static void EnemiesLegacyOverridesJsonBlobStillImports(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesLegacyOverridesJsonBlobStillImports));
            const string csv = """
            name,archetype,baseHealth,overrides,actions,isLiving,description
            Goblin,Assassin,100,"{""health"":0.9}",JAB|TAUNT,true,A goblin
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertFalse(first.TryGetProperty("overrides", out _), "overrides removed after promote", ref run, ref pass, ref fail);
            TestBase.AssertEqual(90, first.GetProperty("baseHealth").GetInt32(), "baseHealth * legacy override health", ref run, ref pass, ref fail);
        }

        private static void EnemiesTwoRowHeaderCsvImports(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesTwoRowHeaderCsvImports));
            const string csv = """
            ,,base attributes,growth
            name,archetype,strength,strength
            Imp,Assassin,3,0.1
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(3, first.GetProperty("baseAttributes").GetProperty("strength").GetInt32(), "ba.str", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.1, first.GetProperty("growthPerLevel").GetProperty("strength").GetDouble(), "gp.str", ref run, ref pass, ref fail);
        }

        /// <summary>Historical root <c>strength</c> / <c>agility</c> must not create extra trailing sheet columns; they map into canonical base/growth cells.</summary>
        private static void EnemiesPushOmitsLegacyRootStatExtraColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesPushOmitsLegacyRootStatExtraColumns));
            const string json = """
            [{"name":"G","archetype":"Assassin","strength":1.05,"agility":0.22,"baseHealth":40,"actions":["J"],"isLiving":true,"description":"d"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            TestBase.AssertTrue(rows.Count >= 3, "category+header+data", ref run, ref pass, ref fail);
            var shortHeaders = rows[1].Select(o => o?.ToString() ?? "").ToList();
            TestBase.AssertEqual(JsonArraySheetConverter.EnemiesCanonicalHeaders.Length, shortHeaders.Count,
                "no trailing legacy root columns", ref run, ref pass, ref fail);
        }

        private static void EnemiesCsvHoistsLegacyRootStatsIntoNested(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesCsvHoistsLegacyRootStatsIntoNested));
            const string csv = """
            name,archetype,strength,agility,actions,isLiving,description
            X,Berserker,1.1,0.2,J,true,Hi
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(1.1, first.GetProperty("growthPerLevel").GetProperty("strength").GetDouble(), "legacy strength -> growth", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.2, first.GetProperty("growthPerLevel").GetProperty("agility").GetDouble(), "legacy agility -> growth", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("strength", out _), "root strength removed", ref run, ref pass, ref fail);
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

        private static void EnvironmentsWithEnemiesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnvironmentsWithEnemiesRoundTrip));
            const string json = """
            [{"name":"Wolf Den","description":"Den.","theme":"Forest","isHostile":true,"actions":[{"name":"Trap Spring","weight":1}],"enemies":[{"name":"Wolf","weight":0.7},{"name":"Spider","weight":0.3}]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Environments);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Environments);
            using var a = JsonDocument.Parse(outJson);
            var enemies = a.RootElement[0].GetProperty("enemies");
            TestBase.AssertTrue(enemies.ValueKind == JsonValueKind.Array, "enemies array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wolf", enemies[0].GetProperty("name").GetString(), "enemy0", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.7, enemies[0].GetProperty("weight").GetDouble(), "w0", ref run, ref pass, ref fail);
        }

        private static void DungeonsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(DungeonsRoundTrip));
            const string json = """
            [{"name":"Ancient Forest","theme":"Forest","minLevel":1,"maxLevel":10,"possibleEnemies":["Goblin","Wolf"]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Dungeons);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Dungeons);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Ancient Forest", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Forest", first.GetProperty("theme").GetString(), "theme", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, first.GetProperty("minLevel").GetInt32(), "minLevel", ref run, ref pass, ref fail);
            var pe = first.GetProperty("possibleEnemies");
            TestBase.AssertTrue(pe.ValueKind == JsonValueKind.Array, "possibleEnemies array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Goblin", pe[0].GetString(), "e0", ref run, ref pass, ref fail);
        }

        private static void DungeonsPossibleEnemiesPipeDelimitedNormalized(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(DungeonsPossibleEnemiesPipeDelimitedNormalized));
            const string csv = """
            name,theme,minLevel,maxLevel,possibleEnemies,colorOverride
            Haunted Crypt,Crypt,1,100,Skeleton|Zombie|Wraith,
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Dungeons);
            using var a = JsonDocument.Parse(outJson);
            var pe = a.RootElement[0].GetProperty("possibleEnemies");
            TestBase.AssertTrue(pe.ValueKind == JsonValueKind.Array, "array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Skeleton", pe[0].GetString(), "e0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wraith", pe[2].GetString(), "e2", ref run, ref pass, ref fail);
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
