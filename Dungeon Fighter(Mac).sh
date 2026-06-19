#!/bin/bash

# Dungeon Fighter v2 - macOS Launcher

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR" || exit 1

# Lock file to prevent duplicate launcher execution (double-click / rapid re-launch)
LOCKFILE="/tmp/DF2_Launcher_${USER}.lock"
if [ -f "$LOCKFILE" ]; then
    OLD_PID=$(cat "$LOCKFILE" 2>/dev/null)
    if [ -n "$OLD_PID" ] && kill -0 "$OLD_PID" 2>/dev/null; then
        echo "Launcher is already running (PID $OLD_PID)."
        exit 1
    fi
    rm -f "$LOCKFILE"
fi
echo "$$" > "$LOCKFILE"

# Cleanup function
cleanup() {
    rm -f "$LOCKFILE"
    exit "$1"
}

# True if this game is already running. Do NOT use pgrep -f "DF" — it matches many
# unrelated processes (e.g. any command line containing the substring "PDF").
df_game_running() {
    pgrep -f 'DF\.dll' >/dev/null 2>&1
}

# Check for .NET 8.0 SDK
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET 8.0 SDK..."
    if [ ! -f "Scripts/install-dotnet.sh" ]; then
        echo "ERROR: Install script not found!"
        cleanup 1
    fi
    bash "Scripts/install-dotnet.sh"
    if [ $? -ne 0 ]; then
        echo "ERROR: Failed to install .NET 8.0 SDK"
        cleanup 1
    fi
else
    DOTNET_VERSION=$(dotnet --version 2>&1 | grep -E "^8\.")
    if [ -z "$DOTNET_VERSION" ]; then
        echo "Installing .NET 8.0 SDK..."
        bash "Scripts/install-dotnet.sh"
        if [ $? -ne 0 ]; then
            echo "ERROR: Failed to install .NET 8.0 SDK"
            cleanup 1
        fi
    fi
fi

# Check if game is already running
if df_game_running; then
    echo "Game is already running!"
    cleanup 1
fi

# Build the game
echo "Building game..."
dotnet build Code/Code.csproj --configuration Debug > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed!"
    dotnet build Code/Code.csproj --configuration Debug
    cleanup 1
fi

# Launch the game using dotnet run (works on Mac without standalone executable)
echo "Launching game..."
cd "$SCRIPT_DIR/Code" || cleanup 1
nohup dotnet run --configuration Debug > /dev/null 2>&1 &
GAME_PID=$!
cd "$SCRIPT_DIR" || cleanup 1

# Poll for startup (Avalonia on Mac often needs more than 2s before DF.dll appears in ps)
STARTUP_WAIT_SECONDS=30
for _ in $(seq 1 "$STARTUP_WAIT_SECONDS"); do
    if df_game_running; then
        echo "Game launched successfully."
        cleanup 0
    fi
    if ! kill -0 "$GAME_PID" 2>/dev/null; then
        break
    fi
    sleep 1
done

# Background launcher still alive — do not start a second copy
if kill -0 "$GAME_PID" 2>/dev/null; then
    echo "Game is still starting (PID $GAME_PID)..."
    cleanup 0
fi

if df_game_running; then
    echo "Game launched successfully."
    cleanup 0
fi

echo "ERROR: Game failed to start!"
echo "Trying to run in foreground to see errors..."
cd "$SCRIPT_DIR/Code" || cleanup 1
dotnet run --configuration Debug
cd "$SCRIPT_DIR" || cleanup 1
cleanup 1

