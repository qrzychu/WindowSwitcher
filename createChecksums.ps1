param (
    [Parameter(Mandatory = $true)]
    [ValidateScript({Test-Path $_ -PathType 'Container'})]
    [string]$Path
)

# Get the absolute path to the checksums file
$checksumsFile = ".\checksums.txt"

# Generate checksums for all files recursively and save them to checksums.txt
Get-ChildItem -Path $Path -Recurse -File | ForEach-Object {
    $file = $_.FullName
    $checksum = Get-FileHash -Path $file -Algorithm SHA256 | Select-Object -ExpandProperty Hash
    $line = "File: $file`nAlgorithm: SHA256`nChecksum: $checksum`n"
    Add-Content -Path $checksumsFile -Value $line
}