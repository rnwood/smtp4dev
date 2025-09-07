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

# Install Chrome for E2E testing
wget https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
sudo apt install -y ./google-chrome-stable_current_amd64.deb