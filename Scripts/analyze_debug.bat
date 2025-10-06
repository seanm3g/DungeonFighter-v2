@echo off
echo Analyzing latest debug output...
echo.

REM Find the most recent debug analysis file
for /f "delims=" %%i in ('dir /b /o-d "Code\DebugAnalysis\debug_analysis_*.txt" 2^>nul') do (
    echo Latest debug file: %%i
    echo.
    echo ===== DEBUG ANALYSIS =====
    type "Code\DebugAnalysis\%%i"
    goto :done
)

echo No debug analysis files found.
echo Run the game and select option 5 to generate debug output.

:done
pause
