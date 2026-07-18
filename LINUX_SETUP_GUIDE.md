# Running DungeonFighter-v2 on Linux

## Prerequisites

1. **.NET 8.0 SDK** — required to build and run
2. A **desktop session** (X11 or Wayland) — Avalonia needs a display
3. `curl` (for the optional auto-installer) and usual build tools

## Method 1: Linux launcher (easiest)

From the repo root:

```bash
chmod +x "Dungeon Fighter(Linux).sh" Scripts/install-dotnet.sh play.sh
./"Dungeon Fighter(Linux).sh"
```

The launcher will:

- Ensure a .NET **8.x** SDK is available (installs via apt or Microsoft’s script if needed)
- Build `Code/Code.csproj` (Debug)
- Run the game in the **foreground** (errors stay visible in the terminal)

Background launch:

```bash
./"Dungeon Fighter(Linux).sh" --bg
```

Log file when using `--bg`: `/tmp/DF2_linux_launch.log`

## Method 2: Manual `dotnet` commands

```bash
cd Code
dotnet build
dotnet run
```

If you installed the SDK under `~/.dotnet` and `dotnet` is missing from PATH:

```bash
export PATH="$HOME/.dotnet:$PATH"
```

## Installing .NET 8 yourself

**Ubuntu / Debian (apt):**

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

**Any distro (official script):**

```bash
bash Scripts/install-dotnet.sh
```

Or download from: https://dotnet.microsoft.com/download/dotnet/8.0

## Troubleshooting

| Symptom | What to try |
|---|---|
| Window never appears | Run from a logged-in desktop; check `echo $DISPLAY` / `$WAYLAND_DISPLAY` |
| `dotnet: command not found` | `export PATH="$HOME/.dotnet:$PATH"` then open a new terminal |
| Build works, run crashes on native libs | Ensure you used the **SDK** (not only the runtime); `dotnet --list-sdks` should show an `8.x` line |
| Game already running | Close the other DF window, or `pkill -f 'DF\.dll'` |

## Related launchers

- Windows: `Dungeon Fighter(PC).bat`
- macOS: `Dungeon Fighter(Mac).sh`
- CLI play helper: `./play.sh` (from repo root)
