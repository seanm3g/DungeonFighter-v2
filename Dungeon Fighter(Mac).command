#!/bin/bash
# Double-click this file in Finder — macOS runs .command scripts in Terminal.
# (Double-clicking the .sh file usually opens it as plain text instead.)
DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$DIR" || exit 1
exec bash "./Dungeon Fighter(Mac).sh"
