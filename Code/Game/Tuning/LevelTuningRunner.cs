using System;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Full CLI loop: TUNETUNING / LEVELTUNING — repeated TUNESIM → TUNEANALYZE → TUNEAPPLY.
    /// </summary>
    public static class LevelTuningRunner
    {
        public static async Task Run(
            int maxIterations = 10,
            int battlesPerCombination = 25,
            string profileId = BalanceTuningProfileLoader.DefaultProfileId,
            bool stopWhenPass = true,
            bool dryRun = false,
            string[]? cliArgs = null)
        {
            Console.WriteLine("═════════════════════════════════════════");
            Console.WriteLine("  Balance Tuning (full loop)");
            Console.WriteLine($"  Profile: {profileId}");
            Console.WriteLine($"  Max iterations: {maxIterations}, stop-when-pass: {stopWhenPass}, dry-run: {dryRun}");
            Console.WriteLine("  Tip: use TUNESIM / TUNEANALYZE / TUNEAPPLY for step-by-step control");
            Console.WriteLine("═════════════════════════════════════════\n");

            double previousScore = -1;
            string[] simArgs = BuildSimArgs(cliArgs, battlesPerCombination);

            try
            {
                for (int iteration = 1; iteration <= maxIterations; iteration++)
                {
                    Console.WriteLine($"\n{new string('=', 50)}");
                    Console.WriteLine($"ITERATION {iteration}/{maxIterations}");
                    Console.WriteLine($"{new string('=', 50)}\n");

                    var snapshotPatch = BalancePatchManager.CreatePatch(
                        $"tuning_snapshot_{profileId}_{iteration}",
                        "LevelTuningRunner",
                        "Pre-iteration config snapshot",
                        $"0.{iteration}");

                    await BalanceTuningWorkflow.RunSimAsync(profileId, simArgs);

                    var session = LevelTuningSessionStore.Load();
                    if (session.Simulation == null && session.Comprehensive == null && session.Fundamentals == null)
                    {
                        Console.WriteLine("Simulation failed to persist; stopping.");
                        break;
                    }

                    if (stopWhenPass && session.Simulation?.AllAnchorsWithinTolerance == true)
                    {
                        Console.WriteLine("All curve anchors within tolerance.");
                        break;
                    }

                    double currentScore = session.Simulation?.OverallCurveScore ?? 0;

                    if (previousScore >= 0 && session.Simulation != null && currentScore < previousScore)
                    {
                        Console.WriteLine($"Curve score regressed ({currentScore:F1} < {previousScore:F1}). Reverting.");
                        BalancePatchManager.ApplyPatch(snapshotPatch);
                        GameConfiguration.ResetInstance();
                        _ = GameConfiguration.Instance;
                        continue;
                    }

                    if (session.Simulation != null)
                        previousScore = session.Simulation.OverallCurveScore;

                    await BalanceTuningWorkflow.RunAnalyzeAsync();

                    session = LevelTuningSessionStore.Load();
                    if (!string.IsNullOrEmpty(session.Analysis?.PrimaryDial))
                        Console.WriteLine($"Primary dial: {session.Analysis.PrimaryDial} — {session.Analysis.DialDiagnosis}");

                    if (stopWhenPass && session.Analysis?.AllAnchorsPass == true)
                    {
                        Console.WriteLine("All checks pass.");
                        break;
                    }

                    if (session.Analysis?.TopSuggestion == null)
                    {
                        Console.WriteLine("No suggestion; stopping.");
                        break;
                    }

                    int applyCode = await BalanceTuningWorkflow.RunApplyAsync(dryRun);
                    if (applyCode != 0 && !dryRun)
                    {
                        Console.WriteLine("Apply failed; stopping.");
                        break;
                    }
                }

                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("TUNING LOOP COMPLETE");
                Console.WriteLine(new string('=', 50));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static string[] BuildSimArgs(string[]? cliArgs, int battlesPerCombination)
        {
            if (cliArgs != null && cliArgs.Length > 0)
                return cliArgs;

            return new[] { "TUNETUNING", "--battles", battlesPerCombination.ToString() };
        }
    }
}
