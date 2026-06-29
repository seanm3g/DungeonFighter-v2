namespace RPGGame.Tuning
{
    /// <summary>
    /// Coarse progress reporting for long simulations (workbench log + encounter batches).
    /// </summary>
    public static class SimulationProgressReporter
    {
        /// <summary>Log every 10 steps for smaller runs; every 50 for runs of 500+ items.</summary>
        public static int GetReportInterval(int total) => total >= 500 ? 50 : 10;

        public static bool ShouldReport(int completed, int total, int lastReportedCompleted)
        {
            if (total <= 0)
                return completed > 0;
            if (completed >= total)
                return true;
            if (completed <= 0)
                return true;
            if (lastReportedCompleted < 0)
                return true;

            int interval = GetReportInterval(total);
            return completed - lastReportedCompleted >= interval;
        }
    }
}
