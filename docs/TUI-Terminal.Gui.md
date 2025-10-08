# Terminal User Interface (TUI) for smtp4dev

The smtp4dev TUI provides a feature-complete terminal-based interface using Terminal.Gui, offering an alternative to the web interface for server environments or terminal-based workflows.

## Starting the TUI

```bash
smtp4dev --tui --smtpport=2525 --imapport=1143
```

The TUI starts alongside the SMTP and IMAP servers, providing full access to smtp4dev functionality through a rich terminal interface.

## Features

### Main Interface

The TUI mirrors the web UI layout with:
- **Tabbed Interface**: Navigate between Messages and Sessions tabs
- **Split Panes**: Each tab shows a list view (40%) and details panel (60%)
- **Status Bar**: Function keys for quick actions (F1-Help, F5-Refresh, F9-Settings, F10-Quit)
- **Auto-Refresh**: Background refresh every 3 seconds

### Messages Tab

**Features:**
- View all received emails with From, To, Subject, and timestamp
- **Search**: Real-time filtering by From, To, or Subject
- **Multi-view Details**:
  - **Overview**: Sender, recipient, subject, date, size, attachments
  - **Body**: Message body content (extracted from MIME)
  - **Headers**: Complete email headers from raw message
  - **Raw Source**: Full RFC822 message source
- **Actions**:
  - Delete individual messages or all messages
  - Compose and send new messages
  - Manual refresh (F5)

**Compose Message:**
- Click "Compose" button
- Fill in From, To, Subject, and Body
- Send via smtp4dev server
- Integrates with message tracking

### Sessions Tab

**Features:**
- View all SMTP sessions with client address, timestamp, status, and message count
- **Search**: Filter by client address or name
- **Error Filter**: Show only failed sessions checkbox
- **Detailed Session Log**:
  - Session information (client, start/end times, duration)
  - Error messages if session failed
  - Complete SMTP protocol log
- **Actions**:
  - Delete individual sessions or all sessions
  - Manual refresh (F5)

### Settings Dialog (F9)

**Comprehensive Configuration:**
- **SMTP Settings**: Port, hostname, remote connections, TLS mode
- **IMAP Settings**: Port configuration
- **Relay Settings**: Server, port, TLS mode, auto-relay
- **Storage Settings**: Message and session retention limits
- **User Management**: Add/remove SMTP authentication users
- **Mailbox Management**: Add/remove mailboxes with recipient patterns

**Settings Persistence:**
- All changes saved to `appsettings.json`
- Server restart required for changes to take effect

## Navigation

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| F1 | Show help dialog |
| F5 | Refresh current tab |
| F9 | Open settings dialog |
| F10 | Quit application |
| Tab | Switch between UI elements |
| ↑/↓ | Navigate lists |
| Enter | View item details |
| Esc | Close dialogs |

### Mouse Support

- Click to select items in lists
- Click buttons to perform actions
- Scroll in text views
- Resize split panes (if supported by terminal)

## Data Synchronization

The TUI uses the same data repositories and services as the web UI:

**Benefits:**
- All operations trigger SignalR notifications
- Web UI automatically refreshes when TUI performs actions
- Multiple clients stay synchronized
- Consistent data handling

**Architecture:**
```
TUI → Repository/Service → NotificationsHub → SignalR → All Connected Clients
```

## Requirements

- Terminal with Unicode support (recommended)
- Minimum size: 80x24 characters
- Recommended size: 100x30+ for optimal experience
- Modern terminal emulator (Windows Terminal, iTerm2, GNOME Terminal, etc.)

## Limitations

- HTML emails displayed as plain text or parsed body
- Best experience requires modern terminal
- Some advanced MIME features limited compared to web UI
- Settings changes require server restart

## Technical Details

### Components

- **Terminal.Gui**: Cross-platform terminal UI framework (v1.19.0)
- **TerminalGuiApp**: Main application with TabView and StatusBar
- **MessagesTab**: Message list and multi-view details
- **SessionsTab**: Session list and protocol logs
- **SettingsDialog**: Form-based configuration editor
- **ManagementDialogs**: User and mailbox CRUD operations

### Integration

- Uses `IMessagesRepository` for message operations
- Uses `ISmtp4devServer` for session and sending operations
- Proper SignalR integration via task queue
- Consistent with web UI data flow

## Examples

### Viewing Messages

1. Start TUI: `smtp4dev --tui`
2. Navigate to Messages tab (default)
3. Use search field to filter messages
4. Select a message to view details
5. Switch between Overview, Body, Headers, and Raw tabs

### Managing Sessions

1. Navigate to Sessions tab (Tab key)
2. Check "Errors Only" to filter failed sessions
3. Select a session to view complete SMTP log
4. Delete old sessions to clean up data

### Sending Messages

1. In Messages tab, click "Compose"
2. Fill in From, To, Subject, and Body
3. Click "Send"
4. Message appears in message list

### Configuring Settings

1. Press F9 to open Settings
2. Modify SMTP, IMAP, Relay, or Storage settings
3. Click "Save" to persist changes
4. Restart server for changes to take effect

## Troubleshooting

**TUI doesn't start:**
- Ensure you're running in an interactive terminal (not piped or redirected)
- Check terminal supports required features
- Try a different terminal emulator

**Display issues:**
- Increase terminal size (minimum 80x24)
- Ensure Unicode support enabled
- Check color support in terminal

**Search not working:**
- Type in search field
- Search applies automatically as you type
- Clear search field to show all items

**Settings not saving:**
- Check file permissions for appsettings.json
- Verify data directory is writable
- Check console output for errors
