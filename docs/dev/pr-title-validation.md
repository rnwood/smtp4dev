# PR Title Validation

This document explains how the automated PR title validation works for smtp4dev maintainers.

## Overview

The PR title validation system automatically checks that all pull requests follow the [Conventional Commits](https://www.conventionalcommits.org/) specification. This ensures consistent commit history and enables automated tooling.

## How It Works

### Automatic Checking
- The `PR Title Check` GitHub Action runs on every pull request
- It triggers when PRs are opened, edited, or synchronized
- Uses the `amannn/action-semantic-pull-request` action for validation
- Provides immediate feedback via PR status checks

### Validation Rules

**Required Format:** `type(optional-scope): description`

**Valid Types:**
- `feat`: New features
- `fix`: Bug fixes  
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Test additions or fixes
- `build`: Build system changes
- `ci`: CI/CD changes
- `chore`: Other maintenance tasks
- `revert`: Reverting previous commits

**Rules:**
- Type must be lowercase
- Description must not start with uppercase
- Scope is optional but helpful for context
- No ending period in description

### User Experience

**When validation fails:**
- PR status check shows as failed
- Automated comment explains the issue with examples
- Comment includes the current invalid title
- Links to contributing guidelines

**When validation passes:**
- PR status check shows as passed
- Any previous error comments are automatically removed
- PR can proceed with review

### Examples

✅ **Valid titles:**
- `feat: add email template support`
- `fix(smtp): resolve connection timeout issue`
- `docs: update installation guide`
- `chore(deps): update dependencies`
- `refactor(api): simplify message handling`

❌ **Invalid titles:**
- `Add email template support` (missing type)
- `feat: Add new feature` (description starts with uppercase)
- `feature: add support` (invalid type)
- `fix(smtp): resolve issue.` (ends with period)

## Troubleshooting

### Workflow Not Running
- Ensure GitHub Actions are enabled for the repository
- Check that the workflow file has proper permissions
- Verify the workflow is triggered on the correct events

### False Positives/Negatives
- Review the validation rules in the workflow configuration
- Check if the action version needs updating
- Verify the custom patterns are correctly configured

### Comment Not Posting
- Ensure the workflow has `pull-requests: write` permission
- Check that `GITHUB_TOKEN` is available
- Review GitHub Actions logs for error messages

## Configuration

The validation is configured in `.github/workflows/pr-title-check.yml`:

- **Allowed types**: Defined in the `types` parameter
- **Subject pattern**: Ensures descriptions don't start with uppercase
- **Custom messages**: Provides helpful error messages
- **Comment management**: Automatically adds/removes validation comments

## Security Considerations

- Uses only standard GitHub Actions permissions
- Relies on `GITHUB_TOKEN` provided by GitHub
- No external dependencies beyond the validation action
- Comments are posted by the GitHub Actions bot user

## Files Involved

- `.github/workflows/pr-title-check.yml` - The validation workflow
- `CONTRIBUTING.md` - User-facing documentation
- `AGENTS.md` - AI agent guidance
- `docs/dev/pr-title-validation.md` - This maintainer documentation

For more information about conventional commits, see: https://www.conventionalcommits.org/