using namespace System
using namespace System.IO

# NOTE: We have to see MyDocuments folder here
Set-Location $([Environment]::GetFolderPath("MyDocuments"))
Write-Output "Delete Dev Save Files..."
if ([Directory]::Exists("VMagicMirror_Dev_Files\Save"))
{
    [Directory]::Delete("VMagicMirror_Dev_Files\Save")
    Write-Output "Deleted."
}
else
{
    Write-Output "Dev save files does not exist."
}

Write-Output "Completed."

Set-Location $PSScriptRoot
