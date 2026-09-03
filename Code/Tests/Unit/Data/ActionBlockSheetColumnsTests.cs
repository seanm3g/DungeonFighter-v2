using System;
using System.Text.Json;
using RPGGame;
using RPGGame.Combat.Calculators;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>BLOCK sheet column, free percent points, CSV/JSON round-trip, Settings merge persist.</summary>
    public static class ActionBlockSheetColumnsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Block Sheet Column Tests ===\n");
            int run = 0, pass = 0, fail = 0;

            EnsureHeaderInsertsAfterSpeed(ref run, ref pass, ref fail);
            EnsureHeaderInsertsAfterSpeedX(ref run, ref pass, ref fail);
            EnsureHeaderIdempotent(ref run, ref pass, ref fail);
            CollectInsertedColumnsFindsBlockOnly(ref run, ref pass, ref fail);
            RenameEnergyToBlockInPlace(ref run, ref pass, ref fail);
            RenameSkipsWhenBlockExists(ref run, ref pass, ref fail);
            CollectLeftoverEnergyWhenBlockExists(ref run, ref pass, ref fail);
            ConvertParsesMissingAndFreeValues(ref run, ref pass, ref fail);
            CsvRoundTripBlock(ref run, ref pass, ref fail);
            JsonRoundTripBlock(ref run, ref pass, ref fail);
            ConverterJsonRoundTripBlock(ref run, ref pass, ref fail);
            LoadJsonMigratesLegacyEnergy(ref run, ref pass, ref fail);
            MergeWritesBlockColumn(ref run, ref pass, ref fail);
            MapperClampsBlockPercent(ref run, ref pass, ref fail);

            TestBase.PrintSummary("Action Block Sheet Column Tests", run, pass, fail);
        }

        private static void EnsureHeaderInsertsAfterSpeed(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "", "" },
                new[] { "ACTION", "SPEED", "OPENER", "FINISHER" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (ensured, added) = ActionBlockSheetColumns.EnsureHeader(header);
            int idx = ensured.GetColumnIndex(null, ActionBlockSheetColumns.Label);
            int speedAt = ensured.GetColumnIndex(null, "SPEED");
            if (added && idx == speedAt + 1)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL EnsureHeaderInsertsAfterSpeed: added={added} idx={idx} speedAt={speedAt}");
            }
        }

        private static void EnsureHeaderInsertsAfterSpeedX(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "", "" },
                new[] { "ACTION", "SPEED(x)", "OPENER", "FINISHER" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (ensured, added) = ActionBlockSheetColumns.EnsureHeader(header);
            int idx = ensured.GetColumnIndex(null, ActionBlockSheetColumns.Label);
            int speedAt = ensured.GetColumnIndex(null, "SPEED(x)");
            if (added && idx == speedAt + 1 && ensured.LabelByIndex[2] == "BLOCK")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL EnsureHeaderInsertsAfterSpeedX: added={added} idx={idx} speedAt={speedAt}");
            }
        }

        private static void CollectInsertedColumnsFindsBlockOnly(ref int run, ref int pass, ref int fail)
        {
            run++;
            var before = new SpreadsheetHeader(
                new[] { "", "", "", "KEYWORD BONUS", "KEYWORD BONUS", "KEYWORD BONUS" },
                new[] { "ACTION", "SPEED(x)", "OPENER", "bonus per keyword", "effect", "keyword" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (after, added) = ActionBlockSheetColumns.EnsureHeader(before);
            var inserted = SpreadsheetHeader.CollectInsertedColumnIndices(before, after);
            bool customKept = after.GetColumnIndex(null, "bonus per keyword") >= 0
                && after.GetColumnIndex(null, "effect") >= 0
                && after.GetColumnIndex(null, "keyword") >= 0;
            if (added && inserted.Count == 1 && inserted[0] == 2 && customKept)
                pass++;
            else
            {
                fail++;
                Console.WriteLine(
                    $"FAIL CollectInsertedColumnsFindsBlockOnly: added={added} inserted=[{string.Join(",", inserted)}] customKept={customKept}");
            }
        }

        private static void EnsureHeaderIdempotent(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "" },
                new[] { "ACTION", "FINISHER" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (ensured, added) = ActionBlockSheetColumns.EnsureHeader(header);
            int idx = ensured.GetColumnIndex(null, ActionBlockSheetColumns.Label);
            var (again, addedAgain) = ActionBlockSheetColumns.EnsureHeader(ensured);
            if (added && !addedAgain && again.GetColumnIndex(null, ActionBlockSheetColumns.Label) == idx)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL EnsureHeaderIdempotent: added={added} addedAgain={addedAgain} idx={idx}");
            }
        }

        private static void RenameEnergyToBlockInPlace(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "", "" },
                new[] { "ACTION", "SPEED", "ENERGY", "OPENER" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (renamed, indices) = ActionBlockSheetColumns.TryRenameEnergyToBlock(header);
            int blockAt = renamed.GetColumnIndex(null, ActionBlockSheetColumns.Label);
            int energyAt = renamed.GetColumnIndex(null, "ENERGY");
            var (ensured, added) = ActionBlockSheetColumns.EnsureHeader(renamed);
            if (indices.Count == 1 && indices[0] == 2
                && blockAt == 2
                && energyAt < 0
                && !added
                && ensured.GetColumnIndex(null, "BLOCK") == 2)
                pass++;
            else
            {
                fail++;
                Console.WriteLine(
                    $"FAIL RenameEnergyToBlockInPlace: indices=[{string.Join(",", indices)}] blockAt={blockAt} energyAt={energyAt} added={added}");
            }
        }

        private static void RenameSkipsWhenBlockExists(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "", "" },
                new[] { "ACTION", "SPEED", "BLOCK", "ENERGY" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (renamed, indices) = ActionBlockSheetColumns.TryRenameEnergyToBlock(header);
            if (indices.Count == 0
                && renamed.GetColumnIndex(null, "BLOCK") == 2
                && renamed.GetColumnIndex(null, "ENERGY") == 3)
                pass++;
            else
            {
                fail++;
                Console.WriteLine(
                    $"FAIL RenameSkipsWhenBlockExists: indices=[{string.Join(",", indices)}] block={renamed.GetColumnIndex(null, "BLOCK")} energy={renamed.GetColumnIndex(null, "ENERGY")}");
            }
        }

        private static void CollectLeftoverEnergyWhenBlockExists(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "", "" },
                new[] { "ACTION", "SPEED", "BLOCK", "ENERGY" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var leftover = ActionBlockSheetColumns.CollectEnergyColumnIndicesToRemove(header);
            if (leftover.Count == 1 && leftover[0] == 3)
                pass++;
            else
            {
                fail++;
                Console.WriteLine(
                    $"FAIL CollectLeftoverEnergyWhenBlockExists: leftover=[{string.Join(",", leftover)}]");
            }
        }

        private static void ConvertParsesMissingAndFreeValues(ref int run, ref int pass, ref int fail)
        {
            run++;
            var missing = SpreadsheetToActionDataConverter.Convert(new SpreadsheetActionData
            {
                Action = "NO BLOCK",
                Damage = "100",
                Speed = "1"
            });
            var zero = SpreadsheetToActionDataConverter.Convert(new SpreadsheetActionData
            {
                Action = "ZERO",
                Damage = "100",
                Speed = "1",
                Block = "0"
            });
            var sixty = SpreadsheetToActionDataConverter.Convert(new SpreadsheetActionData
            {
                Action = "SIXTY",
                Damage = "100",
                Speed = "1",
                Block = "60"
            });
            var ten = SpreadsheetToActionDataConverter.Convert(new SpreadsheetActionData
            {
                Action = "TEN",
                Damage = "100",
                Speed = "1",
                Block = "10"
            });
            if (missing.BlockPercent == 0.25
                && zero.BlockPercent == 0.0
                && sixty.BlockPercent == 0.60
                && ten.BlockPercent == 0.10)
                pass++;
            else
            {
                fail++;
                Console.WriteLine(
                    $"FAIL ConvertParsesMissingAndFreeValues: missing={missing.BlockPercent} zero={zero.BlockPercent} sixty={sixty.BlockPercent} ten={ten.BlockPercent}");
            }
        }

        private static void CsvRoundTripBlock(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "" },
                new[] { "ACTION", "SPEED", "BLOCK" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var parsed = SpreadsheetActionDataCsvParser.FromCsvRow(new[] { "JAB", "1.00", "45" }, header);
            var data = SpreadsheetToActionDataConverter.Convert(parsed);
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            if (parsed.Block == "45" && data.BlockPercent == 0.45 && row.Block == "45")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL CsvRoundTripBlock: cell={parsed.Block} pct={data.BlockPercent} merge={row.Block}");
            }
        }

        private static void JsonRoundTripBlock(ref int run, ref int pass, ref int fail)
        {
            run++;
            var json = new SpreadsheetActionJson { Action = "CAST", Speed = "1.00", Block = "0", Damage = "100%" };
            var sheet = json.ToSpreadsheetActionData();
            var back = SpreadsheetActionJson.FromSpreadsheetActionData(sheet);
            if (sheet.Block == "0" && back.Block == "0")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL JsonRoundTripBlock: sheet={sheet.Block} back={back.Block}");
            }
        }

        private static void ConverterJsonRoundTripBlock(ref int run, ref int pass, ref int fail)
        {
            run++;
            var original = new SpreadsheetActionJson { Action = "JAB", Speed = "1.00", Block = "45", Damage = "100%" };
            var options = new JsonSerializerOptions { Converters = { new SpreadsheetActionJsonConverter() } };
            string text = JsonSerializer.Serialize(original, options);
            var loaded = JsonSerializer.Deserialize<SpreadsheetActionJson>(text, options);
            if (text.Contains("\"block\"", StringComparison.Ordinal)
                && text.Contains("\"45\"", StringComparison.Ordinal)
                && loaded?.Block == "45")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL ConverterJsonRoundTripBlock: json={text} block={loaded?.Block}");
            }
        }

        private static void LoadJsonMigratesLegacyEnergy(ref int run, ref int pass, ref int fail)
        {
            run++;
            const string json = "[{\"action\":\"HEAVY\",\"damage\":\"150%\",\"speed\":\"1.00\",\"energy\":\"3\"}]";
            var options = new JsonSerializerOptions { Converters = { new SpreadsheetActionJsonConverter() } };
            var list = JsonSerializer.Deserialize<System.Collections.Generic.List<SpreadsheetActionJson>>(json, options);
            var data = SpreadsheetToActionDataConverter.Convert(list![0]);
            if (list[0].Block == "0" && data.BlockPercent == 0.0)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL LoadJsonMigratesLegacyEnergy: block={list[0].Block} pct={data.BlockPercent}");
            }
        }

        private static void MergeWritesBlockColumn(ref int run, ref int pass, ref int fail)
        {
            run++;
            var data = new ActionData
            {
                Name = "STRIKE",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 1.0,
                Length = 1.0,
                BlockPercent = 0.45
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            if (row.Block == "45")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL MergeWritesBlockColumn: Block={row.Block}");
            }
        }

        private static void MapperClampsBlockPercent(ref int run, ref int pass, ref int fail)
        {
            run++;
            var data = new ActionData { Name = "BAD", Type = "Attack", BlockPercent = 1.5, DamageMultiplier = 1.0, Length = 1.0 };
            var action = ActionDataToActionMapper.CreateAction(data);
            if (Math.Abs(action.BlockPercent - 1.0) < 0.0001)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL MapperClampsBlockPercent: {action.BlockPercent}");
            }
        }
    }
}
