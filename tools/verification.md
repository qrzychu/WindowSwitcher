To verify if the package contains binaries built from the source [repository](https://github.com/qrzychu/WindowSwitche), go to [releases](https://github.com/qrzychu/WindowSwitcher/releases) and download `checksums.txt` for the version you want to verify.

Then run following script (it is the same as [createChecksums.ps1](../createChecksums.ps1)):

```powershell
$libFolderPath = Join-Path -Path $env:ChocolateyInstall -ChildPath "lib\Window-Switcher\tools\app"

$checksumsFile = ".\checksums.txt"

# Generate checksums for all files recursively and save them to checksums.txt
Get-ChildItem -Path $libFolderPath -Recurse -File | ForEach-Object {
    $file = $_.FullName
    $checksum = Get-FileHash -Path $file -Algorithm SHA256 | Select-Object -ExpandProperty Hash
    $line = "File: $file`nAlgorithm: SHA256`nChecksum: $checksum`n"
    Add-Content -Path $checksumsFile -Value $line
}
```

this will generate checksums for installed binaries, which can be compared with the file downloaded from Github release