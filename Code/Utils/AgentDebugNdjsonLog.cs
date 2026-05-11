using System;
using System.IO;
using System.Text.Json;

namespace RPGGame.Utils
{
    /// <summary>Session-scoped NDJSON file log for debug-mode reproduction (no secrets).</summary>
    internal static class AgentDebugNdjsonLog
    {
        private const string SessionId = "c9a928";
        private const string FileName = "debug-c9a928.log";

        internal static void Write(string hypothesisId, string location, string message, object? data = null)
        {
            try
            {
                string path = ResolvePath();
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var line = JsonSerializer.Serialize(new
                {
                    sessionId = SessionId,
                    hypothesisId,
                    location,
                    message,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    threadId = global::System.Environment.CurrentManagedThreadId,
                    data
                });
                File.AppendAllText(path, line + "\n");
            }
            catch { /* never break game for debug log */ }
        }

        private static string ResolvePath()
        {
            string baseDir = AppContext.BaseDirectory;
            // net8.0 -> Debug -> bin -> Code -> repo root (4 levels)
            string fromExe = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", FileName));
            if (Directory.Exists(Path.GetDirectoryName(fromExe) ?? ""))
                return fromExe;
            return Path.Combine(baseDir, FileName);
        }
    }
}
