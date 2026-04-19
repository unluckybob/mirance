@echo off
setlocal enabledelayedexpansion

echo ============================================
echo MIRANCE Installer
echo ============================================
echo.

set "INSTALL_DIR=%ProgramFiles%\MIRANCE"
echo Installing to: %INSTALL_DIR%
echo.

if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
    if errorlevel 1 (
        echo ERROR: Cannot create installation directory.
        echo Please run as Administrator.
        pause
        exit /b 1
    )
)

echo Installing files...
xcopy /E /Y /Q "*.*" "%INSTALL_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy files.
    pause
    exit /b 1
)

echo Creating Start Menu shortcut...
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%ProgramData%\Microsoft\Windows\Start Menu\Programs\MIRANCE.lnk'); $s.TargetPath = '%INSTALL_DIR%\Mirance.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Description = 'iPhone USB Screen Mirroring'; $s.IconLocation = '%INSTALL_DIR%\Mirance.ico'; $s.Save()"

echo Creating Desktop shortcut...
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%USERPROFILE%\Desktop\MIRANCE.lnk'); $s.TargetPath = '%INSTALL_DIR%\Mirance.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Description = 'iPhone USB Screen Mirroring'; $s.IconLocation = '%INSTALL_DIR%\Mirance.ico'; $s.Save()"

echo.
echo ============================================
echo Installation Complete!
echo ============================================
echo.
echo You can launch MIRANCE from:
echo - Desktop shortcut
echo - Start Menu  
echo - Or run: %INSTALL_DIR%\Mirance.exe
echo.
echo Note: You need .NET 8 Runtime installed.
echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
echo.
pause