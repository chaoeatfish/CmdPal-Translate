@echo off
setlocal
set "SCRIPT=%~dp0FinishInstall.ps1"
set "RESULT=%~dp0install-result.txt"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
findstr /C:"INSTALL OK" "%RESULT%" >nul 2>&1
if %errorlevel%==0 goto done
findstr /C:"ALREADY INSTALLED" "%RESULT%" >nul 2>&1
if %errorlevel%==0 goto done
net session >nul 2>&1
if %errorlevel%==0 goto done
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
exit /b
:done
echo.
echo Install finished. See install-result.txt in this folder.
pause
