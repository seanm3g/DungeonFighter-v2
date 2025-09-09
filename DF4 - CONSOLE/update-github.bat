@echo off
echo ========================================
echo    Dungeon Fighter - GitHub Update
echo ========================================
echo.

echo [1/6] Checking current branch...
git branch --no-pager | findstr "*"
echo.

echo [2/6] Adding all changes...
git add .
if %errorlevel% neq 0 (
    echo ERROR: Failed to add files
    pause
    exit /b 1
)
echo Files staged successfully.
echo.

echo [3/6] Checking what will be committed...
git status --porcelain
echo.

echo [4/6] Committing changes...
git commit -m "Update: Latest changes from local development

- Enhanced combat system with new features
- Updated game mechanics and balance
- Improved testing and documentation
- Latest bug fixes and improvements"
if %errorlevel% neq 0 (
    echo ERROR: Failed to commit changes
    pause
    exit /b 1
)
echo Changes committed successfully.
echo.

echo [5/6] Pushing to GitHub...
git push origin v4
if %errorlevel% neq 0 (
    echo ERROR: Failed to push to GitHub
    pause
    exit /b 1
)
echo Pushed to v4 branch successfully.
echo.

echo [6/6] Merging to main branch...
git checkout main
if %errorlevel% neq 0 (
    echo ERROR: Failed to switch to main branch
    pause
    exit /b 1
)

git merge v4
if %errorlevel% neq 0 (
    echo ERROR: Failed to merge v4 to main
    pause
    exit /b 1
)

git push origin main
if %errorlevel% neq 0 (
    echo ERROR: Failed to push main branch
    pause
    exit /b 1
)
echo Merged to main branch and pushed successfully.
echo.

echo ========================================
echo    SUCCESS! Repository Updated
echo ========================================
echo.
echo Your changes are now live on GitHub:
echo - v4 branch: https://github.com/seanm3g/DungeonFighter-v2/tree/v4
echo - main branch: https://github.com/seanm3g/DungeonFighter-v2
echo.
echo Switching back to v4 branch for continued development...
git checkout v4
echo.
echo Done! Press any key to exit.
pause >nul
