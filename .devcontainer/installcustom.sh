#!/bin/sh
sudo apt update
sudo apt install -y telnet curl

# Install .NET 8.0 SDK (as specified in AGENTS.md: tested with 8.0.119)
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/repos/microsoft-debian-bookworm-prod bookworm main" | sudo tee /etc/apt/sources.list.d/microsoft-prod.list
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Install Node.js 20.x and npm (as specified in AGENTS.md)
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs

# Verify versions
echo "Installed versions:"
echo ".NET version: $(dotnet --version)"
echo "Node.js version: $(node --version)"
echo "npm version: $(npm --version)"

# Install Playwright browsers for E2E testing
echo "Installing Playwright browsers..."
# Extract Playwright version from the test project file
PLAYWRIGHT_VERSION=$(grep 'Microsoft\.Playwright.*Version=' ../Rnwood.Smtp4dev.Tests/Rnwood.Smtp4dev.Tests.csproj | head -1 | sed 's/.*Version="\([^"]*\)".*/\1/')
if [ -z "$PLAYWRIGHT_VERSION" ]; then
    echo "Warning: Could not detect Playwright version, falling back to latest"
    PLAYWRIGHT_VERSION="latest"
fi
echo "Detected Playwright version: $PLAYWRIGHT_VERSION"
npx --yes playwright@$PLAYWRIGHT_VERSION install chromium
npx --yes playwright@$PLAYWRIGHT_VERSION install-deps chromium