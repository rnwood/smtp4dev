# Developer Documentation

This directory contains documentation specifically for developers working on smtp4dev itself, including CI/CD pipeline configuration and development workflows.

## Contents

- [Azure DevOps PR Notifications](azure-devops-pr-notifications.md) - Automated build failure notifications for @copilot
- [Enhanced Coverage Reports](enhanced-coverage-reports.md) - Code coverage reporting pipeline implementation
- [CLA Management](CLA_MANAGEMENT.md) - Contributor License Agreement system management
- [PR Title Validation](pr-title-validation.md) - Conventional commit title enforcement system

## Pull Request Requirements

All pull requests must have titles that follow the [Conventional Commits](https://www.conventionalcommits.org/) specification. This is automatically validated by the `PR Title Check` GitHub Actions workflow.

See the main [Contributing Guidelines](../../CONTRIBUTING.md) for detailed requirements and examples.

## For Users

If you're looking for user documentation (installation, configuration, usage), please see the main [documentation directory](../README.md).