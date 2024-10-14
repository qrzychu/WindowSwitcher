$process = Get-Process "WindowSwitcher" -ErrorAction SilentlyContinue
if ($process) {
    $process | Stop-Process -Force
    Start-Sleep -Seconds 1  # Wait for the process to fully terminate
}

# Remove from startup folder
$startupFolder = [environment]::GetFolderPath([environment+specialfolder]::Startup)
$startupPath = Join-Path $startupFolder "WindowSwitcher.lnk"
if (Test-Path $startupPath) { Remove-Item $startupPath }