using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Identifies exactly what a headless tool run measured: the built code, the repo commit, the GameData it read and
    /// the balance patch. Printed as one "Provenance:" line so two results can be checked for comparability.
    /// </summary>
    public static class RunProvenance
    {
        private static string? _cached;

        /// <summary>
        /// <c>build=&lt;dll hash&gt; git=&lt;commit&gt;[+dirty] data=&lt;GameData hash&gt; patch=&lt;name&gt;@&lt;hash&gt;</c>.
        /// Results are comparable when build, data and patch match.
        /// </summary>
        public static string Describe()
        {
            if (_cached != null)
                return _cached;
            string build = SafeHash(() => HashFile(typeof(RunProvenance).Assembly.Location));
            string git = SafeGit();
            string data = SafeHash(HashGameData);
            string patchPath = SafeHash(() => GameConfiguration.LoadedBalancePatchPath);
            string patch = $"{Path.GetFileNameWithoutExtension(patchPath)}@{SafeHash(() => HashFile(patchPath))}";
            _cached = $"build={build} git={git} data={data} patch={patch}";
            return _cached;
        }

        /// <summary>Hash of the game's static data: top-level GameData *.json minus saves, logs and run output.</summary>
        private static string HashGameData()
        {
            string root = GameConstants.ResolveGameDataPath();
            var files = Directory.GetFiles(root, "*.json", SearchOption.TopDirectoryOnly)
                .Where(f =>
                {
                    string n = Path.GetFileName(f);
                    return !n.StartsWith("character_", StringComparison.OrdinalIgnoreCase)
                        && !n.Equals("build_execution_metrics.json", StringComparison.OrdinalIgnoreCase)
                        && !n.Equals("PatchProfile.json", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);
            using var sha = SHA256.Create();
            foreach (string f in files)
            {
                byte[] name = Encoding.UTF8.GetBytes(Path.GetFileName(f).ToLowerInvariant());
                sha.TransformBlock(name, 0, name.Length, null, 0);
                byte[] body = File.ReadAllBytes(f);
                sha.TransformBlock(body, 0, body.Length, null, 0);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha.Hash!)[..10].ToLowerInvariant();
        }

        private static string HashFile(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream))[..10].ToLowerInvariant();
        }

        private static string SafeHash(Func<string> f)
        {
            try { return f(); }
            catch { return "unknown"; }
        }

        /// <summary>Commit of the repo the tool was built from, with +dirty when tracked files differ from it.</summary>
        private static string SafeGit()
        {
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
                    dir = dir.Parent;
                if (dir == null)
                    return "none";
                string repo = dir.FullName;
                string commit = RunGit(repo, "rev-parse --short HEAD").Trim();
                bool dirty = RunGit(repo, "status --porcelain --untracked-files=no").Trim().Length > 0;
                return commit.Length == 0 ? "unknown" : commit + (dirty ? "+dirty" : "");
            }
            catch
            {
                return "unknown";
            }
        }

        private static string RunGit(string repo, string args)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = repo,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return output;
        }
    }
}
