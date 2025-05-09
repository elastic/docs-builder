# PowerShell script to install docs-builder binary

# Use AppData\Local for user-specific installation instead of Program Files
$targetDir = Join-Path -Path $env:LOCALAPPDATA -ChildPath "docs-builder"
$targetPath = Join-Path -Path $targetDir -ChildPath "docs-builder.exe"

# Check if docs-builder already exists
if (Test-Path -Path $targetPath) {
    Write-Host "docs-builder is already installed."
    $choice = Read-Host -Prompt "Do you want to update/overwrite it? (y/n)"
    switch ($choice.ToLower()) {
        'y' { Write-Host "Updating docs-builder..." }
        'n' { Write-Host "Installation aborted."; exit 0 }
        Default { Write-Host "Invalid choice. Installation aborted."; exit 1 }
    }
}

# Create target directory if it doesn't exist
if (-not (Test-Path -Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

# Download the latest Windows binary from releases
$tempZipPath = "$env:TEMP\docs-builder-win-x64.zip"
Write-Host "Downloading docs-builder binary..."
Invoke-WebRequest -Uri "https://github.com/elastic/docs-builder/releases/latest/download/docs-builder-win-x64.zip" -OutFile $tempZipPath

# Create a temporary directory for extraction
$tempExtractPath = "$env:TEMP\docs-builder-extract"
if (Test-Path -Path $tempExtractPath) {
    Remove-Item -Path $tempExtractPath -Recurse -Force
}
New-Item -ItemType Directory -Path $tempExtractPath -Force | Out-Null

# Extract the binary
Write-Host "Extracting binary..."
Expand-Archive -Path $tempZipPath -DestinationPath $tempExtractPath -Force

# Copy the executable to the target location
Write-Host "Installing docs-builder..."
Copy-Item -Path "$tempExtractPath\docs-builder.exe" -Destination $targetPath -Force

# Add to PATH if not already in PATH (using User scope instead of Machine)
$currentPath = [Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::User)
if ($currentPath -notlike "*$targetDir*") {
    Write-Host "Adding docs-builder to the user PATH..."
    [Environment]::SetEnvironmentVariable(
        "PATH",
        "$currentPath;$targetDir",
        [EnvironmentVariableTarget]::User
    )
    $env:PATH = "$env:PATH;$targetDir"
}

# Clean up temporary files
Remove-Item -Path $tempZipPath -Force
Remove-Item -Path $tempExtractPath -Recurse -Force

Write-Host "docs-builder has been installed successfully and is available in your PATH."
Write-Host "You may need to restart your terminal or PowerShell session for the PATH changes to take effect."