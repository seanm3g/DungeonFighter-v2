using System;
using System.Collections.Generic;

namespace RPGGame.Config.BalancePatches
{
    /// <summary>
    /// Data structures for balance patch metadata
    /// Extracted from BalancePatchManager to separate metadata structures
    /// </summary>
    public class BalancePatchMetadata
    {
        private const string GameVersion = "6.2";

        /// <summary>
        /// Patch metadata structure
        /// </summary>
        public class PatchMetadata
        {
            public string PatchId { get; set; } = "";
            public string Name { get; set; } = "";
            public string Author { get; set; } = "";
            public string Description { get; set; } = "";
            public string Version { get; set; } = "1.0";
            public string CreatedDate { get; set; } = "";
            public string CompatibleGameVersion { get; set; } = GameVersion;
            public List<string> Tags { get; set; } = new();
            public TestResults? TestResults { get; set; }
        }

        /// <summary>
        /// Test results embedded in patch
        /// </summary>
        public class TestResults
        {
            public double AverageWinRate { get; set; }
            public string TestDate { get; set; } = "";
            public int BattlesTested { get; set; }
        }

        /// <summary>
        /// Complete patch structure
        /// </summary>
        public class BalancePatch
        {
            public PatchMetadata PatchMetadata { get; set; } = new();
            public GameConfiguration TuningConfig { get; set; } = new();
        }

        /// <summary>
        /// Validation result
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
        }

        /// <summary>
        /// Generate unique patch ID
        /// </summary>
        public static string GeneratePatchId(string name, string version)
        {
            string sanitized = name.ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_");
            
            // Remove special characters
            sanitized = new string(sanitized.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            
            return $"{sanitized}_v{version}_{DateTime.Now:yyyyMMdd}";
        }

        /// <summary>
        /// Get current game version
        /// </summary>
        public static string GetGameVersion() => GameVersion;
    }
}

