using System;
using System.IO;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class SheetsCsvFetchTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== SheetsCsvFetch Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestReadLocalCsvFile(ref testsRun, ref testsPassed, ref testsFailed);
            TestMissingLocalFileThrows(ref testsRun, ref testsPassed, ref testsFailed);
            TestNullPathThrows(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("SheetsCsvFetch Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestReadLocalCsvFile(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestReadLocalCsvFile));
            string path = Path.Combine(Path.GetTempPath(), "SheetsCsvFetchTest_" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                File.WriteAllText(path, "a,b,c");
                string text = SheetsCsvFetch.ReadCsvTextAsync(path).GetAwaiter().GetResult();
                TestBase.AssertEqual("a,b,c", text, "reads local CSV file", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { File.Delete(path); } catch { /* best effort */ }
            }
        }

        private static void TestMissingLocalFileThrows(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestMissingLocalFileThrows));
            string path = Path.Combine(Path.GetTempPath(), "SheetsCsvFetch_missing_" + Guid.NewGuid().ToString("N") + ".csv");
            bool threw = false;
            try
            {
                SheetsCsvFetch.ReadCsvTextAsync(path).GetAwaiter().GetResult();
            }
            catch (FileNotFoundException)
            {
                threw = true;
            }

            TestBase.AssertTrue(threw, "missing file throws FileNotFoundException", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestNullPathThrows(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestNullPathThrows));
            bool threw = false;
            try
            {
                SheetsCsvFetch.ReadCsvTextAsync(null!).GetAwaiter().GetResult();
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            TestBase.AssertTrue(threw, "null path throws ArgumentException", ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
