using System;
using System.Text.Json;
using RPGGame;
using RPGGame.Combat.Calculators;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>ENERGY sheet column, clamp 1–3, CSV/JSON round-trip, Settings merge persist.</summary>
    public static class ActionEnergySheetColumnsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Energy Sheet Column Tests ===\n");
            int run = 0, pass = 0, fail = 0;

            EnsureHeaderInsertsAfterSpeed(ref run, ref pass, ref fail);
            EnsureHeaderInsertsAfterSpeedX(ref run, ref pass, ref fail);
            EnsureHeaderIdempotent(ref run, ref pass, ref fail);
            CollectInsertedColumnsFindsEnergyOnly(ref run, ref pass, ref fail);
            ConvertClampsMissingAndOutOfRange(ref run, ref pass, ref fail);
            CsvRoundTripEnergy(ref run, ref pass, ref fail);
            JsonRoundTripEnergy(ref run, ref pass, ref fail);
            ConverterJsonRoundTripEnergy(ref run, ref pass, ref fail);
            LoadJsonEnergyCost(ref run, ref pass, ref fail);
            MergeWritesEnergyColumn(ref run, ref pass, ref fail);
            MapperClampsEnergyCost(ref run, ref pass, ref fail);

            TestBase.PrintSummary("Action Energy Sheet Column Tests", run, pass, fail);
        }

        private static void EnsureHeaderInsertsAfterSpeed(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "", "" },
                new[] { "ACTION", "SPEED", "OPENER", "FINISHER" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (ensured, added) = ActionEnergySheetColumns.EnsureHeader(header);
            int idx = ensured.GetColumnIndex(null, ActionEnergySheetColumns.Label);
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
            var (ensured, added) = ActionEnergySheetColumns.EnsureHeader(header);
            int idx = ensured.GetColumnIndex(null, ActionEnergySheetColumns.Label);
            int speedAt = ensured.GetColumnIndex(null, "SPEED(x)");
            if (added && idx == speedAt + 1 && ensured.LabelByIndex[2] == "ENERGY")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL EnsureHeaderInsertsAfterSpeedX: added={added} idx={idx} speedAt={speedAt}");
            }
        }

        private static void CollectInsertedColumnsFindsEnergyOnly(ref int run, ref int pass, ref int fail)
        {
            run++;
            var before = new SpreadsheetHeader(
                new[] { "", "", "", "KEYWORD BONUS", "KEYWORD BONUS", "KEYWORD BONUS" },
                new[] { "ACTION", "SPEED(x)", "OPENER", "bonus per keyword", "effect", "keyword" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (after, added) = ActionEnergySheetColumns.EnsureHeader(before);
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
                    $"FAIL CollectInsertedColumnsFindsEnergyOnly: added={added} inserted=[{string.Join(",", inserted)}] customKept={customKept}");
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
            var (ensured, added) = ActionEnergySheetColumns.EnsureHeader(header);
            int idx = ensured.GetColumnIndex(null, ActionEnergySheetColumns.Label);
            var (again, addedAgain) = ActionEnergySheetColumns.EnsureHeader(ensured);
            if (added && !addedAgain && again.GetColumnIndex(null, ActionEnergySheetColumns.Label) == idx)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL EnsureHeaderIdempotent: added={added} addedAgain={addedAgain} idx={idx}");
            }
        }

        private static void ConvertClampsMissingAndOutOfRange(ref int run, ref int pass, ref int fail)
        {
            run++;
            var missing = SpreadsheetToActionDataConverter.Convert(new SpreadsheetActionData
            {
                Action = "NO ENERGY",
                Damage = "100",
                Speed = "1"
            });
            var zero = SpreadsheetToActionDataConverter.Convert(new SpreadsheetActionData
            {
                Action = "ZERO",
                Damage = "100",
                Speed = "1",
                Energy = "0"
            });
            var high = SpreadsheetToActionDataConverter.Convert(new SpreadsheetActionData
            {
                Action = "HIGH",
                Damage = "100",
                Speed = "1",
                Energy = "9"
            });
            var ok = SpreadsheetToActionDataConverter.Convert(new SpreadsheetActionData
            {
                Action = "THREE",
                Damage = "100",
                Speed = "1",
                Energy = "3"
            });
            if (missing.EnergyCost == 2 && zero.EnergyCost == 2 && high.EnergyCost == 2 && ok.EnergyCost == 3)
                pass++;
            else
            {
                fail++;
                Console.WriteLine(
                    $"FAIL ConvertClampsMissingAndOutOfRange: missing={missing.EnergyCost} zero={zero.EnergyCost} high={high.EnergyCost} ok={ok.EnergyCost}");
            }
        }

        private static void CsvRoundTripEnergy(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "" },
                new[] { "ACTION", "SPEED", "ENERGY" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var parsed = SpreadsheetActionDataCsvParser.FromCsvRow(new[] { "JAB", "1.00", "1" }, header);
            var data = SpreadsheetToActionDataConverter.Convert(parsed);
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            if (parsed.Energy == "1" && data.EnergyCost == 1 && row.Energy == "1")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL CsvRoundTripEnergy: cell={parsed.Energy} cost={data.EnergyCost} merge={row.Energy}");
            }
        }

        private static void JsonRoundTripEnergy(ref int run, ref int pass, ref int fail)
        {
            run++;
            var json = new SpreadsheetActionJson { Action = "CAST", Speed = "1.00", Energy = "3", Damage = "100%" };
            var sheet = json.ToSpreadsheetActionData();
            var back = SpreadsheetActionJson.FromSpreadsheetActionData(sheet);
            if (sheet.Energy == "3" && back.Energy == "3")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL JsonRoundTripEnergy: sheet={sheet.Energy} back={back.Energy}");
            }
        }

        private static void ConverterJsonRoundTripEnergy(ref int run, ref int pass, ref int fail)
        {
            run++;
            var original = new SpreadsheetActionJson { Action = "JAB", Speed = "1.00", Energy = "1", Damage = "100%" };
            var options = new JsonSerializerOptions { Converters = { new SpreadsheetActionJsonConverter() } };
            string text = JsonSerializer.Serialize(original, options);
            var loaded = JsonSerializer.Deserialize<SpreadsheetActionJson>(text, options);
            if (text.Contains("\"energy\"", StringComparison.Ordinal)
                && text.Contains("\"1\"", StringComparison.Ordinal)
                && loaded?.Energy == "1")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL ConverterJsonRoundTripEnergy: json={text} energy={loaded?.Energy}");
            }
        }

        private static void LoadJsonEnergyCost(ref int run, ref int pass, ref int fail)
        {
            run++;
            const string json = "[{\"action\":\"HEAVY\",\"damage\":\"150%\",\"speed\":\"1.00\",\"energy\":\"3\"}]";
            var options = new JsonSerializerOptions { Converters = { new SpreadsheetActionJsonConverter() } };
            var list = JsonSerializer.Deserialize<System.Collections.Generic.List<SpreadsheetActionJson>>(json, options);
            var data = SpreadsheetToActionDataConverter.Convert(list![0]);
            if (data.EnergyCost == 3)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL LoadJsonEnergyCost: cost={data.EnergyCost}");
            }
        }

        private static void MergeWritesEnergyColumn(ref int run, ref int pass, ref int fail)
        {
            run++;
            var data = new ActionData
            {
                Name = "STRIKE",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 1.0,
                Length = 1.0,
                EnergyCost = 1
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            if (row.Energy == "1")
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL MergeWritesEnergyColumn: Energy={row.Energy}");
            }
        }

        private static void MapperClampsEnergyCost(ref int run, ref int pass, ref int fail)
        {
            run++;
            var data = new ActionData { Name = "BAD", Type = "Attack", EnergyCost = 0, DamageMultiplier = 1.0, Length = 1.0 };
            var action = ActionDataToActionMapper.CreateAction(data);
            if (action.EnergyCost == LeftoverEnergy.DefaultCost)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL MapperClampsEnergyCost: {action.EnergyCost}");
            }
        }
    }
}
