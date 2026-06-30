using System.Threading.Tasks;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Full loop for class-playthrough-balance profile: TUNESIM → TUNEANALYZE → TUNEAPPLY.
    /// </summary>
    public static class PlaythroughTuningRunner
    {
        public const string DefaultProfileId = "class-playthrough-balance";

        public static Task Run(
            int maxIterations = 8,
            int runsPerClass = 10,
            bool stopWhenPass = true,
            bool dryRun = false,
            string[]? cliArgs = null) =>
            LevelTuningRunner.Run(
                maxIterations,
                runsPerClass,
                DefaultProfileId,
                stopWhenPass,
                dryRun,
                BuildCliArgs(cliArgs, runsPerClass));

        private static string[] BuildCliArgs(string[]? cliArgs, int runsPerClass)
        {
            if (cliArgs != null && cliArgs.Length > 0)
                return cliArgs;

            return new[]
            {
                "PLAYTHROUGHTUNING",
                "--profile", DefaultProfileId,
                "--runs-per-class", runsPerClass.ToString()
            };
        }
    }
}
