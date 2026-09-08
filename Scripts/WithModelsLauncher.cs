using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

// Compile with Scripts/Build-WithModelsLauncher.ps1. Keep beside the project folders.
internal static class WithModelsLauncher
{
    [STAThread]
    private static int Main(string[] args)
    {
        string root = AppDomain.CurrentDomain.BaseDirectory;
        string game = Path.Combine(root, "Art", "DemonFighter", "Verification", "bin", "DF.exe");
        bool checkOnly = args.Length == 1 && args[0] == "--check";
        try
        {
            if (!File.Exists(game) || !File.Exists(Path.ChangeExtension(game, ".dll")))
                throw new FileNotFoundException("The build with models is missing. Build the game into Art\\DemonFighter\\Verification\\bin first.", game);
            if (checkOnly) return 0;
            Process.Start(new ProcessStartInfo(game) { WorkingDirectory = root, UseShellExecute = true });
            return 0;
        }
        catch (Exception error)
        {
            if (!checkOnly)
                MessageBox.Show(error.Message, "Demon Fighter — With models", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
