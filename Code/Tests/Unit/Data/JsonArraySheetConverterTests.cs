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
            ModificationsPushIncludesTagsColumn(ref run, ref pass, ref fail);
            ModificationsCsvImportTagsColumn(ref run, ref pass, ref fail);
            ModificationsCsvImportValueAndAttributeRequirementColumns(ref run, ref pass, ref fail);
            MergeJsonRootArraysCoreThenExtra(ref run, ref pass, ref fail);
            SplitModificationsMergedJsonMaterialQualityToSecondFile(ref run, ref pass, ref fail);
            StatBonusesRoundTrip(ref run, ref pass, ref fail);
            StatBonusesLegacyWeightCsvImportsAsCommon(ref run, ref pass, ref fail);
            StatBonusesBracketMechanicsCsvImport(ref run, ref pass, ref fail);
            StatBonusesCsvImportIgnoresColumnsBeyondCanonical(ref run, ref pass, ref fail);
            StatBonusesPushUsesOnlyCanonicalColumns(ref run, ref pass, ref fail);
            StatBonusesRequirementsBracketImport(ref run, ref pass, ref fail);
            StatBonusesRequirementsImportFromStatRequirementHeader(ref run, ref pass, ref fail);
            StatBonusesRequirementsRoundTripPushKeepsBracketCell(ref run, ref pass, ref fail);
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
            EnemiesNewSheetLayoutImport(ref run, ref pass, ref fail);
            EnemiesArchetypeCanonicalization(ref run, ref pass, ref fail);
            EnemiesActionsPipePushFormat(ref run, ref pass, ref fail);
            EnvironmentsRoundTrip(ref run, ref pass, ref fail);
            EnvironmentsLegacyLocationPush(ref run, ref pass, ref fail);
            EnvironmentsSheetColumnsPull(ref run, ref pass, ref fail);
            EnvironmentsWithEnemiesRoundTrip(ref run, ref pass, ref fail);
            DungeonsRoundTrip(ref run, ref pass, ref fail);
            DungeonsPossibleEnemiesPipePushFormat(ref run, ref pass, ref fail);
            DungeonsPossibleEnemiesPipeDelimitedNormalized(ref run, ref pass, ref fail);
            ArmorRoundTrip(ref run, ref pass, ref fail);
            ArmorExtendedColumnsRoundTrip(ref run, ref pass, ref fail);
            ArmorWithTagsRoundTrip(ref run, ref pass, ref fail);
            ArmorCsvUtf8BomOnFirstHeaderImports(ref run, ref pass, ref fail);
            ArmorSpreadsheetTemplateHeadersRoundTrip(ref run, ref pass, ref fail);
            ConsumablesCsvHumanHeadersToCanonicalJson(ref run, ref pass, ref fail);
            ConsumablesJsonPushCsvRoundTrip(ref run, ref pass, ref fail);
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
            TestBase.AssertTrue(csv.Contains("magical, starter") || csv.Contains("magical,starter"),
                "tags pushed as comma-separated cell", ref run, ref pass, ref fail);
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

        private static void ModificationsPushIncludesTagsColumn(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsPushIncludesTagsColumn));
            const string json = """
            [{"DiceResult":0,"ItemRank":"Common","Name":"Bone","prefixCategory":"MATERIAL","MinValue":2,"MaxValue":2,"tags":["bone","barbarian"]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Modifications);
            var headers = rows[0].Select(c => c?.ToString() ?? "").ToList();
            TestBase.AssertTrue(headers.Contains("tags"), "push headers include tags", ref run, ref pass, ref fail);
            int tagsIx = headers.FindIndex(h => string.Equals(h, "tags", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertEqual("bone, barbarian", rows[1][tagsIx]?.ToString(), "tags cell", ref run, ref pass, ref fail);
        }

        private static void ModificationsCsvImportTagsColumn(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsCsvImportTagsColumn));
            const string csv = """
            DiceResult,ItemRank,Name,Description,Effect,value,prefixCategory,ATTRIBUTE REQUIREMENT,REQUIREMENT VALUE,ATTRIBUTE REQUREMENT,MaxValue,MinValue,RolledValue,tags
            0,Common,flaming,,BURN,10,ADJECTIVE,,,,10,10,0,fire
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Modifications);
            using var doc = JsonDocument.Parse(outJson);
            var tags = doc.RootElement[0].GetProperty("tags");
            TestBase.AssertEqual(1, tags.GetArrayLength(), "one tag", ref run, ref pass, ref fail);
            TestBase.AssertEqual("fire", tags[0].GetString(), "fire tag", ref run, ref pass, ref fail);
        }

        /// <summary>PREFIX sheet A–I: <c>value</c> maps to Min/Max; <c>ATTRIBUTE REQUIREMENT</c> + <c>REQUIREMENT VALUE</c> → <c>attributeRequirements</c>.</summary>
        private static void ModificationsCsvImportValueAndAttributeRequirementColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ModificationsCsvImportValueAndAttributeRequirementColumns));
            const string csv = """
            DiceResult,ItemRank,Name,Description,Effect,value,prefixCategory,ATTRIBUTE REQUIREMENT,REQUIREMENT VALUE
            0,Common,Reinforced,,ARMOR,1,ADJECTIVE,strength,2
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Modifications);
            using var a = JsonDocument.Parse(outJson);
            var row = a.RootElement[0];
            TestBase.AssertEqual(1, row.GetProperty("MinValue").GetDouble(), "value→MinValue", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, row.GetProperty("MaxValue").GetDouble(), "value→MaxValue", ref run, ref pass, ref fail);
            TestBase.AssertFalse(row.TryGetProperty("value", out _), "value column removed", ref run, ref pass, ref fail);
            var reqs = row.GetProperty("attributeRequirements");
            TestBase.AssertEqual(2, reqs.GetProperty("strength").GetInt32(), "strength requirement", ref run, ref pass, ref fail);
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

        /// <summary>SUFFIXES tab uses A–H only; CSV columns past Requirements must not become JSON properties.</summary>
        private static void StatBonusesCsvImportIgnoresColumnsBeyondCanonical(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesCsvImportIgnoresColumnsBeyondCanonical));
            const string csv = """
