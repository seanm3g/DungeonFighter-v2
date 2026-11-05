using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Settings → Item Modifiers save path. On 2026-10-28 a save turned Modifications.json into 23 copies of its last row
    /// (rows were matched by DiceResult, which is 0 on every sheet row) and dropped every column the DTO didn't name.
    /// </summary>
    public static class ItemModifiersSaveTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            SaveKeepsEveryRowAndExtraColumns(ref run, ref pass, ref fail);
        }

        private static void SaveKeepsEveryRowAndExtraColumns(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(SaveKeepsEveryRowAndExtraColumns));
            const string json = """
            [
              {"ItemRank":"Common","prefixCategory":"ADJECTIVE","Name":"Reinforced","Effect":"ARMOR","attributeRequirements":{"strength":2},"DiceResult":0,"MinValue":1,"MaxValue":1,"RolledValue":0},
              {"ItemRank":"Common","prefixCategory":"ADJECTIVE","Name":"Swift","Effect":"HIT","attributeRequirements":{"agility":2},"DiceResult":0,"MinValue":1,"MaxValue":1,"RolledValue":0},
              {"ItemRank":"Common","prefixCategory":"ADJECTIVE","Name":"Sturdy","Effect":"HEALTH BONUS","attributeRequirements":{"primary":5},"DiceResult":0,"MinValue":25,"MaxValue":25,"RolledValue":0}
            ]
            """;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mods = JsonSerializer.Deserialize<List<ItemModifiersDataService.ModificationData>>(json, opts)!;
            var groups = ItemModifiersDataCoordinator.BuildRarityGroups(mods, new List<string> { "Common", "Rare" });

            // Edit one row in the UI, as the edit dialog does.
            groups[0].Modifiers.First(m => m.Name == "Swift").MaxValue = 3;

            var saved = ItemModifiersDataCoordinator.CollectModifiers(groups);
            string outJson = JsonSerializer.Serialize(saved);
            using var doc = JsonDocument.Parse(outJson);
            var rows = doc.RootElement.EnumerateArray().ToList();

            TestBase.AssertEqual(3, rows.Count, "row count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Reinforced,Swift,Sturdy", string.Join(",", rows.Select(r => r.GetProperty("Name").GetString())),
                "names stay distinct and in order", ref run, ref pass, ref fail);
            TestBase.AssertEqual(3.0, rows[1].GetProperty("MaxValue").GetDouble(), "edit lands on its own row", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1.0, rows[0].GetProperty("MaxValue").GetDouble(), "other rows untouched", ref run, ref pass, ref fail);
            TestBase.AssertTrue(rows.All(r => r.TryGetProperty("prefixCategory", out _)), "prefixCategory kept", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, rows[1].GetProperty("attributeRequirements").GetProperty("agility").GetInt32(),
                "attributeRequirements kept", ref run, ref pass, ref fail);
        }
    }
}
