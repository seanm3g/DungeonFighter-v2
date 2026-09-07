using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterCharmsTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            CharmsCsvHumanHeadersToCanonicalJson(ref run, ref pass, ref fail);
            CharmsJsonPushCsvRoundTrip(ref run, ref pass, ref fail);
        }

        private static void CharmsCsvHumanHeadersToCanonicalJson(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(CharmsCsvHumanHeadersToCanonicalJson));
            const string csv = """
            NAME,DESCRIPTION,LAYER,AMPLIFYMINT,AMPLIFYCONVERT,AMPLIFYCLASSDEFENSE,AMPLIFYCLASSTAGDAMAGE,AMPLIFYANIMALLADDER,UNLOCKANIMALACTION,RARITY,TAGS
            Mark of Class,Amplifies class,class,1,1,1.5,1.25,1,FALSE,Rare,"charm,class"
            """;
            string outJson = SheetConverter.CsvToJsonArrayText(csv.Trim(), GameDataTabularSheetKind.Charms);
            using var a = JsonDocument.Parse(outJson);
            var row = a.RootElement[0];
            TestBase.AssertEqual("Mark of Class", row.GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("class", row.GetProperty("layer").GetString(), "layer", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1.5, row.GetProperty("amplifyClassDefense").GetDouble(), "amplifyClassDefense", ref run, ref pass, ref fail);
            TestBase.AssertEqual(false, row.GetProperty("unlockAnimalAction").GetBoolean(), "unlockAnimalAction", ref run, ref pass, ref fail);
        }

        private static void CharmsJsonPushCsvRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(CharmsJsonPushCsvRoundTrip));
            const string json = """
            [
              {"name":"Forge Sigil","description":"Material amp","layer":"material","amplifyMint":1.5,"amplifyConvert":1.5,"amplifyClassDefense":1,"amplifyClassTagDamage":1,"amplifyAnimalLadder":1,"unlockAnimalAction":false,"rarity":"Rare","tags":["charm","material"]}
            ]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Charms);
            string csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Charms);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual("Forge Sigil", a.RootElement[0].GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("material", a.RootElement[0].GetProperty("layer").GetString(), "layer", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1.5, a.RootElement[0].GetProperty("amplifyMint").GetDouble(), "amplifyMint", ref run, ref pass, ref fail);
        }
    }
}
