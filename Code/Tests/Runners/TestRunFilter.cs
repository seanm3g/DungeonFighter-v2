using System;

namespace RPGGame.Tests.Runners
{
    /// <summary>Substring matching for filtered test runs (--run-test-filter).</summary>
    public static class TestRunFilter
    {
        public static bool Matches(string suiteName, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            if (suiteName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return true;

            string withoutTests = suiteName.EndsWith("Tests", StringComparison.OrdinalIgnoreCase)
                ? suiteName[..^5]
                : suiteName;

            return withoutTests.Contains(filter, StringComparison.OrdinalIgnoreCase)
                   || filter.Contains(withoutTests, StringComparison.OrdinalIgnoreCase);
        }
    }
}
