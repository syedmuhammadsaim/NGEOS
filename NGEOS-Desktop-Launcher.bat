@echo off
title NGEOS Full Desktop Application
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "PORT=5555"

:: 1. Check if port 5555 is already active
netstat -ano | findstr "LISTENING" | findstr ":%PORT%" >nul
if %errorlevel% equ 0 (
    goto launch_app
)

:: 2. Start NGEOS backend server on dedicated desktop port 5555
powershell -Command "Start-Process -FilePath 'dotnet' -ArgumentList 'run --no-build --urls http://localhost:%PORT%' -WorkingDirectory 'C:\Users\Saim\Desktop\NGEOS\NGEOS' -WindowStyle Hidden"

:: 3. Wait for the server to become responsive
:wait_loop
timeout /t 2 /nobreak >nul
powershell -Command "try { $r = (Invoke-WebRequest -Uri 'http://localhost:%PORT%' -UseBasicParsing -TimeoutSec 2).StatusCode; if ($r -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"
if %errorlevel% neq 0 (
    goto wait_loop
)

:launch_app
:: 4. Find Microsoft Edge
set "EDGE_EXE=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
if not exist "!EDGE_EXE!" (
    set "EDGE_EXE=C:\Program Files\Microsoft\Edge\Application\msedge.exe"
)

:: 5. Launch in Native Desktop Application Mode
if exist "!EDGE_EXE!" (
    start "" "!EDGE_EXE!" --app="http://localhost:%PORT%" --window-size=1400,900 --user-data-dir="%LOCALAPPDATA%\NGEOS_DesktopApp"
) else (
    start "" http://localhost:%PORT%
)

exit /b 0
