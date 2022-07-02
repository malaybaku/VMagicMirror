@echo off
REM %1 full/standard (default: full)
REM %2 prod/dev (default: prod)
REM example: `build_unity.cmd full dev`

REM use following instead, if editor is NOT from Hub
REM set UNITY_EXE="%ProgramFiles%\Unity\Editor\Unity.exe"
set UNITY_EXE="%ProgramFiles%\Unity\Hub\Editor\2020.3.22f1\Editor\Unity.exe"

cd %~dp0

REM Check value, though it is not so meaningful
if %1 == standard (
    set APP_EDITION=Standard
) else (
    set APP_EDITION=Full
)

if %2 == dev (
    set APP_ENV=Dev
) else (
    set APP_ENV=Prod
)

if %1 == standard (
    if %2 == dev (
        set BIN_FOLDER=Bin_Standard_Dev
    ) else (
        set BIN_FOLDER=Bin_Standard
    )
) else (
    if %2 == dev (
        set BIN_FOLDER=Bin_Dev
    ) else (
        set BIN_FOLDER=Bin
    )
)

set PROJ_PATH=%~dp0\..\VMagicMirror
set SAVE_PATH=%~dp0\..\%BIN_FOLDER%

%UNITY_EXE% ^
-batchmode -quit ^
-projectPath %PROJ_PATH% ^
-SavePath=%SAVE_PATH% ^
-Edition=%APP_EDITION% ^
-Env=%APP_ENV% ^
-executeMethod Baku.VMagicMirror.BuildHelper.DoBuild

REM Following line works only if readme file is actually prepared. The file is not in git, so make your own if needed.
copy /Y ..\..\ResourceItems\readme.txt ..\%BIN_FOLDER%
