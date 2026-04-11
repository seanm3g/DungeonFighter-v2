using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Handles wiring for the Testing settings panel
    /// </summary>
    public class TestingPanelHandler : ISettingsPanelHandler
    {
        private readonly CanvasUICoordinator? canvasUI;
        private TextBoxTestRunner? textBoxTestRunner;

        public string PanelType => "Testing";

        public TestingPanelHandler(CanvasUICoordinator? canvasUI)
        {
            this.canvasUI = canvasUI;
        }

        public void WireUp(UserControl panel)
        {
            if (panel is not TestingSettingsPanel testingPanel || canvasUI == null) return;

            var outputTextBox = testingPanel.FindControl<TextBox>("TestOutputTextBox");
            var statusTextBlock = testingPanel.FindControl<TextBlock>("TestOutputProgressText");
            textBoxTestRunner = new TextBoxTestRunner(
                outputTextBox,
                statusTextBlock,
                null
            );

            WireUpTestButtons(testingPanel);
            WireUpScriptsSubsection(testingPanel);
        }

        public void LoadSettings(UserControl panel)
        {
        }

        public void SaveSettings(UserControl panel)
        {
        }

        private void WireUpTestButtons(TestingSettingsPanel panel)
        {
            if (panel == null || textBoxTestRunner == null) return;

            var testDiceRollsButton = panel.FindControl<Button>("TestDiceRollsButton");
            var testCombatButton = panel.FindControl<Button>("TestCombatButton");
            var testStatusEffectsButton = panel.FindControl<Button>("TestStatusEffectsButton");
            var testMultiHitButton = panel.FindControl<Button>("TestMultiHitButton");
            var testComboButton = panel.FindControl<Button>("TestComboButton");
            var testActionMechanicsButton = panel.FindControl<Button>("TestActionMechanicsButton");
            var testProgressionButton = panel.FindControl<Button>("TestProgressionButton");
            var testDungeonRewardsButton = panel.FindControl<Button>("TestDungeonRewardsButton");
            var testSaveLoadButton = panel.FindControl<Button>("TestSaveLoadButton");
            var testGameplayFlowButton = panel.FindControl<Button>("TestGameplayFlowButton");
            var actionInteractionLabButton = panel.FindControl<Button>("ActionInteractionLabButton");
            var testAllMechanicsButton = panel.FindControl<Button>("TestAllMechanicsButton");
            var testClearOutputButton = panel.FindControl<Button>("TestClearOutputButton");

            if (testDiceRollsButton != null)
                testDiceRollsButton.Click += async (s, e) => await textBoxTestRunner.RunDiceRollMechanicsTestsAsync();
            if (testCombatButton != null)
                testCombatButton.Click += async (s, e) => await textBoxTestRunner.RunCombatMechanicsTestsAsync();
            if (testStatusEffectsButton != null)
                testStatusEffectsButton.Click += async (s, e) => await textBoxTestRunner.RunStatusEffectsTestsAsync();
            if (testMultiHitButton != null)
                testMultiHitButton.Click += async (s, e) => await textBoxTestRunner.RunMultiHitTestsAsync();
            if (testComboButton != null)
                testComboButton.Click += async (s, e) => await textBoxTestRunner.RunComboSystemTestsAsync();
            if (testActionMechanicsButton != null)
                testActionMechanicsButton.Click += async (s, e) => await textBoxTestRunner.RunActionMechanicsTestsAsync();
            if (testProgressionButton != null)
                testProgressionButton.Click += async (s, e) => await textBoxTestRunner.RunProgressionTestsAsync();
            if (testDungeonRewardsButton != null)
                testDungeonRewardsButton.Click += async (s, e) => await textBoxTestRunner.RunDungeonAndRewardsTestsAsync();
            if (testSaveLoadButton != null)
                testSaveLoadButton.Click += async (s, e) => await textBoxTestRunner.RunSaveLoadSystemTestsAsync();
            if (testGameplayFlowButton != null)
                testGameplayFlowButton.Click += async (s, e) => await textBoxTestRunner.RunGameplayFlowTestsAsync();
            if (actionInteractionLabButton != null)
                actionInteractionLabButton.Click += async (s, e) =>
                {
                    var game = canvasUI?.GetGame();
                    if (game == null || canvasUI == null) return;
                    canvasUI.GetMainWindow()?.Activate();
                    await game.StartActionInteractionLabAsync(canvasUI).ConfigureAwait(true);
                };
            if (testAllMechanicsButton != null)
                testAllMechanicsButton.Click += async (s, e) => await textBoxTestRunner.RunMechanicsAndReliabilityTestsAsync();
            if (testClearOutputButton != null)
                testClearOutputButton.Click += (s, e) => textBoxTestRunner.ClearOutput();
        }

        private void WireUpScriptsSubsection(TestingSettingsPanel panel)
        {
            var scriptsPanel = panel.FindControl<Panel>("ScriptsButtonsPanel");
            if (scriptsPanel == null || textBoxTestRunner == null) return;

            string? scriptsDir = GetScriptsDirectory();
            if (string.IsNullOrEmpty(scriptsDir) || !Directory.Exists(scriptsDir))
            {
                var noScripts = new TextBlock
                {
                    Text = "Scripts folder not found (run from project root).",
                    Foreground = Brushes.Gray
                };
                scriptsPanel.Children.Add(noScripts);
                return;
            }

            var extensions = new[] { "*.ps1", "*.bat" };
            var scriptFiles = new List<string>();
            foreach (var ext in extensions)
            {
                try
                {
                    scriptFiles.AddRange(Directory.GetFiles(scriptsDir, ext));
                }
                catch { /* ignore */ }
            }

            var descriptions = LoadScriptDescriptions(scriptsDir);

            foreach (var path in scriptFiles.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(path);
                string description = descriptions.TryGetValue(fileName, out var d) ? d : "Run this script.";
                var btn = new Button
                {
                    Content = fileName,
                    Width = 200,
                    Height = 32,
                    Background = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(4),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 8, 6)
                };
                ToolTip.SetTip(btn, description);
                string scriptPath = path;
                btn.Click += async (s, e) => await RunScriptAsync(scriptPath, panel);
                scriptsPanel.Children.Add(btn);
            }
        }

        private static Dictionary<string, string> LoadScriptDescriptions(string scriptsDir)
        {
            var path = Path.Combine(scriptsDir, "ScriptDescriptions.json");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return new Dictionary<string, string>();

            try
            {
                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return dict ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static string? GetScriptsDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            var candidates = new[]
            {
                Path.Combine(currentDir, "Scripts"),
                Path.Combine(currentDir, "..", "Scripts"),
                Path.Combine(currentDir, "..", "..", "Scripts"),
                Path.Combine(baseDir, "..", "..", "..", "..", "Scripts"),
                Path.Combine(baseDir, "..", "..", "..", "Scripts"),
                Path.Combine(baseDir, "Scripts")
            };
            foreach (var c in candidates)
            {
                try
                {
                    string full = Path.GetFullPath(c);
                    if (Directory.Exists(full))
                        return full;
                }
                catch { /* ignore */ }
            }
            return null;
        }

        private async Task RunScriptAsync(string scriptPath, TestingSettingsPanel panel)
        {
            if (textBoxTestRunner == null) return;

            string fileName = Path.GetFileName(scriptPath);
            textBoxTestRunner.AppendOutput($"=== Running script: {fileName} ===\n");
            var statusTextBlock = panel.FindControl<TextBlock>("TestOutputProgressText");
            if (statusTextBlock != null)
                statusTextBlock.Text = $"Running {fileName}...";

            try
            {
                string? workingDir = Path.GetDirectoryName(scriptPath);
                workingDir = string.IsNullOrEmpty(workingDir) ? Directory.GetCurrentDirectory() : workingDir;
                string rootDir = Path.GetFullPath(Path.Combine(workingDir, ".."));

                var ext = Path.GetExtension(scriptPath).ToLowerInvariant();
                ProcessStartInfo startInfo;
                if (ext == ".ps1")
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                        WorkingDirectory = rootDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };
                }
                else if (ext == ".bat")
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c \"{scriptPath}\"",
                        WorkingDirectory = rootDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };
                }
                else
                {
                    textBoxTestRunner.AppendOutput($"Unsupported script type: {ext}\n\n");
                    if (statusTextBlock != null) statusTextBlock.Text = "Ready";
                    return;
                }

                using var process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();
                process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await Task.Run(() => process.WaitForExit(120000));

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (output.Length > 0)
                        textBoxTestRunner.AppendOutput(output.ToString());
                    if (error.Length > 0)
                        textBoxTestRunner.AppendOutput("[stderr]\n" + error.ToString());
                    textBoxTestRunner.AppendOutput($"=== Exit code: {process.ExitCode} ===\n\n");
                    if (statusTextBlock != null)
                        statusTextBlock.Text = process.ExitCode == 0 ? $"Done: {fileName}" : $"Exit {process.ExitCode}: {fileName}";
                });
            }
            catch (Exception ex)
            {
                textBoxTestRunner.AppendOutput($"Error: {ex.Message}\n{ex.StackTrace}\n\n");
                if (statusTextBlock != null)
                    statusTextBlock.Text = $"Error: {ex.Message}";
            }
        }
    }
}
