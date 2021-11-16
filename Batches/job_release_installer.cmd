@echo off
REM This script makes two installer, prod standard edition / prod full edition
REM `version.txt` must have appropriate version name before calling this script.

cd %~dp0
echo "Check App Version..."
REM version.txt file is separated, to make this script independent from version
for /f %%a in (version.txt) do (
    set APP_VER=%%a
)
echo "App Ver = %APP_VER%"

echo %time%
echo "0/8. Delete Existing Installer, %APP_VER%"
call delete_installer.cmd standard prod %APP_VER%
call delete_installer.cmd full prod %APP_VER%


echo %time%
echo "1/8. Build Full Edition WPF"
call build_wpf.cmd full prod

echo %time%
echo "2/8. Build Standard Edition WPF"
call build_wpf.cmd standard prod

echo %time%
echo "3/8. Build Full Edition Unity"
call build_unity.cmd full prod

echo %time%
echo "Sleep 10sec, to ensure Unity process ended..."
timeout /t 10 > nul

echo %time%
echo "4/8. Build Standard Edition Unity"
call build_unity.cmd standard prod

echo %time%
echo "Sleep 10sec, to ensure Unity process ended..."
timeout /t 10 > nul

echo %time%
echo "5/8. Build Full Edition Installer"
call create_installer.cmd full prod %APP_VER%

echo %time%
echo "6/8. Build Standard Edition Installer"
call create_installer.cmd standard prod %APP_VER%

echo %time%
echo "7/8. Zip Full Edition"
powershell -NoProfile -ExecutionPolicy Unrestricted ".\zip_backup.ps1 Full %APP_VER%"

echo %time%
echo "8/8. Zip Standard Edition"
powershell -NoProfile -ExecutionPolicy Unrestricted ".\zip_backup.ps1 Standard %APP_VER%"

echo %time%
echo "Completed."
pause

