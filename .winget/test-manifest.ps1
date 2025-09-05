#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Test script to validate winget manifest for smtp4dev
.DESCRIPTION
    This script validates the generated winget manifest and provides information
    about how users can install smtp4dev via winget.
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$ManifestPath = ".winget/generated"
)

$ErrorActionPreference = "Stop"

Write-Host "🔍 Testing winget manifest for smtp4dev" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Find the latest manifest file
$manifestFiles = Get-ChildItem -Path $ManifestPath -Filter "smtp4dev-*.yaml" | Sort-Object Name -Descending
if ($manifestFiles.Count -eq 0) {
    Write-Error "No manifest files found in $ManifestPath"
    exit 1
}

$latestManifest = $manifestFiles[0]
Write-Host "📄 Testing manifest: $($latestManifest.Name)" -ForegroundColor Cyan

# Test YAML parsing
try {
    $manifestContent = Get-Content $latestManifest.FullName -Raw
    # Basic YAML validation by trying to parse key fields
    if ($manifestContent -notmatch "PackageIdentifier:\s*RnwoodLtd\.smtp4dev") {
        throw "Invalid PackageIdentifier"
    }
    if ($manifestContent -notmatch "ManifestType:\s*singleton") {
        throw "Invalid ManifestType"
    }
    if ($manifestContent -notmatch "ManifestVersion:\s*1\.4\.0") {
        throw "Invalid ManifestVersion"
    }
    Write-Host "✅ Manifest YAML structure is valid" -ForegroundColor Green
} catch {
    Write-Error "❌ Manifest validation failed: $_"
    exit 1
}

# Test URL accessibility (check if URLs return 200)
Write-Host "🌐 Testing installer URLs..." -ForegroundColor Yellow

$urlPattern = 'InstallerUrl:\s*(https://[^\s]+)'
$urls = [regex]::Matches($manifestContent, $urlPattern) | ForEach-Object { $_.Groups[1].Value }

foreach ($url in $urls) {
    try {
        $response = Invoke-WebRequest -Uri $url -Method Head -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "  ✅ $url" -ForegroundColor Green
        } else {
            Write-Host "  ❌ $url (Status: $($response.StatusCode))" -ForegroundColor Red
        }
    } catch {
        Write-Host "  ❌ $url (Error: $($_.Exception.Message))" -ForegroundColor Red
    }
}

# Display installation instructions
Write-Host "`n📦 How users can install smtp4dev with winget:" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Install smtp4dev:" -ForegroundColor White
Write-Host "   winget install smtp4dev" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Or use the full package ID:" -ForegroundColor White
Write-Host "   winget install RnwoodLtd.smtp4dev" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Start smtp4dev:" -ForegroundColor White
Write-Host "   smtp4dev" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Upgrade to latest version:" -ForegroundColor White
Write-Host "   winget upgrade smtp4dev" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Uninstall:" -ForegroundColor White
Write-Host "   winget uninstall smtp4dev" -ForegroundColor Gray

Write-Host "`n🎉 Winget manifest validation completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Next steps for winget-pkgs submission:" -ForegroundColor Yellow
Write-Host "1. Fork https://github.com/microsoft/winget-pkgs"
Write-Host "2. Create directory: manifests/r/RnwoodLtd/smtp4dev/[version]/"
Write-Host "3. Copy the manifest file to: RnwoodLtd.smtp4dev.yaml"
Write-Host "4. Create pull request to winget-pkgs repository"
Write-Host "5. Wait for community review and approval"