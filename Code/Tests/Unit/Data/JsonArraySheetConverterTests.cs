using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
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
            WeaponsImportStripsInternalColumnsAndCanonicalizesStats(ref run, ref pass, ref fail);
            WeaponsWithNestedRequirements(ref run, ref pass, ref fail);
            WeaponsWithTagsRoundTrip(ref run, ref pass, ref fail);
            WeaponsImportMapsTypoMinBonusHeaders(ref run, ref pass, ref fail);
            ModificationsRoundTrip(ref run, ref pass, ref fail);
            MergeJsonRootArraysCoreThenExtra(ref run, ref pass, ref fail);
            SplitModificationsMergedJsonMaterialQualityToSecondFile(ref run, ref pass, ref fail);
            StatBonusesRoundTrip(ref run, ref pass, ref fail);
            StatBonusesLegacyWeightCsvImportsAsCommon(ref run, ref pass, ref fail);
            StatBonusesBracketMechanicsCsvImport(ref run, ref pass, ref fail);
            StatBonusesCsvImportIgnoresColumnsBeyondG(ref run, ref pass, ref fail);
            StatBonusesPushUsesOnlyCanonicalSevenColumns(ref run, ref pass, ref fail);
            GameDataJsonNormalizerRepairsSheetExports(ref run, ref pass, ref fail);
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
            EnvironmentsRoundTrip(ref run, ref pass, ref fail);
            EnvironmentsWithEnemiesRoundTrip(ref run, ref pass, ref fail);
            DungeonsRoundTrip(ref run, ref pass, ref fail);
            DungeonsPossibleEnemiesPipeDelimitedNormalized(ref run, ref pass, ref fail);
            ArmorRoundTrip(ref run, ref pass, ref fail);
            ArmorExtendedColumnsRoundTrip(ref run, ref pass, ref fail);
            ArmorWithTagsRoundTrip(ref run, ref pass, ref fail);
            ArmorCsvUtf8BomOnFirstHeaderImports(ref run, ref pass, ref fail);
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
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Weapons);
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
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            var req = a.RootElement[0].GetProperty("attributeRequirements");
            TestBase.AssertTrue(req.ValueKind == JsonValueKind.Object, "req object", ref run, ref pass, ref fail);
            TestBase.AssertEqual(5, req.GetProperty("strength").GetInt32(), "str", ref run, ref pass, ref fail);
        }

        /// <summary>Sheet typo <c>Min BOnus</c> and <c>Max Bonus</c> map to <c>damageBonusMin</c> / <c>damageBonusMax</c>.</summary>
        private static void WeaponsImportMapsTypoMinBonusHeaders(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsImportMapsTypoMinBonusHeaders));
            const string csv = """
            Type,Name,DPS,Base Damage,Min BOnus,Max Bonus,Attack Speed,Tier,tags
            Dagger,Twig,4.0,3,0,2,0.77,1,["starter"]
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual(3, first.GetProperty("baseDamage").GetInt32(), "baseDamage", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0, first.GetProperty("damageBonusMin").GetInt32(), "damageBonusMin", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, first.GetProperty("damageBonusMax").GetInt32(), "damageBonusMax", ref run, ref pass, ref fail);
        }

        private static void WeaponsWithTagsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponsWithTagsRoundTrip));
            const string json = """
            [{"type":"Dagger","name":"Tagged","baseDamage":2,"attackSpeed":1.1,"tier":1,"tags":["magical","starter"]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Weapons);
            using var a = JsonDocument.Parse(outJson);
            var t = a.RootElement[0].GetProperty("tags");
            TestBase.AssertTrue(t.ValueKind == JsonValueKind.Array, "tags array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("magical", t[0].GetString(), "tag0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("starter", t[1].GetString(), "tag1", ref run, ref pass, ref fail);
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

        private static void MergeJsonRootArraysCoreThenExtra(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(MergeJsonRootArraysCoreThenExtra));
            const string core = """[{"DiceResult":1,"Name":"Core"}]""";
            const string extra = """[{"DiceResult":202,"Name":"BRONZE"}]""";
            string merged = JsonArraySheetConverter.MergeJsonRootArrays(core, extra);
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
            var (core, pmq) = JsonArraySheetConverter.SplitModificationsMergedJson(merged);
            using var a = JsonDocument.Parse(core);
            using var b = JsonDocument.Parse(pmq);
            TestBase.AssertEqual(1, a.RootElement.GetArrayLength(), "core count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Sharp", a.RootElement[0].GetProperty("Name").GetString(), "core row", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, b.RootElement.GetArrayLength(), "pmq count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("BONE", b.RootElement[0].GetProperty("Name").GetString(), "material", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Broken", b.RootElement[1].GetProperty("Name").GetString(), "quality", ref run, ref pass, ref fail);
        }

        private static void StatBonusesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesRoundTrip));
            const string json = """
            [{"Name":"of Swiftness","Description":"+0.005 attack speed","Value":0.005,"Rarity":"Uncommon","StatType":"AttackSpeed"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.StatBonuses);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("of Swiftness", first.GetProperty("Name").GetString(), "Name", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.005, first.GetProperty("Value").GetDouble(), "Value", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Uncommon", first.GetProperty("Rarity").GetString(), "Rarity", ref run, ref pass, ref fail);
        }

        private static void StatBonusesLegacyWeightCsvImportsAsCommon(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesLegacyWeightCsvImportsAsCommon));
            const string csv = """
