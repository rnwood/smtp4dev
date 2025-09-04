# CLA Management for Maintainers

This document explains how the Contributor License Agreement (CLA) system works for smtp4dev maintainers.

## Overview

The CLA system automatically checks all pull requests to ensure contributors have signed the Contributor License Agreement. This protects the project legally and ensures contributors grant appropriate rights to their contributions.

## How It Works

### Automatic Checking
- The CLA Assistant GitHub Action runs on every pull request
- It checks if all contributors to the PR have signed the CLA
- If any contributor hasn't signed, the PR status check fails

### For New Contributors
1. A new contributor opens a PR
2. The CLA Assistant posts a comment asking them to sign the CLA
3. The contributor reads the CLA document at [CLA.md](../CLA.md)
4. They sign by commenting exactly: `I have read the CLA Document and I hereby sign the CLA`
5. The CLA Assistant records their signature and updates the PR status

### Allowlisted Users
These users are exempt from CLA requirements:
- `rnwood` (project owner)
- `dependabot[bot]` (automated dependency updates)
- `copilot` (GitHub Copilot assistant)

## Signature Storage

CLA signatures are stored in the repository at `signatures/version1/cla.json`. This file:
- Is automatically created when the first signature is recorded
- Contains a JSON array of signed contributors with timestamps
- Should not be manually edited (managed by the CLA Assistant)

## Managing the CLA

### Updating the Allowlist
To add users who should be exempt from CLA requirements:

1. Edit `.github/workflows/cla.yml`
2. Update the `allowlist` parameter to include new usernames
3. Commit and push the changes

### Forcing a Re-check
If you need to force the CLA Assistant to re-check a PR:
- Comment `recheck` on the pull request

### Updating the CLA Document
If you need to update the CLA:

1. Update [CLA.md](../CLA.md) with the new terms
2. Consider whether existing signatures remain valid or if contributors need to re-sign
3. If re-signing is required, you may need to:
   - Create a new signature file path (e.g., `signatures/version2/cla.json`)
   - Update the workflow to use the new path
   - Clear existing signatures so contributors are prompted to re-sign

## Troubleshooting

### CLA Check Not Running
- Ensure the workflow file has proper permissions (contents: write, pull-requests: write)
- Check that the GitHub Actions are enabled for the repository

### Signatures Not Being Saved
- Verify the bot has write access to the repository
- Check that the master branch is not protected in a way that prevents automated commits
- Review GitHub Actions logs for error messages

### False Positives/Negatives
- Check the allowlist for typos in usernames
- Verify the contributor's GitHub username matches exactly (case-sensitive)
- Review the signatures file to confirm the signature was recorded

## Security Notes

- The CLA workflow uses `pull_request_target` which runs in the context of the base branch
- This provides necessary permissions to write signatures back to the repository
- The workflow only responds to specific comment patterns to prevent abuse

## Files Involved

- `.github/workflows/cla.yml` - The CLA workflow configuration
- `CLA.md` - The Contributor License Agreement document
- `CONTRIBUTING.md` - Contributing guidelines that reference the CLA
- `signatures/version1/cla.json` - Signature storage (auto-created)

For more information about the CLA Assistant, see: https://github.com/contributor-assistant/github-action