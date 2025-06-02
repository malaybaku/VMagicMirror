@echo off
setlocal
set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%..\docs_buddy\_site"
start http://localhost:8000
python -m http.server 