using System;
using System.Diagnostics;
using System.IO;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Platform
{
    /// <summary>
    /// Guards the Windows friend-share launcher: extract-the-folder checks,
    /// user-local SDK install, Mark-of-the-Web unblock, and prebuilt exe fallback.
    /// </summary>
    public static class WindowsLauncherTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== WindowsLauncher Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            string? repoRoot = FindRepoRoot();
            TestBase.AssertTrue(
                repoRoot != null && Directory.Exists(repoRoot),
                "Should resolve the repository root next to GameData / Scripts",
                ref testsRun, ref testsPassed, ref testsFailed);

            if (repoRoot == null)
            {
                TestBase.PrintSummary("WindowsLauncher Tests", testsRun, testsPassed, testsFailed);
                return;
            }

            TestTrampolineFilesExist(repoRoot, ref testsRun, ref testsPassed, ref testsFailed);
            TestTrampolinesCallScriptLauncher(repoRoot, ref testsRun, ref testsPassed, ref testsFailed);
            TestLaunchScriptHandlesShareFailures(repoRoot, ref testsRun, ref testsPassed, ref testsFailed);
            TestInstallScriptUsesUserLocalSdk(repoRoot, ref testsRun, ref testsPassed, ref testsFailed);
            TestInstallScriptParsesAsPowerShell(repoRoot, ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("WindowsLauncher Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestTrampolineFilesExist(
            string repoRoot, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestTrampolineFilesExist));

            TestBase.AssertTrue(
                File.Exists(Path.Combine(repoRoot, "DungeonFighter-PC.bat")),
                "DungeonFighter-PC.bat should exist at repo root (no parentheses in the name)",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                File.Exists(Path.Combine(repoRoot, "Dungeon Fighter(PC).bat")),
                "Dungeon Fighter(PC).bat wrapper should still exist",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                File.Exists(Path.Combine(repoRoot, "Scripts", "launch-windows.bat")),
                "Scripts/launch-windows.bat should hold the real launcher",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                File.Exists(Path.Combine(repoRoot, "Scripts", "install-dotnet.ps1")),
                "Scripts/install-dotnet.ps1 should exist",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestTrampolinesCallScriptLauncher(
            string repoRoot, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestTrampolinesCallScriptLauncher));

            string preferred = File.ReadAllText(Path.Combine(repoRoot, "DungeonFighter-PC.bat"));
            string legacy = File.ReadAllText(Path.Combine(repoRoot, "Dungeon Fighter(PC).bat"));

            TestBase.AssertTrue(
                preferred.Contains(@"Scripts\launch-windows.bat", StringComparison.OrdinalIgnoreCase),
                "DungeonFighter-PC.bat should call Scripts\\launch-windows.bat",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                legacy.Contains(@"Scripts\launch-windows.bat", StringComparison.OrdinalIgnoreCase),
                "Dungeon Fighter(PC).bat should call Scripts\\launch-windows.bat",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                preferred.Contains("Extract the whole game folder", StringComparison.OrdinalIgnoreCase),
                "Preferred trampoline should tell the player to extract the zip",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestLaunchScriptHandlesShareFailures(
            string repoRoot, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestLaunchScriptHandlesShareFailures));

            string script = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "launch-windows.bat"));

            TestBase.AssertTrue(
                script.Contains(@"%USERPROFILE%\.dotnet", StringComparison.OrdinalIgnoreCase),
                "Launcher should probe the user-local .dotnet folder",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                script.Contains("Unblock-File", StringComparison.OrdinalIgnoreCase),
                "Launcher should unblock Mark-of-the-Web on downloaded files",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                script.Contains(@"Code\Code.csproj", StringComparison.OrdinalIgnoreCase)
                    && script.Contains("GameData", StringComparison.Ordinal),
                "Launcher should require Code.csproj and GameData before building",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                script.Contains(".zip", StringComparison.OrdinalIgnoreCase),
                "Launcher should reject running from inside an unextracted zip path",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                script.Contains(@"dist\DF.exe", StringComparison.OrdinalIgnoreCase)
                    && script.Contains("NEED_BUILD", StringComparison.Ordinal),
                "Launcher should launch a pre-built DF.exe when no SDK is available",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                script.Contains("pushd \"%~dp0..\"", StringComparison.Ordinal),
                "Launcher should cd to repo root with \"%~dp0..\" so trailing backslashes cannot break quotes",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestInstallScriptUsesUserLocalSdk(
            string repoRoot, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestInstallScriptUsesUserLocalSdk));

            string script = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "install-dotnet.ps1"));

            TestBase.AssertTrue(
                script.Contains(".dotnet", StringComparison.Ordinal)
                    && script.Contains("USERPROFILE", StringComparison.OrdinalIgnoreCase),
                "install-dotnet.ps1 should target %USERPROFILE%\\.dotnet",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                script.Contains("-InstallDir $userDotNetDir", StringComparison.Ordinal)
                    || script.Contains("-InstallDir $localDotnet", StringComparison.Ordinal),
                "install-dotnet.ps1 should pass a user-local -InstallDir to dotnet-install.ps1",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertFalse(
                script.Contains("-InstallDir \"$env:ProgramFiles\\dotnet\"", StringComparison.Ordinal),
                "install-dotnet.ps1 should not require Program Files (admin) as the install directory",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                script.Contains("no administrator", StringComparison.OrdinalIgnoreCase)
                    || script.Contains("No administrator", StringComparison.OrdinalIgnoreCase),
                "install-dotnet.ps1 should state that admin rights are not required",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestInstallScriptParsesAsPowerShell(
            string repoRoot, ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestInstallScriptParsesAsPowerShell));

            string scriptPath = Path.Combine(repoRoot, "Scripts", "install-dotnet.ps1");
            if (!OperatingSystem.IsWindows())
            {
                TestBase.AssertTrue(true, "PowerShell parse skipped off Windows", ref testsRun, ref testsPassed, ref testsFailed);
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                ArgumentList =
                {
                    "-NoProfile",
                    "-NonInteractive",
                    "-Command",
                    "$e=$null; [void][System.Management.Automation.Language.Parser]::ParseFile('"
                        + scriptPath.Replace("'", "''")
                        + "', [ref]$null, [ref]$e); if ($e) { $e | ForEach-Object { $_.ToString() }; exit 1 } else { exit 0 }"
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    TestBase.AssertTrue(false, "Could not start powershell to parse install-dotnet.ps1", ref testsRun, ref testsPassed, ref testsFailed);
                    return;
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(30000);
                TestBase.AssertTrue(
                    process.ExitCode == 0,
                    "install-dotnet.ps1 should parse as valid PowerShell"
                        + (string.IsNullOrWhiteSpace(stdout + stderr) ? "" : ": " + (stdout + stderr).Trim()),
                    ref testsRun, ref testsPassed, ref testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, "PowerShell parse failed to run: " + ex.Message, ref testsRun, ref testsPassed, ref testsFailed);
            }
        }

        private static string? FindRepoRoot()
        {
            string? gameData = GameConstants.GetSettingsDirectory();
            if (!string.IsNullOrEmpty(gameData))
            {
                string? root = Path.GetDirectoryName(gameData);
                if (root != null && File.Exists(Path.Combine(root, "Scripts", "install-dotnet.ps1")))
                    return root;
            }

            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Scripts", "install-dotnet.ps1"))
                    && Directory.Exists(Path.Combine(dir.FullName, "GameData")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}
