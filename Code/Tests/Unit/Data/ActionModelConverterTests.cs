using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Data.ActionModel;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Live Actions.json → draft ActionModel. Every row must convert without throwing; the printed report lists
    /// each action's Losses (row information the model can't hold, or copies that disagree) and Notes.
    /// Set DF_DUMP_ACTION_MODELS=1 to also print every converted model.
    /// </summary>
    public static class ActionModelConverterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionModelConverter Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestKnownShapes(ref run, ref passed, ref failed);
            TestLiveActionsReport(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionModelConverter Tests", run, passed, failed);
        }

        private static void TestKnownShapes(ref int run, ref int passed, ref int failed)
        {
            static SpreadsheetActionJson Might(int bonusCopyValue) => new SpreadsheetActionJson
            {
                Action = "MIGHT", Damage = "100%", Speed = "1.00", NumberOfHits = "1", Block = "0", DPS = "100%", HeroSTR = "3",
                CadenceBundlesJson = "[{\"cadence\":\"DUNGEON\",\"enable\":\"1\",\"duration\":\"1\",\"mechanics\":\"hero_stat_bonus\"}]",
                ActionAttackBonusesJson = "{\"bonusGroups\":[{\"keyword\":\"DUNGEON\",\"cadenceType\":\"DUNGEON\",\"count\":1,\"bonuses\":[{\"type\":\"STR\",\"value\":"
                    + bonusCopyValue + "}],\"durationType\":\"DUNGEON\"}]}",
            };
            var (model, issues) = ActionModelConverter.Convert(Might(3));
            TestBase.AssertEqual("Self STR Add 3 [Dungeon]", model.Modifiers.SingleOrDefault()?.ToString() ?? "",
                "MIGHT converts to one dungeon STR modifier", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, issues.Count, "MIGHT converts losslessly", ref run, ref passed, ref failed);

            var (_, mismatchIssues) = ActionModelConverter.Convert(Might(2));
            TestBase.AssertTrue(mismatchIssues.Any(i => i.Severity == IssueSeverity.Loss && i.Field == "actionAttackBonusesJson"),
                "attack-bonus copy that disagrees is a Loss", ref run, ref passed, ref failed);

            var stray = new SpreadsheetActionJson { Action = "STRAY", Damage = "100%", Speed = "1", NumberOfHits = "1", HeroHit = "1" };
            var (_, strayIssues) = ActionModelConverter.Convert(stray);
            TestBase.AssertTrue(strayIssues.Any(i => i.Severity == IssueSeverity.Loss && i.Field == nameof(SpreadsheetActionJson.HeroHit)),
                "column no mechanic points at is a Loss", ref run, ref passed, ref failed);
        }

        private static void TestLiveActionsReport(ref int run, ref int passed, ref int failed)
        {
            string path = ActionLoader.ResolveActionsFilePath();
            var rows = JsonLoader.LoadJson<List<SpreadsheetActionJson>>(path, useCache: false, fallbackValue: new List<SpreadsheetActionJson>());
            TestBase.AssertTrue(rows.Count > 0, $"Actions.json rows load from {path}", ref run, ref passed, ref failed);

            bool dump = System.Environment.GetEnvironmentVariable("DF_DUMP_ACTION_MODELS") == "1";
            int lossless = 0, withLoss = 0, threw = 0;
            var lossFields = new Dictionary<string, int>(StringComparer.Ordinal);

            Console.WriteLine($"--- ActionModel conversion report ({rows.Count} actions) ---");
            foreach (var row in rows)
            {
                ActionModel model;
                List<ConversionIssue> issues;
                try
                {
                    (model, issues) = ActionModelConverter.Convert(row);
                }
                catch (Exception ex)
                {
                    threw++;
                    Console.WriteLine($"  THREW {row.Action}: {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                var losses = issues.Where(i => i.Severity == IssueSeverity.Loss).ToList();
                if (losses.Count == 0) lossless++; else withLoss++;
                foreach (var l in losses)
                    lossFields[l.Field] = lossFields.TryGetValue(l.Field, out int n) ? n + 1 : 1;

                if (issues.Count == 0 && !dump)
                    continue;
                Console.WriteLine($"  {row.Action}{(losses.Count > 0 ? "  [LOSSY]" : "")}");
                if (dump)
                {
                    var c = model.Chassis;
                    Console.WriteLine($"      chassis {c.DamagePercent:0.#}% x{c.Hits} @ {c.Speed:0.##} block {c.Block:0} -> {c.StrikeTarget}"
                        + (model.Opener ? " OPENER" : "") + (model.Finisher ? " FINISHER" : ""));
                    foreach (var m in model.Modifiers) Console.WriteLine($"      {m}");
                    foreach (var r in model.RollOps) Console.WriteLine($"      {r}");
                    foreach (var s in model.StripOps) Console.WriteLine($"      {s}");
                }
                foreach (var issue in issues.OrderBy(i => i.Severity))
                    Console.WriteLine($"      {issue}");
            }

            Console.WriteLine($"--- {lossless} lossless, {withLoss} with losses, {threw} threw ---");
            foreach (var (field, n) in lossFields.OrderByDescending(kv => kv.Value))
                Console.WriteLine($"      loss field {field}: {n}");
            Console.WriteLine();

            TestBase.AssertEqual(0, threw, "every Actions.json row converts without throwing", ref run, ref passed, ref failed);
        }
    }
}
