# smtp4dev Development Instructions

**Always follow these instructions first and fallback to additional search and context gathering only if the information in the instructions is incomplete or found to be in error.**

smtp4dev is a fake SMTP email server for development and testing, built as a .NET 8.0 web application with a Vue.js 3 frontend. It provides SMTP, IMAP, and web interfaces for testing email functionality during development.

## Working Effectively

### Prerequisites and Dependencies
- .NET 8.0 SDK (tested with 8.0.119)
- Node.js 20+ and npm 10+ (tested with Node.js 20.19.4, npm 10.8.2)
- Git
- Optional: Chrome/Chromium for E2E tests

### Bootstrap, Build, and Test Repository

**NEVER CANCEL: All build and test commands can take significant time. Use appropriate timeouts.**

1. **Restore .NET dependencies:**
   ```bash
   dotnet restore
   ```
   - Takes approximately 40-60 seconds
   - NEVER CANCEL - Set timeout to 120+ seconds

2. **Install frontend dependencies:**
   ```bash
   cd Rnwood.Smtp4dev/ClientApp
   npm install
   ```
   - Takes approximately 35-50 seconds
   - NEVER CANCEL - Set timeout to 120+ seconds
   - May show peer dependency warnings (expected and harmless)

3. **Build the application:**
   ```bash
   # Build main web application (recommended for development)
   dotnet build Rnwood.Smtp4dev/Rnwood.Smtp4dev.csproj -c Release
   ```
   - Takes approximately 3-5 seconds after initial frontend build
   - NEVER CANCEL - Set timeout to 60+ seconds
   - The Desktop project will fail on Linux (expected - Windows-only)

4. **Publish the application:**
   ```bash
   dotnet publish Rnwood.Smtp4dev/Rnwood.Smtp4dev.csproj -c Release -o ./published
   ```
   - Takes approximately 20-25 seconds including frontend Vite build
   - NEVER CANCEL - Set timeout to 120+ seconds

### Run the Application

**Development mode (with live reload):**
```bash
dotnet run --project Rnwood.Smtp4dev/Rnwood.Smtp4dev.csproj --urls="http://localhost:5000" --smtpport=2525 --imapport=1143
```

**Published mode:**
```bash
cd ./published
./Rnwood.Smtp4dev --urls="http://localhost:5000" --smtpport=2525 --imapport=1143
```

