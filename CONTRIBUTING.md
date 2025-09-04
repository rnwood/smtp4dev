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
3. **Test your changes** - ensure all existing tests pass and add new tests if needed
4. **Open a pull request** with a clear description of your changes and a conventional commit title
5. **Sign the CLA** when prompted by the CLA Assistant
6. **Wait for review** - maintainers will review your changes and provide feedback

## Pull Request Title Requirements

**All pull requests must have titles that follow the [Conventional Commits](https://www.conventionalcommits.org/) specification.**

### Format

```
type(optional-scope): description
```

### Valid Types

- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
- `refactor`: A code change that neither fixes a bug nor adds a feature
- `perf`: A code change that improves performance
- `test`: Adding missing tests or correcting existing tests
- `build`: Changes that affect the build system or external dependencies
- `ci`: Changes to our CI configuration files and scripts
- `chore`: Other changes that don't modify src or test files
- `revert`: Reverts a previous commit

### Examples

- `feat: add email template support`
- `fix(smtp): resolve connection timeout issue`
- `docs: update installation guide`
- `chore(deps): update dependencies`
- `refactor(api): simplify message handling`

### Tips

- Use lowercase for the type and description
- Keep descriptions concise but descriptive
- Use imperative mood ("add" not "added" or "adds")
- Don't end with a period
- Scope is optional but helpful for clarifying the area of change

## Development Setup

Please refer to the [developer documentation](./AGENTS.md) for detailed setup instructions.

## Questions?

If you have questions about the contribution process, please open an issue or reach out to the maintainers.

Thank you for contributing to smtp4dev! 🎉