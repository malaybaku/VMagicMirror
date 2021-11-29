using namespace System
using namespace System.IO

# Create zip backup for build output.
# This script assumes output folder (e.g. Bin) is clean.
# AppEdition: Full or Standard
# AppVer    : Version name (e.g. v2.3.4)
# NOTE: this script is only for prod build, and does not support dev.

param($AppEdition, $AppVer)

Set-Location $PSScriptRoot

if ($AppEdition -eq "Full")
{
    $BinFolder = "Bin"
}
else
{
    $BinFolder = "Bin_Standard"
}

$FolderName = "VMM_" + $AppVer + "_" + $AppEdition

$ZipSrc = "..\" + $BinFolder + "\*"
$ZipDest = "..\Releases\zip\" + $FolderName + ".zip"

if (![Directory]::Exists("..\Releases\zip"))
{
    [Directory]::CreateDirectory("..\Releases\zip")
}

Compress-Archive -Path $ZipSrc -DestinationPath $ZipDest -Force
