# VS Code Debugging Guide for smtp4dev

This document explains how to use the enhanced debugging setup for smtp4dev in VS Code.

## Architecture

The smtp4dev application uses an integrated development setup where:
- **ASP.NET Core backend** runs on http://localhost:5000
- **Vite dev server** is automatically launched by the .NET app on port 5173 (in development mode only)
- **Vue CLI middleware** proxies frontend requests from :5000 to the Vite dev server
- **All debugging happens through the single :5000 endpoint**

## Available Debug Configurations

### Individual Configurations

1. **`.NET Core Launch (web)`** - Debug the ASP.NET Core backend
   - Starts the server on http://localhost:5000
   - Automatically launches Vite dev server on port 5173 in development mode
   - Includes server-ready action to automatically open browser

2. **`Attach to .NET Core`** - Attach to a running .NET process
   - Useful when the server is already running

3. **`Chrome (Client Debug)`** - Debug client-side code in Chrome
   - Connects to http://localhost:5000 (which proxies to Vite)
   - Enables TypeScript/Vue.js debugging with source maps

4. **`Chrome (Full App)`** - Debug the production build in Chrome
   - Tests the production build with source maps

5. **`Edge (Client Debug)`** - Alternative browser for client debugging
   - Same as Chrome but uses Microsoft Edge

### Compound Configurations (Recommended)

1. **`Full Stack (Development)`** ⭐ **RECOMMENDED**
   - Starts .NET Core backend (which auto-starts Vite dev server)
   - Opens Chrome for both server and client debugging
   - Best for full-stack development with hot reload

2. **`Full Stack (Production Build)`**
   - Builds client assets and starts .NET Core backend
   - Tests the production build

3. **`Full Stack (Edge)`**
   - Same as development but uses Microsoft Edge

## How to Use

### For Full-Stack Development (Most Common)

1. Open VS Code in the smtp4dev repository root
2. Press `F5` or go to Run and Debug → "Full Stack (Development)"
3. Wait for the .NET server to start (it will auto-launch Vite)
4. Chrome will open automatically at http://localhost:5000
5. Set breakpoints in both C# (.cs files) and TypeScript/Vue (.ts/.vue files)

### Development Workflow

1. **Backend Changes**: Edit C# files → breakpoints work immediately
2. **Frontend Changes**: Edit Vue/TS files → hot reload updates the browser automatically
3. **API Testing**: All requests go through http://localhost:5000

### Port Configuration

- **Backend (.NET)**: http://localhost:5000 (main entry point)
- **Frontend (Vite)**: http://127.0.0.1:5173 (auto-started, proxied by backend)
- **Debugging**: Always use http://localhost:5000

### Debugging Features

- **Source Maps**: Both client and server debugging with full source map support
- **Hot Reload**: Frontend changes update immediately without losing state via Vite integration
- **Breakpoints**: Set breakpoints in TypeScript, Vue components, and C# code
- **Multiple Browsers**: Separate debug profiles for Chrome and Edge
- **Console Debugging**: Full access to browser and server console output
- **Integrated Architecture**: Single entry point (localhost:5000) handles everything

## Troubleshooting

### Port Already in Use
If you get port conflicts:
- Stop other instances of the application
- Check for running dotnet processes: `Get-Process dotnet` (PowerShell)
- Port 5173 conflicts: The .NET app manages Vite, so stop the .NET process

### Windows-Specific Issues
- The application automatically handles cross-platform Vite launching
- No need to manually start Vite - it's integrated into the .NET startup process

### Source Maps Not Working
- Ensure the .NET application is running (it manages the Vite dev server)
- Check the browser's Developer Tools → Sources tab
- Verify TypeScript compilation is successful

### Frontend Not Updating
- Check that hot reload is working (should see updates automatically)
- Ensure you're browsing to http://localhost:5000 (not 5173 directly)
- The .NET app proxies requests to Vite automatically

### Backend API Not Responding
- Ensure the .NET Core backend is running on port 5000
- Check the Debug Console for any startup errors
- Verify appsettings.json configuration

## Tips

1. **Use "Full Stack (Development)" for daily development** - it provides the best experience
2. **Always browse to localhost:5000** - this is the integrated entry point
3. **Set breakpoints before starting debugging** - they'll be hit as soon as the code runs
4. **Use the integrated terminal** - you can see both .NET and Vite output
5. **Browser DevTools** - Use alongside VS Code debugging for complete frontend inspection
6. **Stop All** - Use Ctrl+Shift+F5 to stop all debug sessions at once
7. **Don't manually start Vite** - let the .NET app handle it automatically

## File Structure for Debugging

```
.vscode/
├── launch.json     # Debug configurations
├── tasks.json      # Build and preparation tasks
├── settings.json   # VS Code settings for better debugging
└── DEBUG.md        # This file

Rnwood.Smtp4dev/
├── ClientApp/
│   ├── vite.config.js  # Updated with proxy and source map config
│   └── src/            # Frontend source files
└── *.cs               # Backend source files
```