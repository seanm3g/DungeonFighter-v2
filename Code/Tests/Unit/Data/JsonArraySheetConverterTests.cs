using System;
using RPGGame.Tests;

using JsonSheetConverter = RPGGame.Tests.Unit.Data.JsonArraySheetConverter;

namespace RPGGame.Tests.Unit.Data
{
    public static class JsonArraySheetConverterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== JsonArraySheetConverter Tests ===\n");
            int run = 0, pass = 0, fail = 0;
            JsonSheetConverter.JsonArraySheetConverterWeaponsTests.RunAll(ref run, ref pass, ref fail);
            JsonSheetConverter.JsonArraySheetConverterModificationsTests.RunAll(ref run, ref pass, ref fail);
            JsonSheetConverter.JsonArraySheetConverterStatBonusesTests.RunAll(ref run, ref pass, ref fail);
            JsonSheetConverter.JsonArraySheetConverterEnemiesTests.RunAll(ref run, ref pass, ref fail);
            JsonSheetConverter.JsonArraySheetConverterEnvironmentsTests.RunAll(ref run, ref pass, ref fail);
            JsonSheetConverter.JsonArraySheetConverterDungeonsTests.RunAll(ref run, ref pass, ref fail);
            JsonSheetConverter.JsonArraySheetConverterArmorTests.RunAll(ref run, ref pass, ref fail);
            JsonSheetConverter.JsonArraySheetConverterConsumablesTests.RunAll(ref run, ref pass, ref fail);
            TestBase.PrintSummary("JsonArraySheetConverter Tests", run, pass, fail);
        }
    }
}
