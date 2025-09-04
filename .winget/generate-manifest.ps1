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
.PARAMETER TemplateFile
    Path to the template manifest file (default: .winget/smtp4dev.yaml)
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
    [string]$TemplateFile = ".winget/smtp4dev.yaml",
    
    [Parameter(Mandatory = $true)]
    [string]$X64ArtifactPath,
    
    [Parameter(Mandatory = $true)]
    [string]$Arm64ArtifactPath,
    
    [Parameter(Mandatory = $true)]
    [bool]$IsReleaseBuild,
    
    [Parameter(Mandatory = $true)]
    [bool]$IsCiBuild,
    
    [Parameter(Mandatory = $true)]
    [string]$X64Url,
    
    [Parameter(Mandatory = $true)]
    [string]$Arm64Url
)

$ErrorActionPreference = "Stop"

# Boolean parameters are already the correct type
$IsReleaseBuildBool = $IsReleaseBuild
$IsCiBuildBool = $IsCiBuild

Write-Host "Generating winget manifest for smtp4dev version: $Version" -ForegroundColor Green
Write-Host "Build type: Release=$IsReleaseBuildBool, CI=$IsCiBuildBool" -ForegroundColor Yellow

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

# Function to upload file to filebin.net and get download URL
function Upload-ToFilebin {
    param([string]$FilePath, [string]$FileName)
    
    Write-Host "  Uploading $FileName to filebin.net..."
    
    try {
        # Generate a random bin name
        $binName = [System.Guid]::NewGuid().ToString("N").Substring(0, 8)
        
        # Upload file to filebin.net using the correct API format /{bin}/{filename}
        $uri = "https://filebin.net/$binName/$FileName"
        $fileContent = [System.IO.File]::ReadAllBytes($FilePath)
        
        $response = Invoke-RestMethod -Uri $uri -Method Post -Body $fileContent -ContentType "application/octet-stream"
        
        if ($response) {
            $downloadUrl = "https://filebin.net/$binName/$FileName"
            Write-Host "    Uploaded successfully: $downloadUrl" -ForegroundColor Green
            return $downloadUrl
        } else {
            Write-Error "Failed to upload to filebin.net - no response received"
            throw
        }
    }
    catch {
        Write-Error "Failed to upload file $FilePath to filebin.net: $_"
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

# Compute SHA256 hashes from local artifacts
Write-Host "Computing SHA256 hashes from local artifacts..." -ForegroundColor Yellow
$x64Hash = Get-FileSha256 -FilePath $X64ArtifactPath
$arm64Hash = Get-FileSha256 -FilePath $Arm64ArtifactPath

Write-Host "SHA256 Hashes computed:" -ForegroundColor Green
Write-Host "  x64:   $x64Hash"
Write-Host "  arm64: $arm64Hash"

# For non-CI and non-release builds, upload to filebin.net to work around AzDO double zipping
$finalX64Url = $X64Url
$finalArm64Url = $Arm64Url

if (-not $IsReleaseBuildBool -and -not $IsCiBuildBool) {
    Write-Host "Non-CI and non-release build detected. Uploading to filebin.net to avoid AzDO double zipping..." -ForegroundColor Yellow
    
    try {
        # Extract filenames for filebin
        $x64FileName = Split-Path $X64ArtifactPath -Leaf
        $arm64FileName = Split-Path $Arm64ArtifactPath -Leaf
        
        # Upload files and get download URLs
        $finalX64Url = Upload-ToFilebin -FilePath $X64ArtifactPath -FileName $x64FileName
        $finalArm64Url = Upload-ToFilebin -FilePath $Arm64ArtifactPath -FileName $arm64FileName
        
        Write-Host "Updated URLs from filebin.net:" -ForegroundColor Green
        Write-Host "  x64 URL: $finalX64Url"
        Write-Host "  arm64 URL: $finalArm64Url"
    }
    catch {
        Write-Warning "Failed to upload to filebin.net. Falling back to original URLs: $_"
        Write-Host "Using original URLs:" -ForegroundColor Yellow
        Write-Host "  x64 URL: $finalX64Url"
        Write-Host "  arm64 URL: $finalArm64Url"
    }
}

# Read template
if (!(Test-Path $TemplateFile)) {
    Write-Error "Template file not found: $TemplateFile"
    exit 1
}

$templateContent = Get-Content $TemplateFile -Raw

# Replace placeholders in template
$manifestContent = $templateContent `
    -replace "PLACEHOLDER_VERSION", $Version `
    -replace "PLACEHOLDER_SHA256_X64", $x64Hash `
    -replace "PLACEHOLDER_SHA256_ARM64", $arm64Hash `
    -replace "PLACEHOLDER_X64_URL", $finalX64Url `
    -replace "PLACEHOLDER_ARM64_URL", $finalArm64Url

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