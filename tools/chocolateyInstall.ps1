$toolsDir = "$( Split-Path -parent $MyInvocation.MyCommand.Definition )"
$installDir = Join-Path $toolsDir "app"
$exePath = Join-Path $installDir "WindowSwitcher.exe"

Write-Host "Installing WindowsSwitcher to $exepath"

# Function to create a shortcut
function Create-Shortcut
{
    param (
        [string]$ShortcutPath,
        [string]$TargetPath,
        [string]$Description
    )

    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $TargetPath
    $Shortcut.Description = $Description
    $Shortcut.WorkingDirectory = $installDir
    $Shortcut.Save()
}

# Startup shortcut
$startup = [environment]::GetFolderPath([environment+specialfolder]::Startup)
$startupShortcut = Join-Path $startup "WindowSwitcher.lnk"
Create-Shortcut -ShortcutPath $startupShortcut `
                -TargetPath $exePath `
                -Description "Auto-start WindowSwitcher"

Write-Host "WindowsSwitcher added to auto-start"

Write-Host "Starting WindowSwitcher in the background."
Start-Process -FilePath $exePath -WindowStyle Hidden -WorkingDirectory $installDir


