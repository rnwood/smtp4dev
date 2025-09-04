# Contributing to smtp4dev

Thank you for your interest in contributing to smtp4dev! We welcome contributions from the community.

## Contributor License Agreement

**Before we can accept your pull request, you need to sign our Contributor License Agreement (CLA).**

📝 [Read the CLA Document](./CLA.md)

When you open a pull request, our CLA Assistant will automatically check if you've signed the CLA. If you haven't, you'll see a comment asking you to sign it by commenting:

```
I have read the CLA Document and I hereby sign the CLA
```

This is a one-time requirement for new contributors.

## How to Contribute

1. **Fork the repository** and create your feature branch from `master`
2. **Make your changes** following the existing code style
3. **Follow conventional commit format** - see [Commit Message Guidelines](#commit-message-guidelines) below
4. **Test your changes** - ensure all existing tests pass and add new tests if needed
5. **Open a pull request** with a clear description of your changes
6. **Sign the CLA** when prompted by the CLA Assistant
7. **Wait for review** - maintainers will review your changes and provide feedback

## Commit Message Guidelines

We use [Conventional Commits](https://www.conventionalcommits.org/) to ensure clear and consistent commit history. This format is **required** for all contributions.

### Format

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Types

- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Changes that do not affect the meaning of the code (white-space, formatting, etc.)
- **refactor**: A code change that neither fixes a bug nor adds a feature
- **test**: Adding missing tests or correcting existing tests
- **chore**: Changes to the build process or auxiliary tools and libraries
- **perf**: A code change that improves performance
- **ci**: Changes to CI configuration files and scripts
- **build**: Changes that affect the build system or external dependencies
- **revert**: Reverts a previous commit

### Examples

```
feat: add email attachment support
fix: resolve SMTP connection timeout issue
docs: update installation instructions
chore(deps): update Vue.js to version 3.4
ci: add conventional commit validation
```

### Validation

All commit messages are automatically validated when you create a pull request. If your commits don't follow the conventional format, the validation will fail and you'll need to update your commit messages.

## Development Setup

Please refer to the [developer documentation](./AGENTS.md) for detailed setup instructions.

## Questions?

If you have questions about the contribution process, please open an issue or reach out to the maintainers.

Thank you for contributing to smtp4dev! 🎉