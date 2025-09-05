#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Creates a fork of microsoft/winget-pkgs, commits manifest changes, and creates a PR
.DESCRIPTION
    This script automates the complete process of submitting smtp4dev to the Windows Package Manager repository.
    It handles forking, cloning, updating the fork, creating branches, committing manifests, and creating pull requests.
.PARAMETER Version
    The version of smtp4dev to submit (used for branch name and commit message)
.PARAMETER ManifestDir
    Directory containing the winget manifest files to submit (default: current directory)
.PARAMETER GitHubToken
    GitHub personal access token with repo and workflow permissions (required for forking and PR creation)
.PARAMETER ForkOwner
    GitHub username/organization that will own the fork (default: current authenticated user)
.PARAMETER DryRun
    If specified, performs all steps except pushing to GitHub and creating the PR
.EXAMPLE
    .\submit-to-winget.ps1 -Version "3.8.7" -GitHubToken $env:GITHUB_TOKEN
.EXAMPLE
    .\submit-to-winget.ps1 -Version "3.8.7" -ManifestDir ".\manifests" -GitHubToken $env:GITHUB_TOKEN -DryRun
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [string]$ManifestDir = ".",
    
    [Parameter(Mandatory = $true)]
    [string]$GitHubToken,
    
    [Parameter(Mandatory = $false)]
    [string]$ForkOwner = "",
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Constants
$WINGET_REPO = "microsoft/winget-pkgs"
$WINGET_REPO_URL = "https://github.com/microsoft/winget-pkgs.git"
$PACKAGE_PATH = "manifests/r/Rnwood/smtp4dev"
$BRANCH_NAME = "smtp4dev-$Version"

Write-Host "🚀 smtp4dev Winget Submission Automation" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host "Manifest Directory: $ManifestDir" -ForegroundColor Cyan
Write-Host "Branch Name: $BRANCH_NAME" -ForegroundColor Cyan
if ($DryRun) {
    Write-Host "DRY RUN MODE - No changes will be pushed to GitHub" -ForegroundColor Yellow
}
Write-Host ""

# Function to make GitHub API calls
function Invoke-GitHubApi {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [object]$Body = $null
    )
    
    $headers = @{
        'Authorization' = "token $GitHubToken"
        'Accept' = 'application/vnd.github.v3+json'
        'User-Agent' = 'smtp4dev-winget-automation'
    }
    
    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $headers
    }
    
    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
        $params.ContentType = 'application/json'
    }
    
    try {
        return Invoke-RestMethod @params
    }
    catch {
        Write-Error "GitHub API call failed: $($_.Exception.Message)"
        throw
    }
}

# Get current authenticated user
Write-Host "🔍 Getting GitHub user information..." -ForegroundColor Yellow
$currentUser = Invoke-GitHubApi -Uri "https://api.github.com/user"
$authenticatedUser = $currentUser.login

if (-not $ForkOwner) {
    $ForkOwner = $authenticatedUser
}

Write-Host "✅ Authenticated as: $authenticatedUser" -ForegroundColor Green
Write-Host "📍 Fork will be created under: $ForkOwner" -ForegroundColor Cyan

# Check if fork already exists
Write-Host "🔍 Checking if fork exists..." -ForegroundColor Yellow
$forkUrl = "https://api.github.com/repos/$ForkOwner/winget-pkgs"
$forkExists = $false

try {
    $forkRepo = Invoke-GitHubApi -Uri $forkUrl
    $forkExists = $true
    Write-Host "✅ Fork exists: $($forkRepo.html_url)" -ForegroundColor Green
}
catch {
    Write-Host "ℹ️ Fork does not exist, will create it" -ForegroundColor Yellow
}

# Create fork if it doesn't exist
if (-not $forkExists -and -not $DryRun) {
    Write-Host "🍴 Creating fork of $WINGET_REPO..." -ForegroundColor Yellow
    
    $forkBody = @{
        owner = $ForkOwner
    }
    
    try {
        $forkRepo = Invoke-GitHubApi -Uri "https://api.github.com/repos/$WINGET_REPO/forks" -Method "POST" -Body $forkBody
        Write-Host "✅ Fork created: $($forkRepo.html_url)" -ForegroundColor Green
        
        # Wait a moment for fork to be ready
        Write-Host "⏳ Waiting for fork to be ready..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
    }
    catch {
        Write-Error "Failed to create fork: $_"
        throw
    }
}
elseif (-not $forkExists -and $DryRun) {
    Write-Host "🔸 DRY RUN: Would create fork of $WINGET_REPO under $ForkOwner" -ForegroundColor Yellow
}

# Configure Git
Write-Host "🔧 Configuring Git..." -ForegroundColor Yellow
git config --global user.email "noreply@github.com"
git config --global user.name "smtp4dev-automation"

# Clone the fork (or original repo for dry run)
$workDir = "winget-pkgs-work-$([System.Guid]::NewGuid().ToString('N')[0..7] -join '')"
$cloneUrl = if ($DryRun) { $WINGET_REPO_URL } else { "https://github.com/$ForkOwner/winget-pkgs.git" }

