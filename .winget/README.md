# Winget Package Manifests for smtp4dev

This directory contains the tools and templates needed to generate Windows Package Manager (winget) manifests for smtp4dev.

## Files

- `smtp4dev.yaml` - Template manifest file with placeholders
- `generate-manifest.ps1` - PowerShell script to generate actual manifests from releases
- `generated/` - Directory where generated manifests are created (excluded from git)

## How it Works

1. **During Release Build**: The Azure Pipelines build automatically generates winget manifests for release builds using the `generate-manifest.ps1` script.

2. **Manual Generation**: You can also generate manifests manually for any existing release:
   ```powershell
   .\.winget\generate-manifest.ps1 -Version "3.8.7"
   ```

3. **Submission Process**: Generated manifests are created in the `winget-pkgs-submission` format, ready for submission to the [Microsoft winget-pkgs repository](https://github.com/microsoft/winget-pkgs).

## Submitting to winget-pkgs

To submit a new version to the Windows Package Manager community repository:

1. **Automated**: Download the `winget-manifests` artifact from a release build in Azure Pipelines.

2. **Manual**: Run the generation script locally:
   ```powershell
   .\.winget\generate-manifest.ps1 -Version "X.Y.Z"
   ```

3. **Submit to winget-pkgs**:
   - Fork https://github.com/microsoft/winget-pkgs
   - Copy the generated manifest files to `manifests/r/RnwoodLtd/smtp4dev/[version]/`
   - Create a pull request

## Package Information

- **Package ID**: `RnwoodLtd.smtp4dev`
- **Moniker**: `smtp4dev` (allows `winget install smtp4dev`)
- **Publisher**: Robert N Wood
- **Installer Type**: Portable (ZIP archives)
- **Architectures**: x64, ARM64

## Notes

- The winget package provides the console version of smtp4dev (Rnwood.Smtp4dev.exe)
- For the desktop GUI version, users should download from GitHub releases
- Manifests are automatically generated with correct SHA256 hashes from GitHub releases
- The package supports silent installation and follows winget best practices