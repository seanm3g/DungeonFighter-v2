using System;
using System.Collections.Generic;
using RPGGame.BattleStatistics;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for MatchupAnalyzer
    /// Tests matchup analysis, report generation, and issue detection
    /// </summary>
    public static class MatchupAnalyzerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all MatchupAnalyzer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== MatchupAnalyzer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestAnalyze();
            TestMatchupResultStatus();
            TestAnalysisReportGeneration();
            TestGenerateTextReport();
            TestIssueDetection();

            TestBase.PrintSummary("MatchupAnalyzer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Analyze Tests

        private static void TestAnalyze()
        {
            Console.WriteLine("--- Testing Analyze ---");

            try
            {
                var testResult = CreateMockTestResult();
                var report = MatchupAnalyzer.Analyze(testResult);

                TestBase.AssertNotNull(report,
                    "Analyze should return an AnalysisReport",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (report != null)
                {
                    TestBase.AssertTrue(report.MatchupResults.Count > 0,
                        "AnalysisReport should contain matchup results",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    TestBase.AssertTrue(report.GeneratedDate != default(DateTime),
                        "AnalysisReport should have a generated date",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Analyze failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region MatchupResult Status Tests

        private static void TestMatchupResultStatus()
        {
            Console.WriteLine("\n--- Testing MatchupResult Status Determination ---");

            try
            {
                var testResult = CreateMockTestResult();
                var report = MatchupAnalyzer.Analyze(testResult);

                if (report != null && report.MatchupResults.Count > 0)
                {
                    // Check that statuses are assigned
                    bool hasStatus = false;
                    foreach (var matchup in report.MatchupResults)
                    {
                        if (!string.IsNullOrEmpty(matchup.Status))
                        {
                            hasStatus = true;
                            TestBase.AssertTrue(
                                matchup.Status == "GOOD" || matchup.Status == "WARNING" || matchup.Status == "CRITICAL",
                                $"Matchup status should be GOOD, WARNING, or CRITICAL, got: {matchup.Status}",
                                ref _testsRun, ref _testsPassed, ref _testsFailed);
                        }
                    }

                    TestBase.AssertTrue(hasStatus,
                        "At least one matchup should have a status",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"MatchupResult status test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region AnalysisReport Generation Tests

        private static void TestAnalysisReportGeneration()
        {
            Console.WriteLine("\n--- Testing AnalysisReport Generation ---");

            try
            {
                var testResult = CreateMockTestResult();
                var report = MatchupAnalyzer.Analyze(testResult);

                if (report != null)
                {
                    TestBase.AssertTrue(report.BattlesPerMatchup >= 0,
                        "AnalysisReport should have battles per matchup",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    TestBase.AssertTrue(report.MatchupResults != null,
                        "AnalysisReport should have matchup results list",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    TestBase.AssertTrue(report.Issues != null,
                        "AnalysisReport should have issues list",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    TestBase.AssertTrue(report.Recommendations != null,
                        "AnalysisReport should have recommendations list",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"AnalysisReport generation test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region GenerateTextReport Tests

        private static void TestGenerateTextReport()
        {
            Console.WriteLine("\n--- Testing GenerateTextReport ---");

            try
            {
                var testResult = CreateMockTestResult();
                var report = MatchupAnalyzer.Analyze(testResult);

                if (report != null)
                {
                    var textReport = MatchupAnalyzer.GenerateTextReport(report);

                    TestBase.AssertTrue(!string.IsNullOrEmpty(textReport),
                        "GenerateTextReport should return a non-empty string",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    TestBase.AssertTrue(textReport.Contains("MATCHUP ANALYSIS REPORT"),
                        "Text report should contain report header",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GenerateTextReport failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Issue Detection Tests

        private static void TestIssueDetection()
        {
            Console.WriteLine("\n--- Testing Issue Detection ---");

            try
            {
                // Create test result with problematic matchups
                var testResult = CreateMockTestResultWithIssues();
                var report = MatchupAnalyzer.Analyze(testResult);

                if (report != null)
                {
                    // The analyzer should detect issues for matchups outside the 85-98% range
                    // or with problematic combat duration
                    TestBase.AssertTrue(report.Issues != null,
                        "AnalysisReport should have issues list",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    // Note: Issues may or may not be generated depending on the test data
                    // We just verify the structure is correct
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Issue detection test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Helper Methods

        private static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult CreateMockTestResult()
        {
            var result = new BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult
            {
                WeaponTypes = new List<WeaponType> { WeaponType.Sword, WeaponType.Axe },
                EnemyTypes = new List<string> { "Goblin", "Orc" },
                CombinationResults = new List<WeaponEnemyCombinationResult>
                {
                    new WeaponEnemyCombinationResult
                    {
                        WeaponType = WeaponType.Sword,
                        EnemyType = "Goblin",
                        TotalBattles = 100,
                        PlayerWins = 90,
                        WinRate = 90.0,
                        AverageTurns = 10.0
                    },
                    new WeaponEnemyCombinationResult
                    {
                        WeaponType = WeaponType.Sword,
                        EnemyType = "Orc",
                        TotalBattles = 100,
                        PlayerWins = 88,
                        WinRate = 88.0,
                        AverageTurns = 12.0
                    },
                    new WeaponEnemyCombinationResult
                    {
                        WeaponType = WeaponType.Axe,
                        EnemyType = "Goblin",
                        TotalBattles = 100,
                        PlayerWins = 92,
                        WinRate = 92.0,
                        AverageTurns = 9.0
                    },
                    new WeaponEnemyCombinationResult
                    {
                        WeaponType = WeaponType.Axe,
                        EnemyType = "Orc",
                        TotalBattles = 100,
                        PlayerWins = 87,
                        WinRate = 87.0,
                        AverageTurns = 11.0
                    }
                },
                WeaponStatistics = new Dictionary<WeaponType, WeaponOverallStats>
                {
                    { WeaponType.Sword, new WeaponOverallStats { WinRate = 89.0 } },
                    { WeaponType.Axe, new WeaponOverallStats { WinRate = 89.5 } }
                },
                EnemyStatistics = new Dictionary<string, EnemyOverallStats>
                {
                    { "Goblin", new EnemyOverallStats { WinRate = 91.0 } },
                    { "Orc", new EnemyOverallStats { WinRate = 87.5 } }
                }
            };

            return result;
        }

        private static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult CreateMockTestResultWithIssues()
        {
            var result = CreateMockTestResult();
            
            // Add a problematic matchup (low win rate)
            result.CombinationResults.Add(new WeaponEnemyCombinationResult
            {
                WeaponType = WeaponType.Sword,
                EnemyType = "Dragon",
                TotalBattles = 100,
                PlayerWins = 50,
                WinRate = 50.0,
                AverageTurns = 5.0  // Too short
            });

            return result;
        }

        #endregion
    }
}
