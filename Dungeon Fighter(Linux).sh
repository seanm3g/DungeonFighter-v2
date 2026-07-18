#!/usr/bin/env bash
# Dungeon Fighter v2 — Linux launcher (repo root)
# Usage:
#   ./Dungeon\ Fighter\(Linux\).sh          # build + run (foreground)
#   ./Dungeon\ Fighter\(Linux\).sh --bg     # build + run in background
#   ./Dungeon\ Fighter\(Linux\).sh --help

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR" || exit 1

RUN_BACKGROUND=0
for arg in "$@"; do
    case "$arg" in
        --bg|-b) RUN_BACKGROUND=1 ;;
        --help|-h)
            echo "Dungeon Fighter v2 — Linux launcher"
            echo ""
            echo "Usage: $0 [--bg]"
            echo "  (default)  Build Debug, then run in the foreground"
            echo "  --bg, -b   Build Debug, then run in the background"
            echo ""
            echo "Requires: .NET 8 SDK, a graphical session (X11 or Wayland)"
            exit 0
            ;;
        *)
            echo "Unknown option: $arg (try --help)"
            exit 1
            ;;
    esac
done

LOCKFILE="/tmp/DF2_Launcher_${USER}.lock"
if [[ -f "$LOCKFILE" ]]; then
    OLD_PID="$(cat "$LOCKFILE" 2>/dev/null || true)"
    if [[ -n "${OLD_PID:-}" ]] && kill -0 "$OLD_PID" 2>/dev/null; then
        echo "Launcher is already running (PID $OLD_PID)."
        exit 1
    fi
    rm -f "$LOCKFILE"
fi
echo "$$" > "$LOCKFILE"

release_lock() {
    rm -f "$LOCKFILE"
}
trap release_lock EXIT

df_game_running() {
    # Match DF.dll only — do not use a bare "DF" pattern (matches unrelated processes).
    pgrep -f 'DF\.dll' >/dev/null 2>&1
}

ensure_dotnet8() {
    export PATH="${HOME}/.dotnet:${PATH}"

    if ! command -v dotnet >/dev/null 2>&1; then
        echo "Installing .NET 8.0 SDK..."
        if [[ ! -f "Scripts/install-dotnet.sh" ]]; then
            echo "ERROR: Scripts/install-dotnet.sh not found."
            exit 1
        fi
        bash "Scripts/install-dotnet.sh"
        export PATH="${HOME}/.dotnet:${PATH}"
    fi

    # Prefer list-sdks so a default 9.x host does not hide an installed 8.x SDK.
    if ! dotnet --list-sdks 2>/dev/null | grep -qE '^8\.'; then
        echo ".NET 8.x SDK not found — installing..."
        bash "Scripts/install-dotnet.sh"
        export PATH="${HOME}/.dotnet:${PATH}"
    fi

    if ! command -v dotnet >/dev/null 2>&1; then
        echo "ERROR: dotnet is still not on PATH after install."
        echo "Try: export PATH=\"\$HOME/.dotnet:\$PATH\""
        exit 1
    fi

    echo "Using: $(command -v dotnet) ($(dotnet --version 2>/dev/null || echo unknown))"
}

echo ""
echo "========================================"
echo "   Dungeon Fighter v2 - Linux Launcher"
echo "========================================"
echo ""
echo "Repo: $SCRIPT_DIR"
echo ""

if [[ -z "${DISPLAY:-}" && -z "${WAYLAND_DISPLAY:-}" ]]; then
    echo "WARNING: No DISPLAY or WAYLAND_DISPLAY — the Avalonia window may not open."
    echo "Run this from a desktop session (or enable X11 forwarding if remote)."
    echo ""
fi

ensure_dotnet8

if df_game_running; then
    echo "Game is already running!"
    exit 1
fi

echo "Building game..."
dotnet build Code/Code.csproj --configuration Debug

echo ""
echo "Launching game..."
cd "$SCRIPT_DIR/Code"

if [[ "$RUN_BACKGROUND" -eq 1 ]]; then
    nohup dotnet run --configuration Debug --no-build >/tmp/DF2_linux_launch.log 2>&1 &
    GAME_PID=$!
    cd "$SCRIPT_DIR"

    STARTUP_WAIT_SECONDS=30
    for _ in $(seq 1 "$STARTUP_WAIT_SECONDS"); do
        if df_game_running; then
            echo "Game launched successfully (background). Log: /tmp/DF2_linux_launch.log"
            exit 0
        fi
        if ! kill -0 "$GAME_PID" 2>/dev/null; then
            break
        fi
        sleep 1
    done

    if kill -0 "$GAME_PID" 2>/dev/null; then
        echo "Game is still starting (PID $GAME_PID)..."
        exit 0
    fi

    echo "ERROR: Game failed to start. Log:"
    cat /tmp/DF2_linux_launch.log 2>/dev/null || true
    exit 1
fi

# Foreground (default): keep this terminal attached so build/runtime errors are visible.
release_lock
trap - EXIT
exec dotnet run --configuration Debug --no-build
