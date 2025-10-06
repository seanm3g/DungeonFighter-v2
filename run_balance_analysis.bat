@echo off
echo Running Balance Analysis and saving to file...
echo.

REM Create timestamped filename
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"
set "timestamp=%HH%%Min%%Sec%"

REM Run the game and save output to timestamped file
echo Saving output to: balance_%timestamp%.txt
dotnet run > balance_%timestamp%.txt 2>&1

echo.
echo Balance analysis complete!
echo Output saved to: balance_%timestamp%.txt
echo.
pause