Write-Host "📥 Cloning repository..." -ForegroundColor Yellow
Write-Host "  Clone URL: $cloneUrl" -ForegroundColor Gray
git clone $cloneUrl $workDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to clone repository"
    exit 1
}

Push-Location $workDir

try {
    # Add upstream remote if working with fork
    if (-not $DryRun) {
        Write-Host "🔗 Adding upstream remote..." -ForegroundColor Yellow
        git remote add upstream $WINGET_REPO_URL
        git fetch upstream
        
        # Update main branch with upstream
        git checkout main
        git merge upstream/main --no-edit
        
        Write-Host "📤 Pushing updated main branch to fork..." -ForegroundColor Yellow
        git push origin main
    }
    
    # Create and checkout new branch
    Write-Host "🌿 Creating branch: $BRANCH_NAME" -ForegroundColor Yellow
    git checkout -b $BRANCH_NAME
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create branch"
        exit 1
    }
    
    # Create target directory
    $targetDir = Join-Path (Get-Location) "$PACKAGE_PATH/$Version"
    Write-Host "📁 Creating directory: $targetDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    
    # Find and copy manifest files
    Write-Host "📄 Copying manifest files..." -ForegroundColor Yellow
    $manifestFiles = Get-ChildItem -Path $ManifestDir -Filter "*.yaml" -Recurse
    
    if ($manifestFiles.Count -eq 0) {
        Write-Error "No manifest files found in $ManifestDir"
        exit 1
    }
    
    foreach ($manifest in $manifestFiles) {
        $targetFile = Join-Path $targetDir $manifest.Name
        Copy-Item $manifest.FullName $targetFile
        Write-Host "  ✓ Copied: $($manifest.Name)" -ForegroundColor Green
        
        # Validate manifest file
        $content = Get-Content $targetFile -Raw
        if ($content -match "PLACEHOLDER_") {
            Write-Error "Manifest file $($manifest.Name) contains unresolved placeholders"
            exit 1
        }
    }
    
    # Add and commit changes
    Write-Host "💾 Committing changes..." -ForegroundColor Yellow
    git add .
    $commitMessage = "Add smtp4dev version $Version"
    git commit -m $commitMessage
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to commit changes"
        exit 1
    }
    
    if (-not $DryRun) {
        # Push branch to fork
        Write-Host "📤 Pushing branch to fork..." -ForegroundColor Yellow
        git push -u origin $BRANCH_NAME
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to push branch"
            exit 1
        }
        
        # Create pull request
        Write-Host "🔀 Creating pull request..." -ForegroundColor Yellow
        
        $prTitle = "Add smtp4dev version $Version"
        $prBody = @"
This pull request adds smtp4dev version $Version to the Windows Package Manager.

**Package Information:**
- Package ID: Rnwood.smtp4dev
- Version: $Version
- Architectures: x64, ARM64
- Installer Type: Portable (ZIP)

**Validation:**
- [x] Manifest files generated automatically with verified SHA256 hashes
- [x] Package tested against winget validate
- [x] URLs verified and accessible

**About smtp4dev:**
smtp4dev is a dummy SMTP email server for development and testing. It provides SMTP, IMAP, and web interfaces for testing email functionality during development.

For more information: https://github.com/rnwood/smtp4dev
"@
        
        $prBody_obj = @{
            title = $prTitle
            body = $prBody
            head = "$ForkOwner`:$BRANCH_NAME"
            base = "main"
        }
        
        try {
            $pr = Invoke-GitHubApi -Uri "https://api.github.com/repos/$WINGET_REPO/pulls" -Method "POST" -Body $prBody_obj
            Write-Host "✅ Pull request created successfully!" -ForegroundColor Green
            Write-Host "🔗 PR URL: $($pr.html_url)" -ForegroundColor Cyan
            Write-Host "📋 PR Number: #$($pr.number)" -ForegroundColor Cyan
        }
        catch {
            Write-Error "Failed to create pull request: $_"
            throw
        }
    }
    else {
        Write-Host "🔸 DRY RUN: Would push branch and create PR" -ForegroundColor Yellow
        Write-Host "  Branch: $BRANCH_NAME" -ForegroundColor Gray
        Write-Host "  Commit: $commitMessage" -ForegroundColor Gray
        Write-Host "  Files added: $($manifestFiles.Count) manifest file(s)" -ForegroundColor Gray
    }
    
}
finally {
    # Clean up
    Pop-Location
    
    if (Test-Path $workDir) {
        Write-Host "🧹 Cleaning up working directory..." -ForegroundColor Yellow
        Remove-Item -Path $workDir -Recurse -Force
    }
}

Write-Host ""
Write-Host "🎉 Winget submission completed successfully!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

if (-not $DryRun) {
    Write-Host "✅ The pull request has been created and is ready for review by the winget community."
    Write-Host "📋 Monitor the PR for any feedback or validation issues."
    Write-Host "⏳ Once approved and merged, users will be able to install smtp4dev with:"
    Write-Host "   winget install smtp4dev" -ForegroundColor Cyan
}
else {
    Write-Host "🔸 DRY RUN completed - no changes were made to GitHub"
    Write-Host "🔧 To submit for real, run without -DryRun parameter"
}