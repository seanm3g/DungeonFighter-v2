using System.Threading.Tasks;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Backward-compatible aliases for the level-curve profile.
    /// </summary>
    public static class LevelTuningWorkflow
    {
        public static Task<int> RunSimAsync(int battlesPerCombination = 25, string? levelsCsv = null, string? sessionPath = null)
        {
            string[] args = levelsCsv != null
                ? new[] { "LEVELSIM", battlesPerCombination.ToString(), levelsCsv }
                : new[] { "LEVELSIM", battlesPerCombination.ToString() };

            return BalanceTuningWorkflow.RunSimAsync("level-curve", args, sessionPath);
        }

        public static Task<int> RunAnalyzeAsync(string? sessionPath = null) =>
            BalanceTuningWorkflow.RunAnalyzeAsync(sessionPath);

        public static Task<int> RunApplyAsync(bool dryRun = false, string? sessionPath = null) =>
            BalanceTuningWorkflow.RunApplyAsync(dryRun, sessionPath);

        public static Task EnsureGameInitializedAsync() => BalanceTuningWorkflow.EnsureGameInitializedAsync();
    }
}
