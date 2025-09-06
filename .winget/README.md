# Windows Package Manager (winget) Submission

This directory contains the automation scripts for submitting smtp4dev to the Windows Package Manager repository.

## Files

- **`smtp4dev.yaml`** - Template manifest file for winget package definition
- **`generate-manifest.ps1`** - Script to generate actual manifest files with computed SHA256 hashes
- **`submit-to-winget.ps1`** - Complete automation script for submitting to winget-pkgs repository
- **`test-manifest.ps1`** - Script for testing manifest validity

## Automated Submission Process

The Azure Pipelines automatically generates winget manifests for every build and includes them in the `winget-manifests` build artifact along with the submission script.

### For Release Builds

1. Download the `winget-manifests` artifact from the Azure DevOps build
2. Extract the contents to a local directory
3. Ensure GitHub CLI is authenticated: `gh auth login`
4. Run the submission script:

```powershell
.\submit-to-winget.ps1 -Version "3.8.7"
```

### Prerequisites

- **GitHub CLI (gh)** installed and authenticated (`gh auth login`)
- **PowerShell 7+** or Windows PowerShell 5.1+
- **Git** installed and accessible from PATH

### What the Script Does

The `submit-to-winget.ps1` script fully automates the submission process:

1. **Authentication Check**: Verifies GitHub CLI is authenticated
2. **Fork Management**: Creates a fork of `microsoft/winget-pkgs` under `rnwood` organization if it doesn't exist
3. **Repository Setup**: Clones the fork and syncs with upstream
4. **Branch Creation**: Creates a version-specific branch for the submission
5. **Manifest Deployment**: Copies manifest files to the correct winget-pkgs directory structure
6. **Commit & Push**: Commits changes and pushes the branch to the fork
7. **Pull Request**: Creates a pull request using GitHub CLI with proper title and description

### Testing Before Submission

You can test the script without making actual changes:

```powershell
.\submit-to-winget.ps1 -Version "3.8.7" -DryRun
```

### GitHub CLI Setup

Install and authenticate with GitHub CLI:
```bash
# Install GitHub CLI (if not already installed)
# On Windows: winget install GitHub.cli
# On macOS: brew install gh
# On Linux: see https://github.com/cli/cli#installation

# Authenticate with GitHub
gh auth login
```

## Build Artifact Contents

The `winget-manifests` artifact contains:
- **Generated manifest files** with real SHA256 hashes computed from build artifacts
- **submit-to-winget.ps1** - The submission automation script using GitHub CLI
- **Directory structure** ready for winget-pkgs submission

## Manual Process (Alternative)

If you prefer manual submission:

1. Download the `winget-manifests` artifact
2. Fork `microsoft/winget-pkgs` on GitHub (to `rnwood` organization)
3. Clone your fork locally
4. Copy manifest files to `manifests/r/Rnwood/smtp4dev/{version}/`
5. Commit and push to a new branch
6. Create a pull request to `microsoft/winget-pkgs`

## Package Information

- **Package ID**: `Rnwood.smtp4dev`
- **Moniker**: `smtp4dev` (allows `winget install smtp4dev`)
- **Installer Type**: Portable (ZIP archives)
- **Architectures**: x64, ARM64
- **Command**: `smtp4dev`

Once submitted and approved, users can install smtp4dev with:
```bash
winget install smtp4dev
```