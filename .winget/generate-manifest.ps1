#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generates winget manifest files for smtp4dev releases
.DESCRIPTION
    This script generates winget package manifest files for smtp4dev by computing
    SHA256 hashes for the Windows binaries from local artifact files.
.PARAMETER Version
    The version/tag of the release to generate manifests for
.PARAMETER OutputDir
    Directory to write the generated manifest files (default: .winget/generated)
.PARAMETER InstallerTemplateFile
    Path to the installer template manifest file (default: .winget/smtp4dev.installer.yaml)
.PARAMETER LocaleTemplateFile
    Path to the locale template manifest file (default: .winget/smtp4dev.locale.en-US.yaml)
.PARAMETER VersionTemplateFile
    Path to the version template manifest file (default: .winget/smtp4dev.version.yaml)
.PARAMETER X64ArtifactPath
    Path to the local x64 Windows artifact file (required)
.PARAMETER Arm64ArtifactPath
    Path to the local ARM64 Windows artifact file (required)
.PARAMETER IsReleaseBuild
    Whether this is a release build (boolean)
.PARAMETER IsCiBuild
    Whether this is a CI build (boolean)
.PARAMETER X64Url
    The complete URL for the x64 installer (GitHub release or Azure DevOps artifact URL)
.PARAMETER Arm64Url
    The complete URL for the ARM64 installer (GitHub release or Azure DevOps artifact URL)
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [string]$OutputDir = ".winget/generated",
    
    [Parameter(Mandatory = $false)]
    [string]$InstallerTemplateFile = ".winget/smtp4dev.installer.yaml",
    
    [Parameter(Mandatory = $false)]
    [string]$LocaleTemplateFile = ".winget/smtp4dev.locale.en-US.yaml",
    
    [Parameter(Mandatory = $false)]
    [string]$VersionTemplateFile = ".winget/smtp4dev.version.yaml",
    
    [Parameter(Mandatory = $true)]
    [string]$X64ArtifactPath,
    
    [Parameter(Mandatory = $true)]
    [string]$Arm64ArtifactPath,
    
    [Parameter(Mandatory = $true)]
    [string]$X64Url,
    
    [Parameter(Mandatory = $true)]
    [string]$Arm64Url
)

$ErrorActionPreference = "Stop"

Write-Host "Generating winget manifest for smtp4dev version: $Version" -ForegroundColor Green

# Ensure output directory exists
if (!(Test-Path $OutputDir)) {
    Write-Host "Creating output directory: $OutputDir"
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Validate required artifacts exist
if (!(Test-Path $X64ArtifactPath)) {
    Write-Error "x64 artifact file not found: $X64ArtifactPath"
    exit 1
}

if (!(Test-Path $Arm64ArtifactPath)) {
    Write-Error "ARM64 artifact file not found: $Arm64ArtifactPath"
    exit 1
}

Write-Host "Using artifacts:" -ForegroundColor Green
Write-Host "  x64 artifact: $X64ArtifactPath"
Write-Host "  arm64 artifact: $Arm64ArtifactPath"
Write-Host "Installer URLs:" -ForegroundColor Green
Write-Host "  x64 URL: $X64Url"
Write-Host "  arm64 URL: $Arm64Url"



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

# Compute SHA256 hashes from local artifacts
Write-Host "Computing SHA256 hashes from local artifacts..." -ForegroundColor Yellow
$x64Hash = Get-FileSha256 -FilePath $X64ArtifactPath
$arm64Hash = Get-FileSha256 -FilePath $Arm64ArtifactPath

Write-Host "SHA256 Hashes computed:" -ForegroundColor Green
Write-Host "  x64:   $x64Hash"
Write-Host "  arm64: $arm64Hash"

# Always use the provided URLs (GitHub release URLs regardless of build type)
$finalX64Url = $X64Url
$finalArm64Url = $Arm64Url

# Read templates
if (!(Test-Path $InstallerTemplateFile)) {
    Write-Error "Installer template file not found: $InstallerTemplateFile"
    exit 1
}

if (!(Test-Path $LocaleTemplateFile)) {
    Write-Error "Locale template file not found: $LocaleTemplateFile"
    exit 1
}

if (!(Test-Path $VersionTemplateFile)) {
    Write-Error "Version template file not found: $VersionTemplateFile"
    exit 1
}

$installerTemplate = Get-Content $InstallerTemplateFile -Raw
$localeTemplate = Get-Content $LocaleTemplateFile -Raw
$versionTemplate = Get-Content $VersionTemplateFile -Raw

# Replace placeholders in templates
$installerContent = $installerTemplate `
    -replace "PLACEHOLDER_VERSION", $Version `
    -replace "PLACEHOLDER_SHA256_X64", $x64Hash `
    -replace "PLACEHOLDER_SHA256_ARM64", $arm64Hash `
    -replace "PLACEHOLDER_X64_URL", $finalX64Url `
    -replace "PLACEHOLDER_ARM64_URL", $finalArm64Url

$localeContent = $localeTemplate `
    -replace "PLACEHOLDER_VERSION", $Version

$versionContent = $versionTemplate `
    -replace "PLACEHOLDER_VERSION", $Version

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Write manifest files
$installerFile = Join-Path $OutputDir "Rnwood.smtp4dev.installer.yaml"
$localeFile = Join-Path $OutputDir "Rnwood.smtp4dev.locale.en-US.yaml"
$versionFile = Join-Path $OutputDir "Rnwood.smtp4dev.yaml"

$installerContent | Set-Content $installerFile -Encoding UTF8
$localeContent | Set-Content $localeFile -Encoding UTF8
$versionContent | Set-Content $versionFile -Encoding UTF8

Write-Host "Generated winget manifests:" -ForegroundColor Green
Write-Host "  Installer: $installerFile"
Write-Host "  Locale: $localeFile"
Write-Host "  Version: $versionFile"

Write-Host "Winget manifest generation completed successfully!" -ForegroundColor Green