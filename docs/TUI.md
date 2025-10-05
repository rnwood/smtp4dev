# TUI (Terminal User Interface) Mode

smtp4dev now includes a comprehensive Terminal User Interface (TUI) mode for environments where a graphical web interface is not desired or available. The TUI provides full functionality including message viewing, session inspection, server monitoring, settings viewing, and message composition directly in the terminal.

## Enabling TUI Mode

To run smtp4dev with the TUI instead of the web interface, use the `--tui` command line option:

```bash
smtp4dev --tui --smtpport=2525 --imapport=1143
```

## Technology

The TUI is built using [Spectre.Console](https://spectreconsole.net/), a popular .NET library for creating beautiful, cross-platform console applications with rich text formatting, tables, trees, and interactive prompts.

## Main Features

The TUI provides a menu-driven interface with the following options:

### 📧 Messages
Complete message management with multiple views:

- **Message List**: View all received emails with From, To, Subject, and timestamp
- **Message Details**: Select any message to view:
  - **Overview**: Summary of From, To, Subject, Received date, and size
  - **Body**: Full message body content (plain text or HTML)
  - **Headers**: Complete message headers and MIME metadata
  - **Parts**: MIME parts information
  - **Raw Source**: Complete RFC822 message source
- **Actions**:
  - View individual messages
  - Delete all messages
  - Auto-refresh message list

### 📊 Sessions
SMTP session monitoring and inspection:

- **Session List**: View all SMTP sessions with:
  - Start date/time
  - Client address
  - Number of messages
  - Status (OK or ERROR)
- **Session Details**: Select any session to view:
  - Client address
  - Start and end timestamps
  - Message count
  - Error messages (if any)
  - Complete session log (SMTP protocol conversation)
- **Actions**:
  - View individual sessions
  - Delete all sessions
  - Auto-refresh session list

### 📝 Server Logs
Real-time server logging view:

- **Log Display**: Last 50 log entries with:
  - Timestamp (HH:mm:ss format)
  - Log level (Error, Warning, Information)
  - Color-coded messages:
    - Error messages in red
    - Warnings in yellow
    - Information in white
- **Auto-refresh**: Continuously updates as new logs are generated
- **Features**:
  - Captures all smtp4dev logging
  - Automatic log rotation (keeps last 500 entries)
  - Scroll through recent activity

### ⚙️ Server Status
Real-time server monitoring:

- **SMTP Server**:
  - Running status
  - Listening endpoints and ports
  - Error messages (if any)
- **IMAP Server**:
  - Running status
  - Current configuration

### ⚙️ Settings
View current server configuration:

- **SMTP Server Settings**:
  - Port configuration
  - Hostname
  - TLS mode
  - Remote connections setting
- **IMAP Server Settings**:
  - Port configuration
- **Relay Settings**:
  - SMTP relay server
  - Port
  - TLS mode
- **Storage Settings**:
  - Messages to keep
  - Sessions to keep

Note: Settings cannot be modified in TUI mode. Use `appsettings.json`, environment variables, or command-line arguments to change configuration.

### ✉️ Compose/Send Message
Interactive email composition and sending:

- **Input Fields**:
  - From email address
  - To email address (comma-separated for multiple recipients)
  - Subject line
  - Message body (type END on a new line to finish)
- **Send Options**:
  - Send immediately via smtp4dev server
  - Error handling and feedback

### 🔄 Refresh All
Manually trigger refresh of all data

## Navigation

The TUI uses Spectre.Console's interactive prompts:

- **Arrow keys**: Navigate through menu options
- **Enter**: Select an option
- **Type and Enter**: Make selections in prompts
- **Escape**: Generally not used (use "Back" options instead)

Each screen provides context-specific options like:
- "Back to Main Menu" - Return to main menu
- "Back" - Return to previous screen
- "Refresh" - Manually refresh current view

## Auto-Refresh

The following views support auto-refresh:
- **Messages**: Automatically refreshes when you select "Refresh" or cycle through actions
- **Sessions**: Automatically refreshes when you select "Refresh" or cycle through actions
- **Server Logs**: Continuously captures logs in real-time (buffer of last 500 entries)

## Example Usage

### Basic startup
```bash
smtp4dev --tui
```

### With custom ports
```bash
smtp4dev --tui --smtpport=2525 --imapport=1143 --pop3port=0
```

### With in-memory database
```bash
smtp4dev --tui --db=""
```

### With custom web interface URL (if needed in background)
```bash
smtp4dev --tui --urls="http://localhost:5000"
```

## Workflow Examples

### Viewing a Received Email
1. Run `smtp4dev --tui`
2. Select "📧 Messages"
3. View the message list
4. Select "View Message"
5. Choose the message from the list
6. Select "Body" to read the content
7. Select "Headers" to view MIME headers
8. Select "Raw Source" to see the complete RFC822 message
9. Select "Back" to return to message list
10. Select "Back to Main Menu"

### Monitoring SMTP Sessions
1. Select "📊 Sessions"
2. View the list of SMTP connections
3. Select "View Session"
4. Choose a session to inspect
5. Review the SMTP protocol conversation
6. Press any key to return
7. Select "Back to Main Menu"

### Sending a Test Email
1. Select "✉️ Compose/Send Message"
2. Enter from address: `test@example.com`
3. Enter to address: `recipient@example.com`
4. Enter subject: `Test Email`
5. Enter body (type END when done):
   ```
   This is a test message
   END
   ```
6. Confirm send: Yes
7. Message is sent and appears in Messages tab

### Watching Server Logs
1. Select "📝 Server Logs"
2. View real-time log entries
3. Note color-coding for different log levels
4. Select "Refresh" to update
5. Select "Back to Main Menu" when done

## Screenshots

*(Screenshots to be added showing the TUI in action)*

## Requirements

- Terminal with Unicode support recommended
- Minimum terminal size: 80x24 characters
- Works on Linux, macOS, and Windows
- Modern terminal emulator for best experience

## Limitations

When running in TUI mode:
- Web interface still starts in background (but not intended for use)
- HTML email rendering not available (plain text view only)
- HTML validation features not accessible
- Advanced MIME part inspection limited
- Best experience with modern terminal emulators

## Technical Details

The TUI implementation:
- Uses Spectre.Console for rich terminal UI
- Leverages existing smtp4dev services and data repositories
- Provides menu-driven navigation
- Includes error handling and user feedback
- Captures server logs via custom Serilog sink
- Maintains consistency with web UI data access patterns

## Future Enhancements

Potential improvements for future versions:
- Enhanced MIME parts inspection
- Search and filter capabilities
- HTML rendering in capable terminals
- Keyboard shortcuts
- Theme customization
- Split-screen views
- Real-time auto-refresh without manual intervention
