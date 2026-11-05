using System;
using System.IO;
using System.Text.Json.Nodes;
using RPGGame.Config;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>Overlay balance patches (base + differences) and step-band level curves.</summary>
    public static class BalancePatchOverlayTests
    {
        private static int _run, _pass, _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== BalancePatchOverlay Tests ===\n");
            _run = _pass = _fail = 0;
            TestResolveMergesAndChains();
            TestStoredJsonKeepsOnlyDifferences();
            TestCurveBands();
            TestBase.PrintSummary("BalancePatchOverlay Tests", _run, _pass, _fail);
        }

        private static string TempDir()
        {
            string d = Path.Combine(Path.GetTempPath(), "df-overlay-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(d);
            return d;
        }

        private static void TestResolveMergesAndChains()
        {
            Console.WriteLine("--- Resolve merges objects, replaces arrays, follows chains ---");
            string d = TempDir();
            File.WriteAllText(Path.Combine(d, "a.json"), "{\"combat\":{\"x\":1,\"y\":2},\"list\":[1,2,3],\"keep\":true}");
            File.WriteAllText(Path.Combine(d, "b.json"), "{\"base\":\"a\",\"combat\":{\"y\":5},\"list\":[9]}");
            File.WriteAllText(Path.Combine(d, "c.json"), "{\"base\":\"b\",\"combat\":{\"z\":7}}");
            var r = JsonNode.Parse(BalancePatchOverlay.ResolveJson(Path.Combine(d, "c.json")))!.AsObject();
            TestBase.AssertEqual(1, (int)r["combat"]!["x"]!, "x from the root base", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(5, (int)r["combat"]!["y"]!, "y overridden by the middle overlay", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(7, (int)r["combat"]!["z"]!, "z added by the top overlay", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1, r["list"]!.AsArray().Count, "arrays replace, not merge", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(r["base"] == null, "resolved JSON has no base key", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue((bool)r["keep"]!, "untouched keys survive", ref _run, ref _pass, ref _fail);
            Directory.Delete(d, true);
        }

        private static void TestStoredJsonKeepsOnlyDifferences()
        {
            Console.WriteLine("--- Saving an overlay writes only its differences ---");
            string d = TempDir();
            File.WriteAllText(Path.Combine(d, "a.json"), "{\"combat\":{\"x\":1,\"y\":2},\"keep\":true}");
            string over = Path.Combine(d, "b.json");
            File.WriteAllText(over, "{\"base\":\"a\",\"combat\":{\"y\":5}}");
            string full = "{\"combat\":{\"x\":1,\"y\":6},\"keep\":true}";
            var stored = JsonNode.Parse(BalancePatchOverlay.ToStoredJson(over, full))!.AsObject();
            TestBase.AssertEqual("a", (string)stored["base"]!, "base name kept", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(6, (int)stored["combat"]!["y"]!, "changed value stored", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(stored["combat"]!["x"] == null && stored["keep"] == null, "unchanged values left to the base", ref _run, ref _pass, ref _fail);
            File.WriteAllText(over, stored.ToJsonString());
            var round = JsonNode.Parse(BalancePatchOverlay.ResolveJson(over))!;
            TestBase.AssertTrue(JsonNode.DeepEquals(round, JsonNode.Parse(full)), "stored overlay resolves back to the full config", ref _run, ref _pass, ref _fail);
            string plain = Path.Combine(d, "a.json");
            TestBase.AssertEqual(full, BalancePatchOverlay.ToStoredJson(plain, full), "a full patch is stored whole", ref _run, ref _pass, ref _fail);
            Directory.Delete(d, true);
        }

        private static void TestCurveBands()
        {
            Console.WriteLine("--- Step bands ---");
            var c = new EnemyDamageCurveConfig
            {
                Enabled = true,
                BaseMultiplier = 2.0,
                Bands = { new CurveBand { From = 1, To = 5, Multiplier = 1.5 }, new CurveBand { From = 6, To = 10, Multiplier = 3.0 } },
                Anchors = { new EnemyDamageCurveAnchor { Level = 1, Multiplier = 99 } }
            };
            TestBase.AssertEqual(3.0, c.MultiplierAt(3), "inside the first band (x base)", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(6.0, c.MultiplierAt(6), "first level of the second band", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(6.0, c.MultiplierAt(40), "past the last band takes the nearest band", ref _run, ref _pass, ref _fail);
            c.Bands.Clear();
            TestBase.AssertEqual(198.0, c.MultiplierAt(1), "no bands falls back to anchors", ref _run, ref _pass, ref _fail);
        }
    }
}
