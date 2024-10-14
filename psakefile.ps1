properties {
    $projectName = "WindowSwitcher"
    $projectPath = "src\\WindowSwitcher\$projectName.csproj"
    $buildConfiguration = "Release"
    $toolsPath = ".\tools"
    $templatePath = ".\template.nuspec"
    $nuspecPath = "$projectName.nuspec"
    $publishDir = "src\WindowSwitcher\bin\Release\net8.0-windows\win-x64\publish\"
}

task default -depends Package

task Clean {
    exec { dotnet clean $projectPath }
}

task Publish {
    exec { dotnet publish $projectPath -c $buildConfiguration /p:Version=$version /p:DebugType=None /p:DebugSymbols=false -o "$toolsPath\app" }
}

task UpdateNuspec -depends Publish {
    if (-not (Test-Path $templatePath)) {
        throw "Template file not found at $templatePath"
    }

    if (-not (Test-Path $toolsPath)) {
        New-Item -ItemType Directory -Path $toolsPath | Out-Null
        Write-Host "Created tools directory."
    }

    Copy-Item -Path $templatePath -Destination $nuspecPath -Force
    Write-Host "Copied template to $nuspecPath"

    $content = Get-Content -Path $nuspecPath -Raw
    $updatedContent = $content -replace '{{version}}', $version
    Set-Content -Path $nuspecPath -Value $updatedContent

    Write-Host "Updated version to $version in $nuspecPath"
}

task CreateZip -depends Publish {
    $zipPath = Join-Path $toolsPath "$projectName.zip"

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publishDir, $zipPath)

    Write-Host "Created zip archive at $zipPath"
}

task Package -depends UpdateNuspec {
    exec { choco pack $nuspecPath }
}

task TestInstall {
    exec { choco install $projectName -s . -y }
    # Add more tests here as needed
    exec { choco uninstall $projectName -y }
}