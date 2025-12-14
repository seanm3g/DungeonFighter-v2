using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// Partial class for Unit Test Tools, Specialized Agent Tools, and Development Agent Tools
    /// </summary>
    public static partial class McpTools
    {
        // ========== Unit Test Tools ==========

        [McpServerTool(Name = "run_combo_dice_roll_tests", Title = "Run Combo Dice Roll Tests")]
        [Description("Runs comprehensive tests for combo sequences and dice rolls. Tests dice mechanics (1-5 fail, 6-13 normal, 14-20 combo), action selection based on rolls, combo sequence information, IsCombo flag behavior, and conditional triggers (OnCombo vs OnHit).")]
        public static Task<string> RunComboDiceRollTests()
        {
            return Tools.TestTools.RunComboDiceRollTests();
        }

        [McpServerTool(Name = "run_action_sequence_tests", Title = "Run Action Sequence Tests")]
        [Description("Runs comprehensive tests for actions and action sequences. Tests action creation, properties, selection, combo sequences, and execution flow.")]
        public static Task<string> RunActionSequenceTests()
        {
            return Tools.TestTools.RunActionSequenceTests();
        }

        [McpServerTool(Name = "run_combat_system_tests", Title = "Run Combat System Tests")]
        [Description("Runs comprehensive tests for combat system. Tests damage calculation, hit/miss determination, status effects, combat flow, multi-hit attacks, and critical hits.")]
        public static Task<string> RunCombatSystemTests()
        {
            return Tools.TestTools.RunCombatSystemTests();
        }

        [McpServerTool(Name = "run_all_unit_tests", Title = "Run All Unit Tests")]
        [Description("Runs all available unit tests including combo dice roll tests, action sequence tests, and combat system tests.")]
        public static Task<string> RunAllUnitTests()
        {
            return Tools.TestTools.RunAllUnitTests();
        }

        [McpServerTool(Name = "run_all_settings_tests", Title = "Run All Settings Tests")]
        [Description("Runs all comprehensive game system tests available in the settings menu.")]
        public static Task<string> RunAllSettingsTests()
        {
            return Tools.TestTools.RunAllSettingsTests();
        }

        [McpServerTool(Name = "run_specific_test", Title = "Run Specific Test")]
        [Description("Runs a specific test by name from the settings menu.")]
        public static Task<string> RunSpecificTest(
            [Description("Name of the test to run")] string testName)
        {
            return Tools.TestTools.RunSpecificTest(testName);
        }

        [McpServerTool(Name = "run_system_tests", Title = "Run System Tests")]
        [Description("Runs tests for a specific system category (Character, Combat, Inventory, etc.).")]
        public static Task<string> RunSystemTests(
            [Description("System category name")] string systemName)
        {
            return Tools.TestTools.RunSystemTests(systemName);
        }

        // ========== Specialized Agent Tools ==========

        [McpServerTool(Name = "run_full_cycle", Title = "Run Full Automated Balance Cycle")]
        [Description("Orchestrates complete multi-agent balance tuning cycle: Analysis → Tuning → Testing → Gameplay → Save. Coordinates all specialized agents to reach target balance.")]
        public static Task<string> RunFullCycle(
            [Description("Target win rate percentage (default: 90)")] double targetWinRate = 90.0,
            [Description("Maximum number of tuning iterations (default: 5)")] int maxIterations = 5)
        {
            return Tools.AutomatedTuningLoop.RunFullCycle(targetWinRate, maxIterations);
        }

        [McpServerTool(Name = "tester_agent_run", Title = "Tester Agent - Run Tests")]
        [Description("Launches Tester Agent to run comprehensive balance verification tests.")]
        public static Task<string> TesterAgentRun(
            [Description("Test mode: 'full' (all tests), 'quick' (core metrics), 'regression' (baseline comparison). Default: full")] string mode = "full")
        {
            var testMode = mode.ToLower() switch
            {
                "quick" => Tools.TesterAgent.TestMode.Quick,
                "regression" => Tools.TesterAgent.TestMode.Regression,
                _ => Tools.TesterAgent.TestMode.Full
            };
            return Tools.TesterAgent.RunTests(testMode);
        }

        [McpServerTool(Name = "analysis_agent_run", Title = "Analysis Agent - Deep Diagnostics")]
        [Description("Launches Analysis Agent to run targeted diagnostics on specific balance aspects.")]
        public static Task<string> AnalysisAgentRun(
            [Description("Focus area: 'balance' (overall), 'weapons' (variance), 'enemies' (matchups), 'engagement' (fun moments). Default: balance")] string focus = "balance")
        {
            var focusArea = focus.ToLower() switch
            {
                "weapons" => Tools.AnalysisAgent.FocusArea.Weapons,
                "enemies" => Tools.AnalysisAgent.FocusArea.Enemies,
                "engagement" => Tools.AnalysisAgent.FocusArea.Engagement,
                _ => Tools.AnalysisAgent.FocusArea.Balance
            };
            return Tools.AnalysisAgent.AnalyzeAndReport(focusArea);
        }

        [McpServerTool(Name = "balance_tuner_agent_run", Title = "Balance Tuner Agent - Iterative Tuning")]
        [Description("Launches Balance Tuner Agent to iteratively adjust balance toward target metrics.")]
        public static Task<string> BalanceTunerAgentRun(
            [Description("Target win rate percentage (default: 90)")] double targetWinRate = 90.0,
            [Description("Maximize enemy variance (default: true)")] bool maximizeVariance = true)
        {
            return Tools.BalanceTunerAgent.TuneBalance(targetWinRate, maximizeVariance);
        }

        // ========== Development Agent Tools ==========

        [McpServerTool(Name = "code_review_agent_file", Title = "Code Review Agent - Review File")]
        [Description("Launches Code Review Agent to analyze a specific C# file for quality issues.")]
        public static Task<string> CodeReviewAgentFile(
            [Description("File path relative to project root (e.g., Code/Combat/CombatManager.cs)")] string filePath)
        {
            return Tools.CodeReviewAgent.ReviewFile(filePath);
        }

        [McpServerTool(Name = "code_review_agent_diff", Title = "Code Review Agent - Review Diff")]
        [Description("Launches Code Review Agent to review uncommitted git changes.")]
        public static Task<string> CodeReviewAgentDiff()
        {
            return Tools.CodeReviewAgent.ReviewDiff();
        }

        [McpServerTool(Name = "code_review_agent_pr", Title = "Code Review Agent - Review Pull Request")]
        [Description("Launches Code Review Agent to review current branch against main.")]
        public static Task<string> CodeReviewAgentPr()
        {
            return Tools.CodeReviewAgent.ReviewPullRequest();
        }

        [McpServerTool(Name = "test_engineer_agent_generate", Title = "Test Engineer Agent - Generate Tests")]
        [Description("Launches Test Engineer Agent to generate unit and integration tests for a feature.")]
        public static Task<string> TestEngineerAgentGenerate(
            [Description("Feature name or class to generate tests for")] string featureName)
        {
            return Tools.TestEngineerAgent.GenerateTests(featureName);
        }

        [McpServerTool(Name = "test_engineer_agent_run", Title = "Test Engineer Agent - Run Tests")]
        [Description("Launches Test Engineer Agent to run and verify test suites.")]
        public static Task<string> TestEngineerAgentRun(
            [Description("Test category or name to run")] string category)
        {
            return Tools.TestEngineerAgent.RunTests(category);
        }

        [McpServerTool(Name = "test_engineer_agent_coverage", Title = "Test Engineer Agent - Analyze Coverage")]
        [Description("Launches Test Engineer Agent to analyze and report test coverage gaps.")]
        public static Task<string> TestEngineerAgentCoverage()
        {
            return Tools.TestEngineerAgent.AnalyzeCoverage();
        }

        [McpServerTool(Name = "test_engineer_agent_integration", Title = "Test Engineer Agent - Generate Integration Tests")]
        [Description("Launches Test Engineer Agent to generate integration tests for a system.")]
        public static Task<string> TestEngineerAgentIntegration(
            [Description("System name (e.g., Combat, Character, Inventory)")] string systemName)
        {
            return Tools.TestEngineerAgent.GenerateIntegrationTests(systemName);
        }

        [McpServerTool(Name = "bug_investigator_agent_investigate", Title = "Bug Investigator Agent - Investigate Bug")]
        [Description("Launches Bug Investigator Agent to diagnose a bug from description.")]
        public static Task<string> BugInvestigatorAgentInvestigate(
            [Description("Description of the bug issue")] string description)
        {
            return Tools.BugInvestigatorAgent.InvestigateBug(description);
        }

        [McpServerTool(Name = "bug_investigator_agent_reproduce", Title = "Bug Investigator Agent - Reproduce Bug")]
        [Description("Launches Bug Investigator Agent to reproduce a bug with given steps.")]
        public static Task<string> BugInvestigatorAgentReproduce(
            [Description("Steps to reproduce the bug")] string steps)
        {
            return Tools.BugInvestigatorAgent.ReproduceBug(steps);
        }

        [McpServerTool(Name = "bug_investigator_agent_isolate", Title = "Bug Investigator Agent - Isolate Bug")]
        [Description("Launches Bug Investigator Agent to isolate root cause in a system.")]
        public static Task<string> BugInvestigatorAgentIsolate(
            [Description("System name where bug occurs (e.g., Combat, Enemy, UI)")] string systemName)
        {
            return Tools.BugInvestigatorAgent.IsolateBug(systemName);
        }

        [McpServerTool(Name = "bug_investigator_agent_suggest_fix", Title = "Bug Investigator Agent - Suggest Fixes")]
        [Description("Launches Bug Investigator Agent to generate fix suggestions for a bug.")]
        public static Task<string> BugInvestigatorAgentSuggestFix(
            [Description("Bug ID or identifier")] string bugId)
        {
            return Tools.BugInvestigatorAgent.SuggestFix(bugId);
        }

        [McpServerTool(Name = "performance_profiler_agent_profile", Title = "Performance Profiler Agent - Profile System")]
        [Description("Launches Performance Profiler Agent to identify bottlenecks in a system.")]
        public static Task<string> PerformanceProfilerAgentProfile(
            [Description("Component name to profile (Combat, Enemy, Game, etc.)")] string component)
        {
            return Tools.PerformanceProfilerAgent.ProfileSystem(component);
        }

        [McpServerTool(Name = "performance_profiler_agent_compare", Title = "Performance Profiler Agent - Compare Performance")]
        [Description("Launches Performance Profiler Agent to compare current performance with baseline.")]
        public static Task<string> PerformanceProfilerAgentCompare(
            [Description("Baseline name or version to compare against")] string baseline)
        {
            return Tools.PerformanceProfilerAgent.ComparePerformance(baseline);
        }

        [McpServerTool(Name = "performance_profiler_agent_bottlenecks", Title = "Performance Profiler Agent - Identify Bottlenecks")]
        [Description("Launches Performance Profiler Agent to find performance bottlenecks across all systems.")]
        public static Task<string> PerformanceProfilerAgentBottlenecks()
        {
            return Tools.PerformanceProfilerAgent.IdentifyBottlenecks();
        }

        [McpServerTool(Name = "performance_profiler_agent_benchmark", Title = "Performance Profiler Agent - Benchmark Critical Paths")]
        [Description("Launches Performance Profiler Agent to benchmark critical code paths.")]
        public static Task<string> PerformanceProfilerAgentBenchmark()
        {
            return Tools.PerformanceProfilerAgent.BenchmarkCriticalPaths();
        }

        [McpServerTool(Name = "refactoring_agent_suggest", Title = "Refactoring Agent - Suggest Refactorings")]
        [Description("Launches Refactoring Agent to identify refactoring opportunities in a target.")]
        public static Task<string> RefactoringAgentSuggest(
            [Description("Target to analyze (class name, system, file path, etc.)")] string target)
        {
            return Tools.RefactoringAgent.SuggestRefactorings(target);
        }

        [McpServerTool(Name = "refactoring_agent_apply", Title = "Refactoring Agent - Apply Refactoring")]
        [Description("Launches Refactoring Agent to apply specific refactoring type.")]
        public static Task<string> RefactoringAgentApply(
            [Description("Refactoring type: extract, simplify, consolidate, modernize")] string type,
            [Description("Target to refactor (class name, method, file path, etc.)")] string target)
        {
            return Tools.RefactoringAgent.ApplyRefactoring(type, target);
        }

        [McpServerTool(Name = "refactoring_agent_duplicates", Title = "Refactoring Agent - Remove Duplication")]
        [Description("Launches Refactoring Agent to find and suggest removal of duplicated code.")]
        public static Task<string> RefactoringAgentDuplicates()
        {
            return Tools.RefactoringAgent.RemoveDuplication();
        }

        [McpServerTool(Name = "refactoring_agent_simplify", Title = "Refactoring Agent - Simplify Method")]
        [Description("Launches Refactoring Agent to analyze and simplify a complex method.")]
        public static Task<string> RefactoringAgentSimplify(
            [Description("Method name to simplify")] string methodName)
        {
            return Tools.RefactoringAgent.SimplifyMethod(methodName);
        }

        [McpServerTool(Name = "dependency_analyzer_analyze", Title = "Dependency Analyzer Agent - Analyze Dependencies")]
        [Description("Launches Dependency Analyzer Agent to analyze project dependencies.")]
        public static Task<string> DependencyAnalyzerAnalyze()
        {
            return Tools.DependencyAnalyzerAgent.AnalyzeDependencies();
        }

        [McpServerTool(Name = "dependency_analyzer_outdated", Title = "Dependency Analyzer Agent - Find Outdated Packages")]
        [Description("Launches Dependency Analyzer Agent to find outdated packages.")]
        public static Task<string> DependencyAnalyzerOutdated()
        {
            return Tools.DependencyAnalyzerAgent.FindOutdatedPackages();
        }

        [McpServerTool(Name = "dependency_analyzer_unused", Title = "Dependency Analyzer Agent - Find Unused Dependencies")]
        [Description("Launches Dependency Analyzer Agent to find unused dependencies.")]
        public static Task<string> DependencyAnalyzerUnused()
        {
            return Tools.DependencyAnalyzerAgent.FindUnusedDependencies();
        }

        [McpServerTool(Name = "dependency_analyzer_security", Title = "Dependency Analyzer Agent - Check Security Vulnerabilities")]
        [Description("Launches Dependency Analyzer Agent to scan for security vulnerabilities.")]
        public static Task<string> DependencyAnalyzerSecurity()
        {
            return Tools.DependencyAnalyzerAgent.CheckSecurityVulnerabilities();
        }

        [McpServerTool(Name = "feature_builder_feature", Title = "Feature Builder Agent - Build Feature")]
        [Description("Launches Feature Builder Agent to generate implementation plan for a feature.")]
        public static Task<string> FeatureBuilderFeature(
            [Description("Feature specification")] string spec)
        {
            return Tools.FeatureBuilderAgent.BuildFeature(spec);
        }

        [McpServerTool(Name = "feature_builder_class", Title = "Feature Builder Agent - Generate Class")]
        [Description("Launches Feature Builder Agent to generate a class from properties.")]
        public static Task<string> FeatureBuilderClass(
            [Description("Class name")] string name,
            [Description("Properties as comma-separated list: type1 prop1, type2 prop2")] string properties)
        {
            return Tools.FeatureBuilderAgent.GenerateClass(name, properties);
        }

        [McpServerTool(Name = "feature_builder_system", Title = "Feature Builder Agent - Scaffold System")]
        [Description("Launches Feature Builder Agent to scaffold a new system.")]
        public static Task<string> FeatureBuilderSystem(
            [Description("System name to scaffold")] string systemName)
        {
            return Tools.FeatureBuilderAgent.ScaffoldSystem(systemName);
        }

        [McpServerTool(Name = "feature_builder_endpoint", Title = "Feature Builder Agent - Generate API Endpoint")]
        [Description("Launches Feature Builder Agent to generate an API endpoint.")]
        public static Task<string> FeatureBuilderEndpoint(
            [Description("API path (e.g., /api/users)")] string path,
            [Description("HTTP method (Get, Post, Put, Delete)")] string method)
        {
            return Tools.FeatureBuilderAgent.GenerateApiEndpoint(path, method);
        }
    }
}