Name,Description,Value,Weight,StatType,ItemRank
of Test,+1,1,15,Armor,
""";
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("Common", first.GetProperty("Rarity").GetString(), "legacy Weight→Common", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("Weight", out _), "Weight column removed", ref run, ref pass, ref fail);
        }

        private static void StatBonusesBracketMechanicsCsvImport(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesBracketMechanicsCsvImport));
            const string csv = """
Name,Description,Value,Rarity,StatType,ItemRank,Mechanics
of the Tortoise,two stats,0,Uncommon,Armor,,"[ARMOR:5,MAX HEALTH:15]"
""";
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("of the Tortoise", first.GetProperty("Name").GetString(), "Name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Uncommon", first.GetProperty("Rarity").GetString(), "rarity", ref run, ref pass, ref fail);
            TestBase.AssertTrue(first.TryGetProperty("Mechanics", out var mech) && mech.ValueKind == JsonValueKind.Array,
                "Mechanics array", ref run, ref pass, ref fail);
            if (!first.TryGetProperty("Mechanics", out var mechArr) || mechArr.ValueKind != JsonValueKind.Array)
                return;
            TestBase.AssertEqual(2, mechArr.GetArrayLength(), "two mechanics", ref run, ref pass, ref fail);
            var m0 = mechArr[0];
            TestBase.AssertEqual("Armor", m0.GetProperty("StatType").GetString(), "armor key", ref run, ref pass, ref fail);
            TestBase.AssertEqual(5, m0.GetProperty("Value").GetInt32(), "armor value", ref run, ref pass, ref fail);
            var m1 = mechArr[1];
            TestBase.AssertEqual("Health", m1.GetProperty("StatType").GetString(), "max health→Health", ref run, ref pass, ref fail);
            TestBase.AssertEqual(15, m1.GetProperty("Value").GetInt32(), "health value", ref run, ref pass, ref fail);
        }

        /// <summary>SUFFIXES tab uses A–G only; CSV columns H+ must not become JSON properties.</summary>
        private static void StatBonusesCsvImportIgnoresColumnsBeyondG(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesCsvImportIgnoresColumnsBeyondG));
            const string csv = """
