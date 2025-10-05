# TUI (Terminal User Interface) Mode

smtp4dev now includes a Terminal User Interface (TUI) mode for environments where a graphical web interface is not desired or available. The TUI provides core functionality including message viewing, session inspection, and server status monitoring directly in the terminal.

## Enabling TUI Mode

To run smtp4dev with the TUI instead of the web interface, use the `--tui` command line option:

```bash
smtp4dev --tui --smtpport=2525 --imapport=1143
```

## Features

The TUI provides three main tabs accessible via navigation:

### Messages Tab
- View list of received emails with From, To, Subject, and received date
- View message details including headers and body
- Delete individual messages or all messages
- Auto-refresh every 2 seconds

### Sessions Tab
- View list of SMTP sessions with client address, timestamp, and message count
- View session details including full session log
- Delete individual sessions or all sessions
- Auto-refresh every 2 seconds

### Server Status Tab
- View SMTP server status and listening endpoints
- View IMAP server status and listening endpoints
- Real-time status updates

## Navigation

- **Tab key or Arrow keys**: Switch between tabs (Messages, Sessions, Server Status)
- **Up/Down arrows**: Navigate lists
- **Enter**: Open selected item details
- **F1**: Show help dialog
- **F10**: Quit application
- **Mouse**: Click buttons and interact with UI elements

## Screenshot

![TUI Initial View](https://github.com/user-attachments/assets/9c8b1bcb-1c12-4ac4-a96b-6fd95ab0b5e0)

## Technology

The TUI is built using [Terminal.Gui](https://gui-cs.github.io/Terminal.Gui/), a cross-platform terminal UI toolkit for .NET.

## Limitations

When running in TUI mode:
- The web interface is still started but not intended for use
- Email composition features are not available in the TUI
- Some advanced features like HTML validation are not accessible
- The TUI works best in terminals with Unicode support and at least 80x24 characters

## Example Usage

Start smtp4dev in TUI mode with custom ports:

```bash
# Basic usage
smtp4dev --tui

# With custom ports
smtp4dev --tui --smtpport=2525 --imapport=1143 --pop3port=0

# With in-memory database
smtp4dev --tui --db=""

# With custom URL for web interface (if needed)
smtp4dev --tui --urls="http://localhost:5000"
```

## Requirements

- Terminal with Unicode support
- Minimum terminal size: 80x24 characters
- Works on Linux, macOS, and Windows
