#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generates winget manifest files for smtp4dev releases
.DESCRIPTION
    This script generates winget package manifest files for smtp4dev by downloading
    release information from GitHub and computing SHA256 hashes for the Windows binaries.
    Can also use local artifact files from Azure Pipeline builds.
.PARAMETER Version
    The version/tag of the release to generate manifests for
.PARAMETER OutputDir
    Directory to write the generated manifest files (default: .winget/generated)
.PARAMETER TemplateFile
    Path to the template manifest file (default: .winget/smtp4dev.yaml)
.PARAMETER X64ArtifactPath
    Path to the local x64 Windows artifact file (if available from pipeline)
.PARAMETER Arm64ArtifactPath
    Path to the local ARM64 Windows artifact file (if available from pipeline)
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [string]$OutputDir = ".winget/generated",
    
    [Parameter(Mandatory = $false)]
    [string]$TemplateFile = ".winget/smtp4dev.yaml",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipHashValidation,
    
    [Parameter(Mandatory = $false)]
    [string]$X64ArtifactPath,
    
    [Parameter(Mandatory = $false)]
    [string]$Arm64ArtifactPath
)

$ErrorActionPreference = "Stop"

Write-Host "Generating winget manifest for smtp4dev version: $Version" -ForegroundColor Green

# Ensure output directory exists
if (!(Test-Path $OutputDir)) {
    Write-Host "Creating output directory: $OutputDir"
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# GitHub release URLs (used when local artifacts are not available)
$baseUrl = "https://github.com/rnwood/smtp4dev/releases/download"
$x64Url = "$baseUrl/$Version/Rnwood.Smtp4dev-win-x64-$Version.zip"
$arm64Url = "$baseUrl/$Version/Rnwood.Smtp4dev-win-arm64-$Version.zip"

# Check if local artifacts are provided
$useLocalArtifacts = ($X64ArtifactPath -and (Test-Path $X64ArtifactPath)) -and ($Arm64ArtifactPath -and (Test-Path $Arm64ArtifactPath))

if ($useLocalArtifacts) {
    Write-Host "Using local artifacts from pipeline build" -ForegroundColor Green
    Write-Host "  x64 artifact: $X64ArtifactPath"
    Write-Host "  arm64 artifact: $Arm64ArtifactPath"
} else {
    # Detect if this is a CI build (contains -ci in version)
    $isCiBuild = $Version -match "-ci"
    if ($isCiBuild) {
        Write-Host "Detected CI build version: $Version" -ForegroundColor Yellow
        Write-Host "No local artifacts provided - skipping hash computation for CI builds (binaries not yet published)" -ForegroundColor Yellow
    } else {
        Write-Host "Detected release build version: $Version" -ForegroundColor Yellow
        Write-Host "No local artifacts provided - downloading and computing SHA256 hashes..." -ForegroundColor Yellow
    }
}

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

# Function to get SHA256 hash from local file
function Get-FileSha256 {
    param([string]$FilePath)
    
    Write-Host "  Computing hash for: $FilePath"
    
    try {
        if (!(Test-Path $FilePath)) {
            Write-Error "File not found: $FilePath"
            throw
        }
        
        # Compute hash
        $hash = Get-FileHash -Path $FilePath -Algorithm SHA256
        
        return $hash.Hash
    }
    catch {
        Write-Error "Failed to compute hash for file $FilePath : $_"
        throw
    }
}

# Get SHA256 hashes
if ($useLocalArtifacts) {
    Write-Host "Computing SHA256 hashes from local artifacts..." -ForegroundColor Yellow
    $x64Hash = Get-FileSha256 -FilePath $X64ArtifactPath
    $arm64Hash = Get-FileSha256 -FilePath $Arm64ArtifactPath
    
    Write-Host "SHA256 Hashes computed from local artifacts:" -ForegroundColor Green
    Write-Host "  x64:   $x64Hash"
    Write-Host "  arm64: $arm64Hash"
} elseif ($isCiBuild -or $SkipHashValidation) {
    if ($isCiBuild) {
        Write-Host "Using placeholder hashes for CI build" -ForegroundColor Yellow
    } else {
        Write-Host "Skipping hash validation as requested" -ForegroundColor Yellow
    }
    $x64Hash = "PLACEHOLDER_SHA256_X64_CI_BUILD"
    $arm64Hash = "PLACEHOLDER_SHA256_ARM64_CI_BUILD"
} else {
    $x64Hash = Get-UrlSha256 -Url $x64Url
    $arm64Hash = Get-UrlSha256 -Url $arm64Url
    
    Write-Host "SHA256 Hashes computed from GitHub release:" -ForegroundColor Green
    Write-Host "  x64:   $x64Hash"
    Write-Host "  arm64: $arm64Hash"
}

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