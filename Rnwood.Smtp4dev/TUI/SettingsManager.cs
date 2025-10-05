using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.Server.Settings;
using Spectre.Console;

namespace Rnwood.Smtp4dev.TUI
{
    /// <summary>
    /// Manages reading and writing settings to appsettings.json
    /// </summary>
    public class SettingsManager
    {
        private readonly string settingsPath;
        private readonly IOptionsMonitor<ServerOptions> serverOptionsMonitor;
        private readonly IOptionsMonitor<RelayOptions> relayOptionsMonitor;

        public SettingsManager(
            IOptionsMonitor<ServerOptions> serverOptionsMonitor,
            IOptionsMonitor<RelayOptions> relayOptionsMonitor,
            string dataDir)
        {
            this.serverOptionsMonitor = serverOptionsMonitor;
            this.relayOptionsMonitor = relayOptionsMonitor;
            this.settingsPath = Path.Combine(dataDir, "appsettings.json");
        }

        public ServerOptions GetServerOptions() => serverOptionsMonitor.CurrentValue;
        public RelayOptions GetRelayOptions() => relayOptionsMonitor.CurrentValue;

        public async Task SaveSettings(ServerOptions serverOptions, RelayOptions relayOptions)
        {
            var settings = new
            {
                ServerOptions = serverOptions,
                RelayOptions = relayOptions
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(settingsPath, json);
            
            AnsiConsole.MarkupLine("[green]Settings saved successfully![/]");
        }

        public async Task<bool> AddUser(string username, string password)
        {
            try
            {
                var serverOptions = GetServerOptions();
                var usersList = new List<UserOptions>(serverOptions.Users ?? Array.Empty<UserOptions>());
                
                if (usersList.Exists(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    AnsiConsole.MarkupLine("[red]User already exists![/]");
                    return false;
                }

                usersList.Add(new UserOptions { Username = username, Password = password });
                serverOptions.Users = usersList.ToArray();
                
                await SaveSettings(serverOptions, GetRelayOptions());
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error adding user: {ex.Message}[/]");
                return false;
            }
        }

        public async Task<bool> RemoveUser(string username)
        {
            try
            {
                var serverOptions = GetServerOptions();
                var usersList = new List<UserOptions>(serverOptions.Users ?? Array.Empty<UserOptions>());
                
                var removed = usersList.RemoveAll(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (removed == 0)
                {
                    AnsiConsole.MarkupLine("[red]User not found![/]");
                    return false;
                }

                serverOptions.Users = usersList.ToArray();
                await SaveSettings(serverOptions, GetRelayOptions());
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error removing user: {ex.Message}[/]");
                return false;
            }
        }

        public async Task<bool> AddMailbox(string name, string recipients)
        {
            try
            {
                var serverOptions = GetServerOptions();
                var mailboxList = new List<MailboxOptions>(serverOptions.Mailboxes ?? Array.Empty<MailboxOptions>());
                
                if (mailboxList.Exists(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    AnsiConsole.MarkupLine("[red]Mailbox already exists![/]");
                    return false;
                }

                mailboxList.Add(new MailboxOptions { Name = name, Recipients = recipients });
                serverOptions.Mailboxes = mailboxList.ToArray();
                
                await SaveSettings(serverOptions, GetRelayOptions());
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error adding mailbox: {ex.Message}[/]");
                return false;
            }
        }

        public async Task<bool> RemoveMailbox(string name)
        {
            try
            {
                var serverOptions = GetServerOptions();
                var mailboxList = new List<MailboxOptions>(serverOptions.Mailboxes ?? Array.Empty<MailboxOptions>());
                
                var removed = mailboxList.RemoveAll(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (removed == 0)
                {
                    AnsiConsole.MarkupLine("[red]Mailbox not found![/]");
                    return false;
                }

                serverOptions.Mailboxes = mailboxList.ToArray();
                await SaveSettings(serverOptions, GetRelayOptions());
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error removing mailbox: {ex.Message}[/]");
                return false;
            }
        }
    }
}
