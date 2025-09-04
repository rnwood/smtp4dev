#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generates winget manifest files for smtp4dev releases
.DESCRIPTION
    This script generates winget package manifest files for smtp4dev by downloading
    release information from GitHub and computing SHA256 hashes for the Windows binaries.
.PARAMETER Version
    The version/tag of the release to generate manifests for
.PARAMETER OutputDir
    Directory to write the generated manifest files (default: .winget/generated)
.PARAMETER TemplateFile
    Path to the template manifest file (default: .winget/smtp4dev.yaml)
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [string]$OutputDir = ".winget/generated",
    
    [Parameter(Mandatory = $false)]
    [string]$TemplateFile = ".winget/smtp4dev.yaml"
)

$ErrorActionPreference = "Stop"

Write-Host "Generating winget manifest for smtp4dev version: $Version" -ForegroundColor Green

# Ensure output directory exists
if (!(Test-Path $OutputDir)) {
    Write-Host "Creating output directory: $OutputDir"
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# GitHub release URLs
$baseUrl = "https://github.com/rnwood/smtp4dev/releases/download"
$x64Url = "$baseUrl/$Version/Rnwood.Smtp4dev-win-x64-$Version.zip"
$arm64Url = "$baseUrl/$Version/Rnwood.Smtp4dev-win-arm64-$Version.zip"

Write-Host "Downloading and computing SHA256 hashes..." -ForegroundColor Yellow

# Function to get SHA256 hash from URL
function Get-UrlSha256 {
    param([string]$Url)
    
    Write-Host "  Downloading: $Url"
    
    try {
        $tempFile = [System.IO.Path]::GetTempFileName()
        
        # Download file
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($Url, $tempFile)
        
        # Compute hash
        $hash = Get-FileHash -Path $tempFile -Algorithm SHA256
        
        # Cleanup
        Remove-Item $tempFile -Force
        
        return $hash.Hash
    }
    catch {
        Write-Error "Failed to download or hash file from $Url : $_"
        throw
    }
}

# Get SHA256 hashes
$x64Hash = Get-UrlSha256 -Url $x64Url
$arm64Hash = Get-UrlSha256 -Url $arm64Url

Write-Host "SHA256 Hashes computed:" -ForegroundColor Green
Write-Host "  x64:   $x64Hash"
Write-Host "  arm64: $arm64Hash"

# Read template
if (!(Test-Path $TemplateFile)) {
    Write-Error "Template file not found: $TemplateFile"
    exit 1
}

$templateContent = Get-Content $TemplateFile -Raw

# Replace placeholders
$manifestContent = $templateContent `
    -replace "PLACEHOLDER_VERSION", $Version `
    -replace "PLACEHOLDER_SHA256_X64", $x64Hash `
    -replace "PLACEHOLDER_SHA256_ARM64", $arm64Hash

# Write manifest file
$outputFile = Join-Path $OutputDir "smtp4dev-$Version.yaml"
$manifestContent | Set-Content $outputFile -Encoding UTF8

Write-Host "Generated winget manifest: $outputFile" -ForegroundColor Green

# Create directory structure for winget-pkgs submission
$wingetPkgsDir = Join-Path $OutputDir "winget-pkgs-submission"
$packageDir = Join-Path $wingetPkgsDir "manifests/r/Rnwood/smtp4dev/$Version"

if (!(Test-Path $packageDir)) {
    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
}

# Copy manifest to winget-pkgs structure
Copy-Item $outputFile (Join-Path $packageDir "Rnwood.smtp4dev.yaml")

Write-Host "Created winget-pkgs submission structure at: $wingetPkgsDir" -ForegroundColor Green
Write-Host "To submit to winget-pkgs repository:" -ForegroundColor Cyan
Write-Host "  1. Fork https://github.com/microsoft/winget-pkgs"
Write-Host "  2. Copy the contents of $packageDir to manifests/r/Rnwood/smtp4dev/$Version"
Write-Host "  3. Create a pull request"

Write-Host "Winget manifest generation completed successfully!" -ForegroundColor Green