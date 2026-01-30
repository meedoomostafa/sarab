<#
.SYNOPSIS
Uninstalls Sarab CLI on Windows.

.DESCRIPTION
This script removes the Sarab binary and the data directory.
#>

$ErrorActionPreference = "Stop"

# Configuration
$BinaryPath = Join-Path $env:USERPROFILE ".local\bin\sarab.exe"
$DataDir = Join-Path $env:USERPROFILE ".sarab"

function Write-Color {
    param([string]$Message, [ConsoleColor]$Color)
    Write-Host $Message -ForegroundColor $Color
}

Write-Color "Uninstalling Sarab..." Cyan

# Remove Binary
if (Test-Path $BinaryPath) {
    Write-Color "Removing binary at $BinaryPath..." Cyan
    Remove-Item -Path $BinaryPath -Force
    Write-Color "Binary removed." Green
} else {
    Write-Color "Binary not found at $BinaryPath." Yellow
}

Write-Color "Uninstallation of binary complete." Green

# Remove Data Directory
if (Test-Path $DataDir) {
    Write-Color "Removing Sarab data directory at $DataDir..." Cyan
    Remove-Item -Path $DataDir -Recurse -Force
    Write-Color "Data directory removed." Green
}

Write-Color "Sarab has been completely uninstalled." Green
