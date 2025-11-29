using System;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Handles JSON serialization/deserialization for session statistics
    /// </summary>
    public static class StatisticsSerializer
    {
        /// <summary>
        /// Saves session statistics to a JSON file
        /// </summary>
        /// <param name="stats">The statistics to save</param>
        /// <param name="filename">The filename to save to</param>
        public static void SaveSessionStatistics(SessionStatistics stats, string? filename = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    filename = "session_stats.json";
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                string json = JsonSerializer.Serialize(stats, options);
                File.WriteAllText(filename, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save session statistics: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads session statistics from a JSON file
        /// </summary>
        /// <param name="filename">The filename to load from</param>
        /// <returns>Loaded session statistics or new instance if file doesn't exist</returns>
        public static SessionStatistics LoadSessionStatistics(string? filename = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    filename = "session_stats.json";
                }
                
                if (!File.Exists(filename))
                {
                    return new SessionStatistics();
                }
                
                string json = File.ReadAllText(filename);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var stats = JsonSerializer.Deserialize<SessionStatistics>(json, options);
                return stats ?? new SessionStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load session statistics: {ex.Message}");
                return new SessionStatistics();
            }
        }
    }
}

