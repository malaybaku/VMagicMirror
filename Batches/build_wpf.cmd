@echo off
REM %1 full/standard (default: full)
REM %2 prod/dev (default: prod)
REM example: `build_wpf standard prod`

REM main build
cd %~dp0
cd ..\WPF\VMagicMirrorConfig

if %1 == standard (
    if %2 == dev (
        set BIN_FOLDER=Bin_Standard_Dev
        dotnet publish /p:Configuration=Release /p:PublishProfile=FolderProfile_Standard_Dev
    ) else (
        set BIN_FOLDER=Bin_Standard
        dotnet publish /p:Configuration=Release /p:PublishProfile=FolderProfile_Standard
    )
) else (
    if %2 == dev (
        set BIN_FOLDER=Bin_Dev
        dotnet publish /p:Configuration=Release /p:PublishProfile=FolderProfile_Full_Dev
    ) else (
        set BIN_FOLDER=Bin
        dotnet publish /p:Configuration=Release /p:PublishProfile=FolderProfile_Full
    )
)

rem remove unnecessary files
cd %~dp0
cd ..\%BIN_FOLDER%\ConfigApp

rmdir /s /q cs 
rmdir /s /q de
rmdir /s /q es
rmdir /s /q fr
rmdir /s /q it
rmdir /s /q ja
rmdir /s /q ko
rmdir /s /q pl
rmdir /s /q pt-BR
rmdir /s /q ru
rmdir /s /q tr
rmdir /s /q zh-Hans
rmdir /s /q zh-Hant

cd %~dp0

rem copy localization file directly, because it is ignored in publish operation
mkdir ..\%BIN_FOLDER%\ConfigApp\Localization > NUL 2>&1
copy ..\WPF\VMagicMirrorConfig\Localizations\Chinese_Simplified.xaml ..\%BIN_FOLDER%\ConfigApp\Localization\

REM prod build does not need debugger file
if %2 == prod (
    del /Q ..\%BIN_FOLDER%\ConfigApp\VMagicMirrorConfig.pdb > NUL 2>&1
)
