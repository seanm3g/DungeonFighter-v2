using System.Diagnostics;

// A no-argument desktop entry point for launchers which cannot supply ICONLAB.
// Launch through the interactive desktop, without a terminal window.
Process.Start(new ProcessStartInfo
{
    FileName = Path.Combine(AppContext.BaseDirectory, "DF.exe"),
    Arguments = "ICONLAB",
    WorkingDirectory = AppContext.BaseDirectory,
    UseShellExecute = true,
    WindowStyle = ProcessWindowStyle.Normal
});
