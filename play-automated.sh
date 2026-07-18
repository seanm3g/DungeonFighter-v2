#!/usr/bin/env bash
# Simple script to play DungeonFighter v2 with automated commands
# Usage: ./play-automated.sh < commands.txt

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/Code"
exec dotnet run -- PLAY
