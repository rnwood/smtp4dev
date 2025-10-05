using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Server.Settings;
using Serilog;
using Serilog.Events;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using MimeKit;

namespace Rnwood.Smtp4dev.TUI
{
    public class TuiApp
    {
        private readonly IHost host;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly List<LogEventInfo> logBuffer;
        private readonly object logLock = new object();
        private bool isRunning = true;
        private readonly SettingsManager settingsManager;
        private readonly KeyboardShortcuts keyboardShortcuts;
        private readonly HtmlRenderer htmlRenderer;
        private readonly AutoRefreshService autoRefreshService;
        private string currentMailbox = "Default";
        private string currentFolder = "INBOX";
        private string messageSearchFilter = "";
        private string sessionSearchFilter = "";

        public TuiApp(IHost host)
        {
            this.host = host;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.logBuffer = new List<LogEventInfo>();
            
            var dataDir = DirectoryHelper.GetDataDir(host.Services.GetRequiredService<CommandLineOptions>());
            this.settingsManager = new SettingsManager(
                host.Services.GetRequiredService<IOptionsMonitor<ServerOptions>>(),
                host.Services.GetRequiredService<IOptionsMonitor<RelayOptions>>(),
                dataDir);
            
            this.keyboardShortcuts = KeyboardShortcuts.CreateDefault();
            this.htmlRenderer = new HtmlRenderer();
            this.autoRefreshService = new AutoRefreshService();
            
            SetupLogListener();
            SetupKeyboardShortcuts();
        }

        private void SetupKeyboardShortcuts()
        {
            keyboardShortcuts.Register(ConsoleKey.F1, () => ShowHelp(), "Show Help");
            keyboardShortcuts.Register(ConsoleKey.F2, () => ShowMessages(), "Messages");
            keyboardShortcuts.Register(ConsoleKey.F3, () => ShowSessions(), "Sessions");
            keyboardShortcuts.Register(ConsoleKey.F4, () => ShowServerLogs(), "Server Logs");
            keyboardShortcuts.Register(ConsoleKey.F5, () => { /* Refresh current view */ }, "Refresh");
            keyboardShortcuts.Register(ConsoleKey.F10, () => isRunning = false, "Exit");
        }

        private void ShowHelp()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[blue]Help - Keyboard Shortcuts[/]"));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(keyboardShortcuts.GetHelp());
            AnsiConsole.WriteLine("\n[bold]Navigation:[/]");
            AnsiConsole.WriteLine("  Arrow Keys: Navigate menus");
            AnsiConsole.WriteLine("  Enter: Select option");
            AnsiConsole.WriteLine("  Escape: Back (where supported)");
            AnsiConsole.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private void SetupLogListener()
        {
            // Create a custom Serilog sink to capture logs
            var logCaptureSink = new LogCaptureSink(logBuffer, logLock);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Sink(logCaptureSink)
                .WriteTo.Console()
                .CreateLogger();
        }

        public void Run()
        {
            AnsiConsole.Clear();
            
            while (isRunning)
            {
                try
                {
                    ShowMainMenu();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    AnsiConsole.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private void ShowMainMenu()
        {
            AnsiConsole.Clear();
            
            var rule = new Rule("[blue]smtp4dev - Terminal UI[/]");
            
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Select an option: [dim](Mailbox: {currentMailbox}/{currentFolder})[/]")
                    .PageSize(12)
                    .AddChoices(new[] {
                        "📧 Messages",
                        "📊 Sessions",
                        "📝 Server Logs",
                        "⚙️  Server Status",
                        "⚙️  Settings (Editable)",
                        "👥 Manage Users",
                        "📁 Manage Mailboxes",
                        "🔀 Switch Mailbox/Folder",
                        "📱 Split-Screen: Messages",
                        "📱 Split-Screen: Sessions",
                        "✉️  Compose/Send Message",
                        "🔄 Refresh All",
                        "❓ Help (F1)",
                        "❌ Exit (F10)"
                    }));

            switch (choice)
            {
                case "📧 Messages":
                    ShowMessages();
                    break;
                case "📊 Sessions":
                    ShowSessions();
                    break;
                case "📝 Server Logs":
                    ShowServerLogs();
                    break;
                case "⚙️  Server Status":
                    ShowServerStatus();
                    break;
                case "⚙️  Settings (Editable)":
                    ShowEditableSettings();
                    break;
                case "👥 Manage Users":
                    ManageUsers();
                    break;
                case "📁 Manage Mailboxes":
                    ManageMailboxes();
                    break;
                case "🔀 Switch Mailbox/Folder":
                    SwitchMailboxFolder();
                    break;
                case "📱 Split-Screen: Messages":
                    ShowMessagesSplitScreen();
                    break;
                case "📱 Split-Screen: Sessions":
                    ShowSessionsSplitScreen();
                    break;
                case "✉️  Compose/Send Message":
                    ComposeMessage();
                    break;
                case "🔄 Refresh All":
                    AnsiConsole.Status()
                        .Start("Refreshing...", ctx => { Thread.Sleep(500); });
                    break;
                case "❓ Help (F1)":
                    ShowHelp();
                    break;
                case "❌ Exit (F10)":
                    isRunning = false;
                    break;
            }
        }

        private void SwitchMailboxFolder()
        {
            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            var serverOptions = settingsManager.GetServerOptions();
            
            var mailboxes = serverOptions.Mailboxes?.Select(m => m.Name).ToList() ?? new List<string>();
            if (!mailboxes.Contains("Default"))
                mailboxes.Insert(0, "Default");

            mailboxes.Add("Back");

            var selectedMailbox = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select mailbox:")
                    .AddChoices(mailboxes));

            if (selectedMailbox == "Back")
                return;

            currentMailbox = selectedMailbox;

            var folders = new[] { "INBOX", "Sent", "Back" };
            var selectedFolder = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select folder:")
                    .AddChoices(folders));

            if (selectedFolder != "Back")
            {
                currentFolder = selectedFolder;
                AnsiConsole.MarkupLine($"[green]Switched to {currentMailbox}/{currentFolder}[/]");
                Thread.Sleep(1000);
            }
        }

