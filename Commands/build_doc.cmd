@echo off
setlocal
set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%..\docs_buddy"
docfx .\docfx.json
echo.
echo Build finished. Press ENTER to exit.
pause > nul 