**Key startup arguments:**
- `--urls`: Web interface URL (default: http://localhost:5000)
- `--smtpport`: SMTP server port (default: 25, use 2525 for development)
- `--imapport`: IMAP server port (default: 143, use 1143 for development)
- `--help`: Full list of command-line options

The application serves:
- Web UI at the specified URL (e.g., http://localhost:5000)
- SMTP server on the specified port
- IMAP server on the specified port
- API endpoints at `/api/messages`, `/api/sessions`, etc.

### Testing

**Unit and Integration Tests:**
```bash
# SMTP Server library tests
dotnet test smtpserver/Rnwood.SmtpServer.Tests -c Release
```
- Takes approximately 20-30 seconds
- NEVER CANCEL - Set timeout to 60+ seconds
- Some IPv6-related test failures are expected in CI environments

**Frontend Linting:**
```bash
cd Rnwood.Smtp4dev/ClientApp
npm run lint
```
- Takes approximately 10-15 seconds
- Some configuration warnings are expected

**Frontend Tests:**
```bash
cd Rnwood.Smtp4dev/ClientApp
npm test -- --passWithNoTests
```
- Currently has Jest configuration issues (known limitation)

### Validation Scenarios

**ALWAYS manually validate changes with these scenarios:**

1. **Basic SMTP Functionality:**
   ```bash
   # Send a test email via SMTP
   cat > /tmp/test_email.txt << 'EOF'
   HELO localhost
   MAIL FROM: <test@example.com>
   RCPT TO: <user@example.com>
   DATA
   Subject: Test Email
   From: test@example.com
   To: user@example.com

   This is a test email to validate smtp4dev functionality.
   .
   QUIT
   EOF
   
   nc localhost 2525 < /tmp/test_email.txt
   ```

2. **Web UI Functionality:**
   - Navigate to http://localhost:5000
   - Verify the email appears in the Messages tab
   - Click on the email to view details
   - Test the Sessions tab to see SMTP session logs

3. **API Functionality:**
   ```bash
   # Check messages API
   curl -s http://localhost:5000/api/messages | jq '.'
   
   # Check sessions API
   curl -s http://localhost:5000/api/sessions | jq '.'
   ```

## Project Structure

### Key Projects
- `Rnwood.Smtp4dev/` - Main web application (ASP.NET Core + Vue.js)
- `Rnwood.Smtp4dev.Tests/` - Unit and E2E tests
- `Rnwood.Smtp4dev.Desktop/` - Desktop application (Windows only)
- `smtpserver/Rnwood.SmtpServer/` - SMTP server library
- `imapserver/New.LumiSoft.Net/` - IMAP server library

### Important Files
- `Rnwood.Smtp4dev/appsettings.json` - Main configuration with extensive documentation
- `Rnwood.Smtp4dev/ClientApp/package.json` - Frontend dependencies and scripts
- `azure-pipelines.yml` - CI/CD pipeline configuration
- `Rnwood.Smtp4dev/ClientApp/vite.config.js` - Frontend build configuration
- `docs/` - Documentation for users

### Frontend Structure
- Built with Vue.js 3, TypeScript, and Vite
- Located in `Rnwood.Smtp4dev/ClientApp/`
- Uses Element Plus UI components
- Build output goes to `Rnwood.Smtp4dev/wwwroot/`

## Development Workflow

### Commit Message Requirements

**All commits MUST follow Conventional Commits format.** This is enforced by CI and required for PR acceptance.

#### Format
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

#### Required Types
- **feat**: New feature or enhancement
- **fix**: Bug fixes
- **docs**: Documentation changes
- **style**: Code style changes (formatting, whitespace)
- **refactor**: Code restructuring without feature changes
- **test**: Test additions or modifications
- **chore**: Build process, dependency updates, maintenance
- **perf**: Performance improvements
- **ci**: CI/CD configuration changes
- **build**: Build system changes
- **revert**: Revert previous changes

#### Examples
```
feat: add IMAP folder management
fix: resolve memory leak in SMTP session handling
docs: update API documentation for message endpoints
chore(deps): update .NET SDK to 8.0.201
ci: add conventional commit validation workflow
test: add unit tests for email parsing
```

#### Scope Guidelines
- Use component names: `smtp`, `imap`, `ui`, `api`, `docs`, `deps`
- Keep scopes lowercase and concise
- Scope is optional but recommended for larger changes

#### Enforcement
- **Conventional commit format is enforced by GitHub Actions on all PRs**
- PRs with non-conventional commits will fail validation and cannot be merged
- Use `git commit --amend` or `git rebase -i` to fix commit messages if needed

### Making Changes
1. **Always build and test first** to establish a baseline
2. Make minimal, focused changes
3. **Use conventional commit format** for all commits
4. **Always run validation scenarios** after changes
5. Test both development and published modes

### Frontend Development
```bash
cd Rnwood.Smtp4dev/ClientApp
npm run dev  # Development server with hot reload (runs on port 5173)
```

### Backend Development
```bash
dotnet watch run --project Rnwood.Smtp4dev/Rnwood.Smtp4dev.csproj
```

### Configuration
- Default configuration in `appsettings.json` with extensive inline documentation
- User configuration stored in `~/.config/smtp4dev/appsettings.json` on Linux
- Environment variables: Use `ServerOptions__PropertyName` format
- Command line arguments: Use `--propertyname=value` format

## Common Issues and Solutions

### Build Issues
- **Desktop project fails on Linux**: Expected - it's Windows-only
- **NPM peer dependency warnings**: Expected and harmless
- **Sass deprecation warnings**: Expected with current Vite/Sass versions
- **Large chunk size warnings**: Expected due to comprehensive frontend dependencies

### Runtime Issues
- **Port conflicts**: Use non-standard ports (2525 for SMTP, 1143 for IMAP, 5001+ for web)
- **IPv6 test failures**: Expected in some CI environments
- **Database locks**: Application creates SQLite database in user config directory

### Testing Issues
- **Jest configuration errors**: Known issue with current module configuration
- **E2E test complexity**: Requires Chrome and specific environment setup
- **Network-dependent tests**: May fail in restricted environments

## Integration with CI/CD

The project uses Azure Pipelines (`azure-pipelines.yml`) with:
- Multi-platform builds (Windows, Linux, macOS)
- Docker image creation
- NuGet package publishing
- GitHub releases

**Build matrix includes:**
- `win-x64`, `linux-x64`, `linux-musl-x64`, `linux-arm`, `win-arm64`
- Desktop variants (Windows only)
- Docker images (Linux and Windows)

## Command Reference

### Essential Commands
```bash
# Quick development setup
dotnet restore && cd Rnwood.Smtp4dev/ClientApp && npm install && cd ../..

# Build for development
dotnet build Rnwood.Smtp4dev/Rnwood.Smtp4dev.csproj -c Release

# Run with development settings
dotnet run --project Rnwood.Smtp4dev/Rnwood.Smtp4dev.csproj --urls="http://localhost:5000" --smtpport=2525

# Test SMTP functionality
echo -e "HELO test\nMAIL FROM:<test@example.com>\nRCPT TO:<user@example.com>\nDATA\nSubject: Test\n\nTest message\n.\nQUITS" | nc localhost 2525

# Check web UI
curl -s http://localhost:5000/ && echo "Web UI is running"

# Check API
curl -s http://localhost:5000/api/messages | jq '.results | length'
```

Always ensure the application is fully functional by testing email sending, web UI access, and API responses after making any changes.