Name,Description,Value,Rarity,StatType,ItemRank,Mechanics,Requirements,Mechanic 1 type,Junk
of Test,desc,1,Common,Armor,,"[ARMOR:2]","[strength:5]",Armor,999
""";
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("of Test", first.GetProperty("Name").GetString(), "Name", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("Junk", out _), "trailing helper column dropped", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("Mechanic 1 type", out _), "helper col dropped", ref run, ref pass, ref fail);
        }

        /// <summary>Push rows must not add columns past the canonical SUFFIXES set from stray JSON keys.</summary>
        private static void StatBonusesPushUsesOnlyCanonicalColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesPushUsesOnlyCanonicalColumns));
            const string json = """
            [{"Name":"of X","Description":"d","Value":1,"Rarity":"Common","StatType":"Armor","tags":["t"],"mechanics":"[ARMOR:1]"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.StatBonuses);
            TestBase.AssertEqual(2, rows.Count, "header+data", ref run, ref pass, ref fail);
            TestBase.AssertEqual(8, rows[0].Count, "eight headers (A–H including Requirements)", ref run, ref pass, ref fail);
        }

        /// <summary>SUFFIXES <c>Requirements</c> bracket cell parses into a canonical lowercase dictionary.</summary>
        private static void StatBonusesRequirementsBracketImport(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesRequirementsBracketImport));
            const string csv = """
Name,Description,Value,Rarity,StatType,ItemRank,Mechanics,Requirements
of the Titan,test,0,Rare,STR,,"[STR:2]","[STR:5,strength:4,primary:15]"
""";
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertTrue(first.TryGetProperty("Requirements", out var reqEl) && reqEl.ValueKind == JsonValueKind.Object,
                "Requirements parsed to object", ref run, ref pass, ref fail);
            TestBase.AssertTrue(reqEl.TryGetProperty("strength", out var strEl) && strEl.GetInt32() == 5,
                "duplicate strength entries collapse to max (5)", ref run, ref pass, ref fail);
            TestBase.AssertTrue(reqEl.TryGetProperty("primary", out var primEl) && primEl.GetInt32() == 15,
                "primary keyword carried through canonicalization", ref run, ref pass, ref fail);
        }

        /// <summary>Legacy header <c>Stat Requirement</c> now imports into <c>Requirements</c> (was: ItemRank).</summary>
        private static void StatBonusesRequirementsImportFromStatRequirementHeader(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesRequirementsImportFromStatRequirementHeader));
            const string csv = """
Name,Description,Value,Rarity,StatType,ItemRank,Mechanics,Stat Requirement
of Sage,desc,0,Rare,INT,,"[INT:2]","[intelligence:10]"
""";
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertTrue(first.TryGetProperty("Requirements", out var reqEl) && reqEl.ValueKind == JsonValueKind.Object,
                "Stat Requirement column lands in Requirements", ref run, ref pass, ref fail);
            TestBase.AssertTrue(reqEl.TryGetProperty("intelligence", out var intEl) && intEl.GetInt32() == 10,
                "intelligence threshold imported", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!first.TryGetProperty("ItemRank", out var rankEl) || rankEl.ValueKind == JsonValueKind.String,
                "ItemRank stays untouched (catalog rarity filter, not requirements)", ref run, ref pass, ref fail);
        }

        /// <summary>Round-trip: Requirements object on input renders back as a sheet-friendly bracket cell on push.</summary>
        private static void StatBonusesRequirementsRoundTripPushKeepsBracketCell(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesRequirementsRoundTripPushKeepsBracketCell));
            const string json = """
            [{"Name":"of Titan","Description":"","Value":0,"Rarity":"Rare","StatType":"STR","ItemRank":"","Mechanics":[{"StatType":"STR","Value":2}],"Requirements":{"strength":5,"primary":15}}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.StatBonuses);
            TestBase.AssertEqual(2, rows.Count, "header+data", ref run, ref pass, ref fail);

            int reqColIdx = -1;
            for (int i = 0; i < rows[0].Count; i++)
            {
                if (string.Equals(rows[0][i]?.ToString(), "Requirements", StringComparison.Ordinal))
                {
                    reqColIdx = i;
                    break;
                }
            }
            TestBase.AssertTrue(reqColIdx >= 0, "Requirements column present", ref run, ref pass, ref fail);
            string cell = rows[1][reqColIdx]?.ToString() ?? "";
            TestBase.AssertTrue(cell.StartsWith("[", StringComparison.Ordinal) && cell.EndsWith("]", StringComparison.Ordinal),
                "push emits bracket form", ref run, ref pass, ref fail);
            TestBase.AssertTrue(cell.Contains("strength:5", StringComparison.Ordinal),
                "bracket includes strength:5", ref run, ref pass, ref fail);
            TestBase.AssertTrue(cell.Contains("primary:15", StringComparison.Ordinal),
                "bracket includes primary:15", ref run, ref pass, ref fail);
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
            TestBase.AssertTrue(cat.Contains("HEALTH", StringComparison.OrdinalIgnoreCase), "category HEALTH", ref run, ref pass, ref fail);
            TestBase.AssertTrue(hdr.Contains("region", StringComparison.Ordinal), "region col", ref run, ref pass, ref fail);
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
            [{"name":"BossImp","archetype":"Sage","baseHealth":50,"actions":["BOLT"],"isLiving":true,"description":"x","tags":["boss","demon"]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            var csv = RowsToCsv(rows);
            TestBase.AssertTrue(csv.Contains("boss, demon") || csv.Contains("boss,demon"),
                "tags pushed as comma-separated cell", ref run, ref pass, ref fail);
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

        /// <summary>New ENEMIES layout: Region-first row-2 headers, HEALTH band, mixed casing.</summary>
        private static void EnemiesNewSheetLayoutImport(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesNewSheetLayoutImport));
            const string csv = """
            ,,,,,,base attributes,,,,growth,,,,HEALTH,,,,,
            Region,Biome,Location,Rarity,Name,tags,Archetype,Strength,agility,technique,Intelligence,strength,agility,technique,Intelligence,baseHealth,healthGrowthPerLevel,actions,isLiving,description,colorOverride
            forest,Forest,,Common,Goblin,goblin,Assassin,3,4,5,6,0.1,0.2,0.3,0.4,40,2.35,"[""JAB"",""TAUNT""]",true,A quick goblin,
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Enemies);
            using var a = JsonDocument.Parse(outJson);
            var first = a.RootElement[0];
            TestBase.AssertEqual("forest", first.GetProperty("region").GetString(), "region", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Forest", first.GetProperty("biome").GetString(), "biome", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Common", first.GetProperty("rarity").GetString(), "rarity", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Goblin", first.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual(40, first.GetProperty("baseHealth").GetInt32(), "baseHealth", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2.35, first.GetProperty("healthGrowthPerLevel").GetDouble(), "healthGrowthPerLevel", ref run, ref pass, ref fail);
            TestBase.AssertEqual(3, first.GetProperty("baseAttributes").GetProperty("strength").GetInt32(), "ba.str", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.4, first.GetProperty("growthPerLevel").GetProperty("intelligence").GetDouble(), "gp.int", ref run, ref pass, ref fail);
            TestBase.AssertEqual("JAB", first.GetProperty("actions")[0].GetString(), "action0", ref run, ref pass, ref fail);
        }

        private static void EnemiesActionsPipePushFormat(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesActionsPipePushFormat));
            const string json = """
            [{"name":"Goblin","archetype":"Assassin","baseHealth":40,"actions":["JAB","TAUNT"],"isLiving":true,"description":"A goblin"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            int actionsIdx = Array.IndexOf(JsonArraySheetConverter.EnemiesCanonicalHeaders, "actions");
            TestBase.AssertEqual("JAB|TAUNT", rows[2][actionsIdx]?.ToString(), "actions pipe cell", ref run, ref pass, ref fail);
        }

        private static void EnemiesArchetypeCanonicalization(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnemiesArchetypeCanonicalization));
            const string json = """
            [{"name":"Sage Mob","archetype":"sage","baseHealth":40,"actions":["J"],"isLiving":true,"description":"d"}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Enemies);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Enemies);
            using var doc = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Sage", doc.RootElement[0].GetProperty("archetype").GetString(), "archetype title case", ref run, ref pass, ref fail);
        }


        private static void EnvironmentsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(EnvironmentsRoundTrip));
            const string json = """
            [{"region":"north","biome":"Forest","location":"Entrance","tags":["overgrown"],"description":"Big door.","actions":[{"name":"Magical Barrier","weight":1}]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Environments);
            TestBase.AssertEqual(7, rows[0].Count, "seven canonical columns only", ref run, ref pass, ref fail);
            TestBase.AssertEqual("region", rows[0][0]?.ToString(), "header region", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Magical Barrier", rows[1][5]?.ToString(), "actions pipe cell", ref run, ref pass, ref fail);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Environments);
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
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Environments);
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
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Environments);
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
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Environments);
            var csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Environments);
            using var a = JsonDocument.Parse(outJson);
            var enemies = a.RootElement[0].GetProperty("enemies");
            TestBase.AssertTrue(enemies.ValueKind == JsonValueKind.Array, "enemies array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wolf", enemies[0].GetProperty("name").GetString(), "enemy0", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0.7, enemies[0].GetProperty("weight").GetDouble(), "w0", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Wolf:0.7|Spider:0.3", rows[1][6]?.ToString(), "weighted enemies push cell", ref run, ref pass, ref fail);
        }

        private static void DungeonsPossibleEnemiesPipePushFormat(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(DungeonsPossibleEnemiesPipePushFormat));
            const string json = """
            [{"name":"Ancient Forest","theme":"Forest","minLevel":1,"maxLevel":10,"possibleEnemies":["Goblin","Wolf"]}]
            """;
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Dungeons);
            TestBase.AssertEqual(JsonArraySheetConverter.DungeonsCanonicalHeaders.Length, rows[0].Count,
                "six canonical columns only", ref run, ref pass, ref fail);
            int peIdx = Array.IndexOf(JsonArraySheetConverter.DungeonsCanonicalHeaders, "possibleEnemies");
            TestBase.AssertEqual("Goblin|Wolf", rows[1][peIdx]?.ToString(), "possibleEnemies pipe cell", ref run, ref pass, ref fail);
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
            TestBase.AssertTrue(csv.Contains("setpiece, magical") || csv.Contains("setpiece,magical"),
                "tags pushed as comma-separated cell", ref run, ref pass, ref fail);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Armor);
            using var a = JsonDocument.Parse(outJson);
            var t = a.RootElement[0].GetProperty("tags");
            TestBase.AssertTrue(t.ValueKind == JsonValueKind.Array, "tags array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("setpiece", t[0].GetString(), "tag0", ref run, ref pass, ref fail);
        }

        private static void ArmorSpreadsheetTemplateHeadersRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorSpreadsheetTemplateHeadersRoundTrip));
            string csv = """
                slot,name,armor,tags,STRENGTH,AGILITY,TECHNIQUE,INTELLIGENCE,HIT,COMBO,CRIT,# OF ACTION SLOTS,# OF BONUS ACTIONS,tier,attributeRequirements,requirement value
                head,Helm,2,,0,2,0,0,0,0,0,1,0,1,strength,5
                """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Armor);
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
            var pushRows = JsonArraySheetConverter.BuildPushValueRows(roundJson, GameDataTabularSheetKind.Armor);
            var hdr = pushRows[0].Select(x => x?.ToString() ?? "").ToArray();
            TestBase.AssertEqual(JsonArraySheetConverter.ArmorCanonicalHeaders.Length + 3, hdr.Length,
                "canonical + extraActionSlotsMin/Max/attackSpeed", ref run, ref pass, ref fail);
            for (int i = 0; i < JsonArraySheetConverter.ArmorCanonicalHeaders.Length; i++)
            {
                TestBase.AssertEqual(JsonArraySheetConverter.ArmorCanonicalHeaders[i], hdr[i], "hdr " + i, ref run, ref pass, ref fail);
            }

            int attrIdx = Array.IndexOf(hdr, "attributeRequirements");
            int reqIdx = Array.IndexOf(hdr, "requirement value");
            TestBase.AssertTrue(attrIdx >= 0 && reqIdx >= 0, "requirement columns", ref run, ref pass, ref fail);
            TestBase.AssertEqual("TECHNIQUE", pushRows[1][attrIdx]?.ToString(), "pushed attr abbrev", ref run, ref pass, ref fail);
            TestBase.AssertEqual("3", pushRows[1][reqIdx]?.ToString(), "pushed req val", ref run, ref pass, ref fail);
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

        private static void ConsumablesCsvHumanHeadersToCanonicalJson(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ConsumablesCsvHumanHeadersToCanonicalJson));
            const string csv = """
            Display name,Internal kind,Effect (dungeon-scoped until run ends),Typical potency*
            Waxed Apple,Food,heal,1
            """;
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Consumables);
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
            var rows = JsonArraySheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Consumables);
            string csv = RowsToCsv(rows);
            string outJson = JsonArraySheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Consumables);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Waxed Apple", a.RootElement[0].GetProperty("displayName").GetString(), "displayName", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Food", a.RootElement[0].GetProperty("internalKind").GetString(), "internalKind", ref run, ref pass, ref fail);
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
