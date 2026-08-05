using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>Reserve pool sheet column / tag and default-roll exclusion.</summary>
    public static class ReservePoolActionTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Reserve Pool Action Tests ===\n");
            int run = 0, pass = 0, fail = 0;

            ColumnConvertsToTagAndFlag(ref run, ref pass, ref fail);
            TagsCellAloneSetsFlag(ref run, ref pass, ref fail);
            MergeWritesReservePoolColumn(ref run, ref pass, ref fail);
            EnsureHeaderAddsMissingColumn(ref run, ref pass, ref fail);
            SelectActionSkipsReservePool(ref run, ref pass, ref fail);
            SelectActionAllReserveReturnsNull(ref run, ref pass, ref fail);

            TestBase.PrintSummary("Reserve Pool Action Tests", run, pass, fail);
        }

        private static void ColumnConvertsToTagAndFlag(ref int run, ref int pass, ref int fail)
        {
            run++;
            var sheet = new SpreadsheetActionData
            {
                Action = "RESERVE STRIKE",
                Damage = "100",
                Speed = "1",
                ReservePool = "1",
                Category = "WEAPON"
            };
            var data = SpreadsheetToActionDataConverter.Convert(sheet);
            if (data.IsReservePool
                && ActionTagSyncHelper.IsReservePool(data.Tags)
                && GameDataTagHelper.HasReservePoolTag(data.Tags))
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL ColumnConvertsToTagAndFlag: IsReservePool={data.IsReservePool} tags=[{string.Join(",", data.Tags)}]");
            }
        }

        private static void TagsCellAloneSetsFlag(ref int run, ref int pass, ref int fail)
        {
            run++;
            var sheet = new SpreadsheetActionData
            {
                Action = "TAG ONLY RESERVE",
                Damage = "100",
                Speed = "1",
                Tags = "reserve_pool, weapon",
                Category = "WEAPON"
            };
            var data = SpreadsheetToActionDataConverter.Convert(sheet);
            if (data.IsReservePool && ActionTagSyncHelper.IsReservePool(data.Tags))
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL TagsCellAloneSetsFlag: IsReservePool={data.IsReservePool}");
            }
        }

        private static void MergeWritesReservePoolColumn(ref int run, ref int pass, ref int fail)
        {
            run++;
            var data = new ActionData
            {
                Name = "RESERVE",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 1.0,
                Length = 1.0,
                IsReservePool = true
            };
            ActionTagSyncHelper.SyncCanonicalTags(data);
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            if (row.ReservePool == "true"
                && (row.Tags ?? "").Contains("reserve_pool", System.StringComparison.OrdinalIgnoreCase))
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL MergeWritesReservePoolColumn: ReservePool={row.ReservePool} Tags={row.Tags}");
            }
        }

        private static void EnsureHeaderAddsMissingColumn(ref int run, ref int pass, ref int fail)
        {
            run++;
            var header = new SpreadsheetHeader(
                new[] { "", "", "" },
                new[] { "ACTION", "OPENER", "FINISHER" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (ensured, added) = ActionReservePoolSheetColumns.EnsureHeader(header);
            int idx = ensured.GetColumnIndex(null, ActionReservePoolSheetColumns.Label);
            if (added && idx == 3)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL EnsureHeaderAddsMissingColumn: added={added} idx={idx}");
            }

            run++;
            var (again, addedAgain) = ActionReservePoolSheetColumns.EnsureHeader(ensured);
            if (!addedAgain && again.GetColumnIndex(null, ActionReservePoolSheetColumns.Label) == idx)
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL EnsureHeader idempotent");
            }
        }

        private static void SelectActionSkipsReservePool(ref int run, ref int pass, ref int fail)
        {
            run++;
            var hero = TestDataBuilders.CreateTestCharacter("Hero");
            hero.ActionPool.Clear();

            var basic = TestDataBuilders.CreateMockAction("BASIC", ActionType.Attack);
            basic.IsComboAction = false;
            basic.Tags = new System.Collections.Generic.List<string> { "weapon" };

            var reserved = TestDataBuilders.CreateMockAction("RESERVED", ActionType.Attack);
            reserved.IsComboAction = false;
            reserved.Tags = new System.Collections.Generic.List<string> { ActionTagSyncHelper.ReservePoolTag };

            hero.AddAction(reserved, 100.0);
            hero.AddAction(basic, 0.0001);

            bool alwaysBasic = true;
            for (int i = 0; i < 40; i++)
            {
                var picked = hero.SelectAction();
                if (picked == null || !string.Equals(picked.Name, "BASIC", System.StringComparison.OrdinalIgnoreCase))
                {
                    alwaysBasic = false;
                    break;
                }
            }

            if (alwaysBasic)
                pass++;
            else
            {
                fail++;
                Console.WriteLine("FAIL SelectActionSkipsReservePool: reserve action was selected");
            }
        }

        private static void SelectActionAllReserveReturnsNull(ref int run, ref int pass, ref int fail)
        {
            run++;
            var hero = TestDataBuilders.CreateTestCharacter("Hero");
            hero.ActionPool.Clear();
            var reserved = TestDataBuilders.CreateMockAction("ONLY RESERVE", ActionType.Attack);
            reserved.Tags = new System.Collections.Generic.List<string> { ActionTagSyncHelper.ReservePoolTag };
            hero.AddAction(reserved, 1.0);

            var picked = hero.SelectAction();
            if (picked == null)
                pass++;
            else
            {
                fail++;
                Console.WriteLine($"FAIL SelectActionAllReserveReturnsNull: got {picked.Name}");
            }
        }
    }
}