Name,Description,Value,Rarity,StatType,ItemRank,Mechanics,Mechanic 1 type,Junk
of Test,desc,1,Common,Armor,,"[ARMOR:2]",Armor,999
""";
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("of Test", first.GetProperty("Name").GetString(), "Name", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("Junk", out _), "column H dropped", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("Mechanic 1 type", out _), "helper col dropped", ref run, ref pass, ref fail);
        }

        /// <summary>Push rows must not add columns past G from stray JSON keys on suffix templates.</summary>
        private static void StatBonusesPushUsesOnlyCanonicalSevenColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesPushUsesOnlyCanonicalSevenColumns));
            const string json = """
            [{"Name":"of X","Description":"d","Value":1,"Rarity":"Common","StatType":"Armor","tags":["t"],"mechanics":"[ARMOR:1]"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.StatBonuses);
            TestBase.AssertEqual(2, rows.Count, "header+data", ref run, ref pass, ref fail);
            TestBase.AssertEqual(7, rows[0].Count, "seven headers A–G", ref run, ref pass, ref fail);
        }

        private static void GameDataJsonNormalizerRepairsSheetExports(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(GameDataJsonNormalizerRepairsSheetExports));
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            string weaponsRaw =
                """[{"Type":"Dagger","Name":"Razor","Tier":1,"baseDamage":2,"attackSpeed":1,"damageBonusMin":1,"damageBonusMax":4,"attributeRequirements":"TECH","requirement value":15}]""";
            string weaponsNorm = GameDataJsonNormalizer.NormalizeForGameDataFile(GameConstants.WeaponsJson, weaponsRaw);
            var weapons = JsonSerializer.Deserialize<List<WeaponData>>(weaponsNorm, opts);
            TestBase.AssertNotNull(weapons, "weapons list", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, weapons!.Count, "one weapon", ref run, ref pass, ref fail);
            var req = weapons[0].AttributeRequirements;
            TestBase.AssertTrue(req != null && req.TryGetValue("technique", out int tec) && tec == 15,
                "weapon TECH + requirement value → dictionary", ref run, ref pass, ref fail);

            string statRaw =
                """[{"Name":"of Protection","mechanics":"[Armor:1]","Mechanics":[{"StatType":"Armor","Value":1}]}]""";
            string statNorm = GameDataJsonNormalizer.NormalizeForGameDataFile(GameConstants.StatBonusesJson, statRaw);
            var stats = JsonSerializer.Deserialize<List<StatBonus>>(statNorm, opts);
            TestBase.AssertNotNull(stats, "stat bonus list", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, stats!.Count, "one suffix", ref run, ref pass, ref fail);
            TestBase.AssertTrue(stats[0].Mechanics != null && stats[0].Mechanics!.Count == 1,
                "stat bonuses drop duplicate mechanics string", ref run, ref pass, ref fail);

            string modRaw = """[{"DiceResult":null,"MinValue":null,"MaxValue":null,"ItemRank":"Common","Name":"t","Effect":"x"}]""";
            string modNorm = GameDataJsonNormalizer.NormalizeForGameDataFile(GameConstants.ModificationsJson, modRaw);
            var mods = JsonSerializer.Deserialize<List<Modification>>(modNorm, opts);
            TestBase.AssertNotNull(mods, "mods list", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, mods!.Count, "one mod", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0, mods[0].DiceResult, "DiceResult coerced", ref run, ref pass, ref fail);
            TestBase.AssertTrue(mods[0].MinValue == 0.0, "MinValue coerced", ref run, ref pass, ref fail);
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

        private static void EnemiesWithTagsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesWithTagsRoundTrip));
            const string json = """
            [{"name":"BossImp","archetype":"Mage","baseHealth":50,"actions":["BOLT"],"isLiving":true,"description":"x","tags":["boss","demon"]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var t = a.RootElement[0].GetProperty("tags");
            TestBase.AssertTrue(t.ValueKind == JsonValueKind.Array, "tags array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("boss", t[0].GetString(), "tag0", ref run, ref pass, ref fail);
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

        private static void EnemiesTagsPlainStringAndListNormalized(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesTagsPlainStringAndListNormalized));
            // Plain cell and quoted comma list (JSON array in a cell must be quoted or commas break CSV columns).
            const string csv = """
            name,archetype,actions,isLiving,description,tags
            Goblin,Assassin,JAB,true,A goblin,goblin
            Orc,Berserker,SLAM,true,An orc,"orc, brute"
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
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
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
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
                "name,archetype,baseHealth,actions,isLiving,description\n" +
                "Goblin,Assassin,10,\"\"\"JAB\"\",\n\"\"TAUNT\"\"\",true,G\n";
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
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
            [{"name":"G","archetype":"Assassin","strength":1.05,"agility":0.22,"baseHealth":40,"actions":["J"],"isLiving":true,"description":"d"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            TestBase.AssertTrue(rows.Count >= 3, "category+header+data", ref run, ref pass, ref fail);
            var shortHeaders = rows[1].Select(o => o?.ToString() ?? "").ToList();
            TestBase.AssertEqual(JsonArraySheetConverter.EnemiesCanonicalHeaders.Length, shortHeaders.Count,
                "no trailing legacy root columns", ref run, ref pass, ref fail);
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

        private static void ArmorRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorRoundTrip));
            const string json = """
            [{"slot":"chest","name":"Tunic","armor":2,"tier":1,"attributeRequirements":null}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Armor);
            TestBase.AssertTrue(rows.Count >= 2, "header+data", ref run, ref pass, ref fail);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
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
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Armor);
            TestBase.AssertTrue(rows.Count >= 2, "header+data", ref run, ref pass, ref fail);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
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
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Armor);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
            using var a = JsonDocument.Parse(outJson);
            var t = a.RootElement[0].GetProperty("tags");
            TestBase.AssertTrue(t.ValueKind == JsonValueKind.Array, "tags array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("setPiece", t[0].GetString(), "tag0", ref run, ref pass, ref fail);
        }

        private static void ArmorCsvUtf8BomOnFirstHeaderImports(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorCsvUtf8BomOnFirstHeaderImports));
            string csv = "\uFEFFslot,name,armor,tier,attributeRequirements\nhead,Test Helm,2,1,\n";
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("head", first.GetProperty("slot").GetString(), "slot", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Test Helm", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
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
