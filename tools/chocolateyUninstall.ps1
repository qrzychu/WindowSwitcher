# Remove from startup folder
$startupFolder = [environment]::GetFolderPath([environment+specialfolder]::Startup)
$startupPath = Join-Path $startupFolder "WindowSwitcher.lnk"
if (Test-Path $startupPath) { Remove-Item $startupPath }

# Remove logs
$logPath = Join-Path $env:LOCALAPPDATA "WindowSwitcher"
if (Test-Path $logPath) { Remove-Item $logPath -Recurse }