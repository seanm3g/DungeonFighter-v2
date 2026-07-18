#!/usr/bin/env bash
# Run the game with the PLAY CLI mode (repo-relative; works on Linux/macOS/Git Bash).
# Usage: ./play.sh

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/Code"
exec dotnet run -- PLAY
