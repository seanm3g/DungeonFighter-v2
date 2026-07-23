using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    public static class JsonArraySheetConverterTriggersTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            TriggersCsvRoundTrip(ref run, ref pass, ref fail);
            TriggersLoaderResolveByName(ref run, ref pass, ref fail);
            TriggersEquipChannel(ref run, ref pass, ref fail);
        }

        private static void TriggersCsvRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersCsvRoundTrip));
            const string json = """
            [
              {"id":0,"name":"WoundMomentum","when":"ONCONNECT","count":"1","scope":"ACTION","mechanics":"hero_next_action_damage","value":10,"channel":"combat"},
              {"id":19,"name":"ClutchForgive","when":"ONCONNECT","count":"1","scope":"TURN","mechanics":"hero_hit_threshold","value":-1,"filters":"IFCLUTCH","channel":"combat"}
            ]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Triggers);
            string csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Triggers);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual(2, a.RootElement.GetArrayLength(), "row count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("WoundMomentum", a.RootElement[0].GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("ACTION", a.RootElement[0].GetProperty("scope").GetString(), "scope", ref run, ref pass, ref fail);
            TestBase.AssertEqual("IFCLUTCH", a.RootElement[1].GetProperty("filters").GetString(), "filters", ref run, ref pass, ref fail);
        }

        private static void TriggersLoaderResolveByName(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersLoaderResolveByName));
            TriggersLoader.ClearCache();
            TestBase.AssertTrue(TriggersLoader.TryGetByName("WoundMomentum", out var id), "find WoundMomentum", ref run, ref pass, ref fail);
            TestBase.AssertEqual("ONCONNECT", id.When, "when", ref run, ref pass, ref fail);
            TestBase.AssertEqual("hero_next_action_damage", id.Mechanics, "mechanics", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!id.IsEquipEffect, "combat channel", ref run, ref pass, ref fail);
            var bundle = id.ToBundle();
            TestBase.AssertEqual(10d, bundle.Value ?? 0, "value", ref run, ref pass, ref fail);
        }

        private static void TriggersEquipChannel(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersEquipChannel));
            TriggersLoader.ClearCache();
            TriggersLoader.ApplyTriggerNameToLists("GoldSetArmor", out var combat, out var equip);
            TestBase.AssertTrue(combat == null || combat.Count == 0, "no combat bundles", ref run, ref pass, ref fail);
            TestBase.AssertTrue(equip != null && equip.Count == 1, "one equip bundle", ref run, ref pass, ref fail);
            TestBase.AssertEqual("WHILE_EQUIPPED", equip![0].When, "when", ref run, ref pass, ref fail);
            TestBase.AssertEqual("armor", equip[0].Mechanics, "mechanics", ref run, ref pass, ref fail);
        }
    }
}
