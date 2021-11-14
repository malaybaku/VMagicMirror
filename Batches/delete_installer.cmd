@echo off
REM %1 full/standard (default: full)
REM %2 prod/dev (default: prod)
REM %3 app version (e.g. "v1.2.3")
REM example: `delete_installer.cmd prod full v1.0.0`

cd %~dp0

if %1 == standard (
    if %2 == dev (
        set INSTALLER_EXE=..\Installers\Dev\VMM_Dev_%3_Standard_Installer.exe
    ) else (
        set INSTALLER_EXE=..\Installers\Prod\VMM_%3_Standard_Installer.exe
    )
) else (
    if %2 == dev (
        set INSTALLER_EXE=..\Installers\Dev\VMM_Dev_%3_Full_Installer.exe
    ) else (
        set INSTALLER_EXE=..\Installers\Prod\VMM_%3_Full_Installer.exe
    )
)

del /Q %INSTALLER_EXE% > NUL 2>&1
echo Deleted, path=%INSTALLER_EXE%
