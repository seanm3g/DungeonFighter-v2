namespace RPGGame.Tuning
{
    /// <summary>
    /// Coarse progress reporting for long simulations (workbench log + encounter batches).
    /// </summary>
    public static class SimulationProgressReporter
    {
        /// <summary>Log every 10 steps for smaller runs; every 50 for runs of 500+ items.</summary>
        public static int GetReportInterval(int total) => total >= 500 ? 50 : 10;

    }
}
