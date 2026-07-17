using System;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;
using RPGGame.Tests;
using System.Collections.Generic;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterStatBonusesTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            StatBonusesRoundTrip(ref run, ref pass, ref fail);
            StatBonusesLegacyWeightCsvImportsAsCommon(ref run, ref pass, ref fail);
            StatBonusesBracketMechanicsCsvImport(ref run, ref pass, ref fail);
            StatBonusesCsvImportIgnoresColumnsBeyondCanonical(ref run, ref pass, ref fail);
            StatBonusesPushUsesOnlyCanonicalColumns(ref run, ref pass, ref fail);
            StatBonusesRequirementsBracketImport(ref run, ref pass, ref fail);
            StatBonusesRequirementsImportFromStatRequirementHeader(ref run, ref pass, ref fail);
            StatBonusesRequirementsRoundTripPushKeepsBracketCell(ref run, ref pass, ref fail);
            StatBonusesLiveSheetLayoutCsvImport(ref run, ref pass, ref fail);
            GameDataJsonNormalizerRepairsSheetExports(ref run, ref pass, ref fail);
        }



        private static void StatBonusesRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesRoundTrip));
            const string json = """
            [{"Name":"of Swiftness","Description":"+0.005 attack speed","Value":0.005,"Rarity":"Uncommon","StatType":"AttackSpeed"}]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.StatBonuses);
            var csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.StatBonuses);
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
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.StatBonuses);
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
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.StatBonuses);
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
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.StatBonuses);
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
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.StatBonuses);
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
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.StatBonuses);
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
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.StatBonuses);
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
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.StatBonuses);
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


        /// <summary>Matches the live SUFFIXES tab: blank column-A header for affix names, lowercase headers, trailing stat requirement.</summary>
        private static void StatBonusesLiveSheetLayoutCsvImport(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(StatBonusesLiveSheetLayoutCsvImport));
            const string csv = """
            ,tags,rarity,Description,stat type,mechanics,Mechanic 1,Mechaniv 1 value,Mechanc 2,Mechanic 2 values,Mechanic 3,Mechanic 3 values,stat requirement
            of Protection,,Common,armor,,[armor:1],armor,1,,,,,#N/A
            of fleeting,,Common,attack speed,,[speed:5],speed,5,,,,,#N/A
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.StatBonuses);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual(2, a.RootElement.GetArrayLength(), "two suffix rows", ref run, ref pass, ref fail);

            var first = a.RootElement[0];
            TestBase.AssertEqual("of Protection", first.GetProperty("Name").GetString(), "blank header col A → Name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Common", first.GetProperty("Rarity").GetString(), "rarity", ref run, ref pass, ref fail);
            TestBase.AssertEqual("armor", first.GetProperty("Description").GetString(), "description", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("mechanics", out _), "raw mechanics string dropped", ref run, ref pass, ref fail);
            TestBase.AssertFalse(first.TryGetProperty("tags", out _), "tags helper column dropped", ref run, ref pass, ref fail);
            TestBase.AssertTrue(first.TryGetProperty("Mechanics", out var mech) && mech.ValueKind == JsonValueKind.Array,
                "Mechanics array", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Armor", mech[0].GetProperty("StatType").GetString(), "armor mechanic normalized", ref run, ref pass, ref fail);

            var second = a.RootElement[1];
            TestBase.AssertEqual("of fleeting", second.GetProperty("Name").GetString(), "second row name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("AttackSpeed", second.GetProperty("Mechanics")[0].GetProperty("StatType").GetString(),
                "speed→AttackSpeed", ref run, ref pass, ref fail);
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
    }
}
