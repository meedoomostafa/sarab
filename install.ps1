<#
.SYNOPSIS
Installs Sarab CLI on Windows.

.DESCRIPTION
This script attempts to download the latest release of Sarab.
If a release is not found, it falls back to building from source (requires .NET SDK).
It also adds the installation directory to the user's PATH.
#>

$ErrorActionPreference = "Stop"

# Configuration
$RepoUrl = "https://github.com/meedoomostafa/sarab.git"
$InstallDir = Join-Path $PWD "Sarab"
$DestDir = Join-Path $env:USERPROFILE ".local\bin"
$BinaryName = "Sarab.Cli.exe"
$AssetPattern = "sarab-win-x64.zip"
$DownloadUrl = "https://github.com/meedoomostafa/sarab/releases/latest/download/$AssetPattern"

# Ensure destination directory exists
if (-not (Test-Path $DestDir)) {
    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
}

function Write-Color {
    param([string]$Message, [ConsoleColor]$Color)
    Write-Host $Message -ForegroundColor $Color
}

function Setup-Path {
    $UserPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($UserPath -split ";" -notcontains $DestDir) {
        Write-Color "Adding $DestDir to User PATH..." Cyan
        [Environment]::SetEnvironmentVariable("Path", "$UserPath;$DestDir", "User")
        $env:Path += ";$DestDir"
        Write-Color "✔ Added to PATH." Green
    } else {
        Write-Color "Path already configured." Green
    }
}

function Try-DownloadRelease {
    Write-Color "Attempting to download latest release..." Cyan

    $TempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
    New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

    $ProgressPreference = 'SilentlyContinue'
    try {
        $ZipPath = Join-Path $TempDir $AssetPattern
        Write-Color "Downloading $AssetPattern..." Cyan
        Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath

        Write-Color "Extracting..." Cyan
        Expand-Archive -Path $ZipPath -DestinationPath $TempDir -Force

        # Find the binary
        $SourceBin = Get-ChildItem -Path $TempDir -Recurse -Include "sarab.exe", "Sarab.Cli.exe" | Select-Object -First 1

        if ($null -eq $SourceBin) {
            Write-Color "Binary not found in archive." Yellow
            return $false
        }

        Write-Color "Installing to $DestDir..." Cyan
        $TargetBin = Join-Path $DestDir "sarab.exe" 
        Copy-Item -Path $SourceBin.FullName -Destination $TargetBin -Force

        return $true
    }
    catch {
        Write-Color "Release not found or download failed: $_" Yellow
        return $false
    }
    finally {
        if (Test-Path $TempDir) {
            Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

# -------------------- Main Logic --------------------

function Check-UpToDate {
    if (-not (Get-Command "sarab" -ErrorAction SilentlyContinue)) {
        return $false # Not installed
    }

    Write-Color "Checking for updates..." Cyan
    
    try {
        $LatestRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/meedoomostafa/sarab/releases/latest" -ErrorAction Stop
        $LatestTag = $LatestRelease.tag_name
        $ReleaseId = $LatestRelease.id
        
        if ([string]::IsNullOrWhiteSpace($LatestTag) -or [string]::IsNullOrWhiteSpace($ReleaseId)) {
            return $false
        }

        # Strip 'v' prefix
        $CleanTag = $LatestTag -replace '^v',''
        
        # Get local version
        $LocalVersion = sarab --version
        
        # Check Local Release ID
        $IdFile = Join-Path $env:USERPROFILE ".sarab\release.id"
        $LocalId = ""
        if (Test-Path $IdFile) {
            $LocalId = Get-Content $IdFile
        }
        
        if ("$ReleaseId" -eq "$LocalId" -and $LocalVersion -eq $CleanTag) {
            Write-Color "Sarab is already up to date ($LocalVersion)." Green
            
            $Response = Read-Host "Do you want to reinstall? [y/N]"
            if ($Response -match "^[yY]$") {
                 Write-Color "Reinstalling..." Cyan
                 return $false # Proceed to install
            }

            return $true # Stop
        }
        
        if ("$ReleaseId" -ne "$LocalId" -and $LocalVersion -eq $CleanTag) {
             Write-Color "New build detected for version $CleanTag (Release ID: $ReleaseId). Updating..." Cyan
             return $false
        }
        
        Write-Color "New version available: $CleanTag (Current: $LocalVersion)" Cyan
        return $false
    }
    catch {
        # Network error or API limit, proceed with install/build
        return $false
    }
}

function Save-ReleaseId {
    try {
        $LatestRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/meedoomostafa/sarab/releases/latest" -ErrorAction Stop
        $ReleaseId = $LatestRelease.id
        $IdDir = Join-Path $env:USERPROFILE ".sarab"
        if (-not (Test-Path $IdDir)) { New-Item -ItemType Directory -Path $IdDir -Force | Out-Null }
        Set-Content -Path (Join-Path $IdDir "release.id") -Value $ReleaseId -Force
    } catch {}
}

# Check if up to date implies we should skip everything
if (Check-UpToDate) {
    exit 0
}

# Try to download release
if (Try-DownloadRelease) {
    Setup-Path

    Write-Color "Initializing database..." Cyan
    & (Join-Path $DestDir "sarab.exe") init

    Write-Color "Installation Complete (from Release)." Green
    Save-ReleaseId
    Write-Color "Run 'sarab --version' to verify."
    exit 0
}

# Fallback: Build from Source
Write-Color "Falling back to building from source..." Yellow

# Check for .NET SDK
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Color "Error: .NET SDK not found." Red
    Write-Color "Please install .NET 10 SDK or wait for a release to be available."
    exit 1
}

# Prepare Source
if (Test-Path "Sarab.sln") {
    Write-Color "Running inside repository." Cyan
} else {
    if (Test-Path $InstallDir) {
        Write-Color "Updating existing repository in $InstallDir..." Cyan
        Push-Location $InstallDir
        git pull
        Pop-Location
    } else {
        Write-Color "Cloning Sarab from $RepoUrl..." Cyan
        git clone $RepoUrl $InstallDir
    }
    
    # Check if we need to enter the directory (if we just cloned or updated)
    if ($PWD.Path -ne $InstallDir) {
       Push-Location $InstallDir
    }
}

# We might be in the root of the repo (where Sarab.sln is) or outside.
# If we were outside, we pushed location to $InstallDir.

Write-Color "Building Sarab for win-x64..." Cyan
$PublishArgs = @(
    "publish", 
    "Sarab.Cli/Sarab.Cli.csproj", 
    "-c", "Release", 
    "-r", "win-x64", 
    "--self-contained", "true", 
    "-p:PublishSingleFile=true", 
    "-o", "./dist"
)
dotnet @PublishArgs

$BuildBin = Join-Path "dist" $BinaryName
if (-not (Test-Path $BuildBin)) {
     Write-Color "Build failed. Binary not found at $BuildBin" Red
     exit 1
}

Write-Color "Installing to $DestDir..." Cyan
$TargetBin = Join-Path $DestDir "sarab.exe"
Copy-Item -Path $BuildBin -Destination $TargetBin -Force

Setup-Path

Write-Color "Initializing database..." Cyan
& $TargetBin init

Write-Color "Installation Complete (from Source)." Green
Write-Color "Run 'sarab --version' to verify."