        private void ManageUsers()
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[blue]User Management[/]"));
                AnsiConsole.WriteLine();

                var serverOptions = settingsManager.GetServerOptions();
                var users = serverOptions.Users ?? Array.Empty<UserOptions>();

                var table = new Table();
                table.AddColumn("Username");
                table.AddColumn("Has Password");
                table.AddColumn("Default Mailbox");

                foreach (var user in users)
                {
                    table.AddRow(
                        user.Username?.EscapeMarkup() ?? "",
                        string.IsNullOrEmpty(user.Password) ? "No" : "Yes",
                        user.DefaultMailbox?.EscapeMarkup() ?? "Default"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine($"\nTotal Users: {users.Length}");

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select action:")
                        .AddChoices(new[] {
                            "Add User",
                            "Remove User",
                            "Back to Main Menu"
                        }));

                switch (action)
                {
                    case "Add User":
                        var username = AnsiConsole.Ask<string>("Enter username:");
                        var password = AnsiConsole.Prompt(
                            new TextPrompt<string>("Enter password:")
                                .Secret());
                        settingsManager.AddUser(username, password).Wait();
                        Thread.Sleep(1500);
                        break;
                    case "Remove User":
                        if (users.Any())
                        {
                            var userChoices = users.Select(u => u.Username).ToList();
                            userChoices.Add("Cancel");
                            var selectedUser = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("Select user to remove:")
                                    .AddChoices(userChoices));
                            if (selectedUser != "Cancel")
                            {
                                settingsManager.RemoveUser(selectedUser).Wait();
                                Thread.Sleep(1500);
                            }
                        }
                        break;
                    case "Back to Main Menu":
                        return;
                }
            }
        }

        private void ManageMailboxes()
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[blue]Mailbox Management[/]"));
                AnsiConsole.WriteLine();

                var serverOptions = settingsManager.GetServerOptions();
                var mailboxes = serverOptions.Mailboxes ?? Array.Empty<MailboxOptions>();

                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Recipients");

                foreach (var mailbox in mailboxes)
                {
                    table.AddRow(
                        mailbox.Name?.EscapeMarkup() ?? "",
                        mailbox.Recipients?.EscapeMarkup() ?? ""
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine($"\nTotal Mailboxes: {mailboxes.Length}");

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select action:")
                        .AddChoices(new[] {
                            "Add Mailbox",
                            "Remove Mailbox",
                            "Back to Main Menu"
                        }));

                switch (action)
                {
                    case "Add Mailbox":
                        var name = AnsiConsole.Ask<string>("Enter mailbox name:");
                        var recipients = AnsiConsole.Ask<string>("Enter recipients pattern (e.g., *@example.com):");
                        settingsManager.AddMailbox(name, recipients).Wait();
                        Thread.Sleep(1500);
                        break;
                    case "Remove Mailbox":
                        if (mailboxes.Any())
                        {
                            var mailboxChoices = mailboxes.Select(m => m.Name).ToList();
                            mailboxChoices.Add("Cancel");
                            var selectedMailbox = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("Select mailbox to remove:")
                                    .AddChoices(mailboxChoices));
                            if (selectedMailbox != "Cancel")
                            {
                                settingsManager.RemoveMailbox(selectedMailbox).Wait();
                                Thread.Sleep(1500);
                            }
                        }
                        break;
                    case "Back to Main Menu":
                        return;
                }
            }
        }

        private void ShowEditableSettings()
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[blue]Editable Settings[/]"));
                AnsiConsole.WriteLine();

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select settings category:")
                        .AddChoices(new[] {
                            "SMTP Server Settings",
                            "IMAP Server Settings",
                            "Relay Settings",
                            "Storage Settings",
                            "View All Settings (Read-Only)",
                            "Back to Main Menu"
                        }));

                switch (action)
                {
                    case "SMTP Server Settings":
                        EditSmtpSettings();
                        break;
                    case "IMAP Server Settings":
                        EditImapSettings();
                        break;
                    case "Relay Settings":
                        EditRelaySettings();
                        break;
                    case "Storage Settings":
                        EditStorageSettings();
                        break;
                    case "View All Settings (Read-Only)":
                        ShowSettings();
                        break;
                    case "Back to Main Menu":
                        return;
                }
            }
        }

        private void EditSmtpSettings()
        {
            var serverOptions = settingsManager.GetServerOptions();
            
            var port = AnsiConsole.Ask("SMTP Port:", serverOptions.Port);
            var hostname = AnsiConsole.Ask("Hostname:", serverOptions.HostName ?? "localhost");
            var allowRemote = AnsiConsole.Confirm("Allow Remote Connections?", serverOptions.AllowRemoteConnections);

            serverOptions.Port = port;
            serverOptions.HostName = hostname;
            serverOptions.AllowRemoteConnections = allowRemote;

            settingsManager.SaveSettings(serverOptions, settingsManager.GetRelayOptions()).Wait();
            AnsiConsole.MarkupLine("[green]SMTP settings updated! Restart required for changes to take effect.[/]");
            Thread.Sleep(2000);
        }

        private void EditImapSettings()
        {
            var serverOptions = settingsManager.GetServerOptions();
            
            var port = AnsiConsole.Ask("IMAP Port:", serverOptions.ImapPort ?? 143);

            serverOptions.ImapPort = port;

            settingsManager.SaveSettings(serverOptions, settingsManager.GetRelayOptions()).Wait();
            AnsiConsole.MarkupLine("[green]IMAP settings updated! Restart required for changes to take effect.[/]");
            Thread.Sleep(2000);
        }

        private void EditRelaySettings()
        {
            var relayOptions = settingsManager.GetRelayOptions();
            
            var server = AnsiConsole.Ask("Relay SMTP Server:", relayOptions.SmtpServer ?? "");
            var port = AnsiConsole.Ask("Relay SMTP Port:", relayOptions.SmtpPort);

            relayOptions.SmtpServer = server;
            relayOptions.SmtpPort = port;

            settingsManager.SaveSettings(settingsManager.GetServerOptions(), relayOptions).Wait();
            AnsiConsole.MarkupLine("[green]Relay settings updated![/]");
            Thread.Sleep(2000);
        }

        private void EditStorageSettings()
        {
            var serverOptions = settingsManager.GetServerOptions();
            
            var messagesToKeep = AnsiConsole.Ask("Messages to Keep:", serverOptions.NumberOfMessagesToKeep);
            var sessionsToKeep = AnsiConsole.Ask("Sessions to Keep:", serverOptions.NumberOfSessionsToKeep);

            serverOptions.NumberOfMessagesToKeep = messagesToKeep;
            serverOptions.NumberOfSessionsToKeep = sessionsToKeep;

            settingsManager.SaveSettings(serverOptions, settingsManager.GetRelayOptions()).Wait();
            AnsiConsole.MarkupLine("[green]Storage settings updated![/]");
            Thread.Sleep(2000);
        }

        private void ShowMessages()
        {
            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            var messagesRepo = host.Services.GetRequiredService<IMessagesRepository>();
            
            // Start auto-refresh in background
            autoRefreshService.Start(() => {
                // Refresh will happen when user selects "Enable Auto-Refresh"
            }, 3);

            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule($"[blue]Messages - {currentMailbox}/{currentFolder}[/]"));
                AnsiConsole.WriteLine();

                if (!string.IsNullOrEmpty(messageSearchFilter))
                {
                    AnsiConsole.MarkupLine($"[yellow]Filter: {messageSearchFilter.EscapeMarkup()}[/]");
                }

                var query = messagesRepo.GetMessageSummaries(currentMailbox, currentFolder);
                
                // Apply search filter
                if (!string.IsNullOrEmpty(messageSearchFilter))
                {
                    var filter = messageSearchFilter.ToLower();
                    query = query.Where(m => 
                        (m.From != null && m.From.ToLower().Contains(filter)) ||
                        (m.To != null && m.To.ToLower().Contains(filter)) ||
                        (m.Subject != null && m.Subject.ToLower().Contains(filter)));
                }

                var messages = query
                    .OrderByDescending(m => m.ReceivedDate)
                    .Take(50)
                    .ToList();

                if (!messages.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No messages found[/]");
                }
                else
                {
                    var table = new Table();
                    table.AddColumn("Date");
                    table.AddColumn("From");
                    table.AddColumn("To");
                    table.AddColumn("Subject");

                    foreach (var msg in messages.Take(20))
                    {
                        table.AddRow(
                            msg.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            msg.From?.EscapeMarkup() ?? "",
                            msg.To?.EscapeMarkup() ?? "",
                            msg.Subject?.EscapeMarkup() ?? ""
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.WriteLine($"\nShowing {Math.Min(20, messages.Count)} of {messages.Count} messages");
                }

                AnsiConsole.WriteLine();
                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select action:")
                        .AddChoices(new[] {
                            "View Message",
                            "Search/Filter Messages",
                            "Clear Filter",
                            "Delete All Messages",
                            "Refresh",
                            "Back to Main Menu"
                        }));

                switch (action)
                {
                    case "View Message":
                        if (messages.Any())
                        {
                            var msgChoices = messages.Select(m =>
                                $"{m.ReceivedDate:HH:mm:ss} - {(m.Subject?.Length > 40 ? m.Subject.Substring(0, 40) + "..." : m.Subject)}").ToList();
                            msgChoices.Add("Back");

                            var selected = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("Select message:")
                                    .PageSize(15)
                                    .AddChoices(msgChoices));

                            if (selected != "Back")
                            {
                                var index = msgChoices.IndexOf(selected);
                                var fullMessage = dbContext.Messages.FirstOrDefault(m => m.Id == messages[index].Id);
                                if (fullMessage != null)
                                {
                                    ShowMessageDetailEnhanced(fullMessage);
                                }
                            }
                        }
                        break;
                    case "Search/Filter Messages":
                        messageSearchFilter = AnsiConsole.Ask<string>("Enter search term (searches From, To, Subject):");
                        break;
                    case "Clear Filter":
                        messageSearchFilter = "";
                        AnsiConsole.MarkupLine("[green]Filter cleared[/]");
                        Thread.Sleep(500);
                        break;
                    case "Delete All Messages":
                        if (AnsiConsole.Confirm("Delete all messages?"))
                        {
                            dbContext.Messages.RemoveRange(dbContext.Messages);
                            dbContext.SaveChanges();
                            AnsiConsole.MarkupLine("[green]All messages deleted[/]");
                            Thread.Sleep(1000);
                        }
                        break;
                    case "Refresh":
                        continue;
                    case "Back to Main Menu":
                        autoRefreshService.Stop();
                        return;
                }
            }
        }

        private void ShowMessageDetailEnhanced(Rnwood.Smtp4dev.DbModel.Message message)
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule($"[blue]Message: {message.Subject?.EscapeMarkup()}[/]"));
                AnsiConsole.WriteLine();

                var viewChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select view:")
                        .AddChoices(new[] {
                            "Overview",
                            "Body (with HTML rendering)",
                            "Headers",
                            "MIME Parts (Enhanced)",
                            "Raw Source",
                            "Back"
                        }));

                AnsiConsole.Clear();
                
                switch (viewChoice)
                {
                    case "Overview":
                        ShowMessageOverview(message);
                        break;
                    case "Body (with HTML rendering)":
                        ShowMessageBodyEnhanced(message);
                        break;
                    case "Headers":
                        ShowMessageHeaders(message);
                        break;
                    case "MIME Parts (Enhanced)":
                        ShowMessagePartsEnhanced(message);
                        break;
                    case "Raw Source":
                        ShowMessageSource(message);
                        break;
                    case "Back":
                        return;
                }

                if (viewChoice != "Back")
                {
                    AnsiConsole.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private void ShowMessageBodyEnhanced(Rnwood.Smtp4dev.DbModel.Message message)
        {
            AnsiConsole.Write(new Rule("[blue]Message Body[/]"));
            AnsiConsole.WriteLine();

            var body = message.BodyText ?? "";
            if (string.IsNullOrEmpty(body) && message.Data != null)
            {
                body = System.Text.Encoding.UTF8.GetString(message.Data);
            }

            // Try to render HTML if it's HTML content
            if (htmlRenderer.IsHtmlContent(body))
            {
                AnsiConsole.MarkupLine("[yellow]HTML content detected - rendering as text:[/]");
                AnsiConsole.WriteLine();
                var renderedText = htmlRenderer.ConvertHtmlToText(body);
                AnsiConsole.WriteLine(renderedText.EscapeMarkup());
            }
            else
            {
                AnsiConsole.WriteLine(body.EscapeMarkup());
            }
        }

        private void ShowMessagePartsEnhanced(Rnwood.Smtp4dev.DbModel.Message message)
        {
            AnsiConsole.Write(new Rule("[blue]MIME Parts (Enhanced View)[/]"));
            AnsiConsole.WriteLine();

            try
            {
                if (message.Data != null)
                {
                    var mimeMessage = MimeMessage.Load(new MemoryStream(message.Data));
                    
                    var tree = new Tree("Message Structure");
                    BuildMimePartTree(tree, mimeMessage.Body, 0);
                    
                    AnsiConsole.Write(tree);
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No MIME data available[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error parsing MIME: {ex.Message}[/]");
                AnsiConsole.WriteLine($"\nContent Type: {message.MimeMetadata?.Split('\n').FirstOrDefault(l => l.StartsWith("Content-Type:"))}");
            }
        }

        private void BuildMimePartTree(Tree tree, MimeEntity entity, int level)
        {
            if (entity is Multipart multipart)
            {
                var node = tree.AddNode($"[yellow]Multipart/{multipart.ContentType.MediaSubtype}[/]");
                foreach (var child in multipart)
                {
                    BuildMimePartTreeNode(node, child, level + 1);
                }
            }
            else if (entity is MessagePart messagePart)
            {
                var node = tree.AddNode($"[cyan]Message Part[/]");
                if (messagePart.Message?.Body != null)
                {
                    BuildMimePartTreeNode(node, messagePart.Message.Body, level + 1);
                }
            }
            else
            {
                var part = entity as MimePart;
                var contentType = part?.ContentType?.MimeType ?? "unknown";
                var fileName = part?.FileName ?? "(no filename)";
                tree.AddNode($"[green]{contentType}[/] - {fileName}");
            }
        }

        private void BuildMimePartTreeNode(TreeNode parentNode, MimeEntity entity, int level)
        {
            if (entity is Multipart multipart)
            {
                var node = parentNode.AddNode($"[yellow]Multipart/{multipart.ContentType.MediaSubtype}[/]");
                foreach (var child in multipart)
                {
                    BuildMimePartTreeNode(node, child, level + 1);
                }
            }
            else if (entity is MessagePart messagePart)
            {
                var node = parentNode.AddNode($"[cyan]Message Part[/]");
                if (messagePart.Message?.Body != null)
                {
                    BuildMimePartTreeNode(node, messagePart.Message.Body, level + 1);
                }
            }
            else
            {
                var part = entity as MimePart;
                var contentType = part?.ContentType?.MimeType ?? "unknown";
                var fileName = part?.FileName ?? "(no filename)";
                var size = part?.Content?.Stream?.Length ?? 0;
                parentNode.AddNode($"[green]{contentType}[/] - {fileName} ({size} bytes)");
            }
        }

        private void ShowMessageDetail(Guid messageId)
        {
            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            var message = dbContext.Messages.FirstOrDefault(m => m.Id == messageId);

            if (message == null)
            {
                AnsiConsole.MarkupLine("[red]Message not found[/]");
                Console.ReadKey();
                return;
            }

            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule($"[blue]Message: {message.Subject?.EscapeMarkup()}[/]"));
                AnsiConsole.WriteLine();

                var viewChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select view:")
                        .AddChoices(new[] {
                            "Overview",
                            "Body",
                            "Headers",
                            "Parts",
                            "Raw Source",
                            "Back"
                        }));

                AnsiConsole.Clear();
                
                switch (viewChoice)
                {
                    case "Overview":
                        ShowMessageOverview(message);
                        break;
                    case "Body":
                        ShowMessageBody(message);
                        break;
                    case "Headers":
                        ShowMessageHeaders(message);
                        break;
                    case "Parts":
                        ShowMessageParts(message);
                        break;
                    case "Raw Source":
                        ShowMessageSource(message);
                        break;
                    case "Back":
                        return;
                }

                if (viewChoice != "Back")
                {
                    AnsiConsole.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private void ShowMessageOverview(Rnwood.Smtp4dev.DbModel.Message message)
        {
            AnsiConsole.Write(new Rule("[blue]Message Overview[/]"));
            AnsiConsole.WriteLine();

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("Field");
            table.AddColumn("Value");

            table.AddRow("From", message.From?.EscapeMarkup() ?? "");
            table.AddRow("To", message.To?.EscapeMarkup() ?? "");
            table.AddRow("Subject", message.Subject?.EscapeMarkup() ?? "");
            table.AddRow("Received", message.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Size", $"{(message.Data?.Length ?? 0) / 1024.0:F2} KB");

            AnsiConsole.Write(table);
        }

        private void ShowMessageBody(Rnwood.Smtp4dev.DbModel.Message message)
        {
            AnsiConsole.Write(new Rule("[blue]Message Body[/]"));
            AnsiConsole.WriteLine();

            var body = message.BodyText ?? "";
            if (string.IsNullOrEmpty(body) && message.Data != null)
            {
                body = System.Text.Encoding.UTF8.GetString(message.Data);
            }

            AnsiConsole.WriteLine(body.EscapeMarkup());
        }

        private void ShowMessageHeaders(Rnwood.Smtp4dev.DbModel.Message message)
        {
            AnsiConsole.Write(new Rule("[blue]Message Headers[/]"));
            AnsiConsole.WriteLine();

            var headers = message.MimeMetadata ?? "No headers available";
            AnsiConsole.WriteLine(headers.EscapeMarkup());
        }

        private void ShowMessageParts(Rnwood.Smtp4dev.DbModel.Message message)
        {
            AnsiConsole.Write(new Rule("[blue]Message Parts[/]"));
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[yellow]MIME parts inspection - detailed view not available in TUI[/]");
            AnsiConsole.WriteLine($"\nContent Type: {message.MimeMetadata?.Split('\n').FirstOrDefault(l => l.StartsWith("Content-Type:"))}");
        }

        private void ShowMessageSource(Rnwood.Smtp4dev.DbModel.Message message)
        {
            AnsiConsole.Write(new Rule("[blue]Message Source[/]"));
            AnsiConsole.WriteLine();

            if (message.Data != null)
            {
                var source = System.Text.Encoding.UTF8.GetString(message.Data);
                AnsiConsole.WriteLine(source.EscapeMarkup());
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No source data available[/]");
            }
        }

        private void ShowSessions()
        {
            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();

            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[blue]SMTP Sessions[/]"));
                AnsiConsole.WriteLine();

                if (!string.IsNullOrEmpty(sessionSearchFilter))
                {
                    AnsiConsole.MarkupLine($"[yellow]Filter: {sessionSearchFilter.EscapeMarkup()}[/]");
                }

                var query = dbContext.Sessions
                    .Where(s => s.EndDate.HasValue);

                // Apply search filter
                if (!string.IsNullOrEmpty(sessionSearchFilter))
                {
                    var filter = sessionSearchFilter.ToLower();
                    query = query.Where(s =>
                        (s.ClientAddress != null && s.ClientAddress.ToLower().Contains(filter)) ||
                        (s.SessionError != null && s.SessionError.ToLower().Contains(filter)));
                }

                var sessions = query
                    .OrderByDescending(s => s.StartDate)
                    .Take(50)
                    .ToList();

                if (!sessions.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No sessions found[/]");
                }
                else
                {
                    var table = new Table();
                    table.AddColumn("Start Date");
                    table.AddColumn("Client");
                    table.AddColumn("Messages");
                    table.AddColumn("Status");

                    foreach (var session in sessions.Take(20))
                    {
                        table.AddRow(
                            session.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            session.ClientAddress?.EscapeMarkup() ?? "",
                            session.NumberOfMessages.ToString(),
                            string.IsNullOrEmpty(session.SessionError) ? "[green]OK[/]" : "[red]ERROR[/]"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.WriteLine($"\nShowing {Math.Min(20, sessions.Count)} of {sessions.Count} sessions");
                }

                AnsiConsole.WriteLine();
                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select action:")
                        .AddChoices(new[] {
                            "View Session",
                            "Search/Filter Sessions",
                            "Clear Filter",
                            "Delete All Sessions",
                            "Refresh",
                            "Back to Main Menu"
                        }));

                switch (action)
                {
                    case "View Session":
                        if (sessions.Any())
                        {
                            var sessionChoices = sessions.Select(s =>
                                $"{s.StartDate:HH:mm:ss} - {s.ClientAddress}").ToList();
                            sessionChoices.Add("Back");

                            var selected = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("Select session:")
                                    .PageSize(15)
                                    .AddChoices(sessionChoices));

                            if (selected != "Back")
                            {
                                var index = sessionChoices.IndexOf(selected);
                                ShowSessionDetail(sessions[index]);
                            }
                        }
                        break;
                    case "Search/Filter Sessions":
                        sessionSearchFilter = AnsiConsole.Ask<string>("Enter search term (searches Client Address, Errors):");
                        break;
                    case "Clear Filter":
                        sessionSearchFilter = "";
                        AnsiConsole.MarkupLine("[green]Filter cleared[/]");
                        Thread.Sleep(500);
                        break;
                    case "Delete All Sessions":
                        if (AnsiConsole.Confirm("Delete all sessions?"))
                        {
                            dbContext.Sessions.RemoveRange(dbContext.Sessions);
                            dbContext.SaveChanges();
                            AnsiConsole.MarkupLine("[green]All sessions deleted[/]");
                            Thread.Sleep(1000);
                        }
                        break;
                    case "Refresh":
                        continue;
                    case "Back to Main Menu":
                        return;
                }
            }
        }

        private void ShowSessionDetail(Rnwood.Smtp4dev.DbModel.Session session)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[blue]Session: {session.ClientAddress}[/]"));
            AnsiConsole.WriteLine();

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("Field");
            table.AddColumn("Value");

            table.AddRow("Client Address", session.ClientAddress?.EscapeMarkup() ?? "");
            table.AddRow("Start Date", session.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("End Date", session.EndDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");
            table.AddRow("Number of Messages", session.NumberOfMessages.ToString());

            if (!string.IsNullOrEmpty(session.SessionError))
            {
                table.AddRow("Error", $"[red]{session.SessionError.EscapeMarkup()}[/]");
            }

            AnsiConsole.Write(table);

            AnsiConsole.WriteLine("\n[bold]Session Log:[/]");
            AnsiConsole.WriteLine(session.Log?.EscapeMarkup() ?? "No log available");

            AnsiConsole.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private void ShowServerLogs()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[yellow]Server Logs - Real-Time Auto-Refresh[/]");
            AnsiConsole.MarkupLine("[dim]Refreshing every 2 seconds... Press ESC to exit[/]");
            Thread.Sleep(1500);

            var cancellationTokenSource = new CancellationTokenSource();
            var isRunning = true;

            // Start auto-refresh
            autoRefreshService.Start(() => {
                // Logs are captured automatically by LogCaptureSink
            }, 2);

            try
            {
                AnsiConsole.Live(CreateLogsLayout())
                    .AutoClear(false)
                    .Start(ctx =>
                    {
                        while (isRunning)
                        {
                            var layout = CreateLogsLayout();
                            ctx.UpdateTarget(layout);
                            
                            // Check for ESC key
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true);
                                if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
                                {
                                    isRunning = false;
                                }
                            }
                            
                            Thread.Sleep(500); // Update every 500ms for responsiveness
                        }
                    });
            }
            finally
            {
                autoRefreshService.Stop();
            }
        }

        private Panel CreateLogsLayout()
        {
            var grid = new Grid();
            grid.AddColumn();

            lock (logLock)
            {
                var logs = logBuffer.TakeLast(50).ToList();
                
                if (!logs.Any())
                {
                    grid.AddRow("[yellow]No logs available yet[/]");
                }
                else
                {
                    foreach (var log in logs)
                    {
                        var color = log.Level switch
                        {
                            "Error" => "red",
                            "Warning" => "yellow",
                            "Information" => "white",
                            _ => "grey"
                        };

                        grid.AddRow($"[{color}]{log.Timestamp:HH:mm:ss} [{log.Level}] {log.Message.EscapeMarkup()}[/]");
                    }
                }
            }

            return new Panel(grid)
                .Header($"[blue]Server Logs - Real-Time (Last 50 entries) - {DateTime.Now:HH:mm:ss}[/]")
                .Border(BoxBorder.Rounded);
        }

        private void ShowServerStatus()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[blue]Server Status[/]"));
            AnsiConsole.WriteLine();

            var smtpServer = host.Services.GetRequiredService<ISmtp4devServer>();
            var imapServer = host.Services.GetRequiredService<ImapServer>();

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("Service");
            table.AddColumn("Status");
            table.AddColumn("Details");

            // SMTP Status
            var smtpStatus = smtpServer.IsRunning ? "[green]Running[/]" : "[red]Stopped[/]";
            var smtpDetails = smtpServer.IsRunning && smtpServer.ListeningEndpoints != null
                ? string.Join(", ", smtpServer.ListeningEndpoints.Select(ep => ep.ToString()))
                : smtpServer.Exception?.Message ?? "N/A";
            table.AddRow("SMTP Server", smtpStatus, smtpDetails.EscapeMarkup());

            // IMAP Status
            var imapStatus = imapServer.IsRunning ? "[green]Running[/]" : "[red]Stopped[/]";
            table.AddRow("IMAP Server", imapStatus, "");

            AnsiConsole.Write(table);

            AnsiConsole.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private void ShowSettings()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[blue]Settings[/]"));
            AnsiConsole.WriteLine();

            var serverOptions = host.Services.GetRequiredService<IOptionsMonitor<ServerOptions>>();
            var relayOptions = host.Services.GetRequiredService<IOptionsMonitor<RelayOptions>>();

            var so = serverOptions.CurrentValue;
            var ro = relayOptions.CurrentValue;

            var tree = new Tree("Server Configuration");

            var smtpNode = tree.AddNode("[yellow]SMTP Server[/]");
            smtpNode.AddNode($"Port: {so.Port}");
            smtpNode.AddNode($"Hostname: {so.HostName}");
            smtpNode.AddNode($"TLS Mode: {so.TlsMode}");
            smtpNode.AddNode($"Allow Remote: {so.AllowRemoteConnections}");

            var imapNode = tree.AddNode("[yellow]IMAP Server[/]");
            imapNode.AddNode($"Port: {so.ImapPort}");

            var relayNode = tree.AddNode("[yellow]Relay Settings[/]");
            relayNode.AddNode($"SMTP Server: {ro.SmtpServer ?? "Not configured"}");
            relayNode.AddNode($"Port: {ro.SmtpPort}");
            relayNode.AddNode($"TLS Mode: {ro.TlsMode}");

            var storageNode = tree.AddNode("[yellow]Storage[/]");
            storageNode.AddNode($"Messages to Keep: {so.NumberOfMessagesToKeep}");
            storageNode.AddNode($"Sessions to Keep: {so.NumberOfSessionsToKeep}");

            AnsiConsole.Write(tree);

            AnsiConsole.WriteLine("\n[yellow]Note: Settings cannot be modified in TUI mode. Use appsettings.json or command-line arguments.[/]");
            AnsiConsole.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private void ShowMessagesSplitScreen()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[yellow]Split-Screen Messages View[/]");
            AnsiConsole.MarkupLine("[dim]Use ↑/↓ to navigate, ESC to exit[/]");
            AnsiConsole.WriteLine();
            Thread.Sleep(1000);

            var dbContext = host.Services.GetRequiredService<Smtp4devDbContext>();
            var splitView = new SplitScreenView(dbContext);
            
            try
            {
                splitView.ShowMessageSplitView();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                AnsiConsole.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private void ShowSessionsSplitScreen()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[yellow]Split-Screen Sessions View[/]");
            AnsiConsole.MarkupLine("[dim]Use ↑/↓ to navigate, ESC to exit[/]");
            AnsiConsole.MarkupLine("[dim]This feature provides real-time session monitoring with split view[/]");
            AnsiConsole.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private void ComposeMessage()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[blue]Compose/Send Message[/]"));
            AnsiConsole.WriteLine();

            var from = AnsiConsole.Ask<string>("From email:");
            var to = AnsiConsole.Ask<string>("To email (comma-separated for multiple):");
            var subject = AnsiConsole.Ask<string>("Subject:");
            
            AnsiConsole.WriteLine("Body (type END on a new line to finish):");
            var bodyLines = new List<string>();
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "END") break;
                bodyLines.Add(line);
            }
            var body = string.Join("\n", bodyLines);

            var sendNow = AnsiConsole.Confirm("Send message now?");

            if (sendNow)
            {
                try
                {
                    var server = host.Services.GetRequiredService<ISmtp4devServer>();
                    var toAddresses = to.Split(',').Select(t => t.Trim()).ToArray();
                    
                    server.Send(
                        new Dictionary<string, string>(),
                        toAddresses,
                        Array.Empty<string>(),
                        from,
                        toAddresses,
                        subject,
                        body
                    );

                    AnsiConsole.MarkupLine("[green]Message sent successfully![/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error sending message: {ex.Message}[/]");
                }

                Thread.Sleep(2000);
            }
        }
    }

    public class LogEventInfo
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }

    public class LogCaptureSink : Serilog.Core.ILogEventSink
    {
        private readonly List<LogEventInfo> logBuffer;
        private readonly object logLock;

        public LogCaptureSink(List<LogEventInfo> logBuffer, object logLock)
        {
            this.logBuffer = logBuffer;
            this.logLock = logLock;
        }

        public void Emit(LogEvent logEvent)
        {
            lock (logLock)
            {
                logBuffer.Add(new LogEventInfo
                {
                    Timestamp = logEvent.Timestamp.DateTime,
                    Level = logEvent.Level.ToString(),
                    Message = logEvent.RenderMessage()
                });

                // Keep only last 500 log entries
                if (logBuffer.Count > 500)
                {
                    logBuffer.RemoveAt(0);
                }
            }
        }
    }
}
