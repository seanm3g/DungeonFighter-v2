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
            TriggersScaleFromRoundTrip(ref run, ref pass, ref fail);
            TriggersSeedDescriptionsPresent(ref run, ref pass, ref fail);
            TriggersAuthoringMetaPresent(ref run, ref pass, ref fail);
            TriggersSheetMetaDerivation(ref run, ref pass, ref fail);
        }

        private static void TriggersCsvRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersCsvRoundTrip));
            const string json = """
            [
              {"id":0,"name":"WoundMomentum","description":"On connect, your next action deals +10% damage.","effectTarget":"hero","when":"ONCONNECT","whenArg":"","count":"1","scope":"ACTION","mechanics":"hero_next_action_damage","mechanicArg":"","value":10,"channel":"combat","scaleFrom":"STR"},
              {"id":19,"name":"ClutchForgive","description":"While clutch, a connect improves hit threshold by 1 for the turn.","when":"ONCONNECT","count":"1","scope":"TURN","mechanics":"hero_hit_threshold","value":-1,"filters":"IFCLUTCH","channel":"combat"}
            ]
            """;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Triggers);
            string csv = JsonArraySheetConverterTestHelpers.RowsToCsv(rows);
            string outJson = SheetConverter.CsvToJsonArrayText(csv, GameDataTabularSheetKind.Triggers);
            using var a = JsonDocument.Parse(outJson);
            TestBase.AssertEqual(2, a.RootElement.GetArrayLength(), "row count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("WoundMomentum", a.RootElement[0].GetProperty("name").GetString(), "name", ref run, ref pass, ref fail);
            TestBase.AssertEqual("On connect, your next action deals +10% damage.", a.RootElement[0].GetProperty("description").GetString(), "description", ref run, ref pass, ref fail);
            TestBase.AssertEqual("hero", a.RootElement[0].GetProperty("effectTarget").GetString(), "effectTarget", ref run, ref pass, ref fail);
            TestBase.AssertEqual("ACTION", a.RootElement[0].GetProperty("scope").GetString(), "scope", ref run, ref pass, ref fail);
            TestBase.AssertEqual("IFCLUTCH", a.RootElement[1].GetProperty("filters").GetString(), "filters", ref run, ref pass, ref fail);
            TestBase.AssertEqual("STR", a.RootElement[0].GetProperty("scaleFrom").GetString(), "scaleFrom", ref run, ref pass, ref fail);
            // Pull fills blank effectTarget on row 1 from mechanics.
            TestBase.AssertEqual("hero", a.RootElement[1].GetProperty("effectTarget").GetString(), "derived effectTarget", ref run, ref pass, ref fail);
            TestBase.AssertTrue(rows[0].Contains("effectTarget"), "header includes effectTarget", ref run, ref pass, ref fail);
            TestBase.AssertTrue(rows[0].Contains("whenArg"), "header includes whenArg", ref run, ref pass, ref fail);
            TestBase.AssertTrue(rows[0].Contains("mechanicArg"), "header includes mechanicArg", ref run, ref pass, ref fail);
            TestBase.AssertTrue(rows[0].Contains("description"), "header includes description", ref run, ref pass, ref fail);
        }

        private static void TriggersLoaderResolveByName(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersLoaderResolveByName));
            TriggersLoader.ClearCache();
            TestBase.AssertTrue(TriggersLoader.TryGetByName("WoundMomentum", out var id), "find WoundMomentum", ref run, ref pass, ref fail);
            TestBase.AssertEqual("ONCONNECT", id.When, "when", ref run, ref pass, ref fail);
            TestBase.AssertEqual("hero_next_action_damage", id.Mechanics, "mechanics", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(id.Description), "description present", ref run, ref pass, ref fail);
            TestBase.AssertEqual("hero", id.EffectTarget, "effectTarget", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!id.IsEquipEffect, "combat channel", ref run, ref pass, ref fail);
            var bundle = id.ToBundle();
            TestBase.AssertEqual(10d, bundle.Value ?? 0, "value", ref run, ref pass, ref fail);
        }

        private static void TriggersSeedDescriptionsPresent(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersSeedDescriptionsPresent));
            var seed = ItemTriggerIdentityCatalog.BuildSeedRows();
            TestBase.AssertEqual(106, seed.Count, "seed count", ref run, ref pass, ref fail);
            int missing = seed.Count(r => string.IsNullOrWhiteSpace(r.Description));
            TestBase.AssertEqual(0, missing, "all seed descriptions non-empty", ref run, ref pass, ref fail);
        }

        private static void TriggersAuthoringMetaPresent(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersAuthoringMetaPresent));
            var seed = ItemTriggerIdentityCatalog.BuildSeedRows();
            int missingTarget = seed.Count(r => string.IsNullOrWhiteSpace(r.EffectTarget));
            TestBase.AssertEqual(0, missingTarget, "all seed effectTarget non-empty", ref run, ref pass, ref fail);

            TriggersLoader.ClearCache();
            TestBase.AssertTrue(TriggersLoader.TryGetByName("LuckySevenHit", out var lucky), "LuckySevenHit", ref run, ref pass, ref fail);
            TestBase.AssertEqual("7", lucky.WhenArg, "LuckySevenHit whenArg", ref run, ref pass, ref fail);
            TestBase.AssertTrue(TriggersLoader.TryGetByName("SlotTwoEncore", out var encore), "SlotTwoEncore", ref run, ref pass, ref fail);
            TestBase.AssertEqual("2", encore.MechanicArg, "SlotTwoEncore mechanicArg", ref run, ref pass, ref fail);
            TestBase.AssertEqual("strip", encore.EffectTarget, "SlotTwoEncore effectTarget", ref run, ref pass, ref fail);
        }

        private static void TriggersSheetMetaDerivation(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersSheetMetaDerivation));
            TestBase.AssertEqual("hero", TriggerIdentitySheetMeta.DeriveEffectTarget("hero_next_action_damage"), "hero_", ref run, ref pass, ref fail);
            TestBase.AssertEqual("enemy", TriggerIdentitySheetMeta.DeriveEffectTarget("enemy_next_action_damage"), "enemy_", ref run, ref pass, ref fail);
            TestBase.AssertEqual("self", TriggerIdentitySheetMeta.DeriveEffectTarget("harden"), "harden", ref run, ref pass, ref fail);
            TestBase.AssertEqual("foe", TriggerIdentitySheetMeta.DeriveEffectTarget("expose"), "expose", ref run, ref pass, ref fail);
            TestBase.AssertEqual("strip", TriggerIdentitySheetMeta.DeriveEffectTarget("retrigger_slot:2"), "retrigger", ref run, ref pass, ref fail);
            TestBase.AssertEqual("hero", TriggerIdentitySheetMeta.DeriveEffectTarget("salvage_miss"), "salvage", ref run, ref pass, ref fail);
            TestBase.AssertEqual("7", TriggerIdentitySheetMeta.ParseColonArg("ONNATURALROLL:7"), "whenArg", ref run, ref pass, ref fail);
            TestBase.AssertEqual("PUNCH HARD", TriggerIdentitySheetMeta.ParseColonArg("grant_action:PUNCH HARD"), "mechanicArg", ref run, ref pass, ref fail);
            TestBase.AssertEqual("", TriggerIdentitySheetMeta.ParseColonArg("ONCONNECT"), "no arg", ref run, ref pass, ref fail);
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

        private static void TriggersScaleFromRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TriggersScaleFromRoundTrip));
            TriggersLoader.ClearCache();
            TestBase.AssertTrue(TriggersLoader.TryGetByName("StrCleave", out var id) || ItemTriggerIdentityCatalog.Get(81).Name == "StrCleave",
                "StrCleave identity exists", ref run, ref pass, ref fail);
            var seed = ItemTriggerIdentityCatalog.ToBundle(ItemTriggerIdentityCatalog.Get(81));
            TestBase.AssertEqual("STR", seed.ScaleFrom, "seed scaleFrom", ref run, ref pass, ref fail);
        }
    }
}
