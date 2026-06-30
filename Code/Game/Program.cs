using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using RPGGame;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;
using RPGGame.Data;

namespace RPGGame
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static async Task Main(string[] args)
        {
            // Check if metrics summary is requested
            if (args.Length > 0 && args[0] == "METRICS")
            {
                BuildExecutionMetrics.PrintMetricsSummary();
                return;
            }

            // Start tracking execution time and launch time
            BuildExecutionMetrics.StartExecutionTracking();
            string executionMode = "GUI";

            if (args.Length > 0 && ShouldEnableSimulationFastPacing(args))
                SimulationPacing.EnableFastMode();

            try
            {
                // Check if MCP mode is requested
                if (args.Length > 0 && args[0] == "MCP")
                {
                    executionMode = "MCP";
                    BuildExecutionMetrics.RecordLaunchTime("MCP");
                    // Run MCP server instead of GUI
                    await RPGGame.MCP.MCPServerProgram.RunMCPServer(args);
                    return;
                }

                // Headless smoke test for MCP gameplay path
                if (args.Length > 0 && args[0].Equals("MCPSMOKE", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "MCPSMOKE";
                    BuildExecutionMetrics.RecordLaunchTime("MCPSMOKE");
                    await RPGGame.Game.McpSmokeTest.RunAsync();
                    return;
                }

                if (args.Length > 0 && args[0].Equals("PLAYTODEATH", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "PLAYTODEATH";
                    BuildExecutionMetrics.RecordLaunchTime("PLAYTODEATH");
                    await RPGGame.Game.PlayUntilDeath.RunAsync();
                    return;
                }

                if (args.Length > 0 && args[0].Equals("PLAYTODEATHBATCH", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "PLAYTODEATHBATCH";
                    BuildExecutionMetrics.RecordLaunchTime("PLAYTODEATHBATCH");
                    int runsPerClass = 3;
                    if (args.Length > 1 && int.TryParse(args[1], out int parsedRuns))
                        runsPerClass = Math.Max(1, parsedRuns);
                    await RPGGame.Game.PlayUntilDeathBatch.RunAsync(runsPerClass);
                    return;
                }

                if (args.Length > 0 && args[0].Equals("PLAYTHROUGHTUNING", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "PLAYTHROUGHTUNING";
                    BuildExecutionMetrics.RecordLaunchTime("PLAYTHROUGHTUNING");
                    int maxIter = RPGGame.Tuning.Profiles.TuningCliArgs.GetIntFlag(args, "--max-iterations", "-n") ?? 8;
                    int runsPerClass = RPGGame.Tuning.Profiles.TuningCliArgs.GetIntFlag(args, "--runs-per-class", "-r") ?? 10;
                    bool dryRun = args.Any(a => a.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
                    bool stopWhenPass = !args.Any(a => a.Equals("--no-stop-when-pass", StringComparison.OrdinalIgnoreCase));
                    await RPGGame.Tuning.PlaythroughTuningRunner.Run(maxIter, runsPerClass, stopWhenPass, dryRun, args);
                    return;
                }

                // Check if interactive play mode is requested
                if (args.Length > 0 && args[0].Equals("PLAY", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "PLAY";
                    BuildExecutionMetrics.RecordLaunchTime("PLAY");
                    // Run interactive game player using MCP tools
                    await RPGGame.Game.InteractiveMCPGamePlayer.Main(args);
                    return;
                }

                // Check if Claude AI mode is requested
                if (args.Length > 0 && args[0].Equals("CLAUDE", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "CLAUDE";
                    BuildExecutionMetrics.RecordLaunchTime("CLAUDE");
                    // Run Claude AI game player with strategic decisions
                    await RPGGame.Game.ClaudeAIGamePlayer.Main(args);
                    return;
                }

                // Check if tuning mode is requested
                if (args.Length > 0 && args[0] == "TUNING")
                {
                    executionMode = "TUNING";
                    BuildExecutionMetrics.RecordLaunchTime("TUNING");
                    // Run balance tuning runner
                    int iterations = args.Length > 1 && int.TryParse(args[1], out int iter) ? iter : 5;
                    await RPGGame.Tuning.TuningRunner.RunTuning(iterations);
                    return;
                }

                if (args.Length > 0 && args[0] == "TUNEPROFILES")
                {
                    executionMode = "TUNEPROFILES";
                    BuildExecutionMetrics.RecordLaunchTime("TUNEPROFILES");
                    RPGGame.Tuning.BalanceTuningWorkflow.ListProfiles();
                    return;
                }

                if (args.Length > 0 && args[0] == "TUNESIM")
                {
                    executionMode = "TUNESIM";
                    BuildExecutionMetrics.RecordLaunchTime("TUNESIM");
                    string profile = RPGGame.Tuning.Profiles.TuningCliArgs.GetProfileId(args);
                    System.Environment.ExitCode = await RPGGame.Tuning.BalanceTuningWorkflow.RunSimAsync(profile, args);
                    return;
                }

                if (args.Length > 0 && args[0] == "TUNEANALYZE")
                {
                    executionMode = "TUNEANALYZE";
                    BuildExecutionMetrics.RecordLaunchTime("TUNEANALYZE");
                    System.Environment.ExitCode = await RPGGame.Tuning.BalanceTuningWorkflow.RunAnalyzeAsync();
                    return;
                }

                if (args.Length > 0 && args[0] == "TUNEAPPLY")
                {
                    executionMode = "TUNEAPPLY";
                    BuildExecutionMetrics.RecordLaunchTime("TUNEAPPLY");
                    bool dryRun = args.Any(a => a.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
                    System.Environment.ExitCode = await RPGGame.Tuning.BalanceTuningWorkflow.RunApplyAsync(dryRun);
                    return;
                }

                if (args.Length > 0 && args[0] == "LEVELSIM")
                {
                    executionMode = "LEVELSIM";
                    BuildExecutionMetrics.RecordLaunchTime("LEVELSIM");
                    int battles = args.Length > 1 && int.TryParse(args[1], out int bpcSim) ? bpcSim : 25;
                    string? levels = args.Length > 2 ? args[2] : null;
                    System.Environment.ExitCode = await RPGGame.Tuning.LevelTuningWorkflow.RunSimAsync(battles, levels);
                    return;
                }

                if (args.Length > 0 && args[0] == "LEVELANALYZE")
                {
                    executionMode = "LEVELANALYZE";
                    BuildExecutionMetrics.RecordLaunchTime("LEVELANALYZE");
                    System.Environment.ExitCode = await RPGGame.Tuning.LevelTuningWorkflow.RunAnalyzeAsync();
                    return;
                }

                if (args.Length > 0 && args[0] == "LEVELAPPLY")
                {
                    executionMode = "LEVELAPPLY";
                    BuildExecutionMetrics.RecordLaunchTime("LEVELAPPLY");
                    bool dryRun = args.Any(a => a.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
                    System.Environment.ExitCode = await RPGGame.Tuning.LevelTuningWorkflow.RunApplyAsync(dryRun);
                    return;
                }

                if (args.Length > 0 && args[0] == "LEVELTUNING")
                {
                    executionMode = "LEVELTUNING";
                    BuildExecutionMetrics.RecordLaunchTime("LEVELTUNING");
                    int maxIter = args.Length > 1 && int.TryParse(args[1], out int mi) ? mi : 10;
                    int battles = args.Length > 2 && int.TryParse(args[2], out int bpc) ? bpc : 25;
                    string profile = RPGGame.Tuning.Profiles.TuningCliArgs.GetProfileId(args, "level-curve");
                    bool dryRun = args.Any(a => a.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
                    bool stopWhenPass = !args.Any(a => a.Equals("--no-stop-when-pass", StringComparison.OrdinalIgnoreCase));
                    await RPGGame.Tuning.LevelTuningRunner.Run(maxIter, battles, profile, stopWhenPass, dryRun, args);
                    return;
                }

                if (args.Length > 0 && args[0] == "TUNETUNING")
                {
                    executionMode = "TUNETUNING";
                    BuildExecutionMetrics.RecordLaunchTime("TUNETUNING");
                    int maxIter = RPGGame.Tuning.Profiles.TuningCliArgs.GetIntFlag(args, "--max-iterations", "-n") ?? 10;
                    string profile = RPGGame.Tuning.Profiles.TuningCliArgs.GetProfileId(args, "combat-dials");
                    bool dryRun = args.Any(a => a.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
                    bool stopWhenPass = !args.Any(a => a.Equals("--no-stop-when-pass", StringComparison.OrdinalIgnoreCase));
                    await RPGGame.Tuning.LevelTuningRunner.Run(maxIter, 25, profile, stopWhenPass, dryRun, args);
                    return;
                }

                // Check if unit test suite is requested (run-tests.bat / run-tests.ps1)
                if (args.Length > 0 && (args[0] == "--run-tests" || args.Any(a => a == "--run-tests")))
                {
                    executionMode = "TEST";
                    BuildExecutionMetrics.RecordLaunchTime("TEST");
                    RPGGame.Tests.Runners.ComprehensiveTestRunner.RunAllTests();
                    return;
                }

                if (args.Length > 0 && args[0] == "--run-data-tests")
                {
                    executionMode = "TEST";
                    BuildExecutionMetrics.RecordLaunchTime("TEST");
                    RPGGame.Tests.Runners.DataSystemTestRunner.RunAllTests();
                    return;
                }

                if (args.Length > 0 && args[0] == "--run-game-system-tests")
                {
                    executionMode = "TEST";
                    BuildExecutionMetrics.RecordLaunchTime("TEST");
                    RPGGame.Tests.Runners.GameSystemTestRunner.RunAllTests();
                    return;
                }

                if (args.Length > 0 && args[0] == "--list-test-suites")
                {
                    executionMode = "TEST";
                    BuildExecutionMetrics.RecordLaunchTime("TEST");
                    RPGGame.Tests.Runners.FilteredTestRunner.ListSuites();
                    return;
                }

                if (args.Length > 0 && args[0] == "--run-test-filter")
                {
                    executionMode = "TEST";
                    BuildExecutionMetrics.RecordLaunchTime("TEST");
                    string filter = args.Length > 1 ? string.Join(' ', args.Skip(1)) : "";
                    System.Environment.ExitCode = RPGGame.Tests.Runners.FilteredTestRunner.Run(filter);
                    return;
                }

                // Check if test mode is requested (battle comparison)
                if (args.Length > 0 && args[0] == "TEST")
                {
                    executionMode = "TEST";
                    BuildExecutionMetrics.RecordLaunchTime("TEST");
                    // Run test battle comparison
                    await RPGGame.Tests.TestBattleComparison.Main(args);
                    return;
                }

                if (args.Length > 0 && args[0].Equals("AUDIT_ACTIONS_HEADERS", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "AUDIT_ACTIONS_HEADERS";
                    BuildExecutionMetrics.RecordLaunchTime("AUDIT_ACTIONS_HEADERS");
                    string csvPathOrUrl = args.Length > 1
                        ? args[1]
                        : (RPGGame.Data.SheetsConfig.Load().ActionsSheetUrl
                           ?? "https://docs.google.com/spreadsheets/d/1bN3vmkQGdbO_4TkdgRXy_5KeuxUAcPtuarzSOOAyArc/export?format=csv&gid=2020359111");
                    await RPGGame.Data.SpreadsheetParserRunner.AuditHeadersAsync(csvPathOrUrl);
                    return;
                }

                // Check if parse mode is requested (for spreadsheet parser)
                if (args.Length > 0 && args[0].Equals("PARSE", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "PARSE";
                    BuildExecutionMetrics.RecordLaunchTime("PARSE");
                    // Run spreadsheet parser — default URL from SheetsConfig (matches live edit layout)
                    string csvPathOrUrl = args.Length > 1
                        ? args[1]
                        : (RPGGame.Data.SheetsConfig.Load().ActionsSheetUrl
                           ?? "https://docs.google.com/spreadsheets/d/1bN3vmkQGdbO_4TkdgRXy_5KeuxUAcPtuarzSOOAyArc/export?format=csv&gid=2020359111");
                    string outputPath = args.Length > 2 ? args[2] : "GameData/Actions.json";
                    await RPGGame.Data.SpreadsheetParserRunner.ParseAndGenerateAsync(csvPathOrUrl, outputPath);
                    return;
                }

                // Check if update actions mode is requested (for updating from Google Sheets)
                if (args.Length > 0 && args[0].Equals("UPDATE_ACTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "UPDATE_ACTIONS";
                    BuildExecutionMetrics.RecordLaunchTime("UPDATE_ACTIONS");
                    // Update Actions.json from Google Sheets
                    string? googleSheetsUrl = args.Length > 1 ? args[1] : null;
                    string? outputPath = args.Length > 2 ? args[2] : null;
                    await RPGGame.Data.ActionUpdateService.UpdateFromGoogleSheetsAsync(googleSheetsUrl, outputPath);
                    return;
                }

                if (args.Length > 0 && args[0].Equals("PUSH_ACTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "PUSH_ACTIONS";
                    BuildExecutionMetrics.RecordLaunchTime("PUSH_ACTIONS");
                    await RPGGame.Data.ActionSheetsPushService.PushActionsToGoogleSheetsAsync();
                    return;
                }

                if (args.Length > 0 && args[0].Equals("PUSH_SHEETS", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "PUSH_SHEETS";
                    BuildExecutionMetrics.RecordLaunchTime("PUSH_SHEETS");
                    var pushResult = await RPGGame.Data.GameDataSheetsPushService.PushAllGameDataSheetsAsync();
                    foreach (string line in pushResult.SummaryLines)
                        Console.WriteLine(line);
                    return;
                }

                if (args.Length > 0 && args[0].Equals("PULL_SHEETS", StringComparison.OrdinalIgnoreCase))
                {
                    executionMode = "PULL_SHEETS";
                    BuildExecutionMetrics.RecordLaunchTime("PULL_SHEETS");
                    await RPGGame.Data.GameDataSheetsPullService.PullAllFromSheetsConfigAsync();
                    return;
                }

                // Launch Avalonia GUI (execution time tracked until app closes)
                // Launch time will be recorded when the window is ready
                const string guiMutexName = "DungeonFighter-v2-GUI-SingleInstance";
                using var guiMutex = new Mutex(true, guiMutexName, out bool createdNewGuiInstance);
                if (!createdNewGuiInstance)
                {
                    Console.WriteLine("Dungeon Fighter is already running.");
                    return;
                }

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                // Stop tracking execution time (only for non-GUI modes)
                // GUI mode will track until the application closes
                if (executionMode != "GUI")
                {
                    BuildExecutionMetrics.StopExecutionTracking(executionMode);
                }
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        // Utility methods that are still needed by other classes
        public static async Task<WeaponItem?> CreateFallbackWeaponAsync(int playerLevel)
        {
            try
            {
                // Try to load weapon data and create a tier 1 weapon as fallback
                string? filePath = FindGameDataFile("Weapons.json");
                if (filePath == null)
                {
                    Console.WriteLine("   ERROR: Weapons.json file not found in any expected location");
                    return null;
                }

                string json = await File.ReadAllTextAsync(filePath);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var weaponData = System.Text.Json.JsonSerializer.Deserialize<List<WeaponData>>(json, options);
                if (weaponData == null)
                {
                    Console.WriteLine("   ERROR: Failed to deserialize weapon data from Weapons.json");
                    return null;
                }

                // Find a tier 1 weapon
                var tier1Weapons = weaponData.Where(w => w.Tier == 1).ToList();
                if (!tier1Weapons.Any())
                {
                    Console.WriteLine($"   ERROR: No Tier 1 weapons found in Weapons.json (total weapons: {weaponData.Count})");
                    return null;
                }

                // Pick a random tier 1 weapon
                var random = new Random();
                var selectedWeapon = tier1Weapons[random.Next(tier1Weapons.Count)];

                var weapon = ItemGenerator.GenerateWeaponItem(selectedWeapon);
                weapon.Rarity = "Common";

                return weapon;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ERROR: Exception in CreateFallbackWeapon: {ex.Message}");
                return null;
            }
        }

        public static string? FindGameDataFile(string fileName)
        {
            // Try current directory first
            if (File.Exists(fileName)) return fileName;
            
            // Try GameData subdirectory
            string gameDataPath = Path.Combine("GameData", fileName);
            if (File.Exists(gameDataPath)) return gameDataPath;
            
            // Try parent directory + GameData
            string parentGameDataPath = Path.Combine("..", "GameData", fileName);
            if (File.Exists(parentGameDataPath)) return parentGameDataPath;
            
            return null;
        }

        private static bool ShouldEnableSimulationFastPacing(string[] args)
        {
            if (args.Length == 0)
                return false;

            string cmd = args[0];
            if (cmd.StartsWith("--run-", StringComparison.OrdinalIgnoreCase)
                || cmd.Equals("--list-test-suites", StringComparison.OrdinalIgnoreCase))
                return true;

            return cmd.ToUpperInvariant() switch
            {
                "MCP" or "MCPSMOKE" or "PLAYTODEATH" or "PLAYTODEATHBATCH" or "PLAYTHROUGHTUNING"
                    or "TUNESIM" or "TUNEANALYZE" or "TUNEAPPLY" or "TUNETUNING" or "TUNEPROFILES"
                    or "LEVELSIM" or "LEVELANALYZE" or "LEVELAPPLY" or "LEVELTUNING" or "TEST" => true,
                _ => false
            };
        }
        
    }
}
