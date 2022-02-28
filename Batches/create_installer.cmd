@echo off
REM %1 full/standard (default: full)
REM %2 prod/dev (default: prod)
REM %3 app version (e.g. "v1.2.3")
REM example: `create_installer full prod v2.0.0`

set ISCC_EXE="%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
cd %~dp0

if %1 == standard (
    if %2 == dev (
        set ISS_FILE=inno\vmm_dev_standard.iss
    ) else (
        set ISS_FILE=inno\vmm_prod_standard.iss
    )
) else (
    if %2 == dev (
        set ISS_FILE=inno\vmm_dev_full.iss
    ) else (
        set ISS_FILE=inno\vmm_prod_full.iss
    )
)

%ISCC_EXE% /Qp "/DMyAppVersion=%~3" "/DReposFolder=%~dp0..\" %ISS_FILE% 
