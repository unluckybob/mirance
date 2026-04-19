@echo off
echo ============================================
echo MIRANCE Debug Launcher
echo ============================================
echo.

REM Set environment for debugging
set MIRANCE_DEBUG=1
set DOTNET_DbgEnableEngine=1

REM Create log file with timestamp
echo MIRANCE started at %date% %time% > mirance_debug.log

REM Run with error capture
Mirance.exe >> mirance_debug.log 2>&1
set errorlevel=%ERRORLEVEL%

echo.
echo ============================================
echo Process exited with code: %errorlevel%
echo Log saved to mirance_debug.log
echo ============================================
echo.

REM Show log if there was an error
if %errorlevel% neq 0 (
    echo --- ERROR LOG ---
    type mirance_debug.log
    echo --- END LOG ---
)

pause
