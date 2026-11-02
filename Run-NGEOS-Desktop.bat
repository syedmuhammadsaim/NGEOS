@echo off
title NGEOS Desktop Launcher
setlocal EnableDelayedExpansion

echo ======================================================
echo           NGEOS - NGO Platform Desktop App
echo ======================================================
echo.

:: 1. Check if NGEOS is already running on localhost
powershell -NoProfile -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:5000' -UseBasicParsing -TimeoutSec 2; exit 0 } catch { exit 1 }" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [i] NGEOS service is already running on http://localhost:5000
    goto LaunchWindow
)

powershell -NoProfile -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:5035' -UseBasicParsing -TimeoutSec 2; exit 0 } catch { exit 1 }" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [i] NGEOS service is already running on http://localhost:5035
    set "APP_URL=http://localhost:5035"
    goto LaunchCustomWindow
)

:: 2. Check if running in publish folder with NGEOS.exe
if exist "%~dp0publish\NGEOS.exe" (
    echo [*] Starting NGEOS from published binaries...
    start "NGEOS Server" /min cmd /c "cd /d \"%~dp0publish\" && NGEOS.exe"
    goto WaitForServer
)

if exist "%~dp0NGEOS.exe" (
    echo [*] Starting NGEOS from executable...
    start "NGEOS Server" /min cmd /c "cd /d \"%~dp0\" && NGEOS.exe"
    goto WaitForServer
)

:: 3. Otherwise start via dotnet run
if exist "%~dp0NGEOS\NGEOS.csproj" (
    echo [*] Starting NGEOS via .NET Runtime...
    start "NGEOS Server" /min cmd /c "cd /d \"%~dp0NGEOS\" && dotnet run --launch-profile http"
    goto WaitForServer
)

:WaitForServer
echo [*] Waiting for NGEOS backend to initialize...
for /L %%i in (1,1,25) do (
    powershell -NoProfile -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:5000' -UseBasicParsing -TimeoutSec 1; exit 0 } catch { exit 1 }" >nul 2>&1
    if !ERRORLEVEL! EQU 0 (
        echo [OK] NGEOS is ready!
        goto LaunchWindow
    )
    timeout /t 1 /nobreak >nul
)

:LaunchWindow
set "APP_URL=http://localhost:5000"

:LaunchCustomWindow
echo [*] Launching NGEOS Desktop Window...

:: Try Edge in App Mode (Frameless standalone window)
where msedge >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    start msedge --app="%APP_URL%" --window-size=1366,850
    goto Done
)

:: Try Chrome in App Mode
where chrome >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    start chrome --app="%APP_URL%" --window-size=1366,850
    goto Done
)

:: Fallback to default browser
start "" "%APP_URL%"

:Done
echo.
echo ======================================================
echo    NGEOS Desktop Application launched successfully!
echo ======================================================
timeout /t 2 >nul
exit /b 0
