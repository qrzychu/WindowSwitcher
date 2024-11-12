Write-Host "Stopping WindowSwitcher"

$process = Get-Process "WindowSwitcher" -ErrorAction SilentlyContinue
if ($process) {
    $process | Stop-Process -Force
    Start-Sleep -Seconds 1  # Wait for the process to fully terminate
}

