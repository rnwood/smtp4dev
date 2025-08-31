# GitHub Copilot Instructions for smtp4dev

## Overview
This repository contains smtp4dev, a cross-platform SMTP server and debugging tool. The project includes a .NET backend and a Vue.js frontend for the web interface.

## Project Structure
- `Rnwood.Smtp4dev/` - Main application (ASP.NET Core backend + Vue.js frontend)
- `Rnwood.Smtp4dev.Tests/` - Unit and integration tests, including E2E tests
- `smtpserver/` - SMTP server implementation
- `imapserver/` - IMAP server implementation

## Development Guidelines

### Testing Requirements
**ALWAYS run tests before and after making changes:**

1. **Run all tests before making changes** to understand baseline:
   ```bash
   dotnet test Rnwood.Smtp4dev.Tests/Rnwood.Smtp4dev.Tests.csproj --verbosity normal
   ```

2. **Run specific tests during development** for targeted areas:
   ```bash
   dotnet test Rnwood.Smtp4dev.Tests/Rnwood.Smtp4dev.Tests.csproj --filter <TestName> --verbosity normal
   ```

3. **Always run full test suite after changes** to ensure nothing is broken:
   ```bash
   dotnet test Rnwood.Smtp4dev.Tests/Rnwood.Smtp4dev.Tests.csproj --verbosity normal
   ```

### Build Requirements
- The solution includes both .NET backend and Vue.js frontend components
- Run `dotnet build` from the solution root to build all components
- The Vue.js frontend is built automatically as part of the .NET build process

### E2E Testing
- E2E tests use Selenium WebDriver with headless Chrome
- Tests cover critical user workflows including message viewing and settings changes
- E2E tests may take longer to run (up to 20 seconds each) due to browser automation
- Always ensure E2E tests pass as they validate real user scenarios

### Code Quality
- Follow existing code patterns and conventions
- Make minimal, surgical changes when fixing issues
- Ensure all tests pass before considering work complete
- Use proper error handling and null checking in test code

### Frontend Development
- The Vue.js frontend is located in `Rnwood.Smtp4dev/ClientApp/`
- TypeScript is used for type safety
- Element Plus UI library is used for components
- SignalR is used for real-time communication between frontend and backend

### Key Areas
- **Message Sanitization**: HTML content sanitization for security (XSS prevention)
- **Real-time Updates**: SignalR connections for live message updates
- **Settings Management**: Application configuration through web UI
- **SMTP/IMAP Protocols**: Email protocol implementations

## Testing Checklist
When making changes, ensure:
- [ ] All existing unit tests pass
- [ ] All integration tests pass  
- [ ] All E2E tests pass
- [ ] No new test failures introduced
- [ ] Changes are covered by appropriate tests
- [ ] Build completes successfully

## Common Commands
```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test Rnwood.Smtp4dev.Tests/Rnwood.Smtp4dev.Tests.csproj --verbosity normal

# Run specific test category
dotnet test --filter "Category=E2E"

# Run single test
dotnet test --filter "CheckHtmlSanitizationSettingTakesEffectImmediately"

# Build and run application
dotnet run --project Rnwood.Smtp4dev/Rnwood.Smtp4dev.csproj
```

## Notes
- E2E tests require Chrome/Chromium to be available
- Tests use both in-memory and SQLite databases for different scenarios
- Some tests may require network access for WebDriverManager setup
- Always verify test changes work in both development and CI environments