#!/bin/bash

# Dungeon Fighter v2 - macOS Launcher

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR" || exit 1

# Lock file to prevent duplicate execution
LOCKFILE="/tmp/DF2_Launcher_${USER}.lock"
if [ -f "$LOCKFILE" ]; then
    rm -f "$LOCKFILE"
fi
echo "$(date)" > "$LOCKFILE"

# Cleanup function
cleanup() {
    rm -f "$LOCKFILE"
    exit "$1"
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
if pgrep -f "DF" > /dev/null; then
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

# Verify executable exists
GAME_EXE="$SCRIPT_DIR/Code/bin/Debug/net8.0/DF"
if [ ! -f "$GAME_EXE" ]; then
    echo "ERROR: Executable not found: $GAME_EXE"
    cleanup 1
fi

# Make executable if needed
chmod +x "$GAME_EXE" 2>/dev/null

# Launch the game in background
echo "Launching game..."
nohup "$GAME_EXE" > /dev/null 2>&1 &

# Wait a moment for game to start
sleep 1

# Check if game is running
if ! pgrep -f "DF" > /dev/null; then
    echo "ERROR: Game failed to start!"
    "$GAME_EXE"
    cleanup 1
fi

# Game launched successfully
cleanup 0

