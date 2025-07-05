#!/bin/bash

# Simple script to push updates to GitHub
# Usage: ./push-updates.sh "Your commit message"

echo "=== Dungeon Crawler RPG - Update Pusher ==="
echo ""

# Check if commit message was provided
if [ -z "$1" ]; then
    echo "Error: Please provide a commit message"
    echo "Usage: ./push-updates.sh \"Your commit message\""
    exit 1
fi

COMMIT_MESSAGE="$1"

echo "1. Checking current status..."
git status

echo ""
echo "2. Adding all changes..."
git add .

echo ""
echo "3. Committing changes with message: '$COMMIT_MESSAGE'"
git commit -m "$COMMIT_MESSAGE"

echo ""
echo "4. Pushing to GitHub..."
git push origin main

echo ""
echo "✅ Successfully pushed updates to GitHub!"
echo "🌐 Check your repository at: https://github.com/seanm3g/dungeonfighter-v2" 