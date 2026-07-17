@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0FixPolyworksMaterialLocation.ps1"
if errorlevel 1 (
  echo.
  echo The update did not complete. Read the message above.
  pause
  exit /b 1
)
echo.
pause
